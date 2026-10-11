namespace JobHunt.Api.Features.Tailoring;

public sealed class TailoringFormatException : Exception
{
    public TailoringFormatException(string message)
        : base(message)
    {
    }

    public TailoringFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
