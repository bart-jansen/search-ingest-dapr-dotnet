
namespace Batcher.Models
{
    public class BatcherRequest
    {
        public string? SourceFolderPath { get; set; }
        public string? SearchItemsFolderPath { get; set; }
        public string? SearchIndexerName { get; set; }
    }
}