using System;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;

namespace gisappwpf.Helpers
{
    /// <summary>
    /// GIS 样式与符号化辅助工具类
    /// </summary>
    public static class GisStyleHelper
    {
        public static void SetPolygonSymbol(IFeatureLayer layer, int r, int g, int b, int outlineR = 150, int outlineG = 150, int outlineB = 150)
        {
            if (layer == null) return;
            IRgbColor fillColor = new RgbColor();
            fillColor.Red = r; fillColor.Green = g; fillColor.Blue = b;

            IRgbColor outlineColor = new RgbColor();
            outlineColor.Red = outlineR; outlineColor.Green = outlineG; outlineColor.Blue = outlineB;

            ISimpleLineSymbol outlineSymbol = new SimpleLineSymbol();
            outlineSymbol.Color = outlineColor; outlineSymbol.Width = 0.5;

            ISimpleFillSymbol fillSymbol = new SimpleFillSymbol();
            fillSymbol.Color = fillColor; fillSymbol.Outline = outlineSymbol;

            ISimpleRenderer renderer = new SimpleRenderer();
            renderer.Symbol = (ISymbol)fillSymbol;

            IGeoFeatureLayer geoLayer = layer as IGeoFeatureLayer;
            if (geoLayer != null) geoLayer.Renderer = (IFeatureRenderer)renderer;
        }

        public static void SetLineSymbol(IFeatureLayer layer, int r, int g, int b, double width = 1.5)
        {
            if (layer == null) return;
            IRgbColor lineColor = new RgbColor();
            lineColor.Red = r; lineColor.Green = g; lineColor.Blue = b;

            ISimpleLineSymbol lineSymbol = new SimpleLineSymbol();
            lineSymbol.Color = lineColor; lineSymbol.Width = width;

            ISimpleRenderer renderer = new SimpleRenderer();
            renderer.Symbol = (ISymbol)lineSymbol;

            IGeoFeatureLayer geoLayer = layer as IGeoFeatureLayer;
            if (geoLayer != null) geoLayer.Renderer = (IFeatureRenderer)renderer;
        }

        public static void SetPointSymbol(IFeatureLayer layer, int r, int g, int b, double size = 6.0)
        {
            if (layer == null) return;

            // 1. 设置点的主体颜色
            IRgbColor pointColor = new RgbColor();
            pointColor.Red = r; pointColor.Green = g; pointColor.Blue = b;

            // 2. 实例化点符号
            ISimpleMarkerSymbol markerSymbol = new SimpleMarkerSymbol();
            markerSymbol.Color = pointColor;
            markerSymbol.Style = esriSimpleMarkerStyle.esriSMSCircle; // 圆形点
            markerSymbol.Size = size;

            // 3. 设置点的边框（关键修改点）
            IRgbColor outColor = new RgbColor();
            outColor.Red = 100; outColor.Green = 100; outColor.Blue = 100; // 深灰色边框

            markerSymbol.Outline = true;          // 开启边框 (这里是 bool 类型)
            markerSymbol.OutlineColor = outColor; // 设置边框颜色
            markerSymbol.OutlineSize = 0.5;       // 设置边框粗细

            // 4. 应用渲染器
            ISimpleRenderer renderer = new SimpleRenderer();
            renderer.Symbol = (ISymbol)markerSymbol;

            IGeoFeatureLayer geoLayer = layer as IGeoFeatureLayer;
            if (geoLayer != null) geoLayer.Renderer = (IFeatureRenderer)renderer;
        }
    }
}