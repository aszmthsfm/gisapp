using System;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Output;
using ESRI.ArcGIS.esriSystem;

namespace gisappwpf.Helpers
{
    /// <summary>
    /// GIS 制图输出与导出辅助类
    /// </summary>
    public static class GisExportHelper
    {
        public static void ExportMapToDocument(IMap map, string filePath)
        {
            // 1. 根据文件后缀名决定导出格式 (PDF / PNG / JPG)
            string ext = System.IO.Path.GetExtension(filePath).ToLower();
            IExport export = null;
            if (ext == ".pdf") export = new ExportPDFClass();
            else if (ext == ".png") export = new ExportPNGClass();
            else if (ext == ".jpg" || ext == ".jpeg") export = new ExportJPEGClass();
            else throw new Exception("不支持的导出格式");

            export.ExportFileName = filePath;
            export.Resolution = 300; // 设置为 300 DPI 高清导出

            // 2. 在内存中创建一个虚拟的 A4 横向排版页面 (PageLayout)
            IPageLayout pageLayout = new PageLayoutClass();
            IActiveView layoutView = (IActiveView)pageLayout;
            IPage page = pageLayout.Page;
            page.Units = esriUnits.esriCentimeters; // 使用厘米作为页面单位
            page.FormID = esriPageFormID.esriPageFormA4;
            page.Orientation = 2; // 2 代表横向 (Landscape)

            // 3. 将当前地图 (Map) 塞进页面的地图框中，四周留出一点白边
            IEnvelope pageEnv = new EnvelopeClass();
            pageEnv.PutCoords(1, 1, 28.7, 20); // A4纸大概是 29.7 x 21 cm

            IMapFrame mapFrame = new MapFrameClass();
            mapFrame.Map = map;
            IElement mapElement = (IElement)mapFrame;
            mapElement.Geometry = pageEnv;

            IGraphicsContainer gc = (IGraphicsContainer)pageLayout;
            gc.AddElement(mapElement, 0);

            // 4. 添加地图三要素 (利用 UID 调用 ArcGIS 内置的标准组件)
            UID uid = new UIDClass();

            // --- 4.1 指南针 (North Arrow) ---
            uid.Value = "esriCarto.MarkerNorthArrow";
            IMapSurroundFrame naFrame = mapFrame.CreateSurroundFrame(uid, null);
            IElement naElement = (IElement)naFrame;
            IEnvelope naEnv = new EnvelopeClass();
            naEnv.PutCoords(26, 17, 28, 19); // 放置在右上角
            naElement.Geometry = naEnv;
            gc.AddElement(naElement, 0);

            // --- 4.2 比例尺 (Scale Bar) ---
            uid.Value = "esriCarto.AlternatingScaleBar";
            IMapSurroundFrame sbFrame = mapFrame.CreateSurroundFrame(uid, null);
            IElement sbElement = (IElement)sbFrame;
            IEnvelope sbEnv = new EnvelopeClass();
            sbEnv.PutCoords(2, 1.5, 10, 2.5); // 放置在左下角
            sbElement.Geometry = sbEnv;
            gc.AddElement(sbElement, 0);

            // --- 4.3 图例 (Legend) ---
            uid.Value = "esriCarto.Legend";
            IMapSurroundFrame legFrame = mapFrame.CreateSurroundFrame(uid, null);
            IElement legElement = (IElement)legFrame;
            IEnvelope legEnv = new EnvelopeClass();
            legEnv.PutCoords(23.5, 2, 28, 9); // 放置在右下角
            legElement.Geometry = legEnv;
            gc.AddElement(legElement, 0);

            /* * =========================================================
             * 【未来进阶】：当你找好了自己的指南针图片并放入 Resources 后，
             * 你可以把上面 4.1 指南针的代码注释掉，换成下面这段纯图片加载代码：
             * =========================================================
             * IPictureElement picElement = new PngPictureElementClass(); // 如果是JPG就用 JpgPictureElementClass
             * picElement.ImportPictureFromFile(AppDomain.CurrentDomain.BaseDirectory + @"..\..\Resources\your_compass.png");
             * IElement picEl = (IElement)picElement;
             * picEl.Geometry = naEnv;
             * gc.AddElement(picEl, 0);
             */

            // 5. 执行导出输出
            tagRECT rect = new tagRECT();
            rect.left = 0; rect.top = 0;
            // 将厘米换算为打印像素: 像素 = 厘米 * DPI / 2.54
            rect.right = (int)(page.PrintableBounds.Width * export.Resolution / 2.54);
            rect.bottom = (int)(page.PrintableBounds.Height * export.Resolution / 2.54);

            IEnvelope pixelBounds = new EnvelopeClass();
            pixelBounds.PutCoords(rect.left, rect.bottom, rect.right, rect.top);
            export.PixelBounds = pixelBounds;

            int hDC = export.StartExporting();
            layoutView.Output(hDC, (int)export.Resolution, ref rect, null, null);
            export.FinishExporting();
        }
    }
}