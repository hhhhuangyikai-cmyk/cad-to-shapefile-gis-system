using System;
using System.Windows.Forms;
using CadToShpGISSystem.Core;
using ESRI.ArcGIS;

namespace CadToShpGISSystem
{
    /// <summary>
    /// 程序入口。ArcObjects 是 COM 组件，WinForms 入口必须使用 STAThread。
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LicenseInitializer initializer = new LicenseInitializer();
            try
            {
                // 优先绑定 Engine；如果只安装 Desktop，也允许绑定到 Desktop。
                RuntimeManager.Bind(ProductCode.EngineOrDesktop);

                string message;
                if (!initializer.InitializeApplication(out message))
                {
                    MessageBox.Show(
                        message,
                        "ArcGIS 授权初始化失败",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                LogHelper.Info("Program", "系统启动，ArcGIS License 初始化成功：" + initializer.CurrentLicenseName);
                Application.Run(new MainForm(initializer));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "程序启动失败：" + ex.Message,
                    "系统错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                LogHelper.Error("Program", "程序启动失败", ex);
            }
            finally
            {
                initializer.ShutdownApplication();
                LogHelper.Info("Program", "系统退出，ArcGIS License 已释放。");
            }
        }
    }
}
