using System; // 必须要有这个
using System.Windows;
using System.Windows.Forms.Integration;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase; // 引入地理数据库命名空间
using ESRI.ArcGIS.esriSystem;  // 引入系统属性集命名空间
using ESRI.ArcGIS.Display;   // 用于定义颜色和符号
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geometry; 

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
                IFeatureLayer boundaryLayer = new FeatureLayer { FeatureClass = boundaryFC, Name = "学校范围" };
                SetPolygonSymbol(boundaryLayer, 245, 245, 245); // 浅灰色底
                axMapControl.AddLayer(boundaryLayer);

                // (2) 加载水系
                IFeatureClass waterFC = featureWorkspace.OpenFeatureClass("public.water");
                IFeatureLayer waterLayer = new FeatureLayer { FeatureClass = waterFC, Name = "水系" };
                SetPolygonSymbol(waterLayer, 151, 219, 242, 100, 150, 180); // 浅蓝色填充，深蓝色边框
                axMapControl.AddLayer(waterLayer);

                // (3) 加载绿地
                IFeatureClass greenFC = featureWorkspace.OpenFeatureClass("public.green");
                IFeatureLayer greenLayer = new FeatureLayer { FeatureClass = greenFC, Name = "绿地" };
                SetPolygonSymbol(greenLayer, 195, 230, 175); // 清新的绿色
                axMapControl.AddLayer(greenLayer);

                // (4) 加载道路
                IFeatureClass roadFC = featureWorkspace.OpenFeatureClass("public.road");
                IFeatureLayer roadLayer = new FeatureLayer { FeatureClass = roadFC, Name = "道路路网" };
                SetLineSymbol(roadLayer, 200, 200, 200, 1.5); // 灰色线，宽度1.5
                axMapControl.AddLayer(roadLayer);

                // (5) 加载建筑 (放在最顶层)
                IFeatureClass buildingFC = featureWorkspace.OpenFeatureClass("public.building");
                IFeatureLayer buildingLayer = new FeatureLayer { FeatureClass = buildingFC, Name = "校园建筑" };
                SetPolygonSymbol(buildingLayer, 253, 218, 185, 180, 150, 120); // 暖色/橘黄色建筑，武大特色
                axMapControl.AddLayer(buildingLayer);

                // (6) 加载操场
                IFeatureClass playgroundFC = featureWorkspace.OpenFeatureClass("public.playground");
                IFeatureLayer playgroundLayer = new FeatureLayer { FeatureClass = playgroundFC, Name = "校园操场" };
                SetPolygonSymbol(playgroundLayer, 235, 165, 150); // 塑胶跑道红砖色
                axMapControl.AddLayer(playgroundLayer);

                // 6. 将图层添加到地图并刷新
                axMapControl.AddLayer(buildingLayer);
                axMapControl.Extent = axMapControl.FullExtent;
                axMapControl.ActiveView.Refresh();
                axMapControl.Refresh();

                // 绑定鼠标移动事件（用于显示坐标）
                axMapControl.OnMouseMove += AxMapControl_OnMouseMove;


                MessageBox.Show("成功从 PostgreSQL 数据库读取空间数据！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接或加载失败：\n" + ex.Message);
            }
        }

        // 辅助方法 1：为面要素图层设置填充颜色和边框颜色
        private void SetPolygonSymbol(IFeatureLayer layer, int r, int g, int b, int outlineR = 150, int outlineG = 150, int outlineB = 150)
        {
            // 1. 定义填充颜色 (注意：去掉了Class后缀，并且逐行赋值)
            IRgbColor fillColor = new RgbColor();
            fillColor.Red = r;
            fillColor.Green = g;
            fillColor.Blue = b;

            // 2. 定义边框颜色
            IRgbColor outlineColor = new RgbColor();
            outlineColor.Red = outlineR;
            outlineColor.Green = outlineG;
            outlineColor.Blue = outlineB;

            // 3. 定义边框符号 (去掉了Class后缀)
            ISimpleLineSymbol outlineSymbol = new SimpleLineSymbol();
            outlineSymbol.Color = outlineColor;
            outlineSymbol.Width = 0.5; // 边框粗细

            // 4. 定义面填充符号
            ISimpleFillSymbol fillSymbol = new SimpleFillSymbol();
            fillSymbol.Color = fillColor;
            fillSymbol.Outline = outlineSymbol;

            // 5. 创建渲染器并应用到图层
            ISimpleRenderer renderer = new SimpleRenderer();
            renderer.Symbol = (ISymbol)fillSymbol;

            IGeoFeatureLayer geoLayer = layer as IGeoFeatureLayer;
            if (geoLayer != null)
            {
                geoLayer.Renderer = (IFeatureRenderer)renderer;
            }
        }

        // 辅助方法 2：为线要素图层设置颜色和宽度
        private void SetLineSymbol(IFeatureLayer layer, int r, int g, int b, double width)
        {
            // 实例化颜色
            IRgbColor lineColor = new RgbColor();
            lineColor.Red = r;
            lineColor.Green = g;
            lineColor.Blue = b;

            // 实例化线符号
            ISimpleLineSymbol lineSymbol = new SimpleLineSymbol();
            lineSymbol.Color = lineColor;
            lineSymbol.Width = width;

            // 实例化渲染器
            ISimpleRenderer renderer = new SimpleRenderer();
            renderer.Symbol = (ISymbol)lineSymbol;

            IGeoFeatureLayer geoLayer = layer as IGeoFeatureLayer;
            if (geoLayer != null)
            {
                geoLayer.Renderer = (IFeatureRenderer)renderer;
            }
        }

        // 1. 鼠标拖拽平移工具
        private void BtnPan_Click(object sender, RoutedEventArgs e)
        {
            // 注意：这里吸取了刚才颜色的教训，我们直接 new 去掉 Class 后缀的类型
            ICommand command = new ControlsMapPanTool();
            command.OnCreate(axMapControl.Object);
            axMapControl.CurrentTool = (ITool)command; // 赋予鼠标平移状态
        }

        // 2. 鼠标拉框放大工具
        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ICommand command = new ControlsMapZoomInTool();
            command.OnCreate(axMapControl.Object);
            axMapControl.CurrentTool = (ITool)command; // 赋予鼠标拉框放大状态
        }

        // 3. 鼠标拉框缩小工具
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ICommand command = new ControlsMapZoomOutTool();
            command.OnCreate(axMapControl.Object);
            axMapControl.CurrentTool = (ITool)command; // 赋予鼠标拉框缩小状态
        }

        // 4. 一键恢复全局视图
        private void BtnFullExtent_Click(object sender, RoutedEventArgs e)
        {
            // 全图是一个一次性执行的 Command，而不是一直保持的 Tool，所以直接 OnClick 执行
            ICommand command = new ControlsMapFullExtentCommand();
            command.OnCreate(axMapControl.Object);
            command.OnClick();
        }

        // 实现鼠标移动时实时显示坐标
private void AxMapControl_OnMouseMove(object sender, ESRI.ArcGIS.Controls.IMapControlEvents2_OnMouseMoveEvent e)
{
    // e.mapX 和 e.mapY 就是鼠标当前在地图上的实际坐标
    // Math.Round(..., 2) 用于保留两位小数
    double x = Math.Round(e.mapX, 2);
    double y = Math.Round(e.mapY, 2);
    
    // 获取当前地图比例尺
    double scale = Math.Round(axMapControl.MapScale, 0);

    // 更新底部状态栏
    txtStatusBar.Text = string.Format("坐标系: CGCS2000 / Gauss-Kruger 3度带    X: {0}   Y: {1}    比例尺: 1:{2}", x, y, scale);
}

private void BtnSearch_Click(object sender, RoutedEventArgs e)
{
    // 1. 获取输入框内容并进行空值检查
    string searchText = txtSearchName.Text.Trim();
    if (string.IsNullOrEmpty(searchText))
    {
        MessageBox.Show("请输入要查询的建筑名称！");
        return;
    }

    try
    {
        // 2. 在地图中查找名为“校园建筑”的图层
        IFeatureLayer buildingLayer = null;
        for (int i = 0; i < axMapControl.LayerCount; i++)
        {
            if (axMapControl.get_Layer(i).Name == "校园建筑")
            {
                buildingLayer = (IFeatureLayer)axMapControl.get_Layer(i);
                break;
            }
        }

        if (buildingLayer == null)
        {
            MessageBox.Show("未找到'校园建筑'图层，请检查数据加载情况。");
            return;
        }

        // 3. 构造属性查询过滤器 (使用 string.Format 确保版本兼容)
        // 注意：PostgreSQL 字段名通常为小写，LIKE '%{0}%' 实现模糊匹配
        IQueryFilter queryFilter = new QueryFilter();
        queryFilter.WhereClause = string.Format("name LIKE '%{0}%'", searchText);

        // 4. 执行图层选择
        IFeatureSelection featureSelection = buildingLayer as IFeatureSelection;
        axMapControl.Map.ClearSelection(); // 清除旧的选择
        featureSelection.SelectFeatures(queryFilter, esriSelectionResultEnum.esriSelectionResultNew, false);

        // 5. 处理查询结果
        ISelectionSet selectionSet = featureSelection.SelectionSet;
        if (selectionSet.Count > 0)
        {
            // 刷新地图以显示高亮的蓝色外观
            axMapControl.ActiveView.Refresh();

            // 获取选中的要素枚举
            IEnumFeature enumFeature = (IEnumFeature)axMapControl.Map.FeatureSelection;
            IFeature feature = enumFeature.Next();

            if (feature != null)
            {
                // --- 【核心步骤：提取并展示属性详情】 ---
                lstAttributes.Items.Clear(); // 清空上次查询的列表

                for (int i = 0; i < feature.Fields.FieldCount; i++)
                {
                    IField field = feature.Fields.get_Field(i);
                    object value = feature.get_Value(i);

                    // 过滤掉系统内部字段（如几何、ID等），只展示对用户有意义的业务字段
                    if (field.Type != esriFieldType.esriFieldTypeGeometry &&
                        field.Type != esriFieldType.esriFieldTypeOID &&
                        !field.Name.ToLower().Contains("shape"))
                    {
                        // 将字段别名和具体值添加到界面的 ListView 中
                        lstAttributes.Items.Add(new
                        {
                            Key = field.AliasName,
                            Value = (value != null ? value.ToString() : "无")
                        });
                    }
                }

                // --- 【核心步骤：自动缩放与闪烁定位】 ---
                IEnvelope fullExtent = feature.Shape.Envelope;

                // 如果搜到多个建筑（比如搜“教学楼”），则合并所有建筑的范围
                IFeature nextFeature;
                while ((nextFeature = enumFeature.Next()) != null)
                {
                    fullExtent.Union(nextFeature.Shape.Envelope);
                }

                // 视野范围稍微扩大 1.5 倍，避免建筑贴着边框，视觉更舒服
                fullExtent.Expand(1.5, 1.5, true);
                axMapControl.Extent = fullExtent;

                // 最后刷新视图并闪烁一下查到的形状
                axMapControl.ActiveView.Refresh();
                axMapControl.FlashShape(fullExtent);
            }
        }
        else
        {
            lstAttributes.Items.Clear();
            MessageBox.Show(string.Format("在数据库中未找到包含 '{0}' 的建筑。", searchText));
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show("属性查询发生异常：\n" + ex.Message);
    }
}

    }
}