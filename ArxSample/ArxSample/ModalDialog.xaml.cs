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

using AcAp = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;


namespace ArxSample
{
    /// <summary>
    /// ModalDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ModalDialog : Window
    {
        public ModalDialog()
        {
            InitializeComponent();
        }

        [CommandMethod("testme")]
        public void TestMe()
        {
            this.ShowDialog();
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = AcAp.Application.DocumentManager.MdiActiveDocument.Editor;
            using (var interacting = editor.StartUserInteraction(this))
            {
                var res = editor.GetPoint("请选择坐标点：");
                if (res.Status == PromptStatus.OK)
                {
                    PointText.Text = res.Value.ToString();
                }

                interacting.End();
                this.Focus();
            }
        }
    }
}
