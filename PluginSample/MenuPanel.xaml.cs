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

namespace PluginSample
{
    /// <summary>
    /// MenuPanel.xaml 的交互逻辑
    /// </summary>
    public partial class MenuPanel : UserControl
    {
        private MenuPanelView view;
        private BpDesignView bpView = new BpDesignView();

        public MenuPanel(MenuPanelView menuPanelView)
        {
            InitializeComponent();

            DataContext = view = menuPanelView;
        }

        private void ExpandMenu(int num)
        {
            if (view.Expanded[num] == "*")
            {
                view.Expanded[num] = "0";
                view.Expanded[view.Expanded.Length - 1] = "*";
            }
            else
            {
                view.Expanded[num] = "*";
                for (var i = 0; i < view.Expanded.Length; i++)
                {
                    if (i != num)
                    {
                        view.Expanded[i] = "0";
                    }
                }
            }
            view.Expanded = view.Expanded.ToArray();
        }

        private void Menu1_Click(object sender, RoutedEventArgs e)
        {
            ExpandMenu(0);
        }

        private void Menu2_Click(object sender, RoutedEventArgs e)
        {
            ExpandMenu(1);
        }

        private void ListBoxItem1_1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dsgDlg = new BpDesignDialog(bpView);
            dsgDlg.ShowDialog();
            bpView = dsgDlg.view;
        }
    }
}
