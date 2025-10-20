// 檔案路徑: YourProject/Services/HardwareService.cs
using PCCustomizer.Models.Hardware;
using System.Diagnostics;
using System.Management;

namespace PCCustomizer.Services
{
    public class HardwareService : IHardwareService
    {
        private ComputerInfo _cachedInfo;
        public ComputerInfo CurrentComputerInfo => _cachedInfo;

        private bool _isScanning = false;
        public bool IsScanning
        {
            get => _isScanning;
            private set
            {
                _isScanning = value;
                // 當狀態改變時，立刻發出通知
                NotifyStateChanged();
            }
        }

        public event Action OnStateChanged;
        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        public async Task ScanComputerInfoAsync()
        {
            if (_cachedInfo != null || IsScanning)
            {
                return;
            }

            try
            {
                // 在開始掃描前，立刻將狀態設為 true
                IsScanning = true;

                // 將耗時操作放到背景執行緒
                var info = await Task.Run(() =>
                {
                    var computerInfo = new ComputerInfo();
                    // --- 獲取主機板資訊 ---
                    using var boardSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                    var board = boardSearcher.Get().OfType<ManagementObject>().FirstOrDefault();
                    if (board != null)
                    {
                        computerInfo.MotherboardManufacturer = board["Manufacturer"]?.ToString() ?? "N/A";
                        computerInfo.MotherboardProduct = board["Product"]?.ToString() ?? "N/A";
                    }

                    // --- 獲取 CPU 資訊 ---
                    using var cpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    var cpu = cpuSearcher.Get().OfType<ManagementObject>().FirstOrDefault();
                    if (cpu != null)
                    {
                        computerInfo.CpuName = cpu["Name"]?.ToString()?.Trim() ?? "N/A";
                        computerInfo.CpuCores = (uint)cpu["NumberOfCores"];
                        computerInfo.CpuThreads = (uint)cpu["NumberOfLogicalProcessors"];
                        computerInfo.CpuMaxClockSpeedMhz = (uint)cpu["MaxClockSpeed"];
                    }

                    // --- 獲取 GPU 資訊 (可能有多個) ---
                    using var gpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                    foreach (ManagementObject gpu in gpuSearcher.Get())
                    {
                        var vramBytes = (uint?)gpu["AdapterRAM"] ?? 0;
                        computerInfo.Gpus.Add(new GpuInfo
                        {
                            Name = gpu["Caption"]?.ToString() ?? "N/A",
                            AdapterRamGb = Math.Round(vramBytes / (1024.0 * 1024.0 * 1024.0), 2),
                            DriverVersion = gpu["DriverVersion"]?.ToString() ?? "N/A"
                        });
                    }

                    // --- 獲取記憶體資訊 (可能有多條) ---
                    using var ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                    foreach (ManagementObject stick in ramSearcher.Get())
                    {
                        var capacityBytes = (ulong?)stick["Capacity"] ?? 0;
                        computerInfo.RamSticks.Add(new RamStickInfo
                        {
                            CapacityGb = Math.Round(capacityBytes / (1024.0 * 1024.0 * 1024.0), 2),
                            SpeedMhz = (uint?)stick["Speed"] ?? 0
                        });
                    }
                    computerInfo.TotalPhysicalMemoryGb = Math.Round(computerInfo.RamSticks.Sum(r => r.CapacityGb), 2);

                    // --- 獲取硬碟資訊 (可能有多個) ---
                    using var diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                    foreach (ManagementObject disk in diskSearcher.Get())
                    {
                        var sizeBytes = (ulong?)disk["Size"] ?? 0;
                        // 過濾掉讀卡機等容量為 0 的裝置
                        if (sizeBytes > 0)
                        {
                            computerInfo.Disks.Add(new DiskInfo
                            {
                                Model = disk["Model"]?.ToString() ?? "N/A",
                                SizeGb = Math.Round(sizeBytes / (1024.0 * 1024.0 * 1024.0), 2)
                            });
                        }
                    }

                    return computerInfo;
                });

                // 掃描完成後，將結果存到快取
                _cachedInfo = info;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"獲取硬體資訊時發生錯誤: {ex.Message}");
            }
            finally
            {
                // ⭐ 無論成功或失敗，最後都將狀態設回 false
                IsScanning = false;
            }
        }
    }
}