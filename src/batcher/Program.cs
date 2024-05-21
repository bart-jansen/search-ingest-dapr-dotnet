using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using batcher.Controllers; // Adjust the namespace accordingly

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Dapr configuration and middleware
app.UseCloudEvents();
app.MapSubscribeHandler();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

BatcherController.MapRoutes(app);

await app.RunAsync();