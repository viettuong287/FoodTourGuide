using Api.Application.Services;
using Api.Infrastructure.Persistence;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Api.Domain.Settings;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/mobile")]
    public class MobileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageSettings _blobSettings;
        private readonly INarrationAudioService _narrationService;

        public MobileController(AppDbContext context, IOptions<BlobStorageSettings> blobSettings, INarrationAudioService narrationService)
        {
            _context = context;
            _blobSettings = blobSettings.Value;
            _narrationService = narrationService;
        }

        // Public endpoint: list active stall locations for mobile app
        [HttpGet("locations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLocations()
        {
            var items = await _context.StallLocations
                .Include(l => l.Stall)
                .Where(l => l.IsActive)
                .Select(l => new {
                    StallLocationId = l.Id,
                    StallId = l.StallId,
                    Name = l.Stall.Name,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Address = l.Address
                })
                .ToListAsync();

            return Ok(items);
        }

        // Public endpoint: return synthesized mp3 for a narration content (by narrationContentId)
        // Query: /api/mobile/tts?narrationContentId={guid}&lang={langCode}
        [HttpGet("tts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTts([FromQuery] Guid? narrationContentId, [FromQuery] Guid? stallId, [FromQuery] string? lang)
        {
            if (narrationContentId == null && stallId == null)
                return BadRequest("narrationContentId or stallId is required");

            // Find narration content
            Api.Domain.Entities.StallNarrationContent? content = null;
            if (narrationContentId.HasValue)
            {
                content = await _context.StallNarrationContents
                    .Include(s => s.Language)
                    .FirstOrDefaultAsync(s => s.Id == narrationContentId.Value);
            }
            else if (stallId.HasValue && !string.IsNullOrWhiteSpace(lang))
            {
                // find language id by code
                var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == lang || l.LangCode == lang);
                if (language == null) return NotFound("Language not found");

                content = await _context.StallNarrationContents
                    .Include(s => s.Language)
                    .Where(s => s.StallId == stallId.Value && s.LanguageId == language.Id)
                    .OrderByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync();
            }

            if (content == null)
                return NotFound("Narration content not found");

            // Try to find existing TTS audio
            var audio = await _context.NarrationAudios
                .Where(a => a.NarrationContentId == content.Id && a.IsTts && !string.IsNullOrEmpty(a.BlobId))
                .OrderByDescending(a => a.UpdatedAt)
                .FirstOrDefaultAsync();

            if (audio == null)
            {
                // create via narration service (this will synthesize and upload to blob)
                var results = await _narrationService.CreateOrUpdateFromTtsAsync(content.Id, content.ScriptText, content.LanguageId, null, null);
                audio = results.FirstOrDefault(a => !string.IsNullOrEmpty(a.BlobId));
            }

            if (audio == null || string.IsNullOrWhiteSpace(audio.BlobId))
                return NotFound("Audio not available");

            if (string.IsNullOrWhiteSpace(_blobSettings.ConnectionString) || string.IsNullOrWhiteSpace(_blobSettings.ContainerName))
                return StatusCode(500, "Blob storage not configured");

            var blobClient = new BlobServiceClient(_blobSettings.ConnectionString)
                .GetBlobContainerClient(_blobSettings.ContainerName)
                .GetBlobClient(audio.BlobId);

            if (!await blobClient.ExistsAsync())
                return NotFound("Audio blob not found");

            var dl = await blobClient.DownloadAsync();
            return File(dl.Value.Content, dl.Value.Details.ContentType ?? "audio/mpeg");
        }
    }
}
