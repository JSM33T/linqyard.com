namespace Linqyard.Contracts.Exceptions;

public class GroupServiceException : Exception
{
    public GroupServiceException(string message)
        : base(message)
    {
    }

    public GroupServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class GroupNotFoundException : GroupServiceException
{
    public GroupNotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class GroupForbiddenException : GroupServiceException
{
    public GroupForbiddenException(string message)
        : base(message)
    {
    }
}

public sealed class GroupLimitExceededException : GroupServiceException
{
    public GroupLimitExceededException(string message)
        : base(message)
    {
    }
}

public sealed class GroupValidationException : GroupServiceException
{
    public GroupValidationException(string message)
        : base(message)
    {
    }
}

