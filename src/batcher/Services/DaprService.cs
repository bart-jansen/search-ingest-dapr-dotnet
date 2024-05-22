using Dapr.Client;
using Batcher.Models;
using Batcher.Utilities;
using Azure.Storage.Blobs.Models;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Batcher.Services
{
    public class DaprService
    {
        private readonly DaprClient _daprClient;

        public DaprService(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task<string> GetSecretAsync(string storeName, string key)
        {
            var secret = await _daprClient.GetSecretAsync(storeName, key);
            return secret[key];
        }

        public string PublishEventForBlob(BlobItem blob, string ingestionId)
        {
            var docId = Utility.GenerateRandomString(8);
            var blobEvent = new BlobEvent
            {
                IngestionId = ingestionId,
                DocId = docId,
                BlobName = blob.Name
            };

            var blobEventJson = JsonSerializer.Serialize(blobEvent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            _daprClient.PublishEventAsync("pubsub", "process-document", blobEventJson);

            return docId;
        }

        public async Task SaveStateAsync(string key, IngestionState state)
        {
            await _daprClient.SaveStateAsync("statestore", key, state);
        }
    }
}