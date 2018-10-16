namespace Guts.Domain
{
    public class ProjectComponent : Assignment
    {
        public virtual Project Project { get; set; }
        public int ProjectId { get; set; }
    }
}