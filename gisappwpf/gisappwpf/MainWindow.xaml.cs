using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Data;
using System.Collections.Generic;

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
        private AxTOCControl axTOCControl;

        // 记住当前点击选中的图层，供“图层操作”选项卡使用
        private IFeatureLayer _selectedLayer = null;

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
                // 2. 【解耦体现】：直接呼叫数据库助手获取连接
                IFeatureWorkspace featureWorkspace = GisDbHelper.GetFeatureWorkspace();

                // 3. 批量加载图层 (利用 GisStyleHelper 进行美化渲染)
                // (1) 学校范围
                IFeatureClass boundaryFC = featureWorkspace.OpenFeatureClass("public.boundary");
                IFeatureLayer boundaryLayer = new FeatureLayer { FeatureClass = boundaryFC, Name = "学校范围" };
                GisStyleHelper.SetPolygonSymbol(boundaryLayer, 245, 245, 245);
                axMapControl.AddLayer(boundaryLayer);

                // (2) 水系
                IFeatureClass waterFC = featureWorkspace.OpenFeatureClass("public.water");
                IFeatureLayer waterLayer = new FeatureLayer { FeatureClass = waterFC, Name = "水系" };
                GisStyleHelper.SetPolygonSymbol(waterLayer, 151, 219, 242, 100, 150, 180);
                axMapControl.AddLayer(waterLayer);

                // (3) 绿地
                IFeatureClass greenFC = featureWorkspace.OpenFeatureClass("public.green");
                IFeatureLayer greenLayer = new FeatureLayer { FeatureClass = greenFC, Name = "绿地" };
                GisStyleHelper.SetPolygonSymbol(greenLayer, 195, 230, 175);
                axMapControl.AddLayer(greenLayer);

                // (4) 道路
                IFeatureClass roadFC = featureWorkspace.OpenFeatureClass("public.road");
                IFeatureLayer roadLayer = new FeatureLayer { FeatureClass = roadFC, Name = "道路路网" };
                GisStyleHelper.SetLineSymbol(roadLayer, 200, 200, 200, 1.5);
                axMapControl.AddLayer(roadLayer);

                // (5) 建筑 (最顶层)
                IFeatureClass buildingFC = featureWorkspace.OpenFeatureClass("public.building");
                IFeatureLayer buildingLayer = new FeatureLayer { FeatureClass = buildingFC, Name = "校园建筑" };
                GisStyleHelper.SetPolygonSymbol(buildingLayer, 253, 218, 185, 180, 150, 120);
                axMapControl.AddLayer(buildingLayer);

                // (6) 操场
                IFeatureClass playgroundFC = featureWorkspace.OpenFeatureClass("public.playground");
                IFeatureLayer playgroundLayer = new FeatureLayer { FeatureClass = playgroundFC, Name = "校园操场" };
                GisStyleHelper.SetPolygonSymbol(playgroundLayer, 235, 165, 150);
                axMapControl.AddLayer(playgroundLayer);

                // 4. 视图刷新与事件绑定
                axMapControl.Extent = axMapControl.FullExtent;
                axMapControl.ActiveView.Refresh();

                axMapControl.OnMouseMove += AxMapControl_OnMouseMove;
                axTOCControl.OnMouseDown += AxTOCControl_OnMouseDown;

                MessageBox.Show("武大校园空间数据工程环境已就绪！");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // ==================== 顶部工具栏交互 ====================
        private void BtnPan_Click(object sender, RoutedEventArgs e) { SetTool(new ControlsMapPanTool()); }
        private void BtnZoomIn_Click(object sender, RoutedEventArgs e) { SetTool(new ControlsMapZoomInTool()); }
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e) { SetTool(new ControlsMapZoomOutTool()); }
        private void BtnFullExtent_Click(object sender, RoutedEventArgs e)
        {
            ICommand cmd = new ControlsMapFullExtentCommand();
            cmd.OnCreate(axMapControl.Object); cmd.OnClick();
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

        // ==================== 选项卡：图层操作业务 ====================
        private void AxTOCControl_OnMouseDown(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            esriTOCControlItem itemType = esriTOCControlItem.esriTOCControlItemNone;
            IBasicMap basicMap = null; ILayer layer = null; object unk = null; object data = null;
            axTOCControl.HitTest(e.x, e.y, ref itemType, ref basicMap, ref layer, ref unk, ref data);

            if (itemType == esriTOCControlItem.esriTOCControlItemLayer && layer is IFeatureLayer)
            {
                _selectedLayer = layer as IFeatureLayer;
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
                axTOCControl.Update();
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