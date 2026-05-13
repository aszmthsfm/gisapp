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

        // 记录当前地图处于交互状态
        private string _currentMapAction = "None";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;            

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. 初始化容器并绑定
            axMapControl = new AxMapControl();
           
            mapHost.Child = axMapControl;

            try
            {
                lstLayers.Items.Clear();
                // 2. ：直接呼叫数据库助手获取连接
                IFeatureWorkspace featureWorkspace = GisDbHelper.GetFeatureWorkspace();

                // 3. 【解耦】：从 Helper 批量获取构建好的校园图层
                List<LayerItem> defaultLayers = GisLayerHelper.GetDefaultCampusLayers(featureWorkspace);

                // 将获取到的图层依次加载到地图和 UI 列表中
                foreach (var layerItem in defaultLayers)
                {
                    axMapControl.AddLayer(layerItem.ArcGisLayer);
                    lstLayers.Items.Insert(0, layerItem); // 插入到列表首位，保证层级正确
                }
                // 4. 视图刷新与事件绑定
                axMapControl.Extent = axMapControl.FullExtent;
                axMapControl.ActiveView.Refresh();

                axMapControl.OnMouseMove += AxMapControl_OnMouseMove;
                axMapControl.OnMouseDown += AxMapControl_OnMouseDown;

                cmbQueryLayers.ItemsSource = lstLayers.Items;
                if (cmbQueryLayers.Items.Count > 0) cmbQueryLayers.SelectedIndex = 0;
               
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
                    // 1. 【解耦调用】：使用 Helper 类一键加载并包装数据
                    // newItem 包含了 IFeatureLayer 和 UI 需要的颜色、名称等信息
                    LayerItem newItem = GisLayerHelper.LoadShapefileAsLayerItem(openFileDialog.FileName);

                    // 2. 将图层加载到底层 ArcGIS 地图容器
                    axMapControl.AddLayer(newItem.ArcGisLayer);

                    // 3. 同步更新到左侧 WPF 图层列表 (插入到最顶层)
                    lstLayers.Items.Insert(0, newItem);

                    // 4. 刷新地图并缩放到该图层范围
                    axMapControl.Extent = newItem.ArcGisLayer.AreaOfInterest;
                    axMapControl.ActiveView.Refresh();

                    MessageBox.Show("图层 [" + newItem.Name + "] 导入成功！", "成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导入失败：" + ex.Message);
                }
            }
        }

        // ==================== 移除选中图层 ====================
        private void BtnRemoveLayer_Click(object sender, RoutedEventArgs e)
        {
            // 1. 校验是否选中了图层
            if (lstLayers.SelectedItem == null || _selectedLayer == null)
            {
                MessageBox.Show("请先在下方列表中选中要移除的图层！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. 二次确认提示 (去掉了 $，改用普通的 + 号拼接字符串)
            MessageBoxResult result = MessageBox.Show("确定要移除图层 [" + _selectedLayer.Name + "] 吗？", "确认移除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 3. 从底层 ArcGIS 地图容器中移除该图层
                    axMapControl.Map.DeleteLayer(_selectedLayer);

                    // 4. 从左侧 WPF 自定义的图层列表中移除
                    lstLayers.Items.Remove(lstLayers.SelectedItem);

                    // 5. 清理全局变量并重置右侧状态提示
                    _selectedLayer = null;

                    // 6. 刷新地图视图
                    axMapControl.ActiveView.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("移除图层失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==================== 导出地图 ====================
        /*
        private void BtnExportMap_Click(object sender, RoutedEventArgs e)
{
    SaveFileDialog saveFileDialog = new SaveFileDialog();
    saveFileDialog.Title = "导出地图";
    // 支持你之前在 Helper 中写好的三种格式
    saveFileDialog.Filter = "PNG 图片 (*.png)|*.png|JPEG 图片 (*.jpg)|*.jpg|PDF 文档 (*.pdf)|*.pdf";
    saveFileDialog.FileName = "导出的地图";

    if (saveFileDialog.ShowDialog() == true)
    {
        try
        {
            // 改变鼠标指针为等待状态（导出高分辨率图片可能需要几秒）
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            // 调用已经封装好的导出辅助类方法
            GisExportHelper.ExportMapToDocument(axMapControl.Map, saveFileDialog.FileName);

            MessageBox.Show("地图导出成功！\n保存路径：" + saveFileDialog.FileName, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出地图失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // 恢复正常鼠标指针
            System.Windows.Input.Mouse.OverrideCursor = null;
        }
    }
}
        */

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

        // 2. 鼠标点击选中某个图层时触发灰底高亮
        private void lstLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LayerItem selectedItem = lstLayers.SelectedItem as LayerItem;

            if (selectedItem != null)
            {
                _selectedLayer = selectedItem.ArcGisLayer; // 赋值给全局变量
                if (cmbQueryLayers != null)
                {
                    cmbQueryLayers.SelectedItem = selectedItem;
                }
            }
        }


        // ==================== 呼出 SQL 查询构建器并执行 ====================
        private void btnOpenAdvancedQuery_Click(object sender, RoutedEventArgs e)
        {
            // 1. 实例化我们刚才写的新窗口，并传入左侧所有的图层列表
            AttributeQueryWindow queryWindow = new AttributeQueryWindow(lstLayers.Items);
            queryWindow.Owner = this;

            // 2. 如果用户在窗口里点击了“查找”
            if (queryWindow.ShowDialog() == true)
            {
                // 获取用户构建的条件
                LayerItem targetLayerItem = queryWindow.SelectedLayer;
                string whereClause = queryWindow.WhereClause;

                IFeatureLayer targetLayer = targetLayerItem.ArcGisLayer;
                IQueryFilter qf = new QueryFilterClass(); // 注意加 Class
                qf.WhereClause = whereClause;

                try
                {
                    // 3. 在地图上执行选择
                    IFeatureSelection featureSelection = (IFeatureSelection)targetLayer;
                    axMapControl.Map.ClearSelection();
                    featureSelection.SelectFeatures(qf, esriSelectionResultEnum.esriSelectionResultNew, false);

                    // 4. 【解耦】：通过 Helper 一键获取数据表和边界范围
                    IEnvelope fullEnv;
                    DataTable dt = GisQueryHelper.ConvertSelectionToDataTable(featureSelection, out fullEnv);
                    // 5. 绑定表格并缩放地图
                    dgSearchResults.ItemsSource = dt.DefaultView;

                    // 展开底栏高度到 250 像素
                    RowAttributeTable.Height = new GridLength(250);

                    if (!fullEnv.IsEmpty)
                    {
                        fullEnv.Expand(1.5, 1.5, true);
                        axMapControl.Extent = fullEnv;
                    }
                    axMapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                    axMapControl.ActiveView.Refresh();

                    MessageBox.Show("查询完成，共找到 " + dt.Rows.Count + " 条记录。", "统计", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("查询失败！可能是 SQL 语法有误。\n\n详细报错：" + ex.Message,
                                    "查询异常", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==================== 表格点击联动地图高亮 ====================
        private void dgSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    // 当用户点击 DataGrid 中的某一行时，缩放并居中该要素
    if (dgSearchResults.SelectedItem == null) return;
    
    DataRowView rowView = dgSearchResults.SelectedItem as DataRowView;
    if (rowView == null) return;

    LayerItem selectedItem = cmbQueryLayers.SelectedItem as LayerItem;
    if (selectedItem == null || selectedItem.ArcGisLayer == null) return;

    try
    {
        // 假设图层一定有 OID 字段 (objectid 或 fid)
        string oidFieldName = selectedItem.ArcGisLayer.FeatureClass.OIDFieldName;
        int oid = Convert.ToInt32(rowView[oidFieldName]);

        IFeature feature = selectedItem.ArcGisLayer.FeatureClass.GetFeature(oid);
        if (feature != null)
        {
            IEnvelope env = feature.Shape.Envelope;
            env.Expand(2.0, 2.0, true);
            axMapControl.Extent = env;
            
            // 让该要素闪烁一下 (需要引用 ESRI.ArcGIS.Controls)
            axMapControl.FlashShape(feature.Shape, 3, 200, null);
        }
    }
    catch { /* 忽略点击无法获取几何的异常 */ }
}
        // ==================== 右键菜单：查看属性表 ====================
        // ==================== 右键菜单：查看属性表 (底栏联动版) ====================
        private void MenuItem_ViewAttributes_Click(object sender, RoutedEventArgs e)
        {
            // 1. 获取触发右键的图层项
            LayerItem item = GetLayerItemFromMenuItem(sender);
            if (item == null || item.ArcGisLayer == null) return;

            try
            {
                // 2. 设置鼠标状态为忙碌
                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                // 3. 【解耦调用】：直接呼叫我们之前写的查询助手获取全表数据
                // 注意：这里需要我们在 GisQueryHelper 中实现的 ConvertFeatureClassToDataTable 方法
                DataTable dt = GisQueryHelper.ConvertFeatureClassToDataTable(item.ArcGisLayer.FeatureClass);

                // 4. 将数据展示在底部的 DataGrid 中
                dgSearchResults.ItemsSource = dt.DefaultView;

                // 5. 【关键】：同步底部的“当前查询图层”下拉框，确保后续交互逻辑正确
                cmbQueryLayers.SelectedItem = item;

                // 6. 展开底部面板（设定高度为 300）
                RowAttributeTable.Height = new GridLength(300);

                // 7. 更新状态栏提示
                txtStatusBar.Text = "已加载图层属性表: " + item.Name + " (共 " + dt.Rows.Count + " 条记录)";
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法读取属性表: " + ex.Message, "错误");
            }
            finally
            {
                System.Windows.Input.Mouse.OverrideCursor = null;
            }
        }

        // ==================== 右键菜单：样式更改 ====================
        private void MenuItem_StyleChange_Click(object sender, RoutedEventArgs e)
        {
            // 1. 获取对应的图层项
            LayerItem item = GetLayerItemFromMenuItem(sender);
            if (item == null || item.ArcGisLayer == null) return;

            // 2. 获取当前颜色并弹出选择窗口
            System.Windows.Media.Color current = item.SymbolColor.Color;
            ColorEditWindow colorWin = new ColorEditWindow(current.R, current.G, current.B);
            colorWin.Owner = this;

            if (colorWin.ShowDialog() == true)
            {
                try
                {
                    // 3. 修改 ArcGIS 图层样式
                    if (item.ArcGisLayer.FeatureClass.ShapeType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon)
                        GisStyleHelper.SetPolygonSymbol(item.ArcGisLayer, colorWin.R, colorWin.G, colorWin.B);
                    else
                        GisStyleHelper.SetLineSymbol(item.ArcGisLayer, colorWin.R, colorWin.G, colorWin.B);

                    // 4. 同步更新预览色块
                    item.SymbolColor = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb((byte)colorWin.R, (byte)colorWin.G, (byte)colorWin.B));

                    // 5. 刷新地图
                    axMapControl.ActiveView.PartialRefresh(ESRI.ArcGIS.Carto.esriViewDrawPhase.esriViewGeography, item.ArcGisLayer, null);
                }
                catch (Exception ex) { MessageBox.Show("样式应用失败: " + ex.Message); }
            }
        }

        // ==================== 辅助方法：获取右键点击的图层数据 ====================
        private LayerItem GetLayerItemFromMenuItem(object sender)
        {
            MenuItem mi = sender as MenuItem;
            if (mi == null) return null;

            ContextMenu cm = mi.Parent as ContextMenu;
            if (cm == null) return null;

            System.Windows.FrameworkElement target = cm.PlacementTarget as System.Windows.FrameworkElement;
            if (target == null) return null;

            // 返回数据上下文
            return target.DataContext as LayerItem;
        }

        // ==================== 空间查询：交互绘制事件 ====================

        // ==================== 空间查询：改变交互状态 ====================

        private void btnDrawRect_Click(object sender, RoutedEventArgs e)
        {
            if (cmbQueryLayers.SelectedItem == null) { MessageBox.Show("请先选择图层！"); return; }

            // 1. 设置状态为“准备画矩形”
            _currentMapAction = "DrawRect";
            // 2. 清除平移、放大等其他工具的干扰
            axMapControl.CurrentTool = null;
            // 3. 鼠标变成十字星，提示用户去地图上画
            axMapControl.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
        }

        private void btnDrawPolygon_Click(object sender, RoutedEventArgs e)
        {
            if (cmbQueryLayers.SelectedItem == null) { MessageBox.Show("请先选择图层！"); return; }

            _currentMapAction = "DrawPolygon";
            axMapControl.CurrentTool = null;
            axMapControl.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
        }

        private void btnClearSelect_Click(object sender, RoutedEventArgs e)
        {
            // 1. 重置交互状态和鼠标指针
            _currentMapAction = "None";
            axMapControl.MousePointer = esriControlsMousePointer.esriPointerDefault;

            // 2. 清除所有图层的要素选中状态 (青色高亮)
            axMapControl.Map.ClearSelection();

            // 3. 【关键修复】清除地图上可能残留的任何临时绘制图形 (Graphic Elements)
            IGraphicsContainer gc = axMapControl.Map as IGraphicsContainer;
            if (gc != null)
            {
                gc.DeleteAllElements();
            }

            // 4. 【关键修复】放弃 PartialRefresh，使用最彻底的全局刷新，强制重绘地图
            axMapControl.ActiveView.Refresh();

            // 5. 清空右侧的属性数据表格
            dgSearchResults.ItemsSource = null;

            RowAttributeTable.Height = new GridLength(0);
        }

        // ==================== 地图鼠标按下事件 ====================
        private void AxMapControl_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            // 如果不是左键点击，或者当前没有处于画图状态，则直接忽略
            if (e.button != 1 || _currentMapAction == "None") return;

            IGeometry searchGeometry = null;

            if (_currentMapAction == "DrawRect")
            {
                // 在鼠标按下的位置开始画矩形
                searchGeometry = axMapControl.TrackRectangle();
            }
            else if (_currentMapAction == "DrawPolygon")
            {
                // 在鼠标按下的位置开始画多边形
                searchGeometry = axMapControl.TrackPolygon();
            }

            // 画完之后，马上恢复默认状态（变成普通的鼠标指针）
            _currentMapAction = "None";
            axMapControl.MousePointer = esriControlsMousePointer.esriPointerDefault;

            // 如果用户画出了有效的图形，执行空间查询
            if (searchGeometry != null && !searchGeometry.IsEmpty)
            {
                ExecuteSpatialQuery(searchGeometry);
            }
        }

        // ==================== 空间查询：核心执行逻辑 ====================
        // 将用户画的图形转换为数据库查询条件，并生成表格
        private void ExecuteSpatialQuery(IGeometry searchGeometry)
        {
            LayerItem selectedItem = cmbQueryLayers.SelectedItem as LayerItem;
            if (selectedItem == null || selectedItem.ArcGisLayer == null) return;

            IFeatureLayer targetLayer = selectedItem.ArcGisLayer;

            try
            {
                // 1. 构建空间过滤器 (ISpatialFilter)
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = searchGeometry;

                // 设置空间关系：相交 (Intersects)。只要要素和我们画的框有任何接触，就会被查出来
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                // 2. 在地图上执行选择
                IFeatureSelection featureSelection = (IFeatureSelection)targetLayer;
                axMapControl.Map.ClearSelection();
                // 执行查询：传入空间过滤器
                featureSelection.SelectFeatures(spatialFilter, esriSelectionResultEnum.esriSelectionResultNew, false);

                // 3. 调用数据处理服务
                IEnvelope fullEnv;
                DataTable dt = GisQueryHelper.ConvertSelectionToDataTable(featureSelection, out fullEnv);

                // 4. 更新表格视图并刷新地图
                dgSearchResults.ItemsSource = dt.DefaultView;
                axMapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("您画的区域内没有找到任何要素。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("空间查询失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== 呼出“按位置选择”工具并执行 ====================
        private void btnOpenSpatialQuery_Click(object sender, RoutedEventArgs e)
        {
            // 1. 实例化弹窗，传入左侧图层列表
            SelectByLocationWindow spatialWin = new SelectByLocationWindow(lstLayers.Items);
            spatialWin.Owner = this;

            // 2. 接收弹窗返回的参数
            if (spatialWin.ShowDialog() == true)
            {
                LayerItem targetItem = spatialWin.TargetLayer;
                LayerItem sourceItem = spatialWin.SourceLayer;
                string method = spatialWin.SpatialMethod;
                double distance = spatialWin.BufferDistance;

                IFeatureLayer targetLayer = targetItem.ArcGisLayer;
                IFeatureLayer sourceLayer = sourceItem.ArcGisLayer;

                try
                {
                    // 数据量大时合并图形需要一点时间，让鼠标转个圈
                    System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                    // 3. 【核心】提取源图层的所有几何图形，并合并成一个“大模具”
                    IGeometry sourceGeometry = GetLayerUnionGeometry(sourceLayer);
                    if (sourceGeometry == null || sourceGeometry.IsEmpty)
                    {
                        MessageBox.Show("源图层中没有有效要素，无法作为参考范围！");
                        return;
                    }

                    // 4. 【核心】如果是距离查询，执行 Buffer (缓冲区) 拓扑分析
                    if (method == "Distance" && distance > 0)
                    {
                        ITopologicalOperator topoOp = sourceGeometry as ITopologicalOperator;
                        // 注意：因为你的地图坐标系是 CGCS2000 高斯克吕格投影，这里的单位天然就是“米”！
                        sourceGeometry = topoOp.Buffer(distance); 
                    }

                    // 5. 构建空间过滤器 (ISpatialFilter)
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = sourceGeometry;
                    
                    // 将下拉框的字符串映射为 ArcGIS 底层的空间关系枚举
                    switch (method)
                    {
                        case "Intersects": spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects; break;
                        case "Contains":   spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains; break;
                        case "Within":     spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelWithin; break;
                        case "Distance":   spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects; break; // 缓冲后只要相交就算在距离内
                    }

                    // 6. 在目标图层上执行选择
                    IFeatureSelection featureSelection = (IFeatureSelection)targetLayer;
                    axMapControl.Map.ClearSelection();
                    featureSelection.SelectFeatures(spatialFilter, esriSelectionResultEnum.esriSelectionResultNew, false);

                    // 7. 提取拓扑查询结果数据
                    IEnvelope fullEnv;
                    DataTable dt = GisQueryHelper.ConvertSelectionToDataTable(featureSelection, out fullEnv);

                    // 8. 绑定表格并缩放地图
                    dgSearchResults.ItemsSource = dt.DefaultView;

                    if (!fullEnv.IsEmpty)
                    {
                        fullEnv.Expand(1.5, 1.5, true);
                        axMapControl.Extent = fullEnv;
                    }
                    
                    // 彻底刷新地图高亮
                    axMapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                    axMapControl.ActiveView.Refresh();

                    MessageBox.Show("空间分析完成！\n共找到 " + dt.Rows.Count + " 个符合拓扑关系的要素。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("按位置选择执行失败，可能是几何体运算异常。\n详细信息: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // 无论成功失败，恢复鼠标指针
                    System.Windows.Input.Mouse.OverrideCursor = null; 
                }
            }
        }

        // ==================== 辅助方法：把图层里的所有碎块融合成一个大图形 ====================
        private IGeometry GetLayerUnionGeometry(IFeatureLayer layer)
        {
            IGeometry searchGeom = null;
            IFeatureCursor cursor = layer.FeatureClass.Search(null, false);
            IFeature feature;
            
            while ((feature = cursor.NextFeature()) != null)
            {
                if (searchGeom == null)
                {
                    searchGeom = feature.ShapeCopy;
                }
                else
                {
                    // 使用 ITopologicalOperator 接口进行空间融合 (Union)
                    ITopologicalOperator topoOp = searchGeom as ITopologicalOperator;
                    searchGeom = topoOp.Union(feature.ShapeCopy);
                }
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(cursor);
            return searchGeom;
        }

        // 收起底部属性表面板
        private void BtnCloseAttributeTable_Click(object sender, RoutedEventArgs e)
        {
            RowAttributeTable.Height = new GridLength(0);
        }

        // ==================== 快捷模糊查询 ====================
        private void BtnNameSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchKeyword = txtSearchName.Text.Trim();
            if (string.IsNullOrEmpty(searchKeyword))
            {
                MessageBox.Show("请输入要搜索的名称关键字！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 这里以用户在左侧 TOC (图层列表) 选中的图层作为搜索目标
            if (_selectedLayer == null)
            {
                MessageBox.Show("请先在左侧图层列表中单击选中一个要搜索的图层！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 改变鼠标状态为等待
                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                // 1. 构建模糊查询的 SQL 语句 (ArcGIS Shapefile 使用 % 作为通配符)
                // 【⚠️重要提示】：这里的 "Name" 需要替换为你实际 Shapefile 属性表中的名称字段名（例如 "NAME", "名称", "MC" 等）
                string searchField = "Name";
                string whereClause = string.Format("{0} LIKE '%{1}%'", searchField, searchKeyword);

                IQueryFilter qf = new QueryFilterClass();
                qf.WhereClause = whereClause;

                // 2. 在选中的图层上执行选择
                IFeatureSelection featureSelection = (IFeatureSelection)_selectedLayer;
                axMapControl.Map.ClearSelection();
                featureSelection.SelectFeatures(qf, esriSelectionResultEnum.esriSelectionResultNew, false);

                // 3. 调用你现有的 Helper 转换结果并展示到底部表格
                IEnvelope fullEnv;
                DataTable dt = GisQueryHelper.ConvertSelectionToDataTable(featureSelection, out fullEnv);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("未找到包含该名称的要素。", "查询结果", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 绑定表格数据
                dgSearchResults.ItemsSource = dt.DefaultView;

                // 同步底部的“当前查询图层”下拉框，避免点表格联动地图时报错
                foreach (LayerItem item in cmbQueryLayers.Items)
                {
                    if (item.ArcGisLayer == _selectedLayer)
                    {
                        cmbQueryLayers.SelectedItem = item;
                        break;
                    }
                }

                // 展开底栏面板
                RowAttributeTable.Height = new GridLength(250);

                // 4. 缩放地图到搜索结果的范围
                if (!fullEnv.IsEmpty)
                {
                    fullEnv.Expand(1.5, 1.5, true);
                    axMapControl.Extent = fullEnv;
                }

                // 刷新地图高亮
                axMapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                axMapControl.ActiveView.Refresh();

                txtStatusBar.Text = string.Format("模糊搜索完成: 找到 {0} 条记录。", dt.Rows.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("搜索失败！\n原因可能是当前图层没有该字段，请检查字段名是否正确。\n详细报错：" + ex.Message,
                                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复鼠标状态
                System.Windows.Input.Mouse.OverrideCursor = null;
            }
        }
       
    }
}