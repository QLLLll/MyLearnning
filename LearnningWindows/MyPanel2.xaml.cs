using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LearnningWindows
{
    /// <summary>
    /// MyPanel2.xaml 的交互逻辑
    /// </summary>
    public partial class MyPanel2 : UserControl
    {
        public MyPanel2()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Hello world!");
        }
    }
}
