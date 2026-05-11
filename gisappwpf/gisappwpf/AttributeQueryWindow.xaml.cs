using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.Geodatabase;
using gisappwpf.Models; // 引用你的 LayerItem 模型

namespace gisappwpf
{
    public partial class AttributeQueryWindow : Window
    {
        // 供主窗口读取的查询结果
        public LayerItem SelectedLayer { get; private set; }
        public string WhereClause { get; private set; }

        public AttributeQueryWindow(ItemCollection layers)
        {
            InitializeComponent();
            // 绑定传进来的图层列表
            cmbLayer.ItemsSource = layers;
            if (cmbLayer.Items.Count > 0) cmbLayer.SelectedIndex = 0;
        }

        // ==================== 1. 图层改变，加载字段 ====================
        private void cmbLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lstFields.Items.Clear();
            lstValues.Items.Clear();

            LayerItem item = cmbLayer.SelectedItem as LayerItem;
            if (item == null || item.ArcGisLayer == null) return;

            IFields fields = item.ArcGisLayer.FeatureClass.Fields;
            for (int i = 0; i < fields.FieldCount; i++)
            {
                IField field = fields.get_Field(i);
                // 排除图形和二进制字段
                if (field.Type != esriFieldType.esriFieldTypeGeometry && field.Type != esriFieldType.esriFieldTypeBlob)
                {
                    lstFields.Items.Add(field.Name);
                }
            }
        }

        // ==================== 2. 点击字段，加载唯一值 ====================
        private void lstFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstFields.SelectedItem == null) return;
            string fieldName = lstFields.SelectedItem.ToString();

            LayerItem item = cmbLayer.SelectedItem as LayerItem;
            if (item == null) return;

            lstValues.Items.Clear();
            HashSet<string> uniqueValues = new HashSet<string>();

            try
            {
                // 使用游标遍历获取唯一值 (限制前1000条防止卡顿)
                IFeatureCursor cursor = item.ArcGisLayer.FeatureClass.Search(null, false);
                IFeature feature;
                int fieldIndex = item.ArcGisLayer.FeatureClass.FindField(fieldName);
                int count = 0;

                while ((feature = cursor.NextFeature()) != null && count < 1000)
                {
                    object val = feature.get_Value(fieldIndex);
                    if (val != DBNull.Value && val != null && val.ToString().Trim() != "")
                    {
                        uniqueValues.Add(val.ToString());
                    }
                    count++;
                }
                System.Runtime.InteropServices.Marshal.ReleaseComObject(cursor);

                foreach (var val in uniqueValues) lstValues.Items.Add(val);
            }
            catch { /* 忽略读取唯一值时的错误 */ }
        }

        // ==================== 3. 表达式构建操作 ====================

        // 双击字段名添加到文本框
        private void lstFields_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstFields.SelectedItem != null)
                InsertText(lstFields.SelectedItem.ToString() + " ");
        }

        // 双击取值添加到文本框 (智能判断是否加单引号)
        private void lstValues_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstValues.SelectedItem == null) return;
            string val = lstValues.SelectedItem.ToString();

            // 判断当前选中的字段是否是数字类型
            bool isString = true;
            LayerItem item = cmbLayer.SelectedItem as LayerItem;
            if (item != null && lstFields.SelectedItem != null)
            {
                int idx = item.ArcGisLayer.FeatureClass.FindField(lstFields.SelectedItem.ToString());
                if (idx != -1)
                {
                    esriFieldType type = item.ArcGisLayer.FeatureClass.Fields.get_Field(idx).Type;
                    if (type == esriFieldType.esriFieldTypeInteger || type == esriFieldType.esriFieldTypeDouble || type == esriFieldType.esriFieldTypeSingle)
                        isString = false;
                }
            }

            // 字符串需要加单引号
            if (isString) InsertText("'" + val + "' ");
            else InsertText(val + " ");
        }

        // 操作符按钮点击 (所有计算符号共用)
        private void OperatorBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            InsertText(btn.Content.ToString() + " ");
        }

        private void BtnSpace_Click(object sender, RoutedEventArgs e) { InsertText(" "); }
        private void BtnClear_Click(object sender, RoutedEventArgs e) { txtExpression.Clear(); }

        // 辅助方法：在光标位置插入文本
        private void InsertText(string text)
        {
            int selectionStart = txtExpression.SelectionStart;
            txtExpression.Text = txtExpression.Text.Insert(selectionStart, text);
            txtExpression.SelectionStart = selectionStart + text.Length;
            txtExpression.Focus();
        }

        // ==================== 4. 提交查询 ====================
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (cmbLayer.SelectedItem == null)
            {
                MessageBox.Show("请选择图层！"); return;
            }
            if (string.IsNullOrWhiteSpace(txtExpression.Text))
            {
                MessageBox.Show("表达式不能为空！"); return;
            }

            SelectedLayer = cmbLayer.SelectedItem as LayerItem;
            WhereClause = txtExpression.Text.Trim();
            this.DialogResult = true; // 关闭窗口并返回成功
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}