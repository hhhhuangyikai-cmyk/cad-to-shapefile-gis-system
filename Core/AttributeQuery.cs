using System;
using System.Data;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.Core
{
    /// <summary>
    /// 属性查询与点击识别模块。
    /// </summary>
    public class AttributeQuery
    {
        /// <summary>
        /// 根据 WhereClause 进行属性查询，并将结果在地图中高亮。
        /// 示例：LAYER = '道路'。
        /// </summary>
        public DataTable QueryByAttribute(AxMapControl mapControl, IFeatureLayer featureLayer, string whereClause)
        {
            if (featureLayer == null || featureLayer.FeatureClass == null)
            {
                throw new ArgumentException("请选择有效的要素图层。");
            }

            IQueryFilter queryFilter = new QueryFilterClass();
            queryFilter.WhereClause = whereClause;

            IFeatureSelection selection = featureLayer as IFeatureSelection;
            if (selection != null)
            {
                selection.SelectFeatures(queryFilter, esriSelectionResultEnum.esriSelectionResultNew, false);
            }

            if (mapControl != null)
            {
                mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
            }

            LogHelper.Info("属性查询", featureLayer.Name + " 查询条件：" + whereClause);
            return BuildTableFromQuery(featureLayer.FeatureClass, queryFilter, 1000);
        }

        /// <summary>
        /// 地图点击识别：在点击位置附近建立一个容差范围并做空间查询。
        /// </summary>
        public DataTable IdentifyAt(AxMapControl mapControl, double mapX, double mapY)
        {
            if (mapControl == null || mapControl.Map == null)
            {
                return new DataTable();
            }

            double tolerance = GetMapTolerance(mapControl, 6);
            IPoint point = new PointClass();
            point.PutCoords(mapX, mapY);

            IEnvelope envelope = new EnvelopeClass();
            envelope.PutCoords(mapX - tolerance, mapY - tolerance, mapX + tolerance, mapY + tolerance);
            envelope.SpatialReference = mapControl.Map.SpatialReference;

            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                IFeatureLayer featureLayer = mapControl.get_Layer(i) as IFeatureLayer;
                if (featureLayer == null || featureLayer.FeatureClass == null || !featureLayer.Visible)
                {
                    continue;
                }

                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = envelope;
                spatialFilter.GeometryField = featureLayer.FeatureClass.ShapeFieldName;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                DataTable table = BuildTableFromQuery(featureLayer.FeatureClass, spatialFilter, 20);
                if (table.Rows.Count > 0)
                {
                    IFeatureSelection selection = featureLayer as IFeatureSelection;
                    if (selection != null)
                    {
                        selection.SelectFeatures(spatialFilter, esriSelectionResultEnum.esriSelectionResultNew, false);
                        mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                    }

                    LogHelper.Info("属性查询", "点击识别图层：" + featureLayer.Name + "，结果数：" + table.Rows.Count);
                    return table;
                }
            }

            return new DataTable();
        }

        private double GetMapTolerance(AxMapControl mapControl, int pixels)
        {
            if (mapControl.Extent == null || mapControl.Width <= 0)
            {
                return 0.0;
            }

            return mapControl.Extent.Width / mapControl.Width * pixels;
        }

        private DataTable BuildTableFromQuery(IFeatureClass featureClass, IQueryFilter queryFilter, int maxRows)
        {
            DataTable table = new DataTable();
            IFeatureCursor cursor = null;

            try
            {
                IFields fields = featureClass.Fields;
                for (int i = 0; i < fields.FieldCount; i++)
                {
                    IField field = fields.get_Field(i);
                    if (field.Type != esriFieldType.esriFieldTypeGeometry)
                    {
                        table.Columns.Add(field.Name);
                    }
                }

                cursor = featureClass.Search(queryFilter, true);
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
