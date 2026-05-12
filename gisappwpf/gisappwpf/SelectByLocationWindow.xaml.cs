using System;
using System.Windows;
using System.Windows.Controls;
using gisappwpf.Models;

namespace gisappwpf
{
    public partial class SelectByLocationWindow : Window
    {
        // 定义传出的结果属性，供主窗口读取
        public LayerItem TargetLayer { get; private set; }
        public LayerItem SourceLayer { get; private set; }
        public string SpatialMethod { get; private set; }
        public double BufferDistance { get; private set; }

        public SelectByLocationWindow(ItemCollection layers)
        {
            InitializeComponent();
            // 绑定图层列表
            cmbTargetLayer.ItemsSource = layers;
            cmbSourceLayer.ItemsSource = layers;

            if (cmbTargetLayer.Items.Count > 0) cmbTargetLayer.SelectedIndex = 0;
            if (cmbSourceLayer.Items.Count > 1) cmbSourceLayer.SelectedIndex = 1;
        }

        // 当下拉框切换时，如果是“距离范围内”，则显示输入框
        private void cmbMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (stackBuffer == null) return;

            ComboBoxItem selected = cmbMethod.SelectedItem as ComboBoxItem;
            if (selected != null && selected.Content.ToString().Contains("Distance"))
                stackBuffer.Visibility = Visibility.Visible;
            else
                stackBuffer.Visibility = Visibility.Collapsed;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTargetLayer.SelectedItem == null || cmbSourceLayer.SelectedItem == null)
            {
                MessageBox.Show("请确保选择了目标图层和源图层！");
                return;
            }

            TargetLayer = cmbTargetLayer.SelectedItem as LayerItem;
            SourceLayer = cmbSourceLayer.SelectedItem as LayerItem;

            ComboBoxItem methodItem = cmbMethod.SelectedItem as ComboBoxItem;

            // 修复1：替换掉 ?. 和 ?? 的新语法
            if (methodItem != null && methodItem.Tag != null)
            {
                SpatialMethod = methodItem.Tag.ToString();
            }
            else
            {
                SpatialMethod = "Intersects";
            }

            if (stackBuffer.Visibility == Visibility.Visible)
            {
                // 修复2：把 out double dist 拆开写
                double dist;
                if (!double.TryParse(txtBufferDistance.Text, out dist))
                {
                    MessageBox.Show("请输入有效的缓冲距离数字！");
                    return;
                }
                BufferDistance = dist;
            }

            this.DialogResult = true; // 确认并关闭
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}