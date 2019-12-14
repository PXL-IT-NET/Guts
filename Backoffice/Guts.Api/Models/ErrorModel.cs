using System;

namespace Guts.Api.Models
{
    public class ErrorModel
    {
        public string Message { get; set; }

        private ErrorModel()
        {
        }

        public static ErrorModel FromException(Exception ex)
        {
            return new ErrorModel
            {
                Message = ex.Message
            };
        }

        public static ErrorModel FromString(string message)
        {
            return new ErrorModel
            {
                Message = message
            };
        }
    }
}