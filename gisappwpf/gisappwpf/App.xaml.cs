using System;
using System.Windows;
using ESRI.ArcGIS;           // 引入版本验证库
using ESRI.ArcGIS.esriSystem; // 引入许可初始化库

namespace gisappwpf
{
    public partial class App : Application
    {
        private AoInitialize m_AoInitialize;

        // 重写 OnStartup 方法，让程序一启动就先验证许可
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 1. 绑定 ArcGIS 运行环境 (最关键的一步)
                RuntimeManager.Bind(ProductCode.EngineOrDesktop);

                // 2. 初始化底层组件许可
                m_AoInitialize = new AoInitialize();

                // 尝试检出 ArcGIS Engine 或 Desktop 的最高权限许可
                esriLicenseStatus status = m_AoInitialize.Initialize(esriLicenseProductCode.esriLicenseProductCodeAdvanced);

                if (status != esriLicenseStatus.esriLicenseCheckedOut && status != esriLicenseStatus.esriLicenseAlreadyInitialized)
                {
                    MessageBox.Show("ArcGIS 许可初始化失败，请检查你的 License Manager 是否启动！");
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("许可绑定发生异常：" + ex.Message);
            }
        }
    }
}