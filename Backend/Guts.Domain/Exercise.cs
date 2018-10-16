namespace Guts.Domain
{
    public class Exercise : Assignment
    {
        public virtual Chapter Chapter { get; set; }
        public int ChapterId { get; set; }
    }
}