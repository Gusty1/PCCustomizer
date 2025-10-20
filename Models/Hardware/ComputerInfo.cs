using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCCustomizer.Models.Hardware
{
    /// <summary>
    /// 存放所有電腦硬體與系統資訊的主模型。
    /// </summary>
    public class ComputerInfo
    {
        public string? OsArchitecture { get; set; }
        public string? MotherboardManufacturer { get; set; }
        public string? MotherboardProduct { get; set; }
        public string? CpuName { get; set; }
        public uint? CpuCores { get; set; }
        public uint? CpuThreads { get; set; }
        public uint? CpuMaxClockSpeedMhz { get; set; }
        public double? TotalPhysicalMemoryGb { get; set; }
        public List<GpuInfo>? Gpus { get; set; } = [];
        public List<RamStickInfo>? RamSticks { get; set; } = [];
        public List<DiskInfo>? Disks { get; set; } = [];
    }
}
