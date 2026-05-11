using System;
using System.Windows;

namespace gisappwpf
{
    public partial class ColorEditWindow : Window
    {
        public int R { get; private set; }
        public int G { get; private set; }
        public int B { get; private set; }

        public ColorEditWindow(byte r, byte g, byte b)
        {
            InitializeComponent();
            txtR.Text = r.ToString();
            txtG.Text = g.ToString();
            txtB.Text = b.ToString();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                R = int.Parse(txtR.Text);
                G = int.Parse(txtG.Text);
                B = int.Parse(txtB.Text);

                this.DialogResult = true; // 关闭窗口并返回成功状态
            }
            catch
            {
                MessageBox.Show("请输入有效的数字 (0-255)");
            }
        }
    }
}