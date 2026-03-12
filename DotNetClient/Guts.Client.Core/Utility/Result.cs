namespace Guts.Client.Core.Utility;

public class Result(bool success)
{
    public bool Success { get; set; } = success;
    public string Message { get; set; } = string.Empty;
}