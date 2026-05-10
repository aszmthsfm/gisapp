using System;

namespace gisappwpf.Models
{
    /// <summary>
    /// 用来存储空间查询结果的数据实体类 (Model)
    /// </summary>
    public class SearchResultItem
    {
        /// <summary>
        /// 隐藏存储要素的唯一ID ，用于地图图形进行联动定位
        /// </summary>
        public int OID { get; set; }

        /// <summary>
        /// 建筑名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 建筑类型
        /// </summary>
        public string Type { get; set; }
    }
}