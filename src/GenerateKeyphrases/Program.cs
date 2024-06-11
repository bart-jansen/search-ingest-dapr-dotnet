using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GenerateKeyhprases.Models;
using System.Text.Json;
using System.Collections.Generic;
using System;
using GenerateKeyphrases.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();

var app = builder.Build();

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// format for batch:
// new
// {
//     ingestion_id = ingestionId,
//     doc_id = docId,
//     batch_key = batchKey,
//     batch_nr = batchNr,
//     total_batch_size = totalBatchSize
// }



// Dapr subscription in [Topic] routes orders topic to this route
app.MapPost("/generate-keyphrases", [Topic("pubsub", "generate-keyphrases")] async (Batch batch, DaprClient daprClient) =>
{
    Console.WriteLine("Chunk received : " + batch.batch_key);

    // pause for 5 seconds

    // await Task.Delay(5000);
    // get state from statestore
    var state = await daprClient.GetStateAsync<List<Section>>("statestore", batch.batch_key);
    Console.WriteLine("State received : " + state);

    var chunks = new List<string>();
    if (state != null)
    {
        Console.WriteLine("State retrieved successfully:");
        foreach (var section in state)
        {
            Console.WriteLine($"Section: {section.Content}");
            chunks.Add(section.Content);
        }
    }
    else
    {
        Console.WriteLine("No state found.");
    }

    var keyGenerator = new KeyphraseGenerator(daprClient);

    var keyPhrases = await keyGenerator.ExtractKeyPhrases(chunks, batch.batch_nr);
    Console.WriteLine("Keyphrases extracted: ");
    foreach (var kp in keyPhrases)
    {
        Console.WriteLine(kp);
    }

    var keyphrasesResultKey = $"keyphrases-output-{batch.doc_id}-batch-{batch.batch_nr}";
    await daprClient.SaveStateAsync("statestore", keyphrasesResultKey, JsonSerializer.Serialize(keyPhrases));

    await daprClient.PublishEventAsync("pubsub", "enrichment-completed", new
    {
        ingestion_id = batch.ingestion_id,
        doc_id = batch.doc_id,
        service_name = "generate-keyphrases",
        result_key = keyphrasesResultKey,
        batch_nr = batch.batch_nr,
        total_batch_size = batch.total_batch_size
    });

    Console.WriteLine($"Published completion event for keyphrases with document ID: {batch.doc_id}");

    return Results.Ok(new { success = true });

});

await app.RunAsync();

internal record Batch(string ingestion_id, string doc_id, string batch_key, int batch_nr, int total_batch_size);
