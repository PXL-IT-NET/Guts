namespace Guts.Business
{
    public class TestResultDto 
    {
        public int TestId { get; set; }
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }       
    }
}