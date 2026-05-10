using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

namespace gisappwpf.Helpers
{
    /// <summary>
    /// GIS 数据库访问辅助工具类
    /// 专门负责与 PostgreSQL/PostGIS 数据库建立连接
    /// </summary>
    public static class GisDbHelper
    {
        /// <summary>
        /// 获取 PostgreSQL 空间数据库的要素工作空间
        /// </summary>
        /// <returns>返回可直接操作图层要素的工作空间接口</returns>
        public static IFeatureWorkspace GetFeatureWorkspace()
        {
            try
            {
                // 1. 创建 SDE 工作空间工厂 (SDE 是 ArcGIS 连接关系型数据库的标准引擎)
                Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.SdeWorkspaceFactory");
                IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);

                // 2. 配置连接参数 
                // 这里的参数必须根据你本地 PostgreSQL 的实际环境进行匹配
                IPropertySet propertySet = new PropertySet();
                propertySet.SetProperty("DBCLIENT", "postgresql");
                propertySet.SetProperty("SERVER", "localhost");
                propertySet.SetProperty("INSTANCE", "sde:postgresql:localhost");
                propertySet.SetProperty("DATABASE", "wuhangis");
                propertySet.SetProperty("USER", "postgres");
                propertySet.SetProperty("PASSWORD", "123456"); 
                propertySet.SetProperty("VERSION", "sde.DEFAULT");

                // 3. 打开工作空间
                IWorkspace workspace = workspaceFactory.Open(propertySet, 0);

                // 4. 将通用工作空间强制转换为要素工作空间 (FeatureWorkspace)
                return (IFeatureWorkspace)workspace;
            }
            catch (Exception ex)
            {
                // 抛出异常，让调用者(MainWindow)去显示具体的错误信息
                throw new Exception("底层数据库连接失败，请检查 PostgreSQL 服务是否启动以及连接参数是否正确。\n详细原因：" + ex.Message);
            }
        }
    }
}