using System;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();
app.UseXRay("ppops-notification-svc");
app.MapControllers();
app.MapGet("/health", () => "healthy");
app.Run();

namespace NotificationSvc.Controllers
{
    [ApiController]
    [Route("api/notification")]
    public class NotificationController : ControllerBase
    {
        private static readonly Random _rng = new();

        [HttpPost("alert")]
        public async Task<IActionResult> SendAlert([FromBody] AlertRequest request)
        {
            AWSXRayRecorder.Instance.AddAnnotation("recipient", request.Recipient ?? "unknown");
            AWSXRayRecorder.Instance.AddAnnotation("alertType", request.AlertType ?? "general");
            await Task.Delay(_rng.Next(30, 250));

            if (_rng.NextDouble() < 0.03)
                return StatusCode(503, new { Error = "Notification gateway unavailable" });

            return Ok(new
            {
                MessageId = Guid.NewGuid().ToString(),
                Recipient = request.Recipient,
                Status = "sent",
                SentAt = DateTime.UtcNow
            });
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> SendBulkAlerts([FromBody] BulkAlertRequest request)
        {
            AWSXRayRecorder.Instance.AddAnnotation("bulkCount", (request.Recipients?.Length ?? 0).ToString());
            await Task.Delay(_rng.Next(100, 500));
            var results = (request.Recipients ?? Array.Empty<string>()).Select(r => new
            {
                MessageId = Guid.NewGuid().ToString(),
                Recipient = r,
                Status = "sent",
                SentAt = DateTime.UtcNow
            });
            return Ok(results);
        }
    }

    public class AlertRequest
    {
        public string? Recipient { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? AlertType { get; set; }
    }

    public class BulkAlertRequest
    {
        public string[]? Recipients { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? AlertType { get; set; }
    }
}
