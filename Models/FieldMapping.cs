using ESRI.ArcGIS.Geodatabase;

namespace CadToShpGISSystem.Models
{
    /// <summary>
    /// CAD 字段到 Shapefile 字段的映射。
    /// Shapefile 字段名最长 10 个字符，因此输出字段名必须简短。
    /// </summary>
    public class FieldMapping
    {
        public string SourceName { get; set; }

        public string TargetName { get; set; }

        public esriFieldType FieldType { get; set; }

        public int Length { get; set; }

        public FieldMapping()
        {
        }

        public FieldMapping(string sourceName, string targetName, esriFieldType fieldType, int length)
        {
            SourceName = sourceName;
            TargetName = targetName;
            FieldType = fieldType;
            Length = length;
        }
    }
}
