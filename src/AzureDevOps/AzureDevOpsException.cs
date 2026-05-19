namespace Whidy.AzureDevOps;

public class AzureDevOpsException : Exception
{
    public int StatusCode { get; }

    public AzureDevOpsException(int statusCode, string message) : base(message)
        => StatusCode = statusCode;
}
