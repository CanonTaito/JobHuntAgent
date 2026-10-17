namespace JobHunt.Api.Features.Resume;

public sealed class ResumeParseFormatException(string message, Exception? inner = null)
    : Exception(message, inner);
