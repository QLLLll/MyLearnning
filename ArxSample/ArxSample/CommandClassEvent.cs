using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wds = System.Windows;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using AcGi = Autodesk.AutoCAD.GraphicsInterface;

using ArxDotNetLesson;

namespace ArxSample
{
    public partial class CommandClass : IExtensionApplication
    {
        private static List<ObjectId> appendedIds = new List<ObjectId>();

        public void RegisterAppEvents()
        {
            Application.BeginQuit += (o, e) =>
            {
                if (Wds.MessageBox.Show("是否退出AutoCAD？", "正在退出",
                    Wds.MessageBoxButton.YesNo, Wds.MessageBoxImage.Question,
                    Wds.MessageBoxResult.No) == Wds.MessageBoxResult.No)
                {
                    e.IsVetoed = true;
                }
            };

            Application.DocumentManager.DocumentCreated += (o, e) =>
            {
                using (var lockDoc = e.Document.LockDocument())
                {
                    var editor = e.Document.Editor;
                    editor.WriteMessage($"\n已创建文档[{e.Document.Name}]。");

                    RegisterDocEvents(e.Document);
                }
            };

            Application.DocumentManager.DocumentLockModeChanged += (o, e) =>
            {
                if (e.GlobalCommandName.ToLower() == "#properties")
                {
                    var editor = e.Document.Editor;
                    var sel = editor.SelectImplied();
                    if (sel.Value.Count == 1)
                    {
                        var xdata = sel.Value.GetObjectIds()[0].GetXData("myapp");
                        if (xdata != null && xdata.Length > 0)
                        {
                            e.Veto();
                            Application.ShowAlertDialog($"该特殊实体创建于{xdata[0].Value}");
                        }
                    }
                }

                //e.Veto(); 
            };
        }

        public void RegisterDocEvents(Document doc)
        {
            using (var lockDoc = doc.LockDocument())
            {
                // For database events
                var db = doc.Database;
                // For editor events
                var ed = doc.Editor;

                // Document events
                doc.BeginDocumentClose += (o, e) =>
                {
                    if (Wds.MessageBox.Show("是否关闭文档？", "正在关闭",
                        Wds.MessageBoxButton.YesNo, Wds.MessageBoxImage.Question,
                        Wds.MessageBoxResult.No) == Wds.MessageBoxResult.No)
                    {
                        e.Veto();
                    }
                };

                doc.CommandWillStart += (o, e) =>
                {
                    ed.WriteMessage($"\n开始执行[{e.GlobalCommandName}]");

                    appendedIds.Clear();
                };

                doc.CommandEnded += (o, e) =>
                {
                    ed.WriteMessage($"\n结束执行[{e.GlobalCommandName}]");

                    if (appendedIds.Count > 0)
                    {
                        appendedIds.ForEach(id =>
                        {
                            id.AttachXData("myapp", new[] {
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString,
                                               DateTime.Now.ToString())
                            });
                        });
                    }
                };

                doc.CommandCancelled += (o, e) =>
                {
                    ed.WriteMessage($"\n取消执行[{e.GlobalCommandName}]");

                    if (appendedIds.Count > 0)
                    {
                        appendedIds.ForEach(id =>
                        {
                            id.AttachXData("myapp", new[] {
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString,
                                               DateTime.Now.ToString())
                            });
                        });
                    }
                };

                // Database events
                db.ObjectAppended += (o, e) =>
                {
                    if (e.DBObject is Line)
                    {
                        appendedIds.Add(e.DBObject.Id);
                    }
                };
            }
        }

        public void Initialize()
        {
            //RegisterAppEvents();

            //foreach (Document doc in Application.DocumentManager)
            //{
            //    RegisterDocEvents(doc);
            //}

            JsonInitialize();
        }

        public void Terminate()
        {
        }
    }
}
