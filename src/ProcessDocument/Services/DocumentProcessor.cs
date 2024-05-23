using Dapr.Client;
using Azure.Storage.Blobs;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
// using Azure.AI.DocumentIntelligence;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using ProcessDocument.Models;

namespace ProcessDocument.Services
{
    public class DocumentProcessor
    {
        private readonly DaprClient _daprClient;
        private const string SecretStore = "secretstore";
        private const int BatchSize = 8;

        public DocumentProcessor(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task ProcessDocument(BlobEvent blobEvent)
        {
            var ingestionId = blobEvent.IngestionId;
            var docId = blobEvent.DocId;
            var blobName = blobEvent.BlobName;

            try
            {
                var blobSecret = (await _daprClient.GetSecretAsync(SecretStore, "AZURE_BLOB_CONNECTION_STRING")).FirstOrDefault().Value;
                var blobContainerName = (await _daprClient.GetSecretAsync(SecretStore, "BLOB_CONTAINER_NAME")).FirstOrDefault().Value;
                var blobServiceClient = new BlobServiceClient(blobSecret);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                var blobContent = await blobClient.DownloadContentAsync();

                Console.WriteLine($"Downloaded blob content for {blobName}");

                var frEndpoint = (await _daprClient.GetSecretAsync(SecretStore, "FORM_RECOGNIZER_ENDPOINT")).FirstOrDefault().Value;
                var frKey = (await _daprClient.GetSecretAsync(SecretStore, "FORM_RECOGNIZER_KEY")).FirstOrDefault().Value;

                var formRecognizerResult = await ProcessWithFormRecognizer(blobContent.Value.Content.ToStream(), frEndpoint, frKey);

                if (docId == null || ingestionId == null)
                {
                    throw new ArgumentNullException(docId == null ? nameof(docId) : nameof(ingestionId), "Value cannot be null");
                }

                Console.WriteLine($"Processed document with Form Recognizer for {blobName}");

                var sections = await DocumentChunker.CreateSections(blobName.Split('/').Last(), formRecognizerResult, docId, ingestionId);

                var batchNr = 1;
                var batchContent = new List<Section>();
                var totalBatchSize = (int)Math.Ceiling((double)sections.Count / BatchSize);

                foreach (var section in sections)
                {
                    batchContent.Add(section);
                    if (batchContent.Count == BatchSize)
                    {
                        await SaveAndPublishBatch(ingestionId, docId, batchNr, batchContent, totalBatchSize);
                        batchContent.Clear();
                        batchNr++;
                    }
                }

                if (batchContent.Any())
                {
                    await SaveAndPublishBatch(ingestionId, docId, batchNr, batchContent, totalBatchSize);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while processing the document: {ex.Message}");
            }
        }

        private async Task SaveAndPublishBatch(string ingestionId, string docId, int batchNr, List<Section> batchContent, int totalBatchSize)
        {
            var batchKey = $"section-output-{docId}-batch-{batchNr}";
            await _daprClient.SaveStateAsync("statestore", batchKey, batchContent);

            var topics = new[] { "generate-embeddings", "generate-keyphrases", "generate-summaries" };
            foreach (var topic in topics)
            {
                await _daprClient.PublishEventAsync("pubsub", topic, new
                {
                    ingestion_id = ingestionId,
                    doc_id = docId,
                    batch_key = batchKey,
                    batch_nr = batchNr,
                    total_batch_size = totalBatchSize
                });
            }
        }

        private async Task<List<PageMap>> ProcessWithFormRecognizer(Stream blobContent, string frEndpoint, string frKey)
        {
            var client = new DocumentAnalysisClient(new Uri(frEndpoint), new AzureKeyCredential(frKey));
            var result = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", blobContent);

            var pageMaps = new List<PageMap>();
            var offset = 0;

            // todo: better way to handle this

            foreach (var page in result.Value.Pages)
            {
                var text = string.Join("", page.Lines.Select(l => l.Content));
                pageMaps.Add(new PageMap
                {
                    PageNumber = page.PageNumber,
                    Text = text,
                    Offset = offset
                });
                offset += text.Length;
            }

            return pageMaps;
        }
    }
}