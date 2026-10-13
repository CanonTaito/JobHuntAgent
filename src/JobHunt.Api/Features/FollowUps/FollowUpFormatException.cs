namespace JobHunt.Api.Features.FollowUps;

public sealed class FollowUpFormatException : Exception
{
    public FollowUpFormatException(string message)
        : base(message)
    {
    }

    public FollowUpFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
