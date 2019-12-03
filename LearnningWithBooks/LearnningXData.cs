using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;

namespace LearnningWithBooks
{
    public class LearnningXData
    {
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;
        string appName = "EMPLOYEE";

        [CommandMethod("LNAddXData")]
        public void AddXData()
        {

            var propEnt = new PromptEntityOptions("请选择多行文本");

            propEnt.SetRejectMessage("你选择的不是多行文本");

            propEnt.AddAllowedClass(typeof(MText), true);

            var propEntRes = Ed.GetEntity(propEnt);

            if (propEntRes.Status != PromptStatus.OK) return;

            var id = propEntRes.ObjectId;

            using(var trans = Db.TransactionManager.StartTransaction())
            {

                

                using (var trans2 = Db.TransactionManager.StartTransaction()){

                    var regRecord = new RegAppTableRecord() { Name = appName };

                    var regTbl = trans2.GetObject(Db.RegAppTableId, OpenMode.ForWrite) as RegAppTable;
                    if (!regTbl.Has(appName))
                    {
                        regTbl.Add(regRecord);

                        trans2.AddNewlyCreatedDBObject(regRecord, true);
                    }
                    trans2.Commit();
                }

                var mText = trans.GetObject(id, OpenMode.ForWrite) as MText;

                List<TypedValue> listTV = new List<TypedValue>();
                listTV.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                listTV.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, 1002));
                listTV.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, "董事长"));

                ResultBuffer rb = new ResultBuffer(listTV.ToArray());

                mText.XData = rb;

                mText.DowngradeOpen();

                trans.Commit();

            }
            Application.ShowAlertDialog("OK");

        }

        [CommandMethod("LNModXData")]
        public void ModifyXData()
        {

            //提示用户选择一个多行文本
            PromptEntityOptions opt = new PromptEntityOptions("\n请选择多行文本");
            opt.SetRejectMessage("\n您选择的不是多行文本，请重新选择");
            opt.AddAllowedClass(typeof(MText), true);
            PromptEntityResult entResult = Ed.GetEntity(opt);
            if (entResult.Status != PromptStatus.OK) return;
            ObjectId id = entResult.ObjectId;//用户选择的多行文本的ObjectId
            using (Transaction trans = Db.TransactionManager.StartTransaction())
            {
                //如果扩展数据项（员工编号）为1002，则将其修改为1001


                var dbObject = trans.GetObject(id, OpenMode.ForWrite);

                ResultBuffer rb = dbObject.GetXDataForApplication(appName);

                TypedValue[] arrTV = rb.AsArray();

                for (int i = 0; i < arrTV.Length; i++)
                {
                    var data = arrTV[i];

                    if(data.TypeCode==(short)DxfCode.ExtendedDataInteger32&& (int)data.Value == 1002)
                    {
                        arrTV[i] = new TypedValue(data.TypeCode, 1001);
                        break;
                    }
                }
                dbObject.XData = new ResultBuffer(arrTV);

                dbObject.DowngradeOpen();

                trans.Commit();
            }
            Application.ShowAlertDialog("OK");
        }

        [CommandMethod("DelX")]
        public void DelX()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            //提示用户选择一个多行文本
            PromptEntityOptions opt = new PromptEntityOptions("\n请选择多行文本");
            opt.SetRejectMessage("\n您选择的不是多行文本，请重新选择");
            opt.AddAllowedClass(typeof(MText), true);
            PromptEntityResult entResult = Ed.GetEntity(opt);
            if (entResult.Status != PromptStatus.OK) return;
            ObjectId id = entResult.ObjectId;//用户选择的多行文本的ObjectId
            using (Transaction trans = Db.TransactionManager.StartTransaction())
            {
                id.RemoveXData("EMPLOYEE"); // 删除EMPLOYEE扩展数据
                trans.Commit();
            }
        }

        [CommandMethod("LNMonitorXData")]
        public void MonitorXData()
        {
            Ed.PointMonitor += Ed_PointMonitor;
        }
        [CommandMethod("LNStopMonitorXData")]
        public void StopMonitorXData()
        {
            Ed.PointMonitor -= Ed_PointMonitor;
        }

        private void Ed_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            Editor ed = (Editor)sender;

            FullSubentityPath[] paths = e.Context.GetPickedEntities();

            string toolTipText = string.Empty;

            if (paths.Length > 0)
            {
                using(var trans = Db.TransactionManager.StartTransaction())
                {

                    MText mText = trans.GetObject(paths[0].GetObjectIds()[0], OpenMode.ForRead) as MText;

                    

                    if (mText != null)
                    {
                        TypedValue[] arrTV = mText.GetXDataForApplication(appName)?.AsArray();
                       if(arrTV!=null)
                        toolTipText += "员工编号:" + arrTV[1].Value + "\n职位:" + arrTV[2].Value.ToString();

                    }


                    trans.Commit();

                }
                if (!String.IsNullOrEmpty(toolTipText))
                {

                    e.AppendToolTipText(toolTipText);

                    string s = e.Context.ToolTipText;

                }

            }


        }
    }
}
