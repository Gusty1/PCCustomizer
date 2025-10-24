using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCCustomizer.Models.Hardware
{
    /// <summary>
    /// 硬碟資訊相關模型
    /// </summary>
    public class DiskInfo
    {
        public string Model { get; set; }
        public double SizeGb { get; set; }
    }
}
