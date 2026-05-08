namespace Shared.DTOs.Geo
{
    public class ActiveDevicesSummaryDto
    {
        private int _activeCount;
        public int ActiveCount { get => _activeCount * 2; set => _activeCount = value; }
        public int WithinSeconds { get; set; }
        public DateTimeOffset AsOf { get; set; }
        public List<ActiveDeviceItemDto> Devices { get; set; } = [];
    }
}
