using System;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;

namespace gisappwpf.Helpers
{
    /// <summary>
    /// GIS 样式与符号化辅助工具类
    /// 专门负责给空间图层配置颜色和边框
    /// </summary>
    public static class GisStyleHelper
    {
        /// <summary>
        /// 为面要素图层（如建筑、绿地、水系）设置填充颜色和边框颜色
        /// </summary>
        public static void SetPolygonSymbol(IFeatureLayer layer, int r, int g, int b, int outlineR = 150, int outlineG = 150, int outlineB = 150)
        {
            if (layer == null) return;

            // 1. 定义填充颜色
            IRgbColor fillColor = new RgbColor();
            fillColor.Red = r;
            fillColor.Green = g;
            fillColor.Blue = b;

            // 2. 定义边框颜色
            IRgbColor outlineColor = new RgbColor();
            outlineColor.Red = outlineR;
            outlineColor.Green = outlineG;
            outlineColor.Blue = outlineB;

            // 3. 定义边框符号
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

        /// <summary>
        /// 为线要素图层（如道路、管线）设置颜色和宽度
        /// </summary>
        public static void SetLineSymbol(IFeatureLayer layer, int r, int g, int b, double width = 1.5)
        {
            if (layer == null) return;

            // 1. 实例化颜色
            IRgbColor lineColor = new RgbColor();
            lineColor.Red = r;
            lineColor.Green = g;
            lineColor.Blue = b;

            // 2. 实例化线符号
            ISimpleLineSymbol lineSymbol = new SimpleLineSymbol();
            lineSymbol.Color = lineColor;
            lineSymbol.Width = width;

            // 3. 实例化渲染器
            ISimpleRenderer renderer = new SimpleRenderer();
            renderer.Symbol = (ISymbol)lineSymbol;

            IGeoFeatureLayer geoLayer = layer as IGeoFeatureLayer;
            if (geoLayer != null)
            {
                geoLayer.Renderer = (IFeatureRenderer)renderer;
            }
        }
    }
}