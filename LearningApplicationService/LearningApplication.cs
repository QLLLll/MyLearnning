using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

[assembly:ExtensionApplication(typeof(LearningApplicationService.LearningApplication))]
[assembly:CommandClass(typeof(LearningApplicationService.LearningApplication))]

namespace LearningApplicationService
{
    public class LearningApplication:IExtensionApplication
    {
        public void Initialize()
        {
            
        }

        public void Terminate()
        {
            
        }

        [CommandMethod("Test")]
        public void Test()
        {

            DocumentCollection acDocColl = Application.DocumentManager;

            Editor acEd = Application.DocumentManager.MdiActiveDocument.Editor;

            Window wind = Application.MainWindow;

            AcadApplication acadApplication = Application.AcadApplication as AcadApplication;

            AcadMenuBar acadMenuBa = Application.MenuBar as AcadMenuBar;

            ContextMenuExtension acCtxMenuExten = new ContextMenuExtension();


            /*Document acNewDoc = acDocColl.Add("acac.dwg");
            Document acNewDoc2 = acDocColl.Add("aca.dwg");*/

            /* foreach (Document acDoc in acDocColl)
             {

                 acEd.WriteMessage($"\n{acDoc.Name}");

             }*/



            /*wind.WindowState = Window.State.Minimized;

            wind.Text = "学习学习再学习";
            wind.SetLocation(new System.Drawing.Point(200, 200));
            wind.SetSize(new System.Drawing.Size(500, 500));*/

            /*Application.ShowAlertDialog($"{acadApplication.Width},{acadApplication.Height}");*/


            /*foreach ( AcadPopupMenu pm in acadMenuBa)
            {

                acEd.WriteMessage($"\n{pm.Name}");

            }*/

            /*AcadPopupMenu pm = acadMenuBa.Item(0);
                pm.AddMenuItem(0, "画直线", "Line");*/

            acCtxMenuExten.Title = "自定义菜单";
            MenuItem acNewMenuItem = new MenuItem("创建新文档");

            acNewMenuItem.Click += (o, e) => {

                Document acNewDoc = acDocColl.Add("acac.dwg");

            };

            acCtxMenuExten.MenuItems.Add(acNewMenuItem);
            /*Application.AddDefaultContextMenuExtension(acCtxMenuExten);*/

            /*Application.AddObjectContextMenuExtension(RXObject.GetClass(typeof(Line)), acCtxMenuExten);*/



           


        }

    }
}
