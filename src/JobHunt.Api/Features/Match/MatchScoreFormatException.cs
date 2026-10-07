namespace JobHunt.Api.Features.Match;

public sealed class MatchScoreFormatException(string message, Exception? inner = null)
    : Exception(message, inner);
