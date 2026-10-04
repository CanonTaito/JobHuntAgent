namespace JobHunt.Api.Features.JdScan;

public sealed class JdScanFormatException(string message, Exception? inner = null)
    : Exception(message, inner);