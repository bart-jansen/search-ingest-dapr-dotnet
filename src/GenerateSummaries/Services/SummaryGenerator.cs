using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Client;
using Azure;
using Azure.AI.TextAnalytics;

namespace GenerateSummaries.Services
{
    public class SummaryGenerator
    {
        private readonly DaprClient _daprClient;

        private const string SecretStore = "secretstore";

        public SummaryGenerator(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task<List<string>> ExtractSummaries(List<string> texts, int batchNr)
        {
            var languageEndpoint = (await _daprClient.GetSecretAsync(SecretStore, "AZURE_LANGUAGE_ENDPOINT")).FirstOrDefault().Value;
            var languageKey = (await _daprClient.GetSecretAsync(SecretStore, "AZURE_LANGUAGE_KEY")).FirstOrDefault().Value;

            var credential = new AzureKeyCredential(languageKey);
            var textAnalyticsClient = new TextAnalyticsClient(new Uri(languageEndpoint), credential);

            var response = textAnalyticsClient.AbstractiveSummarize(WaitUntil.Completed, texts);
            //ExtractiveSummarizeOperation operation = client.ExtractiveSummarize(WaitUntil.Completed, texts);
            var output = new List<string>();

            await foreach (AbstractiveSummarizeResultCollection documentsInPage in response.Value)
            {
                Console.WriteLine($"Abstractive Summarize, model version: \"{documentsInPage.ModelVersion}\"");
                Console.WriteLine();

                foreach (AbstractiveSummarizeResult documentResult in documentsInPage)
                {
                    if (documentResult.HasError)
                    {
                        Console.WriteLine($"  Error!");
                        Console.WriteLine($"  Document error code: {documentResult.Error.ErrorCode}");
                        Console.WriteLine($"  Message: {documentResult.Error.Message}");
                        continue;
                    }

                    Console.WriteLine($"  Produced the following abstractive summaries:");
                    Console.WriteLine();

                    foreach (AbstractiveSummary summary in documentResult.Summaries)
                    {
                        Console.WriteLine($"  Text: {summary.Text.Replace("\n", " ")}");
                        output.Add(summary.Text.Replace("\n", " "));
                        Console.WriteLine($"  Contexts:");

                        foreach (AbstractiveSummaryContext context in summary.Contexts)
                        {
                            Console.WriteLine($"    Offset: {context.Offset}");
                            Console.WriteLine($"    Length: {context.Length}");
                        }

                        Console.WriteLine();
                    }
                }
            }
            return output;

        }

    }

}