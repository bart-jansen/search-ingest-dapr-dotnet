using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
app.MapPost("/generate-keyphrases", [Topic("pubsub", "generate-keyphrases")] (Batch batch , DaprClient daprClient) =>
{
    Console.WriteLine("Chunk received : " + batch.batch_key);
    // var processor = new DocumentProcessor(daprClient);
    // await processor.ProcessDocument(blobEvent);

    
    
    return Results.Ok();
});

await app.RunAsync();

internal record Batch(string ingestion_id, string doc_id, string batch_key, int batch_nr, int total_batch_size);
