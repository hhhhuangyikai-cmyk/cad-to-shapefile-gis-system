using System;
using System.Collections.Generic;
using System.Data;
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
    /// Shapefile 读写辅助类。
    /// </summary>
    public static class ShapefileHelper
    {
        /// <summary>
        /// 打开 Shapefile 要素类。
        /// </summary>
        public static IFeatureClass OpenFeatureClass(string shapefilePath)
        {
            if (string.IsNullOrEmpty(shapefilePath) || !File.Exists(shapefilePath))
            {
                throw new FileNotFoundException("Shapefile 不存在。", shapefilePath);
            }

            string folder = Path.GetDirectoryName(shapefilePath);
            string fileName = Path.GetFileName(shapefilePath);

            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(folder, 0);
            return featureWorkspace.OpenFeatureClass(fileName);
        }

        /// <summary>
        /// 打开并加载 Shapefile 到地图控件。
        /// </summary>
        public static IFeatureLayer AddShapefileToMap(AxMapControl mapControl, string shapefilePath)
        {
            IFeatureClass featureClass = OpenFeatureClass(shapefilePath);
            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            featureLayer.Name = Path.GetFileNameWithoutExtension(shapefilePath);

            mapControl.AddLayer((ILayer)featureLayer, 0);
            mapControl.ActiveView.Refresh();

            LogHelper.Info("Shapefile", "加载 Shapefile：" + shapefilePath);
            return featureLayer;
        }

        /// <summary>
        /// 创建简单 Shapefile 要素类。
        /// </summary>
        public static IFeatureClass CreateFeatureClass(
            string outputFolder,
            string shapefileName,
            esriGeometryType geometryType,
            ISpatialReference spatialReference,
            IList<FieldMapping> customFields)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            if (!shapefileName.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
            {
                shapefileName += ".shp";
            }

            string fullPath = Path.Combine(outputFolder, shapefileName);
            DeleteShapefile(fullPath);

            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace featureWorkspace = null;

            try
            {
                featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(outputFolder, 0);

                IFeatureClassDescription featureClassDescription = new FeatureClassDescriptionClass();
                IObjectClassDescription description = (IObjectClassDescription)featureClassDescription;
                IFields fields = description.RequiredFields;
                IFieldsEdit fieldsEdit = (IFieldsEdit)fields;

                int shapeIndex = fields.FindField(featureClassDescription.ShapeFieldName);
                IField shapeField = fields.get_Field(shapeIndex);
                IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)shapeField.GeometryDef;
                geometryDefEdit.GeometryType_2 = geometryType;
                if (!SpatialReferenceHelper.IsUnknown(spatialReference))
                {
                    geometryDefEdit.SpatialReference_2 = spatialReference;
                }

                if (customFields != null)
                {
                    for (int i = 0; i < customFields.Count; i++)
                    {
                        FieldMapping mapping = customFields[i];
                        if (fields.FindField(mapping.TargetName) < 0)
                        {
                            fieldsEdit.AddField(CreateField(mapping.TargetName, mapping.FieldType, mapping.Length));
                        }
                    }
                }

                IFeatureClass featureClass = featureWorkspace.CreateFeatureClass(
                    shapefileName,
                    fields,
                    null,
                    null,
                    esriFeatureType.esriFTSimple,
                    featureClassDescription.ShapeFieldName,
                    string.Empty);

                SpatialReferenceHelper.WritePrjFile(fullPath, spatialReference);
                return featureClass;
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
        /// 创建字段，兼容 Shapefile 字段名长度限制。
        /// </summary>
        public static IField CreateField(string name, esriFieldType fieldType, int length)
        {
            IField field = new FieldClass();
            IFieldEdit fieldEdit = (IFieldEdit)field;
            fieldEdit.Name_2 = MakeSafeFieldName(name);
            fieldEdit.AliasName_2 = name;
            fieldEdit.Type_2 = fieldType;

            if (fieldType == esriFieldType.esriFieldTypeString)
            {
                fieldEdit.Length_2 = length > 0 ? length : 100;
            }

            return field;
        }

        /// <summary>
        /// Shapefile 字段名最多 10 个字符，且不宜包含空格和特殊字符。
        /// </summary>
        public static string MakeSafeFieldName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "FIELD";
            }

            string safe = name.Trim().Replace(" ", "_").Replace("-", "_");
            if (safe.Length > 10)
            {
                safe = safe.Substring(0, 10);
            }

            return safe.ToUpper();
        }

        /// <summary>
        /// 删除 Shapefile 及其附属文件，避免重复转换时 CreateFeatureClass 报错。
        /// </summary>
        public static void DeleteShapefile(string shapefilePath)
        {
            if (string.IsNullOrEmpty(shapefilePath))
            {
                return;
            }

            string folder = Path.GetDirectoryName(shapefilePath);
            string name = Path.GetFileNameWithoutExtension(shapefilePath);
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(name) || !Directory.Exists(folder))
            {
                return;
            }

            string[] extensions = new string[]
            {
                ".shp", ".shx", ".dbf", ".prj", ".sbn", ".sbx", ".cpg", ".xml", ".qix"
            };

            for (int i = 0; i < extensions.Length; i++)
            {
                string path = Path.Combine(folder, name + extensions[i]);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        /// <summary>
        /// 获取地图中的所有要素图层。
        /// </summary>
        public static List<IFeatureLayer> GetFeatureLayers(IMap map)
        {
            List<IFeatureLayer> layers = new List<IFeatureLayer>();
            if (map == null)
            {
                return layers;
            }

            for (int i = 0; i < map.LayerCount; i++)
            {
                CollectFeatureLayers(map.get_Layer(i), layers);
            }

            return layers;
        }

        private static void CollectFeatureLayers(ILayer layer, IList<IFeatureLayer> result)
        {
            if (layer == null)
            {
                return;
            }

            IFeatureLayer featureLayer = layer as IFeatureLayer;
            if (featureLayer != null && featureLayer.FeatureClass != null)
            {
                result.Add(featureLayer);
                return;
            }

            ICompositeLayer compositeLayer = layer as ICompositeLayer;
            if (compositeLayer != null)
            {
                for (int i = 0; i < compositeLayer.Count; i++)
                {
                    CollectFeatureLayers(compositeLayer.get_Layer(i), result);
                }
            }
        }

        /// <summary>
        /// 将要素类属性表转换为 DataTable，用于 DataGridView 显示。
        /// </summary>
        public static DataTable BuildAttributeTable(IFeatureClass featureClass, int maxRows)
        {
            DataTable table = new DataTable();
            if (featureClass == null)
            {
                return table;
            }

            IFields fields = featureClass.Fields;
            for (int i = 0; i < fields.FieldCount; i++)
            {
                IField field = fields.get_Field(i);
                if (field.Type != esriFieldType.esriFieldTypeGeometry)
                {
                    table.Columns.Add(field.Name);
                }
            }

            IFeatureCursor cursor = null;
            try
            {
                cursor = featureClass.Search(null, true);
                IFeature feature;
                int count = 0;
                while ((feature = cursor.NextFeature()) != null)
                {
                    DataRow row = table.NewRow();
                    for (int i = 0; i < fields.FieldCount; i++)
                    {
                        IField field = fields.get_Field(i);
                        if (field.Type != esriFieldType.esriFieldTypeGeometry)
                        {
                            object value = feature.get_Value(i);
                            row[field.Name] = value == null ? DBNull.Value : value;
                        }
                    }

                    table.Rows.Add(row);
                    count++;
                    if (maxRows > 0 && count >= maxRows)
                    {
                        break;
                    }
                }
            }
            finally
            {
                if (cursor != null)
                {
                    Marshal.ReleaseComObject(cursor);
                }
            }

            return table;
        }
    }
}
