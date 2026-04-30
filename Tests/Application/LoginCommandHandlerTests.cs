using AUTH_Sevice.Application.Auth.Commands;
using AUTH_Sevice.Application.Common.Intefaces;
using AUTH_Sevice.Domain.Entities.Enums;
using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Domain.Exceptions;
using AUTH_Sevice.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AUTH_Sevice.Tests.Application
{

    public class LoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly IOptions<JwtSettings> _jwtSettings =
            Options.Create(new JwtSettings { AccessTokenExpiryMinutes = 15, RefreshTokenExpiryDays = 7 });

        private LoginCommandHandler CreateHandler() => new(
            _userRepoMock.Object,
            _refreshTokenRepoMock.Object,
            _auditRepoMock.Object,
            _uowMock.Object,
            _tokenServiceMock.Object,
            _passwordHasherMock.Object,
            _jwtSettings);

        private static User CreateTestUser(string email = "test@example.com", string password = "hashed_pw")
            => User.Create(email, password, "Test", "User", UserRole.User);

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var user = CreateTestUser();
            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com", default))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(h => h.Verify("password123", user.PasswordHash))
                .Returns(true);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh_token");
            _auditRepoMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>(), default)).Returns(Task.CompletedTask);
            _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), default)).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            var command = new LoginCommand("test@example.com", "password123", "127.0.0.1");

            // Act
            var result = await CreateHandler().Handle(command, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("access_token", result.AccessToken);
            Assert.Equal("refresh_token", result.RefreshToken);
            Assert.Equal("Bearer", result.TokenType);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsInvalidCredentialsException()
        {
            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
                .ReturnsAsync((User?)null);
            _auditRepoMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>(), default)).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            var command = new LoginCommand("nobody@example.com", "password", "127.0.0.1");

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => CreateHandler().Handle(command, default));
        }

        [Fact]
        public async Task Handle_WrongPassword_ThrowsInvalidCredentialsException()
        {
            var user = CreateTestUser();
            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com", default)).ReturnsAsync(user);
            _passwordHasherMock.Setup(h => h.Verify("wrongpassword", user.PasswordHash)).Returns(false);
            _userRepoMock.Setup(r => r.Update(user));
            _auditRepoMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>(), default)).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            var command = new LoginCommand("test@example.com", "wrongpassword", "127.0.0.1");

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => CreateHandler().Handle(command, default));
        }

        [Fact]
        public async Task Handle_InactiveUser_ThrowsInvalidCredentialsException()
        {
            var user = CreateTestUser();
            user.Deactivate();
            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com", default)).ReturnsAsync(user);
            _auditRepoMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>(), default)).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            var command = new LoginCommand("test@example.com", "password", "127.0.0.1");

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => CreateHandler().Handle(command, default));
        }
    }

}
