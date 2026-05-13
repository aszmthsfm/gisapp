using System;
using System.Data;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace gisappwpf.Helpers
{
    public static class GisQueryHelper
    {
        /// <summary>
        /// 将 ArcGIS 的要素选择集 (IFeatureSelection) 转换为通用的 DataTable，并返回这些要素的整体边界范围
        /// </summary>
        /// <param name="featureSelection">图层选择集</param>
        /// <param name="fullExtent">输出：选中要素合并后的空间包络线(外接矩形)</param>
        /// <returns>包含属性数据的 DataTable</returns>
        public static DataTable ConvertSelectionToDataTable(IFeatureSelection featureSelection, out IEnvelope fullExtent)
        {
            DataTable dt = new DataTable();
            fullExtent = new EnvelopeClass();

            IFeatureLayer targetLayer = featureSelection as IFeatureLayer;
            if (targetLayer == null) return dt;

            IFeatureClass fc = targetLayer.FeatureClass;

            // 1. 构建表头
            for (int i = 0; i < fc.Fields.FieldCount; i++)
            {
                IField field = fc.Fields.get_Field(i);
                // 排除几何图形字段
                if (field.Type != esriFieldType.esriFieldTypeGeometry)
                {
                    dt.Columns.Add(field.Name);
                }
            }

            // 2. 遍历游标，填充数据，并扩展边界
            ISelectionSet selectionSet = featureSelection.SelectionSet;
            ICursor cursor;
            selectionSet.Search(null, false, out cursor);
            IFeatureCursor featCursor = cursor as IFeatureCursor;
            IFeature feature;

            while ((feature = featCursor.NextFeature()) != null)
            {
                DataRow row = dt.NewRow();
                for (int i = 0; i < fc.Fields.FieldCount; i++)
                {
                    IField field = fc.Fields.get_Field(i);
                    if (field.Type != esriFieldType.esriFieldTypeGeometry)
                    {
                        object val = feature.get_Value(i);
                        row[field.Name] = (val == null || Convert.IsDBNull(val)) ? "" : val.ToString();
                    }
                }
                dt.Rows.Add(row);

                // 将当前要素的边界并入总边界
                fullExtent.Union(feature.Shape.Envelope);
            }

            // 释放 COM 对象
            System.Runtime.InteropServices.Marshal.ReleaseComObject(cursor);

            return dt;
        }

        /// <summary>
        /// 将整个要素类 (IFeatureClass) 转换为 DataTable
        /// </summary>
        public static DataTable ConvertFeatureClassToDataTable(IFeatureClass fc)
        {
            DataTable dt = new DataTable();

            // 构建表头
            for (int i = 0; i < fc.Fields.FieldCount; i++)
            {
                IField field = fc.Fields.get_Field(i);
                if (field.Type != esriFieldType.esriFieldTypeGeometry)
                    dt.Columns.Add(field.Name);
            }

            // 遍历数据
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
            return dt;
        }
    }
}