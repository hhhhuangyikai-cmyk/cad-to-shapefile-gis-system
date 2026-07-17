using System;
using ESRI.ArcGIS.esriSystem;

namespace CadToShpGISSystem.Core
{
    /// <summary>
    /// ArcGIS Engine / Desktop 授权初始化封装。
    /// 课程设计中建议单独封装，便于体现 ArcObjects 软件工程结构。
    /// </summary>
    public class LicenseInitializer
    {
        private IAoInitialize _aoInitialize;
        private esriLicenseProductCode? _initializedProduct;

        /// <summary>
        /// 当前成功初始化的许可名称，用于状态栏和日志显示。
        /// </summary>
        public string CurrentLicenseName
        {
            get
            {
                if (!_initializedProduct.HasValue)
                {
                    return "未初始化";
                }

                switch (_initializedProduct.Value)
                {
                    case esriLicenseProductCode.esriLicenseProductCodeEngine:
                        return "ArcGIS Engine";
                    case esriLicenseProductCode.esriLicenseProductCodeEngineGeoDB:
                        return "ArcGIS Engine GeoDB";
                    case esriLicenseProductCode.esriLicenseProductCodeBasic:
                        return "ArcGIS Desktop Basic";
                    case esriLicenseProductCode.esriLicenseProductCodeStandard:
                        return "ArcGIS Desktop Standard";
                    case esriLicenseProductCode.esriLicenseProductCodeAdvanced:
                        return "ArcGIS Desktop Advanced";
                    default:
                        return _initializedProduct.Value.ToString();
                }
            }
        }

        /// <summary>
        /// 按照从低到高、从常见到完整的顺序尝试许可。
        /// 如果学校机房只安装 Desktop，没有单独 Engine Runtime，也能正常运行。
        /// </summary>
        public bool InitializeApplication(out string message)
        {
            message = string.Empty;

            try
            {
                _aoInitialize = new AoInitializeClass();

                esriLicenseProductCode[] products = new esriLicenseProductCode[]
                {
                    esriLicenseProductCode.esriLicenseProductCodeEngine,
                    esriLicenseProductCode.esriLicenseProductCodeEngineGeoDB,
                    esriLicenseProductCode.esriLicenseProductCodeBasic,
                    esriLicenseProductCode.esriLicenseProductCodeStandard,
                    esriLicenseProductCode.esriLicenseProductCodeAdvanced
                };

                for (int i = 0; i < products.Length; i++)
                {
                    esriLicenseProductCode product = products[i];
                    esriLicenseStatus status = _aoInitialize.IsProductCodeAvailable(product);
                    if (status == esriLicenseStatus.esriLicenseAvailable ||
                        status == esriLicenseStatus.esriLicenseAlreadyInitialized)
                    {
                        esriLicenseStatus initStatus = _aoInitialize.Initialize(product);
                        if (initStatus == esriLicenseStatus.esriLicenseCheckedOut ||
                            initStatus == esriLicenseStatus.esriLicenseAlreadyInitialized)
                        {
                            _initializedProduct = product;
                            message = "ArcGIS 授权初始化成功：" + CurrentLicenseName;
                            return true;
                        }
                    }
                }

                message = "没有可用的 ArcGIS Engine/Desktop 许可。请确认 ArcGIS Desktop/Engine 10.8 已授权，" +
                          "并且项目平台目标设置为 x86。";
                return false;
            }
            catch (Exception ex)
            {
                message = "ArcGIS 授权初始化异常：" + ex.Message;
                LogHelper.Error("License", "ArcGIS 授权初始化异常", ex);
                return false;
            }
        }

        /// <summary>
        /// 程序退出时释放许可。
        /// </summary>
        public void ShutdownApplication()
        {
            try
            {
                if (_aoInitialize != null)
                {
                    _aoInitialize.Shutdown();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("License", "释放 ArcGIS 授权时发生异常", ex);
            }
            finally
            {
                _aoInitialize = null;
                _initializedProduct = null;
            }
        }
    }
}
