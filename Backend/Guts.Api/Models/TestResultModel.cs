namespace Guts.Api.Models
{
    public class TestResultModel
    {
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
    }
}