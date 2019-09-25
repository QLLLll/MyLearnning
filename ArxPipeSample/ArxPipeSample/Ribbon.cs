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

namespace ArxPipeSample
{
    /// <summary>
    /// 带式菜单。
    /// </summary>
    public static class Ribbon
    {
        /// <summary>
        /// 创建带式菜单。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void initRibbon(object sender, RibbonItemEventArgs e)
        {
            var ribCtl = ComponentManager.Ribbon;

            var ribBtn1 = new RibbonButton();
            ribBtn1.Text = "创建管道";
            ribBtn1.ShowText = true;
            //ribBtn.Image = ??;
            //ribBtn.ShowImage = true;
            ribBtn1.CommandParameter = $"{PipeSample.kCmdNameMakePipeType1} ";
            ribBtn1.CommandHandler = new CommandHandler();

            var ribBtn2 = new RibbonButton();
            ribBtn2.Text = "创建阀门";
            ribBtn2.ShowText = true;
            //ribBtn.Image = ??;
            //ribBtn.ShowImage = true;
            ribBtn2.CommandParameter = $"{PipeSample.kCmdNameMakeValveType1} ";
            ribBtn2.CommandHandler = new CommandHandler();

            var ribPnlSrc = new RibbonPanelSource();
            ribPnlSrc.Title = "管道设计";
            ribPnlSrc.Items.Add(ribBtn1);
            ribPnlSrc.Items.Add(ribBtn2);

            var ribPnl = new RibbonPanel();
            ribPnl.Source = ribPnlSrc;

            var ribTab = new RibbonTab();
            ribCtl.Tabs.Add(ribTab);
            ribTab.Title = "管道";
            ribTab.Id = "管道";
            ribTab.IsActive = true;
            ribTab.Panels.Add(ribPnl);

            ComponentManager.ItemInitialized -= initRibbon;
        }

        /// <summary>
        /// 菜单按钮执行处理。
        /// </summary>
        private class CommandHandler : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                var ribBtn = parameter as RibbonButton;

                Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
                    (string)ribBtn.CommandParameter, true, false, true);
            }
        }
    }
}
