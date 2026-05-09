using System; // 必须要有这个
using System.Windows;
using System.Windows.Forms.Integration;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase; // 引入地理数据库命名空间
using ESRI.ArcGIS.esriSystem;  // 引入系统属性集命名空间

namespace gisappwpf
{
    public partial class MainWindow : Window
    {
        private AxMapControl axMapControl;
        private AxTOCControl axTOCControl;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. 初始化容器并绑定
            axMapControl = new AxMapControl();
            axTOCControl = new AxTOCControl();
            mapHost.Child = axMapControl;
            tocHost.Child = axTOCControl;
            axTOCControl.SetBuddyControl(axMapControl);

            try
            {
                // 2. 创建 SDE 工作空间工厂
                Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.SdeWorkspaceFactory");
                IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);

                // 3. 配置 PostgreSQL 连接参数
                // 注意这里：去掉了 PropertySetClass 的 "Class" 后缀
                IPropertySet propertySet = new PropertySet();
                propertySet.SetProperty("DBCLIENT", "postgresql");
                propertySet.SetProperty("SERVER", "localhost");
                propertySet.SetProperty("INSTANCE", "sde:postgresql:localhost");
                propertySet.SetProperty("DATABASE", "wuhangis");
                propertySet.SetProperty("USER", "postgres");
                propertySet.SetProperty("PASSWORD", "123456"); 
                propertySet.SetProperty("VERSION", "sde.DEFAULT");

                // 4. 打开数据库工作空间
                IWorkspace workspace = workspaceFactory.Open(propertySet, 0);
                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;

                // 5. 加载图层
                // 5. 批量加载图层 (注意：AddLayer 默认是加在最顶层，所以我们要先加底图，最后加建筑)

                // (1) 加载学校范围 (底图最底层)
                IFeatureClass boundaryFC = featureWorkspace.OpenFeatureClass("public.boundary");
                IFeatureLayer boundaryLayer = new FeatureLayer();
                boundaryLayer.FeatureClass = boundaryFC;
                boundaryLayer.Name = "学校范围";
                axMapControl.AddLayer(boundaryLayer);

                // (2) 加载水系
                IFeatureClass waterFC = featureWorkspace.OpenFeatureClass("public.water");
                IFeatureLayer waterLayer = new FeatureLayer();
                waterLayer.FeatureClass = waterFC;
                waterLayer.Name = "水系";
                axMapControl.AddLayer(waterLayer);

                // (3) 加载绿地
                IFeatureClass greenFC = featureWorkspace.OpenFeatureClass("public.green");
                IFeatureLayer greenLayer = new FeatureLayer();
                greenLayer.FeatureClass = greenFC;
                greenLayer.Name = "绿地";
                axMapControl.AddLayer(greenLayer);

                // (4) 加载道路
                IFeatureClass roadFC = featureWorkspace.OpenFeatureClass("public.road");
                IFeatureLayer roadLayer = new FeatureLayer();
                roadLayer.FeatureClass = roadFC;
                roadLayer.Name = "道路路网";
                axMapControl.AddLayer(roadLayer);

                // (5) 加载建筑 (放在最顶层，避免被绿地遮挡)
                IFeatureClass buildingFC = featureWorkspace.OpenFeatureClass("public.building");
                IFeatureLayer buildingLayer = new FeatureLayer();
                buildingLayer.FeatureClass = buildingFC;
                buildingLayer.Name = "校园建筑";
                axMapControl.AddLayer(buildingLayer);

                //（6） 加载操场
                IFeatureClass playgroundFC = featureWorkspace.OpenFeatureClass("public.playground");
                IFeatureLayer playgroundLayer = new FeatureLayer();
                playgroundLayer.FeatureClass = playgroundFC;
                playgroundLayer.Name = "校园操场";
                axMapControl.AddLayer(playgroundLayer);

                // 6. 将图层添加到地图并刷新
                axMapControl.AddLayer(buildingLayer);
                axMapControl.Extent = axMapControl.FullExtent;
                axMapControl.ActiveView.Refresh();
                axMapControl.Refresh();

                MessageBox.Show("太棒了！成功从 PostgreSQL 数据库读取空间数据！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接或加载失败：\n" + ex.Message);
            }
        }
    }
}