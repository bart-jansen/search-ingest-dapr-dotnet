using Dapr.Client;
using Azure;
using Azure.AI.TextAnalytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.OpenAI;

namespace GenerateEmbeddings.Services
{
    public class EmbeddingsGenerator
    {
        private readonly DaprClient _daprClient;

        private const string SecretStore = "secretstore";

        public EmbeddingsGenerator(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task<List<string>> ComputeEmbeddings(List<string> texts, int batchNr)
        {
            var openaiEndpoint = (await _daprClient.GetSecretAsync(SecretStore, "AZURE_OPENAI_ENDPOINT")).FirstOrDefault().Value;
            var openaiKey = (await _daprClient.GetSecretAsync(SecretStore, "AZURE_OPENAI_KEY")).FirstOrDefault().Value;

            OpenAIClient client = new OpenAIClient(
                    new Uri(openaiEndpoint),
                    new AzureKeyCredential(openaiKey));

            EmbeddingsOptions embeddingsOptions = new()
            {
                DeploymentName = "text-embedding-ada-002",
                Input = texts,
            };
            Response<Embeddings> response = await client.GetEmbeddingsAsync(embeddingsOptions);

            // The response includes the generated embedding.
            EmbeddingItem item = response.Value.Data[0];
            ReadOnlyMemory<float> embedding = item.Embedding;
            Console.WriteLine($"Embedding: {string.Join(", ", embedding.ToArray())}");

            return response.Value.Data.Select(r => string.Join(", ", r.Embedding.ToArray())).ToList();

        }

    }
}