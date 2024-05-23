using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProcessDocument.Models;
using ProcessDocument.Services;

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
app.MapPost("/process-document", [Topic("pubsub", "process-document")] async (BlobEvent blobEvent, DaprClient daprClient) =>
{
    Console.WriteLine("Document received : " + blobEvent.BlobName);
    var processor = new DocumentProcessor(daprClient);
    await processor.ProcessDocument(blobEvent);
    
    return Results.Ok();

    
});

await app.RunAsync();

