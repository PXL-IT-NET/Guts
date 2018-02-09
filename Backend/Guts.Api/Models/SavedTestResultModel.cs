namespace Guts.Api.Models
{
    public class SavedTestResultModel
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public bool Passed { get; set; }
    }
}