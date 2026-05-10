using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Data;
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.Win32; 
using ESRI.ArcGIS.DataSourcesFile;

// 引入我们刚才拆分出去的命名空间
using gisappwpf.Models;
using gisappwpf.Helpers;

// ArcGIS 核心引用
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geometry;

namespace gisappwpf
{
    public partial class MainWindow : Window
    {
        // ==================== 全局变量区 ====================
        private AxMapControl axMapControl;
     

        // 记住当前点击选中的图层，供“图层操作”选项卡使用
        private IFeatureLayer _selectedLayer = null;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            // ===== 解决空域问题：处理 Popup 悬浮控件的跟随与隐藏 =====

            // 窗口拖动或大小改变时，微调 Popup 坐标强制其重绘以贴合地图
            this.LocationChanged += (s, e) => SyncPopupPosition();
            this.SizeChanged += (s, e) => SyncPopupPosition();

            // 切换到其他软件时隐藏工具栏，切回来时显示，避免幽灵悬浮
            this.Deactivated += (s, e) => MapToolbarPopup.IsOpen = false;
            this.Activated += (s, e) => MapToolbarPopup.IsOpen = true;

        }

        private void SyncPopupPosition()
        {
            if (MapToolbarPopup.IsOpen)
            {
                // WPF 黑科技：极小的偏移量不会被肉眼察觉，但能强制 Popup 重新对齐
                MapToolbarPopup.HorizontalOffset += 0.01;
                MapToolbarPopup.HorizontalOffset -= 0.01;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. 初始化容器并绑定
            axMapControl = new AxMapControl();
           
            mapHost.Child = axMapControl;
           
       

            try
            {
                // 2. 【解耦体现】：直接呼叫数据库助手获取连接
                IFeatureWorkspace featureWorkspace = GisDbHelper.GetFeatureWorkspace();

                // 3. 批量加载图层 (利用 GisStyleHelper 进行美化渲染)
                // (1) 学校范围
                IFeatureClass boundaryFC = featureWorkspace.OpenFeatureClass("public.boundary");
                IFeatureLayer boundaryLayer = new FeatureLayer { FeatureClass = boundaryFC, Name = "学校范围" };
                GisStyleHelper.SetPolygonSymbol(boundaryLayer, 245, 245, 245);
                axMapControl.AddLayer(boundaryLayer);
                lstLayers.Items.Insert(0, new LayerItem
                {
                    ArcGisLayer = boundaryLayer,
                    Name = boundaryLayer.Name,
                    IsVisible = true,
                    SymbolColor = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    ShapeType = "Polygon"
                });
                // (2) 水系
                IFeatureClass waterFC = featureWorkspace.OpenFeatureClass("public.water");
                IFeatureLayer waterLayer = new FeatureLayer { FeatureClass = waterFC, Name = "水系" };
                GisStyleHelper.SetPolygonSymbol(waterLayer, 151, 219, 242, 100, 150, 180);
                axMapControl.AddLayer(waterLayer);
                lstLayers.Items.Insert(0, new LayerItem
                {
                    ArcGisLayer = waterLayer,
                    Name = waterLayer.Name,
                    IsVisible = true,
                    SymbolColor = new SolidColorBrush(Color.FromRgb(151, 219, 242)),
                     ShapeType = "Polygon"
                });
                // (3) 绿地
                IFeatureClass greenFC = featureWorkspace.OpenFeatureClass("public.green");
                IFeatureLayer greenLayer = new FeatureLayer { FeatureClass = greenFC, Name = "绿地" };
                GisStyleHelper.SetPolygonSymbol(greenLayer, 195, 230, 175);
                axMapControl.AddLayer(greenLayer);
                lstLayers.Items.Insert(0, new LayerItem
                {
                    ArcGisLayer = greenLayer,
                    Name = greenLayer.Name,
                    IsVisible = true,
                    SymbolColor = new SolidColorBrush(Color.FromRgb(195, 230, 175)),
                     ShapeType = "Polygon"
                });

                // (4) 道路
                IFeatureClass roadFC = featureWorkspace.OpenFeatureClass("public.road");
                IFeatureLayer roadLayer = new FeatureLayer { FeatureClass = roadFC, Name = "道路路网" };
                GisStyleHelper.SetLineSymbol(roadLayer, 200, 200, 200, 1.5);
                axMapControl.AddLayer(roadLayer);
                lstLayers.Items.Insert(0, new LayerItem
                {
                    ArcGisLayer = roadLayer,
                    Name = roadLayer.Name,
                    IsVisible = true,
                    SymbolColor = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    ShapeType = "Polyline"
                });

                // (5) 建筑 (最顶层)
                IFeatureClass buildingFC = featureWorkspace.OpenFeatureClass("public.building");
                IFeatureLayer buildingLayer = new FeatureLayer { FeatureClass = buildingFC, Name = "校园建筑" };
                GisStyleHelper.SetPolygonSymbol(buildingLayer, 253, 218, 185, 180, 150, 120);
                axMapControl.AddLayer(buildingLayer);
                lstLayers.Items.Insert(0, new LayerItem
                {
                    ArcGisLayer = buildingLayer,
                    Name = buildingLayer.Name,
                    IsVisible = true,
                    SymbolColor = new SolidColorBrush(Color.FromRgb(253, 218, 185)),
                     ShapeType = "Polygon"
                });

                // (6) 操场
                IFeatureClass playgroundFC = featureWorkspace.OpenFeatureClass("public.playground");
                IFeatureLayer playgroundLayer = new FeatureLayer { FeatureClass = playgroundFC, Name = "校园操场" };
                GisStyleHelper.SetPolygonSymbol(playgroundLayer, 235, 165, 150);
                axMapControl.AddLayer(playgroundLayer);
                lstLayers.Items.Insert(0, new LayerItem
                {
                    ArcGisLayer = playgroundLayer,
                    Name = playgroundLayer.Name,
                    IsVisible = true,
                    SymbolColor = new SolidColorBrush(Color.FromRgb(235, 165, 150)),
                     ShapeType = "Polygon"
                });

                // 4. 视图刷新与事件绑定
                axMapControl.Extent = axMapControl.FullExtent;
                axMapControl.ActiveView.Refresh();

                axMapControl.OnMouseMove += AxMapControl_OnMouseMove;
                

               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // ==================== 导入外部数据 ====================
        private void BtnAddShapefile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "选择要导入的 Shapefile 文件";
            openFileDialog.Filter = "Shapefile 文件 (*.shp)|*.shp";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fullPath = openFileDialog.FileName;
                    string folderPath = System.IO.Path.GetDirectoryName(fullPath);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(fullPath);

                    // 1. 建立 Shapefile 工作空间工厂
                    IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactory();
                    IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(folderPath, 0);
                    
                    // 2. 打开要素类并创建图层
                    IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(fileName);
                    IFeatureLayer newLayer = new FeatureLayer
                    {
                        FeatureClass = featureClass,
                        Name = fileName // 默认用文件名做图层名
                    };

                    // 3. 智能判断几何类型，并赋予默认渲染色 (这里用低饱和度的莫兰迪色)
                    string uiShapeType = "Polygon";
                    SolidColorBrush uiColor = new SolidColorBrush(Color.FromRgb(200, 180, 180)); 
                    
                    if (featureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        uiShapeType = "Polygon";
                        GisStyleHelper.SetPolygonSymbol(newLayer, 200, 180, 180);
                    }
                    else if (featureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        uiShapeType = "Polyline";
                        uiColor = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                        GisStyleHelper.SetLineSymbol(newLayer, 150, 150, 150, 2);
                    }
                    else if (featureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        uiShapeType = "Point";
                        uiColor = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // 点默认给个橙色
                        GisStyleHelper.SetPointSymbol(newLayer, 255, 165, 0);
                    }

                    // 4. 将图层加载到底层地图容器
                    axMapControl.AddLayer(newLayer);

                    // 5. 同步更新到我们自定义的 WPF 图层列表 (插入到最顶层)
                    lstLayers.Items.Insert(0, new LayerItem
                    {
                        ArcGisLayer = newLayer,
                        Name = newLayer.Name,
                        IsVisible = true,
                        SymbolColor = uiColor,
                        ShapeType = uiShapeType
                    });

                    // 6. 刷新地图并缩放到该图层范围
                    axMapControl.Extent = newLayer.AreaOfInterest;
                    axMapControl.ActiveView.Refresh();

                    
                    MessageBox.Show("图层 [" + fileName + "] 导入成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导入 Shapefile 失败，请确保文件完整且未被其他程序占用。\n详细信息：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // ==================== 顶部工具栏交互 ====================

        // 重置所有工具按钮为默认样式（白底黑字）
        private void ResetToolButtonsStatus()
        {
            var defaultBg = System.Windows.Media.Brushes.White;
            var defaultFg = System.Windows.Media.Brushes.Black;

            BtnPan.Background = defaultBg;
            BtnPan.Foreground = defaultFg;
            BtnZoomIn.Background = defaultBg;
            BtnZoomIn.Foreground = defaultFg;
            BtnZoomOut.Background = defaultBg;
            BtnZoomOut.Foreground = defaultFg;
        }

        // 高亮当前激活的按钮（珞珈绿底，白字）
        private void HighlightButton(Button activeBtn)
        {
            // 1. 先把所有按钮重置为白色
            ResetToolButtonsStatus();

            // 2. 将当前点击的按钮背景设为珞珈绿 #005A3C，文字设为白色
            activeBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#005A3C"));
            activeBtn.Foreground = System.Windows.Media.Brushes.White;
        }

        private void BtnPan_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(BtnPan); // 界面高亮
            SetTool(new ControlsMapPanTool()); // 核心业务
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(BtnZoomIn);
            SetTool(new ControlsMapZoomInTool());
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(BtnZoomOut);
            SetTool(new ControlsMapZoomOutTool());
        }

        private void BtnFullExtent_Click(object sender, RoutedEventArgs e)
        {
            // “全图”是一个一次性命令，执行完后继续保持之前的工具状态，所以不调用 HighlightButton
            ICommand cmd = new ControlsMapFullExtentCommand();
            cmd.OnCreate(axMapControl.Object);
            cmd.OnClick();
        }

        private void SetTool(ICommand command)
        {
            command.OnCreate(axMapControl.Object);
            axMapControl.CurrentTool = (ITool)command;
        }

        // ==================== 底部状态栏响应 ====================
        private void AxMapControl_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            double x = Math.Round(e.mapX, 2);
            double y = Math.Round(e.mapY, 2);
            double scale = Math.Round(axMapControl.MapScale, 0);
            txtStatusBar.Text = string.Format("坐标系: CGCS2000 / Gauss-Kruger 3度带    X: {0}   Y: {1}    比例尺: 1:{2}", x, y, scale);
        }

      
        // ==================== 自定义图层列表交互 ====================

        // 1. 勾选/取消勾选复选框时，控制地图图层显示/隐藏
        private void LayerVisibility_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            LayerItem item = cb.Tag as LayerItem; // 通过 Tag 获取绑定的实体类
            if (item != null && item.ArcGisLayer != null)
            {
                // 控制底层 ArcGIS 图层的可见性
                item.ArcGisLayer.Visible = cb.IsChecked == true;
                item.IsVisible = cb.IsChecked == true; // 同步数据状态

                // 刷新地图
                axMapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
            }
        }

        // 2. 鼠标点击选中某个图层时（触发灰底高亮，并联动右侧“图层操作”选项卡）
        private void lstLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LayerItem selectedItem = lstLayers.SelectedItem as LayerItem;

            if (selectedItem != null)
            {
                _selectedLayer = selectedItem.ArcGisLayer; // 赋值给全局变量
                txtSelectedLayer.Text = "当前选中图层：" + _selectedLayer.Name;
                txtSelectedLayer.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void BtnApplyColor_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLayer == null) { MessageBox.Show("请先点击选中图层"); return; }
            try
            {
                int r = int.Parse(txtR.Text), g = int.Parse(txtG.Text), b = int.Parse(txtB.Text);
                if (_selectedLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    GisStyleHelper.SetPolygonSymbol(_selectedLayer, r, g, b);
                else
                    GisStyleHelper.SetLineSymbol(_selectedLayer, r, g, b);

                axMapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, _selectedLayer, null);
               
            }
            catch { MessageBox.Show("RGB输入无效"); }
        }

        private void BtnOpenAttrTable_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLayer == null) return;
            try
            {
                DataTable dt = new DataTable();
                IFeatureClass fc = _selectedLayer.FeatureClass;
                for (int i = 0; i < fc.Fields.FieldCount; i++)
                    if (fc.Fields.get_Field(i).Type != esriFieldType.esriFieldTypeGeometry)
                        dt.Columns.Add(fc.Fields.get_Field(i).Name);

                IFeatureCursor cursor = fc.Search(null, false);
                IFeature feature;
                while ((feature = cursor.NextFeature()) != null)
                {
                    DataRow row = dt.NewRow();
                    for (int i = 0; i < fc.Fields.FieldCount; i++)
                    {
                        if (fc.Fields.get_Field(i).Type != esriFieldType.esriFieldTypeGeometry)
                        {
                            object val = feature.get_Value(i);
                            row[fc.Fields.get_Field(i).Name] = (val == null || Convert.IsDBNull(val)) ? "" : val.ToString();
                        }
                    }
                    dt.Rows.Add(row);
                }
                System.Runtime.InteropServices.Marshal.ReleaseComObject(cursor);

                Window tableWin = new Window { Title = "属性表: " + _selectedLayer.Name, Width = 800, Height = 500 };
                tableWin.Content = new DataGrid { ItemsSource = dt.DefaultView, IsReadOnly = true };
                tableWin.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ==================== 选项卡：属性查询与联动业务 ====================
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchText = txtSearchName.Text.Trim();
            if (string.IsNullOrEmpty(searchText)) return;

            try
            {
                IFeatureLayer buildingLayer = null;
                for (int i = 0; i < axMapControl.LayerCount; i++)
                    if (axMapControl.get_Layer(i).Name == "校园建筑") { buildingLayer = (IFeatureLayer)axMapControl.get_Layer(i); break; }

                if (buildingLayer == null) return;

                IQueryFilter qf = new QueryFilter();
                qf.WhereClause = string.Format("name LIKE '%{0}%'", searchText);

                IFeatureSelection fs = (IFeatureSelection)buildingLayer;
                axMapControl.Map.ClearSelection();
                ((IGraphicsContainer)axMapControl.Map).DeleteAllElements();
                fs.SelectFeatures(qf, esriSelectionResultEnum.esriSelectionResultNew, false);

                lstAttributes.Items.Clear();
                IEnumFeature enumFeat = (IEnumFeature)axMapControl.Map.FeatureSelection;
                IFeature feat;
                IEnvelope fullEnv = null;

                while ((feat = enumFeat.Next()) != null)
                {
                    string nameStr = "未知", typeStr = "无";
                    int nIdx = feat.Fields.FindField("name"), tIdx = feat.Fields.FindField("type");
                    if (nIdx >= 0 && !Convert.IsDBNull(feat.get_Value(nIdx))) nameStr = feat.get_Value(nIdx).ToString();
                    if (tIdx >= 0 && !Convert.IsDBNull(feat.get_Value(tIdx))) typeStr = feat.get_Value(tIdx).ToString();

                    lstAttributes.Items.Add(new SearchResultItem { OID = feat.OID, Name = nameStr, Type = typeStr });

                    if (fullEnv == null) fullEnv = feat.Shape.Envelope; else fullEnv.Union(feat.Shape.Envelope);
                }

                if (fullEnv != null) { fullEnv.Expand(1.5, 1.5, true); axMapControl.Extent = fullEnv; axMapControl.ActiveView.Refresh(); }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void lstAttributes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstAttributes.SelectedItem == null) return;
            SearchResultItem item = (SearchResultItem)lstAttributes.SelectedItem;

            try
            {
                IFeatureLayer buildingLayer = null;
                for (int i = 0; i < axMapControl.LayerCount; i++)
                    if (axMapControl.get_Layer(i).Name == "校园建筑") { buildingLayer = (IFeatureLayer)axMapControl.get_Layer(i); break; }

                IFeature feat = buildingLayer.FeatureClass.GetFeature(item.OID);
                IGraphicsContainer gc = (IGraphicsContainer)axMapControl.Map;
                gc.DeleteAllElements();

                IRgbColor red = new RgbColor(); red.Red = 255;
                ISimpleLineSymbol sls = new SimpleLineSymbol(); sls.Color = red; sls.Width = 3;
                ISimpleFillSymbol sfs = new SimpleFillSymbol(); sfs.Outline = sls;
                IRgbColor trans = new RgbColor(); trans.NullColor = true; sfs.Color = trans;

                IElement el = new RectangleElement(); el.Geometry = feat.Shape.Envelope;
                ((IFillShapeElement)el).Symbol = sfs;
                gc.AddElement(el, 0);

                IEnvelope env = feat.Shape.Envelope; env.Expand(2, 2, true);
                axMapControl.Extent = env;
                axMapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}