using Microsoft.AspNetCore.Mvc;
using Batcher.Models;
using Batcher.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Batcher.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BatcherController : ControllerBase
    {
        private readonly BlobService _blobService;
        private readonly DaprService _daprService;

        public BatcherController(BlobService blobService, DaprService daprService)
        {
            _blobService = blobService;
            _daprService = daprService;
        }

        [HttpPost("batcher-trigger")]
        public async Task<IActionResult> BatcherTrigger([FromBody] BatcherRequest request)
        {
            Console.WriteLine($"Trigger received: {request}");
            if (request == null || string.IsNullOrEmpty(request.SourceFolderPath) ||
                string.IsNullOrEmpty(request.SearchItemsFolderPath) || string.IsNullOrEmpty(request.SearchIndexerName))
            {
                return BadRequest(new { success = false, error = "All required fields must be provided." });
            }

            var blobSecret = await _daprService.GetSecretAsync("secretstore", "AZURE_BLOB_CONNECTION_STRING");
            var blobContainerName = await _daprService.GetSecretAsync("secretstore", "BLOB_CONTAINER_NAME");

            var blobs = await _blobService.ListBlobsAsync(blobSecret, blobContainerName, request.SourceFolderPath);

            Console.WriteLine($"Found {blobs.Count} blobs in {request.SourceFolderPath}");

            var ingestionId = _blobService.GenerateIngestionId();
            var docIds = blobs.Select(blob => _daprService.PublishEventForBlob(blob, ingestionId)).ToList();

            var state = new IngestionState
            {
                DocIds = docIds,
                SearchItemsFolderPath = request.SearchItemsFolderPath,
                SearchIndexerName = request.SearchIndexerName
            };

            await _daprService.SaveStateAsync($"ingestion-{ingestionId}", state);

            return Ok(new { success = true });
        }
    }
}