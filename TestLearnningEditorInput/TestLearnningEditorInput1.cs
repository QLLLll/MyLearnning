using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

//[assembly: ExtensionApplication(typeof(TestLearnningEditorInput.TestLearnningEditorInput))]
//[assembly: CommandClass(typeof(TestLearnningEditorInput.TestLearnningEditorInput))]

namespace TestLearnningEditorInput
{
    public class TestLearnningEditorInput1 : IExtensionApplication
    {
        [CommandMethod("Test")]
        public void cmd()
        {
            Application.ShowAlertDialog("234");
        }

        [CommandMethod("Test2", CommandFlags.UsePickSet)]
        public void cmd2()
        {

            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            Editor acEd = acDoc.Editor;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                var blkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                var mdlSpc = acTrans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                //var opt1 = new PromptDoubleOptions("请输入浮点数 [PI/2PI]", "3.14 6.28");

                //var res1 = acEd.GetDouble(opt1);

                //if (res1.Status == PromptStatus.OK)
                //{
                //    Application.ShowAlertDialog(res1.Value.ToString());
                //}
                //else if (res1.Status == PromptStatus.Keyword)
                //{
                //    Application.ShowAlertDialog(res1.StringResult);
                //}

                //var opt2 = new PromptKeywordOptions("请输入关键字 [Key1/Key2/Key3]", "value1 value2 value3");

                //var res2 = acEd.GetKeywords(opt2);

                //if (res2.Status == PromptStatus.OK)
                //{
                //    Application.ShowAlertDialog(res2.StringResult);
                //}

                /*//过滤
                var opt3 = new PromptSelectionOptions();
                var flt = new SelectionFilter(new[]
                {
                    new TypedValue((int) DxfCode.Operator,"<OR"),
                    new TypedValue((int)DxfCode.Start,"LINE"),
                    new TypedValue((int)DxfCode.Start,"CIRCLE"),
                    new TypedValue((int)DxfCode.Operator,"OR>")
                }) ;
                var flt2 = new SelectionFilter(new[]
               {
                    new TypedValue((int) DxfCode.Operator,"<OR"),
                    new TypedValue((int)DxfCode.Start,"LINE"),
                    new TypedValue((int) DxfCode.Operator,"<AND"),
                    new TypedValue((int)DxfCode.Start,"CIRCLE"),
                    new TypedValue((int) DxfCode.Operator,">"),
                    new TypedValue((int)DxfCode.Real ,"1"),
                    new TypedValue((int)DxfCode.Start,"CIRCLE"),
                    new TypedValue((int) DxfCode.Operator,"<"),
                    new TypedValue((int)DxfCode.Real ,"10"),
                    new TypedValue((int) DxfCode.Operator,"AND>"),
                    new TypedValue((int)DxfCode.Operator,"OR>")
                });
                var res3 = acEd.GetSelection(opt3, flt2);

                if (res3.Status == PromptStatus.OK)
                {

                    foreach (ObjectId objId in res3.Value.GetObjectIds())
                    {

                        var ent = acTrans.GetObject(objId, OpenMode.ForWrite) as Entity;

                        ent.ColorIndex = 1;

                    }

                }*/

                //var res4 = acEd.SelectAll();
                //if (res4.Status == PromptStatus.OK)
                //{
                //    acEd.SetImpliedSelection(res4.Value.GetObjectIds());
                //}
                var res5 = acEd.SelectImplied();

                if (res5.Status == PromptStatus.OK)
                {
                    foreach (ObjectId objId in res5.Value.GetObjectIds())
                    {

                        var ent = acTrans.GetObject(objId, OpenMode.ForWrite) as Entity;

                        ent.ColorIndex = 1;

                    }
                }


                acTrans.Commit();
            }

        }

        public void Initialize()
        {
            var pick = Application.GetSystemVariable("PICKFIRST");

            Application.ShowAlertDialog(pick.ToString());

            if (null==pick||!pick.ToString().Equals("1"))
            {
                Application.SetSystemVariable("PICKFIRST", "1");
            }

            Application.ShowAlertDialog(pick.ToString());

        }

        public void Terminate()
        {

        }
        
    }
}
