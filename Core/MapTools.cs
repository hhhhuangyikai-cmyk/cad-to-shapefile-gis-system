using System;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.Core
{
    /// <summary>
    /// 地图鼠标操作类型。
    /// </summary>
    public enum MapMouseAction
    {
        None,
        ZoomIn,
        ZoomOut,
        Pan,
        Identify
    }

    /// <summary>
    /// 地图基本操作工具类。
    /// </summary>
    public static class MapTools
    {
        public static void FullExtent(AxMapControl mapControl)
        {
            if (mapControl == null || mapControl.LayerCount == 0)
            {
                return;
            }

            mapControl.Extent = mapControl.FullExtent;
            mapControl.ActiveView.Refresh();
        }

        public static void ClearLayers(AxMapControl mapControl)
        {
            if (mapControl == null)
            {
                return;
            }

            mapControl.ClearLayers();
            mapControl.ActiveView.Refresh();
            LogHelper.Info("地图操作", "已清空地图图层。");
        }

        public static void ZoomIn(AxMapControl mapControl)
        {
            if (mapControl == null || mapControl.Extent == null)
            {
                return;
            }

            IEnvelope envelope = mapControl.Extent;
            envelope.Expand(0.5, 0.5, true);
            mapControl.Extent = envelope;
            mapControl.ActiveView.Refresh();
        }

        public static void ZoomOut(AxMapControl mapControl)
        {
            if (mapControl == null || mapControl.Extent == null)
            {
                return;
            }

            IEnvelope envelope = mapControl.Extent;
            envelope.Expand(2.0, 2.0, true);
            mapControl.Extent = envelope;
            mapControl.ActiveView.Refresh();
        }

        public static void ZoomToLayer(AxMapControl mapControl, ILayer layer)
        {
            if (mapControl == null || layer == null)
            {
                return;
            }

            IEnvelope envelope = layer.AreaOfInterest;
            if (envelope != null && !envelope.IsEmpty)
            {
                envelope.Expand(1.1, 1.1, true);
                mapControl.Extent = envelope;
                mapControl.ActiveView.Refresh();
            }
        }

        public static void RemoveLayer(AxMapControl mapControl, ILayer layer)
        {
            if (mapControl == null || layer == null)
            {
                return;
            }

            mapControl.Map.DeleteLayer(layer);
            mapControl.ActiveView.Refresh();
            LogHelper.Info("地图操作", "移除图层：" + layer.Name);
        }

        public static void SetLayerVisible(AxMapControl mapControl, ILayer layer, bool visible)
        {
            if (mapControl == null || layer == null)
            {
                return;
            }

            layer.Visible = visible;
            mapControl.ActiveView.Refresh();
        }

        public static ILayer GetLayerByName(AxMapControl mapControl, string layerName)
        {
            if (mapControl == null || string.IsNullOrEmpty(layerName))
            {
                return null;
            }

            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ILayer layer = mapControl.get_Layer(i);
                if (layer != null && layer.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase))
                {
                    return layer;
                }
            }

            return null;
        }
    }
}
