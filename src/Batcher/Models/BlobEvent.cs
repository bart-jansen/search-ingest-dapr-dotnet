namespace Batcher.Models
{
    public class BlobEvent
    {
        public string? IngestionId { get; set; }
        public string? DocId { get; set; }
        public string? BlobName { get; set; }
    }
}