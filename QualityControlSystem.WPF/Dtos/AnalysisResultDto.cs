using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace QualityControlSystem.WPF.Dtos
{
    public class AnalysisResultDto
    {
        public string Status { get; set; } = string.Empty;
        public double MatchValue { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}