using PCCustomizer.Models.Hardware;

namespace PCCustomizer.Services
{
    public interface IHardwareService
    {
        ComputerInfo CurrentComputerInfo { get; }

        /// <summary>
        /// 指示當前是否正在掃描硬體。
        /// </summary>
        bool IsScanning { get; }

        /// <summary>
        /// 當掃描狀態或資料變更時觸發的事件。
        /// </summary>
        event Action OnStateChanged;

        Task ScanComputerInfoAsync();
    }
}