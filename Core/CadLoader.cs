using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using CadToShpGISSystem.Models;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.Core
{
    /// <summary>
    /// CAD 数据加载和预览模块。
    /// CAD 数据在 ArcObjects 中通常按 Point、Polyline、Polygon、Annotation 等要素类读取。
    /// </summary>
    public class CadLoader
    {
        private static readonly string[] CadFeatureTypes = new string[]
        {
            "Point", "Polyline", "Polygon", "Annotation"
        };

        /// <summary>
        /// 将 CAD 中可识别的要素类加载到地图控件，并返回统计信息。
        /// </summary>
        public List<LayerInfo> LoadCadToMap(AxMapControl mapControl, string cadFilePath)
        {
            if (mapControl == null)
            {
                throw new ArgumentNullException("mapControl");
            }

            if (!File.Exists(cadFilePath))
            {
                throw new FileNotFoundException("CAD 文件不存在。", cadFilePath);
            }

            List<LayerInfo> result = new List<LayerInfo>();

            for (int i = 0; i < CadFeatureTypes.Length; i++)
            {
                string featureType = CadFeatureTypes[i];
                IFeatureClass featureClass = null;

                try
                {
                    featureClass = OpenCadFeatureClass(cadFilePath, featureType);
                    if (featureClass == null)
                    {
                        continue;
                    }

                    int count = featureClass.FeatureCount(null);
                    if (count <= 0)
                    {
                        continue;
                    }

                    IFeatureLayer featureLayer = new FeatureLayerClass();
                    featureLayer.FeatureClass = featureClass;
                    featureLayer.Name = Path.GetFileName(cadFilePath) + " - " + featureType;
                    mapControl.AddLayer((ILayer)featureLayer, 0);

                    LayerInfo info = new LayerInfo();
                    info.Name = featureLayer.Name;
                    info.SourcePath = cadFilePath;
                    info.GeometryType = featureClass.ShapeType;
                    info.FeatureCount = count;
                    info.Visible = true;
                    FillCadLayerStatistics(featureClass, info);
                    result.Add(info);

                    LogHelper.Info("CAD加载", string.Format("加载 {0}，要素数：{1}", featureLayer.Name, count));
                }
                catch (Exception ex)
                {
                    LogHelper.Warn("CAD加载", string.Format("读取 CAD {0} 要素类失败：{1}", featureType, ex.Message));
                }
            }

            if (result.Count > 0)
            {
                mapControl.ActiveView.Refresh();
            }

            return result;
        }

        /// <summary>
        /// 打开 CAD 中指定类型的要素类。
        /// 常见命名方式：示例.dwg:Point、示例.dwg:Polyline、示例.dwg:Polygon、示例.dwg:Annotation。
        /// </summary>
        public IFeatureClass OpenCadFeatureClass(string cadFilePath, string cadFeatureType)
        {
            string folder = Path.GetDirectoryName(cadFilePath);
            string cadName = Path.GetFileName(cadFilePath);
            string className = cadName + ":" + cadFeatureType;

            IWorkspaceFactory workspaceFactory = null;
            IFeatureWorkspace featureWorkspace = null;

            try
            {
                workspaceFactory = new CadWorkspaceFactoryClass();
                featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(folder, 0);
                return featureWorkspace.OpenFeatureClass(className);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (workspaceFactory != null)
                {
                    Marshal.ReleaseComObject(workspaceFactory);
                }
            }
        }

        /// <summary>
        /// 统计 CAD 内部 Layer 字段数量，便于答辩展示 CAD 图层读取能力。
        /// </summary>
        private void FillCadLayerStatistics(IFeatureClass featureClass, LayerInfo layerInfo)
        {
            if (featureClass == null || layerInfo == null)
            {
                return;
            }

            int layerIndex = featureClass.FindField("Layer");
            if (layerIndex < 0)
            {
                return;
            }

            IFeatureCursor cursor = null;
            try
            {
                cursor = featureClass.Search(null, true);
                IFeature feature;
                while ((feature = cursor.NextFeature()) != null)
                {
                    object value = feature.get_Value(layerIndex);
                    string layerName = value == null ? "未命名" : value.ToString();
                    if (!layerInfo.LayerStatistics.ContainsKey(layerName))
                    {
                        layerInfo.LayerStatistics[layerName] = 0;
                    }

                    layerInfo.LayerStatistics[layerName]++;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn("CAD加载", "统计 CAD Layer 字段失败：" + ex.Message);
            }
            finally
            {
                if (cursor != null)
                {
                    Marshal.ReleaseComObject(cursor);
                }
            }
        }
    }
}
