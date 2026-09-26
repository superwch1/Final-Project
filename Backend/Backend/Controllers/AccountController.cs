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
        public static readonly TimeSpan LoginAttemptWindow = TimeSpan.FromMinutes(15);
        public const int MaxFailedAttempts = 5;
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
        /// Registers a new account and returns an access token
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
        /// Signs in with email and password, locking the account after too many failed attempts
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
        /// Issues a fresh access token for the account
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
        /// Changes the account's name, password or both
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
        /// Deletes the account
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
        /// Returns true if the account is still locked out
        /// </summary>
        public static bool IsLockedOut(Account account, DateTimeOffset now)
        {
            return account.LockoutEndUtc.HasValue && (account.LockoutEndUtc.Value > now);
        }


        /// <summary>
        /// Counts a failed login and locks the account once the limit is reached within the window
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
        /// Clears the failed login count and lockout
        /// </summary>
        public static void RegisterSuccessfulAttempt(Account account)
        {
            account.FailedAttemptCount = 0;
            account.LastFailedAttemptUtc = null;
            account.LockoutEndUtc = null;
        }

        /// <summary>
        /// Loads the account for the signed-in user, or null if the token has no valid account ID
        /// </summary>
        private Task<Account?> FindSignedInAccountAsync(CancellationToken cancellationToken)
        {
            string? subject = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(subject, out Guid accountId))
            {
                return Task.FromResult<Account?>(null);
            }

            return _accountRepository.FindByIdAsync(accountId, cancellationToken);
        }

        /// <summary>
        /// Builds the error returned while the account is locked out
        /// </summary>
        private ActionResult LockedOutResult(Account account, DateTimeOffset now)
        {
            TimeSpan retryAfter = account.LockoutEndUtc!.Value - now;
            return StatusCode(StatusCodes.Status400BadRequest, $"Too many failed attempts. Try again in {Math.Ceiling(retryAfter.TotalMinutes)} minute(s).");
        }

        /// <summary>
        /// Trims and upper-cases an email
        /// </summary>
        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }
    }
}
