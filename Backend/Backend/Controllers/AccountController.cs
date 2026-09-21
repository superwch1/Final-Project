using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        /// <summary>
        /// Failures accumulate while keep logging in within this window.
        /// </summary>
        public static readonly TimeSpan LoginAttemptWindow = TimeSpan.FromMinutes(15);

        /// <summary>
        /// The number of failures within <see cref="LoginAttemptWindow"/> triggers a lockout.
        /// </summary>
        public const int MaxFailedAttempts = 5;

        /// <summary>
        /// How long an account stays locked once limit is reached.
        /// </summary>
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IAccountRepository _accountRepository;
        private readonly IPasswordHasher<Account> _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public AccountController(
            IAccountRepository accountRepository,
            IPasswordHasher<Account> passwordHasher,
            IJwtTokenService jwtTokenService)
        {
            _accountRepository = accountRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }


        /// <summary>
        /// Create an account and return a token.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Create(CreateAccountRequest request, CancellationToken cancellationToken)
        {
            string email = NormalizeEmail(request.Email);
            if (await _accountRepository.EmailExistsAsync(email, cancellationToken))
            {
                return BadRequest("An account with that email already exists.");
            }

            Account account = new()
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = string.Empty,
                Name = request.Name.Trim(),
                FailedAttemptCount = 0
            };

            account.PasswordHash = _passwordHasher.HashPassword(account, request.Password);
            await _accountRepository.AddAsync(account, cancellationToken);

            return Ok(new AuthResponse { AccessToken = _jwtTokenService.CreateToken(account) });
        }


        /// <summary>
        /// Verify credentials and return access token.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            string email = NormalizeEmail(request.Email);
            
            Account? account = await _accountRepository.FindByEmailAsync(email, cancellationToken);
            if (account is null)
            {
                return Unauthorized("Invalid email or password.");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (IsLockedOut(account, now))
            {
                return LockedOutResult(account, now);
            }

            PasswordVerificationResult verificationResult = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                RegisterFailedAttempt(account, now);
                await _accountRepository.UpdateAsync(account, cancellationToken);

                if (IsLockedOut(account, now))
                {
                    return LockedOutResult(account, now);
                }

                return Unauthorized("Invalid email or password.");
            }

            RegisterSuccessfulAttempt(account);
            await _accountRepository.UpdateAsync(account, cancellationToken);

            return Ok(new AuthResponse { AccessToken = _jwtTokenService.CreateToken(account) });
        }


        /// <summary>
        /// Issue a fresh token for the signed-in account.
        /// </summary>
        [HttpPost("renew")]
        [Authorize]
        public async Task<ActionResult<AuthResponse>> Renew(CancellationToken cancellationToken)
        {
            Account? account = await FindSignedInAccountAsync(cancellationToken);
            if (account is null)
            {
                return Unauthorized();
            }

            return Ok(new AuthResponse { AccessToken = _jwtTokenService.CreateToken(account) });
        }


        /// <summary>
        /// Change the signed-in account's name and/or password.
        /// </summary>
        [HttpPut]
        [Authorize]
        public async Task<ActionResult<AccountResponse>> Update(UpdateAccountRequest request, CancellationToken cancellationToken)
        {
            if ((request.Name is null) && (request.NewPassword is null))
            {
                return BadRequest("Supply a new name, a new password, or both.");
            }

            Account? account = await FindSignedInAccountAsync(cancellationToken);
            if (account is null)
            {
                return Unauthorized();
            }

            if (request.NewPassword is not null)
            {
                account.PasswordHash = _passwordHasher.HashPassword(account, request.NewPassword);
            }

            if (request.Name is not null)
            {
                account.Name = request.Name.Trim();
            }

            await _accountRepository.UpdateAsync(account, cancellationToken);
            return Ok(AccountResponse.FromAccount(account));
        }


        /// <summary>
        /// Delete the signed-in account.
        /// </summary>
        [HttpDelete]
        [Authorize]
        public async Task<ActionResult> Delete(CancellationToken cancellationToken)
        {
            Account? account = await FindSignedInAccountAsync(cancellationToken);
            if (account is null)
            {
                return Unauthorized();
            }

            await _accountRepository.DeleteAsync(account, cancellationToken);
            return Ok();
        }


        /// <summary>
        /// Check whether the account is currently inside a lockout period.
        /// </summary>
        public static bool IsLockedOut(Account account, DateTimeOffset now)
        {
            return account.LockoutEndUtc.HasValue && (account.LockoutEndUtc.Value > now);
        }

        /// <summary>
        /// Record a failed attempt and locking the account once the limit is reached.
        /// </summary>
        public static void RegisterFailedAttempt(Account account, DateTimeOffset now)
        {
            bool isWithinWindow = account.LastFailedAttemptUtc.HasValue && ((now - account.LastFailedAttemptUtc.Value) <= LoginAttemptWindow);

            account.FailedAttemptCount = isWithinWindow ? (account.FailedAttemptCount + 1) : 1;
            account.LastFailedAttemptUtc = now;

            if (account.FailedAttemptCount >= MaxFailedAttempts)
            {
                account.LockoutEndUtc = now + LockoutDuration;
                account.FailedAttemptCount = 0;
            }
        }

        /// <summary>
        /// Clear the failure attempt after a successful log in.
        /// </summary>
        public static void RegisterSuccessfulAttempt(Account account)
        {
            account.FailedAttemptCount = 0;
            account.LastFailedAttemptUtc = null;
            account.LockoutEndUtc = null;
        }

        private Task<Account?> FindSignedInAccountAsync(CancellationToken cancellationToken)
        {
            string? subject = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(subject, out Guid accountId))
            {
                return Task.FromResult<Account?>(null);
            }

            return _accountRepository.FindByIdAsync(accountId, cancellationToken);
        }

        private ActionResult LockedOutResult(Account account, DateTimeOffset now)
        {
            TimeSpan retryAfter = account.LockoutEndUtc!.Value - now;
            return StatusCode(StatusCodes.Status400BadRequest, $"Too many failed attempts. Try again in {Math.Ceiling(retryAfter.TotalMinutes)} minute(s).");
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }
    }
}
