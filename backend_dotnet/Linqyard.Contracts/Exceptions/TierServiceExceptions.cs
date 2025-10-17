namespace Linqyard.Contracts.Exceptions;

/// <summary>
/// Base exception for tier payment and upgrade operations.
/// </summary>
public class TierServiceException : Exception
{
    public TierServiceException(string message) : base(message)
    {
    }

    public TierServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Raised when the requested tier cannot be found.
/// </summary>
public sealed class TierNotFoundException : TierServiceException
{
    public TierNotFoundException(string message) : base(message)
    {
    }
}

/// <summary>
/// Raised when the user already has the requested tier active.
/// </summary>
public sealed class TierAlreadyActiveException : TierServiceException
{
    public TierAlreadyActiveException(string message) : base(message)
    {
    }
}

/// <summary>
/// Raised when Razorpay payment validation fails.
/// </summary>
public sealed class PaymentVerificationException : TierServiceException
{
    public PaymentVerificationException(string message) : base(message)
    {
    }
}
