using System.Text.Json.Serialization;
using Dapr;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();

if (app.Environment.IsDevelopment()) {app.UseDeveloperExceptionPage();}

// Dapr subscription in [Topic] routes orders topic to this route
app.MapPost("/process-document", [Topic("pubsub", "process-document")] (BlobEvent blobEvent) => {
    Console.WriteLine("Document received : " + blobEvent);
    return Results.Ok(blobEvent);
});

await app.RunAsync();




    // public class BlobEvent
    // {
    //     public string? IngestionId { get; set; }
    //     public string? DocId { get; set; }
    //     public string? BlobName { get; set; }
    // }


public record BlobEvent([property: JsonPropertyName("ingestionId")] string IngestionId, [property: JsonPropertyName("docId")] string DocId, [property: JsonPropertyName("blobName")] string BlobName);