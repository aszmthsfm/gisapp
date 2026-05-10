using System.Windows.Media;
using ESRI.ArcGIS.Carto;

namespace gisappwpf.Models
{
    /// <summary>
    /// 用于在 WPF 界面中展示图层列表的自定义实体类
    /// </summary>
    public class LayerItem
    {
        // 绑定底层的 ArcGIS 图层对象，方便后续操作
        public IFeatureLayer ArcGisLayer { get; set; }

        // 图层名称
        public string Name { get; set; }

        // 是否可见 (绑定给 CheckBox)
        public bool IsVisible { get; set; }

        // 符号颜色 (绑定给色块)
        public SolidColorBrush SymbolColor { get; set; }

        // 几何类型标识， "Polygon" (面), "Polyline" (线), "Point" (点)
        public string ShapeType { get; set; }
    }
}