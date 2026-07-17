using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using CadToShpGISSystem.Models;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.Core
{
    /// <summary>
    /// CAD 转 Shapefile 核心模块。
    /// 稳定版实现采用“打开 CAD 要素类 -> 创建 Shapefile -> 游标复制要素”的方式，
    /// 便于定位错误，也便于课程答辩说明 ArcObjects 接口调用过程。
    /// </summary>
    public class CadToShpConverter
    {
        private readonly CadLoader _cadLoader;

        public CadToShpConverter()
        {
            _cadLoader = new CadLoader();
        }

        /// <summary>
        /// 批量转换 CAD 中的点、线、面、注记。
        /// </summary>
        public List<ConversionResult> ConvertCadToShapefiles(
            string cadFilePath,
            string outputFolder,
            string outputPrefix,
            bool exportPoint,
            bool exportPolyline,
            bool exportPolygon,
            bool exportAnnotation,
            ISpatialReference sourceSpatialReference,
            ISpatialReference outputSpatialReference)
        {
            if (string.IsNullOrEmpty(cadFilePath) || !File.Exists(cadFilePath))
            {
                throw new FileNotFoundException("输入 CAD 文件不存在。", cadFilePath);
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentException("输出文件夹不能为空。", "outputFolder");
            }

            if (string.IsNullOrEmpty(outputPrefix))
            {
                outputPrefix = Path.GetFileNameWithoutExtension(cadFilePath);
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            List<ConversionResult> results = new List<ConversionResult>();
            LogHelper.Info("CAD转换", "开始转换 CAD：" + cadFilePath);
            LogHelper.Info(
                "CAD转换",
                "坐标系设置：CAD原始=" + GetSpatialReferenceName(sourceSpatialReference) +
                "，输出=" + GetSpatialReferenceName(outputSpatialReference) +
                (SpatialReferenceHelper.IsUnknown(outputSpatialReference) ? "（沿用CAD原始坐标系）" : string.Empty));

            if (exportPoint)
            {
                results.Add(ConvertOneFeatureClass(cadFilePath, outputFolder, outputPrefix, "Point", "Point", sourceSpatialReference, outputSpatialReference));
            }

            if (exportPolyline)
            {
                results.Add(ConvertOneFeatureClass(cadFilePath, outputFolder, outputPrefix, "Polyline", "Line", sourceSpatialReference, outputSpatialReference));
            }

            if (exportPolygon)
            {
                results.Add(ConvertOneFeatureClass(cadFilePath, outputFolder, outputPrefix, "Polygon", "Polygon", sourceSpatialReference, outputSpatialReference));
            }

            if (exportAnnotation)
            {
                results.Add(ConvertOneFeatureClass(cadFilePath, outputFolder, outputPrefix, "Annotation", "Anno", sourceSpatialReference, outputSpatialReference));
            }

            LogHelper.Info("CAD转换", "CAD 转换结束，输出目录：" + outputFolder);
            return results;
        }

        /// <summary>
        /// 转换 CAD 中的单个要素类。
        /// </summary>
        private ConversionResult ConvertOneFeatureClass(
            string cadFilePath,
            string outputFolder,
            string outputPrefix,
            string cadFeatureType,
            string outputSuffix,
            ISpatialReference sourceSpatialReferenceOverride,
            ISpatialReference outputSpatialReference)
        {
            ConversionResult result = new ConversionResult();
            result.CadFeatureClassName = Path.GetFileName(cadFilePath) + ":" + cadFeatureType;
            result.StartTime = DateTime.Now;

            IFeatureClass sourceFeatureClass = null;
            IFeatureClass targetFeatureClass = null;

            try
            {
                sourceFeatureClass = _cadLoader.OpenCadFeatureClass(cadFilePath, cadFeatureType);
                if (sourceFeatureClass == null)
                {
                    result.Success = false;
                    result.Message = "未找到 CAD 要素类或该类型为空。";
                    return result;
                }

                int sourceCount = sourceFeatureClass.FeatureCount(null);
                if (sourceCount <= 0)
                {
                    result.Success = true;
                    result.ExportCount = 0;
                    result.Message = "该要素类没有可导出的要素。";
                    return result;
                }

                esriGeometryType outputGeometryType = NormalizeGeometryType(sourceFeatureClass.ShapeType, cadFeatureType);
                if (outputGeometryType == esriGeometryType.esriGeometryNull)
                {
                    result.Success = false;
                    result.Message = "Shapefile 不支持该几何类型：" + sourceFeatureClass.ShapeType;
                    return result;
                }

                ISpatialReference sourceSpatialReference = SpatialReferenceHelper.GetSpatialReference(sourceFeatureClass);
                if (!SpatialReferenceHelper.IsUnknown(sourceSpatialReferenceOverride))
                {
                    sourceSpatialReference = sourceSpatialReferenceOverride;
                }

                ISpatialReference targetSpatialReference = SpatialReferenceHelper.IsUnknown(outputSpatialReference)
                    ? sourceSpatialReference
                    : outputSpatialReference;

                List<FieldMapping> fieldMappings = BuildDefaultFieldMappings(sourceFeatureClass);
                string outputName = outputPrefix + "_" + outputSuffix + ".shp";
                string outputPath = Path.Combine(outputFolder, outputName);
                result.OutputShapefilePath = outputPath;
                result.GeometryType = outputGeometryType;

                targetFeatureClass = ShapefileHelper.CreateFeatureClass(
                    outputFolder,
                    outputName,
                    outputGeometryType,
                    targetSpatialReference,
                    fieldMappings);

                result.ExportCount = CopyFeatures(
                    sourceFeatureClass,
                    targetFeatureClass,
                    fieldMappings,
                    sourceSpatialReference,
                    targetSpatialReference);

                result.Success = true;
                result.Message = string.Format("转换成功，导出 {0} 个要素。", result.ExportCount);
                LogHelper.Info("CAD转换", result.CadFeatureClassName + " -> " + outputPath + "，数量：" + result.ExportCount);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                LogHelper.Error("CAD转换", "转换失败：" + result.CadFeatureClassName, ex);
            }
            finally
            {
                result.EndTime = DateTime.Now;

                if (targetFeatureClass != null)
                {
                    Marshal.ReleaseComObject(targetFeatureClass);
                }

                if (sourceFeatureClass != null)
                {
                    Marshal.ReleaseComObject(sourceFeatureClass);
                }
            }

            return result;
        }

        /// <summary>
        /// 将源要素逐条复制到目标 Shapefile。
        /// </summary>
        private int CopyFeatures(
            IFeatureClass sourceFeatureClass,
            IFeatureClass targetFeatureClass,
            IList<FieldMapping> fieldMappings,
            ISpatialReference sourceSpatialReference,
            ISpatialReference targetSpatialReference)
        {
            int count = 0;
            IFeatureCursor searchCursor = null;
            IFeatureCursor insertCursor = null;
            IFeatureBuffer featureBuffer = null;

            try
            {
                searchCursor = sourceFeatureClass.Search(null, true);
                insertCursor = targetFeatureClass.Insert(true);
                featureBuffer = targetFeatureClass.CreateFeatureBuffer();

                IFeature sourceFeature;
                while ((sourceFeature = searchCursor.NextFeature()) != null)
                {
                    IGeometry geometry = sourceFeature.ShapeCopy;
                    if (geometry == null || geometry.IsEmpty)
                    {
                        continue;
                    }

                    featureBuffer.Shape = SpatialReferenceHelper.PrepareGeometryFor2DShapefile(
                        geometry,
                        sourceSpatialReference,
                        targetSpatialReference);
                    CopyAttributeValues(sourceFeature, featureBuffer, sourceFeatureClass, targetFeatureClass, fieldMappings);

                    insertCursor.InsertFeature(featureBuffer);
                    count++;

                    if (count % 500 == 0)
                    {
                        insertCursor.Flush();
                        LogHelper.Info("CAD转换", "已导出 " + count + " 个要素...");
                    }
                }

                insertCursor.Flush();
                return count;
            }
            finally
            {
                if (featureBuffer != null)
                {
                    Marshal.ReleaseComObject(featureBuffer);
                }

                if (insertCursor != null)
                {
                    Marshal.ReleaseComObject(insertCursor);
                }

                if (searchCursor != null)
                {
                    Marshal.ReleaseComObject(searchCursor);
                }
            }
        }

        /// <summary>
        /// 复制 CAD 常用属性字段。
        /// </summary>
        private void CopyAttributeValues(
            IFeature sourceFeature,
            IFeatureBuffer targetBuffer,
            IFeatureClass sourceFeatureClass,
            IFeatureClass targetFeatureClass,
            IList<FieldMapping> fieldMappings)
        {
            for (int i = 0; i < fieldMappings.Count; i++)
            {
                FieldMapping mapping = fieldMappings[i];
                int sourceIndex = FindFieldIgnoreCase(sourceFeatureClass, mapping.SourceName);
                int targetIndex = targetFeatureClass.FindField(mapping.TargetName);

                if (sourceIndex < 0 || targetIndex < 0)
                {
                    continue;
                }

                object value = sourceFeature.get_Value(sourceIndex);
                object converted = ConvertValue(value, mapping.FieldType, mapping.Length);
                if (converted != null)
                {
                    targetBuffer.set_Value(targetIndex, converted);
                }
            }
        }

        /// <summary>
        /// 按 Shapefile 字段类型转换值，避免类型不匹配导致插入失败。
        /// </summary>
        private object ConvertValue(object value, esriFieldType targetType, int length)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            try
            {
                switch (targetType)
                {
                    case esriFieldType.esriFieldTypeInteger:
                    case esriFieldType.esriFieldTypeSmallInteger:
                        return Convert.ToInt32(value);
                    case esriFieldType.esriFieldTypeDouble:
                    case esriFieldType.esriFieldTypeSingle:
                        return Convert.ToDouble(value);
                    case esriFieldType.esriFieldTypeString:
                        string text = value.ToString();
                        if (length > 0 && text.Length > length)
                        {
                            text = text.Substring(0, length);
                        }
                        return text;
                    default:
                        return value;
                }
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// CAD 常用字段映射到 Shapefile 字段。只有源数据存在的字段才会创建。
        /// </summary>
        private List<FieldMapping> BuildDefaultFieldMappings(IFeatureClass sourceFeatureClass)
        {
            List<FieldMapping> all = new List<FieldMapping>();
            all.Add(new FieldMapping("Layer", "LAYER", esriFieldType.esriFieldTypeString, 80));
            all.Add(new FieldMapping("Color", "COLOR", esriFieldType.esriFieldTypeInteger, 0));
            all.Add(new FieldMapping("Linetype", "LINETYPE", esriFieldType.esriFieldTypeString, 40));
            all.Add(new FieldMapping("LineWt", "LINEWT", esriFieldType.esriFieldTypeString, 30));
            all.Add(new FieldMapping("Elevation", "ELEV", esriFieldType.esriFieldTypeDouble, 0));
            all.Add(new FieldMapping("Thickness", "THICKNESS", esriFieldType.esriFieldTypeDouble, 0));
            all.Add(new FieldMapping("Text", "TEXT", esriFieldType.esriFieldTypeString, 254));
            all.Add(new FieldMapping("TxtMemo", "TXTMEMO", esriFieldType.esriFieldTypeString, 254));
            all.Add(new FieldMapping("RefName", "REFNAME", esriFieldType.esriFieldTypeString, 80));

            List<FieldMapping> exists = new List<FieldMapping>();
            for (int i = 0; i < all.Count; i++)
            {
                if (FindFieldIgnoreCase(sourceFeatureClass, all[i].SourceName) >= 0)
                {
                    exists.Add(all[i]);
                }
            }

            return exists;
        }

        /// <summary>
        /// Shapefile 支持点、线、面、多点等几何类型；注记数据在课程设计中按其 Shape 类型导出。
        /// </summary>
        private esriGeometryType NormalizeGeometryType(esriGeometryType sourceType, string cadFeatureType)
        {
            if (sourceType == esriGeometryType.esriGeometryPoint ||
                sourceType == esriGeometryType.esriGeometryPolyline ||
                sourceType == esriGeometryType.esriGeometryPolygon ||
                sourceType == esriGeometryType.esriGeometryMultipoint)
            {
                return sourceType;
            }

            if (cadFeatureType.Equals("Annotation", StringComparison.OrdinalIgnoreCase))
            {
                // 部分 CAD 注记在 ArcObjects 中表现为点状几何，若 ShapeType 异常则按点导出并保留 Text 字段。
                return esriGeometryType.esriGeometryPoint;
            }

            return esriGeometryType.esriGeometryNull;
        }

        private int FindFieldIgnoreCase(IFeatureClass featureClass, string fieldName)
        {
            if (featureClass == null || string.IsNullOrEmpty(fieldName))
            {
                return -1;
            }

            int index = featureClass.FindField(fieldName);
            if (index >= 0)
            {
                return index;
            }

            IFields fields = featureClass.Fields;
            for (int i = 0; i < fields.FieldCount; i++)
            {
                IField field = fields.get_Field(i);
                if (field.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private string GetSpatialReferenceName(ISpatialReference spatialReference)
        {
            if (SpatialReferenceHelper.IsUnknown(spatialReference))
            {
                return "未知/不指定";
            }

            try
            {
                return spatialReference.Name;
            }
            catch
            {
                return "已指定";
            }
        }
    }
}
