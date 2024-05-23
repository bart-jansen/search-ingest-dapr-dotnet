namespace ProcessDocument.Models
{
    public class PageMap
    {
        public int PageNumber { get; set; } = 0;
        public int Offset { get; set; } = 0;
        public string? Text { get; set; }
    }
}