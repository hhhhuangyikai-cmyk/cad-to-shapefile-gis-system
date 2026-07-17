using System;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.Models
{
    /// <summary>
    /// 单个几何类型 CAD 转 Shapefile 的结果。
    /// </summary>
    public class ConversionResult
    {
        public string CadFeatureClassName { get; set; }

        public string OutputShapefilePath { get; set; }

        public esriGeometryType GeometryType { get; set; }

        public int ExportCount { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeSpan Duration
        {
            get { return EndTime - StartTime; }
        }
    }
}
