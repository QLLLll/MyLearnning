using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.Windows;

[assembly:CommandClass(typeof( example2.MyRibbon))]
namespace example2
{
    public class MyRibbon
    {

        public static  void MakeRabbion()
        {
            var ribCtl = ComponentManager.Ribbon;

            var ribon1 = new RibbonButton();

            ribon1.Text = "管道线";
            ribon1.ShowText = true;
            ribon1.CommandParameter = "cmd1 ";
            ribon1.CommandHandler = new CommandHandler();

            var ribon2 = new RibbonButton();

            ribon2.Text = "阀";
            ribon2.ShowText = true;
            ribon2.CommandParameter = "cmd2 ";
            ribon2.CommandHandler = new CommandHandler();

            var ribSrc = new RibbonPanelSource();

            ribSrc.Title = "管道设计";
            ribSrc.Items.Add(ribon1);
            ribSrc.Items.Add(ribon2);

            var ribPanel = new RibbonPanel();
            ribPanel.Source = ribSrc;

            var ribTab = new RibbonTab();

            ribCtl.Tabs.Add(ribTab);
            ribTab.Title = "管道";
            ribTab.Id = "管道";
            ribTab.IsActive = true;
            ribTab.Panels.Add(ribPanel);

        }

    }

    public class CommandHandler : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var ribn = parameter as RibbonButton;

            Application.DocumentManager.MdiActiveDocument
                .SendStringToExecute(ribn.CommandParameter.ToString(), true, false, true);
        }
    }


}
