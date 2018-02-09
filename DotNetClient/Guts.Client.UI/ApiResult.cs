using System.CodeDom;

namespace Guts.Client.UI
{
    public class ApiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static ApiResult CreateSuccess()
        {
            return new ApiResult {Success = true};
        }

        public static ApiResult CreateError(string errorMessage)
        {
            return new ApiResult { Success = false, Message = errorMessage};
        }
    }
}