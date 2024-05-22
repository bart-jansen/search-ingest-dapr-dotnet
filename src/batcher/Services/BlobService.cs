using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Batcher.Utilities;

namespace Batcher.Services
{
    public class BlobService
    {
        public async Task<List<BlobItem>> ListBlobsAsync(string connectionString, string containerName, string prefix)
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = new List<BlobItem>();

            await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobs.Add(blob);
            }

            return blobs;
        }
        public string GenerateIngestionId()
        {
            return Utility.GenerateRandomString(5);
        }
    }
}