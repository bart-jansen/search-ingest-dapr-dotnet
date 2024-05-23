using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ProcessDocument.Models;
using ProcessDocument.Services;

namespace ProcessDocument.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcessDocumentController : ControllerBase
    {
        private readonly DaprClient _daprClient;

        public ProcessDocumentController(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        [HttpPost("process-document")]
        public async Task<IActionResult> ProcessDocument([FromBody] BlobEvent blobEvent)
        {
            var processor = new DocumentProcessor(_daprClient);
            await processor.ProcessDocument(blobEvent);
            return Ok(blobEvent);
        }
    }
}