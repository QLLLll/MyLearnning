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

using  AcAp=Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;


namespace LearnningWindows
{
    /// <summary>
    /// MyPanel1.xaml 的交互逻辑
    /// </summary>
    public partial class MyPanel1 : UserControl
    {
        public MyPanel1()
        {
            InitializeComponent();
        }

        public void UpdatePanel(ObjectId id)
        {

            if (id.IsValid)
            {
                using(var trans = AcAp.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
                {

                    var obj = trans.GetObject(id, OpenMode.ForRead);

                    var exDicId = obj.ExtensionDictionary;

                    if (exDicId.IsValid)
                    {

                        var exDic = trans.GetObject(exDicId, OpenMode.ForRead) as DBDictionary;

                        if (exDic.GetAt("MyWallType").IsValid)
                        {
                            MyTextBlock.Text = "My Wall Type";
                            return;
                        }
                    }
                }
            }
            else
            {
                MyTextBlock.Text = "No Support Type";
            }


        }
    }
}
