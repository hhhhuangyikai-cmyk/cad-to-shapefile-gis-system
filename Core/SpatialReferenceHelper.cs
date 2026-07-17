using System;
using System.IO;
using Path = System.IO.Path;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.Core
{
    /// <summary>
    /// 空间参考辅助类。
    /// CAD 数据经常没有坐标系，课程设计中需要明确提示并允许手动指定。
    /// </summary>
    public static class SpatialReferenceHelper
    {
        /// <summary>
        /// 创建 WGS84 地理坐标系，EPSG:4326。
        /// </summary>
        public static ISpatialReference CreateWgs84()
        {
            ISpatialReferenceFactory2 factory = new SpatialReferenceEnvironmentClass();
            return factory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
        }

        /// <summary>
        /// 创建 CGCS2000 地理坐标系，EPSG:4490。
        /// 如果工程数据需要米制缓冲距离，建议使用 CGCS2000 高斯克吕格投影坐标系。
        /// </summary>
        public static ISpatialReference CreateCgcs2000()
        {
            ISpatialReferenceFactory2 factory = new SpatialReferenceEnvironmentClass();
            return factory.CreateGeographicCoordinateSystem(4490);
        }

        /// <summary>
        /// 示例：创建 CGCS2000 / 3-degree Gauss-Kruger zone 39，EPSG:4547。
        /// 实际项目中应根据所在经度带选择对应 EPSG 编号。
        /// </summary>
        public static ISpatialReference CreateCgcs2000GaussKrugerZone39()
        {
            ISpatialReferenceFactory2 factory = new SpatialReferenceEnvironmentClass();
            return factory.CreateProjectedCoordinateSystem(4547);
        }

        /// <summary>
        /// 创建 WGS 1984 Web Mercator Auxiliary Sphere，EPSG:3857。
        /// 用于经纬度数据的临时米制缓冲分析，输出时仍可投回原坐标系。
        /// </summary>
        public static ISpatialReference CreateWebMercator()
        {
            ISpatialReferenceFactory2 factory = new SpatialReferenceEnvironmentClass();
            return factory.CreateProjectedCoordinateSystem(3857);
        }

        /// <summary>
        /// 从要素类读取空间参考。
        /// </summary>
        public static ISpatialReference GetSpatialReference(IFeatureClass featureClass)
        {
            if (featureClass == null)
            {
                return null;
            }

            IGeoDataset geoDataset = featureClass as IGeoDataset;
            if (geoDataset == null)
            {
                return null;
            }

            return geoDataset.SpatialReference;
        }

        /// <summary>
        /// 判断空间参考是否未知。
        /// </summary>
        public static bool IsUnknown(ISpatialReference spatialReference)
        {
            if (spatialReference == null)
            {
                return true;
            }

            try
            {
                return string.IsNullOrEmpty(spatialReference.Name) ||
                       spatialReference.Name.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// 判断是否为投影坐标系。缓冲距离通常应在投影坐标系下使用米、英尺等线性单位。
        /// </summary>
        public static bool IsProjected(ISpatialReference spatialReference)
        {
            return spatialReference is IProjectedCoordinateSystem;
        }

        /// <summary>
        /// 克隆并投影几何对象。输入为空或目标空间参考未知时直接返回原几何克隆。
        /// </summary>
        public static IGeometry ProjectGeometry(IGeometry geometry, ISpatialReference targetSpatialReference)
        {
            return ProjectGeometry(geometry, null, targetSpatialReference);
        }

        /// <summary>
        /// Clone and project a geometry with an explicit source spatial reference.
        /// This is important for CAD data because many DWG/DXF files are reported as Unknown.
        /// If source is unknown and only target is provided, the method can only define the target reference,
        /// not do a real coordinate transformation.
        /// </summary>
        public static IGeometry ProjectGeometry(
            IGeometry geometry,
            ISpatialReference sourceSpatialReference,
            ISpatialReference targetSpatialReference)
        {
            if (geometry == null)
            {
                return null;
            }

            IClone clone = geometry as IClone;
            IGeometry result = clone != null ? (IGeometry)clone.Clone() : geometry;

            if (IsUnknown(result.SpatialReference) && !IsUnknown(sourceSpatialReference))
            {
                result.SpatialReference = sourceSpatialReference;
            }

            if (!IsUnknown(targetSpatialReference))
            {
                if (IsUnknown(result.SpatialReference))
                {
                    result.SpatialReference = targetSpatialReference;
                }
                else if (result.SpatialReference.FactoryCode != targetSpatialReference.FactoryCode)
                {
                    result.Project(targetSpatialReference);
                }
            }

            return result;
        }

        /// <summary>
        /// 准备写入二维 Shapefile 的几何。
        /// CAD 数据经常带有 Z 值或 M 值，而本系统创建的是普通二维 Shapefile。
        /// 如果不先移除 Z/M，插入要素时会出现“Geometry cannot have Z values.”等错误。
        /// </summary>
        public static IGeometry PrepareGeometryFor2DShapefile(IGeometry geometry, ISpatialReference targetSpatialReference)
        {
            IGeometry result = ProjectGeometry(geometry, null, targetSpatialReference);
            DropZAndM(result);
            return result;
        }

        /// <summary>
        /// Prepare a geometry for a 2D Shapefile and optionally project it from source to target.
        /// </summary>
        public static IGeometry PrepareGeometryFor2DShapefile(
            IGeometry geometry,
            ISpatialReference sourceSpatialReference,
            ISpatialReference targetSpatialReference)
        {
            IGeometry result = ProjectGeometry(geometry, sourceSpatialReference, targetSpatialReference);
            DropZAndM(result);
            return result;
        }

        /// <summary>
        /// 移除几何对象的 Z 值和 M 值，使其可以写入普通二维 Shapefile。
        /// </summary>
        public static void DropZAndM(IGeometry geometry)
        {
            if (geometry == null)
            {
                return;
            }

            try
            {
                IZAware zAware = geometry as IZAware;
                if (zAware != null && zAware.ZAware)
                {
                    zAware.DropZs();
                    zAware.ZAware = false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn("SpatialReference", "移除几何 Z 值失败：" + ex.Message);
            }

            try
            {
                IMAware mAware = geometry as IMAware;
                if (mAware != null && mAware.MAware)
                {
                    mAware.DropMs();
                    mAware.MAware = false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn("SpatialReference", "移除几何 M 值失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 生成 .prj 文件。创建 Shapefile 时如果 GeometryDef 已设置空间参考，ArcGIS 一般会自动写入；
        /// 此方法作为补充，方便答辩说明坐标系文件的作用。
        /// </summary>
        public static void WritePrjFile(string shapefilePath, ISpatialReference spatialReference)
        {
            if (string.IsNullOrEmpty(shapefilePath) || IsUnknown(spatialReference))
            {
                return;
            }

            try
            {
                string prjText;
                int prjSize;
                IPRJSpatialReferenceGEN prjSpatialReference = (IPRJSpatialReferenceGEN)spatialReference;
                prjSpatialReference.ExportSpatialReferenceToPRJ(out prjText, out prjSize);

                string prjPath = Path.ChangeExtension(shapefilePath, ".prj");
                File.WriteAllText(prjPath, prjText);
            }
            catch (Exception ex)
            {
                LogHelper.Warn("SpatialReference", "写入 PRJ 文件失败：" + ex.Message);
            }
        }
    }
}
