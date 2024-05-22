using System.Collections.Generic;

namespace Batcher.Models
{
    public class IngestionState
    {
        public List<string> DocIds { get; set; }
        public string SearchItemsFolderPath { get; set; }
        public string SearchIndexerName { get; set; }
    }
}