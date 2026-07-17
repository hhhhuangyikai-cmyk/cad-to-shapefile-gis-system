using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using CadToShpGISSystem.Models;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.Core
{
    /// <summary>
    /// 缓冲距离单位。
    /// ArcObjects 的 Buffer(distance) 默认使用数据坐标单位，本枚举用于让界面表达更明确。
    /// </summary>
    public enum BufferDistanceUnit
    {
        MapUnit,
        Meter,
        Kilometer,
        Degree
    }

    /// <summary>
    /// 缓冲区分析模块。
    /// </summary>
    public class BufferAnalysis
    {
        /// <summary>
        /// 兼容旧调用：默认按数据坐标单位进行缓冲。
        /// </summary>
        public string CreateBuffer(
            AxMapControl mapControl,
            IFeatureLayer sourceLayer,
            double distance,
            bool selectedOnly,
            string outputFolder,
            string outputName)
        {
            return CreateBuffer(
                mapControl,
                sourceLayer,
                distance,
                BufferDistanceUnit.MapUnit,
                selectedOnly,
                outputFolder,
                outputName);
        }

        /// <summary>
        /// 对选中要素或整个图层生成缓冲区，输出为 Shapefile 并加载到地图。
        /// </summary>
        public string CreateBuffer(
            AxMapControl mapControl,
            IFeatureLayer sourceLayer,
            double distance,
            BufferDistanceUnit distanceUnit,
            bool selectedOnly,
            string outputFolder,
            string outputName)
        {
            if (sourceLayer == null || sourceLayer.FeatureClass == null)
            {
                throw new ArgumentException("请选择一个有效的要素图层。");
            }

            if (distance <= 0)
            {
                throw new ArgumentException("缓冲距离必须大于 0。");
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentException("输出目录不能为空。");
            }

            if (string.IsNullOrEmpty(outputName))
            {
                outputName = sourceLayer.Name + "_Buffer.shp";
            }

            if (!outputName.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
            {
                outputName += ".shp";
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            ISpatialReference sourceSpatialReference = SpatialReferenceHelper.GetSpatialReference(sourceLayer.FeatureClass);
            BufferUnitContext unitContext = BuildUnitContext(sourceSpatialReference, distance, distanceUnit);

            List<FieldMapping> fields = new List<FieldMapping>();
            fields.Add(new FieldMapping("SOURCEOID", "SRC_OID", esriFieldType.esriFieldTypeInteger, 0));
            fields.Add(new FieldMapping("DISTANCE", "DISTANCE", esriFieldType.esriFieldTypeDouble, 0));
            fields.Add(new FieldMapping("UNIT", "UNIT", esriFieldType.esriFieldTypeString, 20));

            string outputPath = Path.Combine(outputFolder, outputName);
            IFeatureClass targetFeatureClass = null;
            IFeatureCursor sourceCursor = null;
            IFeatureCursor insertCursor = null;
            IFeatureBuffer featureBuffer = null;

            try
            {
                targetFeatureClass = ShapefileHelper.CreateFeatureClass(
                    outputFolder,
                    outputName,
                    esriGeometryType.esriGeometryPolygon,
                    sourceSpatialReference,
                    fields);

                sourceCursor = GetSourceCursor(sourceLayer, selectedOnly);
                insertCursor = targetFeatureClass.Insert(true);
                featureBuffer = targetFeatureClass.CreateFeatureBuffer();

                int srcOidIndex = targetFeatureClass.FindField("SRC_OID");
                int distanceIndex = targetFeatureClass.FindField("DISTANCE");
                int unitIndex = targetFeatureClass.FindField("UNIT");

                IFeature sourceFeature;
                int count = 0;
                while ((sourceFeature = sourceCursor.NextFeature()) != null)
                {
                    IGeometry sourceGeometry = sourceFeature.ShapeCopy;
                    if (sourceGeometry == null || sourceGeometry.IsEmpty)
                    {
                        continue;
                    }

                    IGeometry analysisGeometry = PrepareSourceGeometry(
                        sourceGeometry,
                        sourceSpatialReference,
                        unitContext.AnalysisSpatialReference);

                    ITopologicalOperator topo = analysisGeometry as ITopologicalOperator;
                    if (topo == null)
                    {
                        continue;
                    }

                    IGeometry bufferGeometry = topo.Buffer(unitContext.AnalysisDistance);
                    if (bufferGeometry == null || bufferGeometry.IsEmpty)
                    {
                        continue;
                    }

                    if (SpatialReferenceHelper.IsUnknown(bufferGeometry.SpatialReference) &&
                        !SpatialReferenceHelper.IsUnknown(unitContext.AnalysisSpatialReference))
                    {
                        bufferGeometry.SpatialReference = unitContext.AnalysisSpatialReference;
                    }

                    featureBuffer.Shape = SpatialReferenceHelper.PrepareGeometryFor2DShapefile(
                        bufferGeometry,
                        sourceSpatialReference);

                    if (srcOidIndex >= 0)
                    {
                        featureBuffer.set_Value(srcOidIndex, sourceFeature.OID);
                    }

                    if (distanceIndex >= 0)
                    {
                        featureBuffer.set_Value(distanceIndex, distance);
                    }

                    if (unitIndex >= 0)
                    {
                        featureBuffer.set_Value(unitIndex, GetUnitDisplayName(distanceUnit));
                    }

                    insertCursor.InsertFeature(featureBuffer);
                    count++;
                }

                insertCursor.Flush();
                LogHelper.Info(
                    "缓冲分析",
                    string.Format(
                        "生成缓冲区 {0} 个，输入距离：{1} {2}，计算距离：{3:F6}，输出：{4}",
                        count,
                        distance,
                        GetUnitDisplayName(distanceUnit),
                        unitContext.AnalysisDistance,
                        outputPath));

                if (mapControl != null)
                {
                    ShapefileHelper.AddShapefileToMap(mapControl, outputPath);
                }

                return outputPath;
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

                if (sourceCursor != null)
                {
                    Marshal.ReleaseComObject(sourceCursor);
                }

                if (targetFeatureClass != null)
                {
                    Marshal.ReleaseComObject(targetFeatureClass);
                }
            }
        }

        /// <summary>
        /// 根据用户选择的单位构造真正传给 Buffer 的分析距离。
        /// 如果源数据是经纬度坐标系且用户选择米/千米，则临时投影到 Web Mercator 后再缓冲。
        /// </summary>
        private BufferUnitContext BuildUnitContext(
            ISpatialReference sourceSpatialReference,
            double distance,
            BufferDistanceUnit distanceUnit)
        {
            BufferUnitContext context = new BufferUnitContext();
            context.AnalysisSpatialReference = sourceSpatialReference;
            context.AnalysisDistance = distance;

            if (distanceUnit == BufferDistanceUnit.MapUnit)
            {
                if (!SpatialReferenceHelper.IsProjected(sourceSpatialReference))
                {
                    LogHelper.Warn("缓冲分析", "当前选择“数据坐标单位”，但图层不是投影坐标系，距离可能不是米。");
                }

                return context;
            }

            if (distanceUnit == BufferDistanceUnit.Degree)
            {
                if (SpatialReferenceHelper.IsProjected(sourceSpatialReference))
                {
                    throw new ArgumentException("当前图层是投影坐标系，不建议使用“度”作为缓冲单位。请选择米、千米或数据坐标单位。");
                }

                return context;
            }

            double meters = distanceUnit == BufferDistanceUnit.Kilometer ? distance * 1000.0 : distance;

            if (SpatialReferenceHelper.IsUnknown(sourceSpatialReference))
            {
                throw new ArgumentException("当前图层坐标系未知，无法把米/千米换算为数据坐标单位。请先指定坐标系，或选择“数据坐标单位/度”。");
            }

            IProjectedCoordinateSystem projectedCoordinateSystem = sourceSpatialReference as IProjectedCoordinateSystem;
            if (projectedCoordinateSystem != null)
            {
                ILinearUnit linearUnit = projectedCoordinateSystem.CoordinateUnit;
                double metersPerUnit = linearUnit == null ? 1.0 : linearUnit.MetersPerUnit;
                if (metersPerUnit <= 0)
                {
                    metersPerUnit = 1.0;
                }

                context.AnalysisDistance = meters / metersPerUnit;
                return context;
            }

            // 地理坐标系下不能直接 Buffer(50) 当作 50 米。
            // 课程设计中采用 Web Mercator 作为临时分析坐标系，缓冲完成后再投回源坐标系输出。
            context.AnalysisSpatialReference = SpatialReferenceHelper.CreateWebMercator();
            context.AnalysisDistance = meters;
            LogHelper.Info("缓冲分析", "源图层为地理坐标系，已临时投影到 Web Mercator 进行米制缓冲。");
            return context;
        }

        private IGeometry PrepareSourceGeometry(
            IGeometry sourceGeometry,
            ISpatialReference sourceSpatialReference,
            ISpatialReference analysisSpatialReference)
        {
            if (sourceGeometry == null)
            {
                return null;
            }

            if (SpatialReferenceHelper.IsUnknown(sourceGeometry.SpatialReference) &&
                !SpatialReferenceHelper.IsUnknown(sourceSpatialReference))
            {
                sourceGeometry.SpatialReference = sourceSpatialReference;
            }

            if (!SpatialReferenceHelper.IsUnknown(analysisSpatialReference) &&
                !SpatialReferenceHelper.IsUnknown(sourceGeometry.SpatialReference) &&
                sourceGeometry.SpatialReference.FactoryCode != analysisSpatialReference.FactoryCode)
            {
                sourceGeometry.Project(analysisSpatialReference);
            }

            SpatialReferenceHelper.DropZAndM(sourceGeometry);
            return sourceGeometry;
        }

        private IFeatureCursor GetSourceCursor(IFeatureLayer sourceLayer, bool selectedOnly)
        {
            IFeatureSelection featureSelection = sourceLayer as IFeatureSelection;
            if (selectedOnly && featureSelection != null && featureSelection.SelectionSet != null &&
                featureSelection.SelectionSet.Count > 0)
            {
                ICursor cursor;
                featureSelection.SelectionSet.Search(null, true, out cursor);
                return (IFeatureCursor)cursor;
            }

            if (selectedOnly)
            {
                LogHelper.Warn("缓冲分析", "未找到选中要素，自动改为对整个图层生成缓冲区。");
            }

            return sourceLayer.FeatureClass.Search(null, true);
        }

        private string GetUnitDisplayName(BufferDistanceUnit distanceUnit)
        {
            switch (distanceUnit)
            {
                case BufferDistanceUnit.Meter:
                    return "米";
                case BufferDistanceUnit.Kilometer:
                    return "千米";
                case BufferDistanceUnit.Degree:
                    return "度";
                default:
                    return "数据坐标单位";
            }
        }

        private class BufferUnitContext
        {
            public ISpatialReference AnalysisSpatialReference { get; set; }

            public double AnalysisDistance { get; set; }
        }
    }
}
