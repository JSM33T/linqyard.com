namespace Linqyard.Contracts.Exceptions;

public class LinkServiceException : Exception
{
    public LinkServiceException(string message)
        : base(message)
    {
    }

    public LinkServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class LinkNotFoundException : LinkServiceException
{
    public LinkNotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class LinkForbiddenException : LinkServiceException
{
    public LinkForbiddenException(string message)
        : base(message)
    {
    }
}

public sealed class LinkValidationException : LinkServiceException
{
    public LinkValidationException(string message)
        : base(message)
    {
    }
}

public sealed class LinkLimitExceededException : LinkServiceException
{
    public LinkLimitExceededException(string message)
        : base(message)
    {
    }
}

