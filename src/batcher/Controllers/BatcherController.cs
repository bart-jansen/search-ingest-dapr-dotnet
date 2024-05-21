using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using batcher.Models;

namespace batcher.Controllers;
public static class BatcherController
{
    public static void MapRoutes(WebApplication app)
    {
        app.MapPost("/batcher-trigger", HandleBatcherTrigger);
    }

    private static IResult HandleBatcherTrigger(IngestRequest request)
    {
        Console.WriteLine($"Trigger received: {request}");
        // You might want to log more specific details from the request, for example:
        Console.WriteLine($"Source Folder Path: {request.SourceFolderPath}");
        Console.WriteLine($"Destination Folder Path: {request.DestinationFolderPath}");
        Console.WriteLine($"Search Items Folder Path: {request.SearchItemsFolderPath}");
        Console.WriteLine($"Search Indexer Name: {request.SearchIndexerName}");

        return Results.Ok(request);
    }
}