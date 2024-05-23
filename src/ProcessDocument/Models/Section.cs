namespace ProcessDocument.Models
{
    public class Section
    {
        public string? Id { get; set; }
        public string? Content { get; set; }
        public string? Category { get; set; }
        public int SourcePage { get; set; } = 0;
        public string? SourceFile { get; set; }
    }
}