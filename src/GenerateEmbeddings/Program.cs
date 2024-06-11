using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using GenerateEmbeddings.Services;
using GenerateEmbeddings.Models;

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

// Dapr subscription in [Topic] routes orders topic to this route
app.MapPost("/generate-embeddings", [Topic("pubsub", "generate-embeddings")] async (Batch batch, DaprClient daprClient) =>
{
    Console.WriteLine("Chunk received : " + batch.batch_key);

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

    var embeddingGenerator = new EmbeddingsGenerator(daprClient);
    
    var embeddings = await embeddingGenerator.ComputeEmbeddings(chunks, batch.batch_nr);
    
    var embeddingResultKey = $"embedding-output-{batch.doc_id}-batch-{batch.batch_nr}";
    await daprClient.SaveStateAsync("statestore", embeddingResultKey, JsonSerializer.Serialize(embeddings));

    await daprClient.PublishEventAsync("pubsub", "enrichment-completed", new
    {
        ingestion_id = batch.ingestion_id,
        doc_id = batch.doc_id,
        service_name = "generate-embeddings",
        result_key = embeddingResultKey,
        batch_nr = batch.batch_nr,
        total_batch_size = batch.total_batch_size
    });

    Console.WriteLine($"Published completion event for embeddings with document ID: {batch.doc_id}");

    return Results.Ok(new { success = true });

});

await app.RunAsync();

internal record Batch(string ingestion_id, string doc_id, string batch_key, int batch_nr, int total_batch_size);