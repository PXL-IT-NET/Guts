namespace Guts.Client.Core.Utility
{
    public class Result
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public Result(bool success)
        {
            Success = success;
            Message = string.Empty;
        }
    }
}