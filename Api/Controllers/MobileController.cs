using Api.Application.Services;
using Api.Domain.Entities;
using Api.Domain.Settings;
using Api.Infrastructure.Persistence;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/mobile")]
    public class MobileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageSettings _blobSettings;
        private readonly INarrationAudioService _narrationService;
        private readonly ILogger<MobileController> _logger;

        public MobileController(
            AppDbContext context,
            IOptions<BlobStorageSettings> blobSettings,
            INarrationAudioService narrationService,
            ILogger<MobileController> logger)
        {
            _context = context;
            _blobSettings = blobSettings.Value;
            _narrationService = narrationService;
            _logger = logger;
        }

        // Public endpoint: list active stall locations for mobile app
        [HttpGet("locations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLocations()
        {
            var items = await _context.StallLocations
                .Include(l => l.Stall)
                .Where(l => l.IsActive)
                .Select(l => new
                {
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

        // Mobile gọi endpoint này khi user bấm "Nghe thuyết minh" + chọn ngôn ngữ.
        // Trả về JSON nhẹ gồm scriptText + audioUrl đã sinh sẵn ở Blob Storage.
        // Mobile chỉ cần stream / download từ AudioUrl là phát được.
        // Không gọi Azure TTS realtime → nhanh, ổn định, không tốn quota.
        //
        // Query: /api/mobile/narration?stallId={guid}&lang={code}&voiceId={guid?}
        [HttpGet("narration")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNarration(
            [FromQuery] Guid stallId,
            [FromQuery] string? lang,
            [FromQuery] Guid? voiceId,
            CancellationToken cancellationToken = default)
        {
            if (stallId == Guid.Empty)
                return BadRequest(new { message = "stallId is required" });

            if (string.IsNullOrWhiteSpace(lang))
                return BadRequest(new { message = "lang is required" });

            // 1) Resolve Language theo mã. Mobile có thể gửi "en-US" hoặc "en".
            // Dùng resolver chung để chấp nhận cả mã đầy đủ và mã ngắn.
            var language = await ResolveLanguageAsync(lang, cancellationToken);

            if (language == null)
            {
                _logger.LogInformation("Narration: không tìm thấy ngôn ngữ {Lang}", lang);
                return NotFound(new { message = "Language not found" });
            }

            // 2) Lấy tất cả narration content active của stall để có thể tìm audio đúng ngôn ngữ.
            //    Web thường lưu nhiều audio ngoại ngữ dưới cùng một content gốc, nên không được
            //    chỉ nhìn vào content có LanguageId trùng ngôn ngữ đang chọn.
            var contents = await _context.StallNarrationContents
                .AsNoTracking()
                .Include(c => c.NarrationAudios)
                    .ThenInclude(a => a.TtsVoiceProfile)
                .Where(c => c.StallId == stallId && c.IsActive)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.UpdatedAt)
                .ToListAsync(cancellationToken);

            if (contents.Count == 0)
            {
                _logger.LogInformation("Narration: chưa có content cho StallId {StallId}, Lang {Lang}", stallId, lang);
                return NotFound(new { message = "Narration content not found" });
            }

            var content = contents.FirstOrDefault(c => c.LanguageId == language.Id) ?? contents[0];

            // 3) Chọn audio theo thứ tự ưu tiên:
            //    - Khớp voiceId người dùng chọn (nếu có)
            //    - Bản TTS thuộc đúng ngôn ngữ người dùng chọn
            //    - Bất kỳ audio nào thuộc đúng ngôn ngữ người dùng chọn
            // Không fallback bừa sang audio ngôn ngữ khác để tránh chọn English nhưng phát tiếng Việt.
            var selected = SelectAudioForLanguage(content.NarrationAudios, language.Id, voiceId, content.LanguageId == language.Id);

            // Nếu content ưu tiên không có audio URL ngoại ngữ, quét các content khác của cùng stall.
            // Trường hợp này đúng với màn web đang gom nhiều audio theo ngôn ngữ trong một content.
            if (selected == null)
            {
                foreach (var candidate in contents.Where(c => c.Id != content.Id))
                {
                    selected = SelectAudioForLanguage(candidate.NarrationAudios, language.Id, voiceId, candidate.LanguageId == language.Id);
                    if (selected != null)
                    {
                        content = candidate;
                        break;
                    }
                }
            }

            return Ok(new
            {
                NarrationContentId = content.Id,
                StallId = content.StallId,
                LanguageId = language.Id,
                SourceLanguageId = content.LanguageId,
                LanguageCode = language.Code,
                Title = content.Title,
                Description = content.Description,
                ScriptText = content.ScriptText,
                AudioUrl = selected?.AudioUrl,
                AudioId = selected?.Id,
                TtsVoiceProfileId = selected?.TtsVoiceProfileId,
                IsTts = selected?.IsTts ?? false,
                UpdatedAt = content.UpdatedAt
            });
        }

        // Public endpoint: return synthesized mp3 for a narration content.
        // Endpoint nặng — chỉ dùng khi audio chưa sinh sẵn ở Blob.
        // Query: /api/mobile/tts?narrationContentId={guid}  hoặc  ?stallId={guid}&lang={code}[&voiceId={guid}]
        [HttpGet("tts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTts(
            [FromQuery] Guid? narrationContentId,
            [FromQuery] Guid? stallId,
            [FromQuery] string? lang,
            [FromQuery] Guid? voiceId,
            CancellationToken cancellationToken = default)
        {
            if (narrationContentId == null && stallId == null)
                return BadRequest(new { message = "narrationContentId or stallId is required" });

            // Bước 1: Resolve narration content
            StallNarrationContent? content = null;
            if (narrationContentId.HasValue)
            {
                content = await _context.StallNarrationContents
                    .Include(s => s.Language)
                    .FirstOrDefaultAsync(s => s.Id == narrationContentId.Value, cancellationToken);
            }
            else if (stallId.HasValue && !string.IsNullOrWhiteSpace(lang))
            {
                var language = await ResolveLanguageAsync(lang, cancellationToken);
                if (language == null)
                    return NotFound(new { message = "Language not found" });

                content = await _context.StallNarrationContents
                    .Include(s => s.Language)
                    .Where(s => s.StallId == stallId.Value && s.LanguageId == language.Id)
                    .OrderByDescending(s => s.IsActive)
                    .ThenByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                // Nếu không có content đúng ngôn ngữ, dùng content gốc của stall để sinh/chọn audio.
                // Audio đa ngôn ngữ được phân biệt bằng TtsVoiceProfile.LanguageId chứ không phải LanguageId của content.
                content ??= await _context.StallNarrationContents
                    .Include(s => s.Language)
                    .Where(s => s.StallId == stallId.Value)
                    .OrderByDescending(s => s.IsActive)
                    .ThenByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (content == null)
                return NotFound(new { message = "Narration content not found" });

            // Bước 2: Tìm audio TTS đã có (ưu tiên đúng voice nếu được chọn)
            var requestedLanguage = !string.IsNullOrWhiteSpace(lang)
                ? await ResolveLanguageAsync(lang, cancellationToken)
                : null;

            var audioQuery = _context.NarrationAudios
                .Include(a => a.TtsVoiceProfile)
                .Where(a => a.NarrationContentId == content.Id
                            && a.IsTts
                            && !string.IsNullOrEmpty(a.BlobId));

            if (voiceId.HasValue)
                audioQuery = audioQuery.Where(a => a.TtsVoiceProfileId == voiceId.Value);
            else if (requestedLanguage != null)
                audioQuery = audioQuery.Where(a => a.TtsVoiceProfile != null && a.TtsVoiceProfile.LanguageId == requestedLanguage.Id);

            var audio = await audioQuery
                .OrderByDescending(a => a.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Bước 3: Nếu chưa có audio thì sinh TTS rồi lưu
            //         (NarrationAudioService tự xử lý voice profile bên trong, không cần truyền voiceId)
            if (audio == null)
            {
                _logger.LogInformation("TTS: chưa có audio sẵn, sinh mới — ContentId {ContentId}", content.Id);
                var results = await _narrationService.CreateOrUpdateFromTtsAsync(
                    content.Id,
                    content.ScriptText,
                    content.LanguageId,
                    voice: null,
                    provider: null);

                audio = voiceId.HasValue
                    ? results.FirstOrDefault(a => a.TtsVoiceProfileId == voiceId.Value && !string.IsNullOrEmpty(a.BlobId))
                    : null;

                if (audio == null && requestedLanguage != null)
                {
                    var profileIds = await _context.TtsVoiceProfiles
                        .Where(v => v.LanguageId == requestedLanguage.Id)
                        .Select(v => v.Id)
                        .ToListAsync(cancellationToken);

                    audio = results.FirstOrDefault(a =>
                        a.TtsVoiceProfileId.HasValue
                        && profileIds.Contains(a.TtsVoiceProfileId.Value)
                        && !string.IsNullOrEmpty(a.BlobId));
                }

                audio ??= results.FirstOrDefault(a => !string.IsNullOrEmpty(a.BlobId));
            }

            if (audio == null || string.IsNullOrWhiteSpace(audio.BlobId))
                return NotFound(new { message = "Audio not available" });

            if (string.IsNullOrWhiteSpace(_blobSettings.ConnectionString) || string.IsNullOrWhiteSpace(_blobSettings.ContainerName))
                return StatusCode(500, new { message = "Blob storage not configured" });

            // Bước 4: Stream file mp3 từ Blob về cho client
            var blobClient = new BlobServiceClient(_blobSettings.ConnectionString)
                .GetBlobContainerClient(_blobSettings.ContainerName)
                .GetBlobClient(audio.BlobId);

            if (!await blobClient.ExistsAsync(cancellationToken))
                return NotFound(new { message = "Audio blob not found" });

            var dl = await blobClient.DownloadAsync(cancellationToken);
            return File(dl.Value.Content, dl.Value.Details.ContentType ?? "audio/mpeg");
        }

        // Tìm Language theo mã user gửi lên.
        // Ưu tiên theo thứ tự: trùng tuyệt đối, trùng phần ngắn (vi/en/zh), rồi mới đến biến thể (zh-CN, en-GB).
        // Sắp xếp ưu tiên IsActive=true để admin có thể tắt biến thể không dùng nữa.
        private async Task<Language?> ResolveLanguageAsync(string? lang, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(lang)) return null;

            var normalized = lang.Trim();
            var shortCode = normalized.Split('-')[0];

            return await _context.Languages
                .Where(l =>
                    l.Code == normalized
                    || l.Code == shortCode
                    || l.Code.StartsWith(shortCode + "-"))
                .OrderByDescending(l => l.IsActive)
                .ThenByDescending(l => l.Code == normalized)
                .ThenByDescending(l => l.Code == shortCode)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static NarrationAudio? SelectAudioForLanguage(
            IEnumerable<NarrationAudio>? audios,
            Guid languageId,
            Guid? voiceId,
            bool allowLegacyAudio)
        {
            var list = audios?
                .Where(a => !string.IsNullOrWhiteSpace(a.AudioUrl))
                .OrderByDescending(a => a.UpdatedAt)
                .ToList() ?? [];

            if (list.Count == 0) return null;

            NarrationAudio? selected = null;
            if (voiceId.HasValue)
                selected = list.FirstOrDefault(a => a.TtsVoiceProfileId == voiceId.Value);

            selected ??= list.FirstOrDefault(a => a.IsTts && a.TtsVoiceProfile?.LanguageId == languageId)
                ?? list.FirstOrDefault(a => a.TtsVoiceProfile?.LanguageId == languageId);

            // Chỉ cho dùng audio legacy không gắn voice profile khi content thật sự thuộc ngôn ngữ đó.
            // Nhờ vậy chọn English/French sẽ không phát nhầm file tiếng Việt.
            if (selected == null && allowLegacyAudio)
                selected = list.FirstOrDefault(a => a.TtsVoiceProfileId == null);

            return selected;
        }
    }
}
