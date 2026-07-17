using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.Models
{
    /// <summary>
    /// 图层列表中显示的图层信息。
    /// 对 CAD 数据，LayerStatistics 用来统计 CAD 内部 Layer 字段的数量。
    /// </summary>
    public class LayerInfo
    {
        public LayerInfo()
        {
            LayerStatistics = new Dictionary<string, int>();
        }

        public string Name { get; set; }

        public string SourcePath { get; set; }

        public esriGeometryType GeometryType { get; set; }

        public int FeatureCount { get; set; }

        public bool Visible { get; set; }

        public Dictionary<string, int> LayerStatistics { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} ({1}, {2} 个要素)", Name, GeometryType, FeatureCount);
        }
    }
}
