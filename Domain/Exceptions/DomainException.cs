namespace AUTH_Sevice.Domain.Exceptions
{
    public abstract class DomainException (string message) : Exception(message);
    public class UserNotFoundException(string identifier)
    : DomainException($"User '{identifier}' was not found.");

    public class InvalidCredentialsException()
        : DomainException("Invalid email or password.");

    public class UserLockedException(DateTime lockedUntil)
        : DomainException($"Account locked until {lockedUntil:u}. Too many failed attempts.");

    public class EmailAlreadyExistsException(string email)
        : DomainException($"A user with email '{email}' already exists.");

    public class InvalidRefreshTokenException()
        : DomainException("The provided refresh token is invalid or expired.");

    public class UserInactiveException()
        : DomainException("This account has been deactivated.");

}
