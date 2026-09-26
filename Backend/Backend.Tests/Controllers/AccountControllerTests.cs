using Backend.Controllers;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Backend.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Backend.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<IAccountRepository> _accountRepository = new();
        private readonly Mock<IPasswordHasher<Account>> _passwordHasher = new();
        private readonly Mock<IJwtTokenService> _jwtTokenService = new();

        public AccountControllerTests()
        {
            _passwordHasher.Setup(x => x.HashPassword(It.IsAny<Account>(), It.IsAny<string>())).Returns("hash");
            _jwtTokenService.Setup(x => x.CreateToken(It.IsAny<Account>())).Returns("token");
        }

        [Fact]
        public async Task Create_EmailAlreadyUsed_ReturnsBadRequest()
        {
            _accountRepository.Setup(x => x.EmailExistsAsync("USER@EXAMPLE.COM", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            ActionResult<AuthResponse> result = await CreateController(null).Create(CreateRequest("user@example.com"), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_NewEmail_SavesNormalisedEmail()
        {
            await CreateController(null).Create(CreateRequest("  user@example.com "), CancellationToken.None);

            _accountRepository.Verify(x => x.AddAsync(It.Is<Account>(a => a.Email == "USER@EXAMPLE.COM"), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Create_NewEmail_ReturnsToken()
        {
            ActionResult<AuthResponse> result = await CreateController(null).Create(CreateRequest("user@example.com"), CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("token", Assert.IsType<AuthResponse>(ok.Value).AccessToken);
        }

        [Fact]
        public async Task Login_UnknownEmail_ReturnsUnauthorized()
        {
            ActionResult<AuthResponse> result = await CreateController(null).Login(LoginRequest("Password1!"), CancellationToken.None);

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_LockedOutAccount_ReturnsBadRequest()
        {
            Account account = TestData.Account();
            account.LockoutEndUtc = DateTimeOffset.UtcNow.AddMinutes(10);
            SetUpAccount(account);

            ActionResult<AuthResponse> result = await CreateController(null).Login(LoginRequest("Password1!"), CancellationToken.None);

            ObjectResult objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            SetUpAccount(TestData.Account(), PasswordVerificationResult.Failed);

            ActionResult<AuthResponse> result = await CreateController(null).Login(LoginRequest("wrong"), CancellationToken.None);

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_WrongPassword_CountsFailedAttempt()
        {
            Account account = TestData.Account();
            SetUpAccount(account, PasswordVerificationResult.Failed);

            await CreateController(null).Login(LoginRequest("wrong"), CancellationToken.None);

            Assert.Equal(1, account.FailedAttemptCount);
        }

        [Fact]
        public async Task Login_FifthWrongPassword_LocksAccount()
        {
            Account account = TestData.Account();
            account.FailedAttemptCount = AccountController.MaxFailedAttempts - 1;
            account.LastFailedAttemptUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            SetUpAccount(account, PasswordVerificationResult.Failed);

            await CreateController(null).Login(LoginRequest("wrong"), CancellationToken.None);

            Assert.NotNull(account.LockoutEndUtc);
        }

        [Fact]
        public async Task Login_CorrectPassword_ReturnsToken()
        {
            SetUpAccount(TestData.Account(), PasswordVerificationResult.Success);

            ActionResult<AuthResponse> result = await CreateController(null).Login(LoginRequest("Password1!"), CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("token", Assert.IsType<AuthResponse>(ok.Value).AccessToken);
        }

        [Fact]
        public async Task Login_CorrectPassword_ClearsFailedAttempts()
        {
            Account account = TestData.Account();
            account.FailedAttemptCount = 3;
            SetUpAccount(account, PasswordVerificationResult.Success);

            await CreateController(null).Login(LoginRequest("Password1!"), CancellationToken.None);

            Assert.Equal(0, account.FailedAttemptCount);
        }

        [Fact]
        public async Task Renew_SignedOut_ReturnsUnauthorized()
        {
            ActionResult<AuthResponse> result = await CreateController(null).Renew(CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task Renew_SignedIn_ReturnsToken()
        {
            Account account = TestData.Account();
            _accountRepository.Setup(x => x.FindByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

            ActionResult<AuthResponse> result = await CreateController(account.Id).Renew(CancellationToken.None);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task Update_NothingToChange_ReturnsBadRequest()
        {
            ActionResult<AccountResponse> result = await CreateController(Guid.NewGuid()).Update(new UpdateAccountRequest(), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Update_NewName_SavesTrimmedName()
        {
            Account account = TestData.Account();
            _accountRepository.Setup(x => x.FindByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

            await CreateController(account.Id).Update(new UpdateAccountRequest { Name = "  New name " }, CancellationToken.None);

            Assert.Equal("New name", account.Name);
        }

        [Fact]
        public async Task Update_NewPassword_SavesNewHash()
        {
            Account account = TestData.Account();
            _accountRepository.Setup(x => x.FindByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
            _passwordHasher.Setup(x => x.HashPassword(account, "NewPassword1!")).Returns("new-hash");

            await CreateController(account.Id).Update(new UpdateAccountRequest { NewPassword = "NewPassword1!" }, CancellationToken.None);

            Assert.Equal("new-hash", account.PasswordHash);
        }

        [Fact]
        public async Task Delete_SignedOut_ReturnsUnauthorized()
        {
            ActionResult result = await CreateController(null).Delete(CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_SignedIn_DeletesAccount()
        {
            Account account = TestData.Account();
            _accountRepository.Setup(x => x.FindByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

            await CreateController(account.Id).Delete(CancellationToken.None);

            _accountRepository.Verify(x => x.DeleteAsync(account, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void IsLockedOut_LockoutExpired_ReturnsFalse()
        {
            Account account = TestData.Account();
            account.LockoutEndUtc = DateTimeOffset.UtcNow.AddMinutes(-1);

            Assert.False(AccountController.IsLockedOut(account, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void RegisterFailedAttempt_OutsideWindow_RestartsCount()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Account account = TestData.Account();
            account.FailedAttemptCount = 3;
            account.LastFailedAttemptUtc = now - AccountController.LoginAttemptWindow - TimeSpan.FromMinutes(1);

            AccountController.RegisterFailedAttempt(account, now);

            Assert.Equal(1, account.FailedAttemptCount);
        }

        [Fact]
        public void RegisterSuccessfulAttempt_LockedAccount_ClearsLockout()
        {
            Account account = TestData.Account();
            account.LockoutEndUtc = DateTimeOffset.UtcNow.AddMinutes(10);

            AccountController.RegisterSuccessfulAttempt(account);

            Assert.Null(account.LockoutEndUtc);
        }

        /// <summary>
        /// Creates the controller and signed in as the account
        /// </summary>
        private AccountController CreateController(Guid? accountId)
        {
            return new AccountController(_accountRepository.Object, _passwordHasher.Object, _jwtTokenService.Object)
                .SignedInAs(accountId);
        }

        /// <summary>
        /// Make the account findable by its email
        /// </summary>
        private void SetUpAccount(Account account, PasswordVerificationResult verificationResult = PasswordVerificationResult.Success)
        {
            _accountRepository.Setup(x => x.FindByEmailAsync(account.Email, It.IsAny<CancellationToken>())).ReturnsAsync(account);
            _passwordHasher.Setup(x => x.VerifyHashedPassword(account, account.PasswordHash, It.IsAny<string>())).Returns(verificationResult);
        }

        /// <summary>
        /// Build a valid registration request for the email
        /// </summary>
        private static CreateAccountRequest CreateRequest(string email)
        {
            return new CreateAccountRequest { Email = email, Password = "Password1!", Name = "User" };
        }

        /// <summary>
        /// Build a login request for the account's email
        /// </summary>
        private static LoginRequest LoginRequest(string password)
        {
            return new LoginRequest { Email = "user@example.com", Password = password };
        }
    }
}
