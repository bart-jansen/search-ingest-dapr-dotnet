using System.Text.Json.Serialization;
namespace batcher.Models;
public record IngestRequest(
    [property: JsonPropertyName("source_folder_path")] string SourceFolderPath,
    [property: JsonPropertyName("destination_folder_path")] string DestinationFolderPath,
    [property: JsonPropertyName("searchitems_folder_path")] string SearchItemsFolderPath,
    [property: JsonPropertyName("searchindexer_name")] string SearchIndexerName);