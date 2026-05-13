using System;
using System.Collections.Generic;
using System.Windows.Media;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geometry;
using gisappwpf.Models;

namespace gisappwpf.Helpers
{
    public static class GisLayerHelper
    {
        /// <summary>
        /// 导入外部 Shapefile 并生成供 UI 绑定的 LayerItem
        /// </summary>
        public static LayerItem LoadShapefileAsLayerItem(string fullPath)
        {
            string folderPath = System.IO.Path.GetDirectoryName(fullPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(fullPath);

            // 1. 建立 Shapefile 工作空间
            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(folderPath, 0);

            // 2. 打开要素类并创建图层
            IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(fileName);
            IFeatureLayer newLayer = new FeatureLayer
            {
                FeatureClass = featureClass,
                Name = fileName
            };

            // 3. 智能判断几何类型，并赋予默认渲染色
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
                uiColor = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // 点默认橙色
                GisStyleHelper.SetPointSymbol(newLayer, 255, 165, 0);
            }

            // 4. 包装并返回 UI 认识的实体类
            return new LayerItem
            {
                ArcGisLayer = newLayer,
                Name = newLayer.Name,
                IsVisible = true,
                SymbolColor = uiColor,
                ShapeType = uiShapeType
            };
        }

        /// <summary>
        /// 批量加载默认的校园数据库图层
        /// </summary>
        public static List<LayerItem> GetDefaultCampusLayers(IFeatureWorkspace featureWorkspace)
        {
            List<LayerItem> layers = new List<LayerItem>();

            // 按照渲染顺序添加 (注意这里把 featureWorkspace 传给了外面的辅助方法)
            layers.Add(CreateDbLayer(featureWorkspace, "public.boundary", "学校范围", "Polygon", 245, 245, 245));
            layers.Add(CreateDbLayer(featureWorkspace, "public.water", "水系", "Polygon", 151, 219, 242));
            layers.Add(CreateDbLayer(featureWorkspace, "public.green", "绿地", "Polygon", 195, 230, 175));
            layers.Add(CreateDbLayer(featureWorkspace, "public.road", "道路路网", "Polyline", 200, 200, 200));
            layers.Add(CreateDbLayer(featureWorkspace, "public.building", "校园建筑", "Polygon", 253, 218, 185));
            layers.Add(CreateDbLayer(featureWorkspace, "public.playground", "校园操场", "Polygon", 235, 165, 150));

            return layers;
        }

        /// <summary>
        /// 【解耦出来的私有方法】用于统一构建单个图层实体 (兼容老版本C#语法)
        /// </summary>
        private static LayerItem CreateDbLayer(IFeatureWorkspace featureWorkspace, string className, string name, string shapeType, byte r, byte g, byte b)
        {
            IFeatureClass fc = featureWorkspace.OpenFeatureClass(className);
            IFeatureLayer layer = new FeatureLayer { FeatureClass = fc, Name = name };

            if (shapeType == "Polygon")
                GisStyleHelper.SetPolygonSymbol(layer, r, g, b);
            else
                GisStyleHelper.SetLineSymbol(layer, r, g, b, 1.5);

            return new LayerItem
            {
                ArcGisLayer = layer,
                Name = layer.Name,
                IsVisible = true,
                SymbolColor = new SolidColorBrush(Color.FromRgb(r, g, b)),
                ShapeType = shapeType
            };
        }
    }
}