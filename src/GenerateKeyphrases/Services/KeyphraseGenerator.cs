using Dapr.Client;
using Azure;
using Azure.AI.TextAnalytics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenerateKeyphrases.Services
{
    public class KeyphraseGenerator
    {
        private readonly DaprClient _daprClient;

        private const string SecretStore = "secretstore";

        public KeyphraseGenerator(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task<List<string>> ExtractKeyPhrases(List<string> texts, int batchNr)
        {
            var languageEndpoint = (await _daprClient.GetSecretAsync(SecretStore, "AZURE_LANGUAGE_ENDPOINT")).FirstOrDefault().Value;
            var languageKey = (await _daprClient.GetSecretAsync(SecretStore, "AZURE_LANGUAGE_KEY")).FirstOrDefault().Value;

            var credential = new AzureKeyCredential(languageKey);
            var textAnalyticsClient = new TextAnalyticsClient(new Uri(languageEndpoint), credential);

            var response = textAnalyticsClient.ExtractKeyPhrasesBatch(texts);

            if (response.Value.Count == texts.Count && response.Value.All(r => r.HasError == false))
            {
                return response.Value.SelectMany(r => r.KeyPhrases).ToList();
            }
            else
            {
                var errorMessage = $"Error occurred in keyphrases batch_nr: {batchNr}";
                Console.WriteLine(errorMessage);
                throw new RequestFailedException(errorMessage);
            }

        }

    }
}