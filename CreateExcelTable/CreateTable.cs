using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Interop.Common;
using Autodesk.AutoCAD.Interop;
using System.Runtime.InteropServices;

namespace CreateExcelTable
{
    public class CreateTable
    {

        Document AcadDoc = Application.DocumentManager.MdiActiveDocument;
        Editor AcadEd = Application.DocumentManager.MdiActiveDocument.Editor;
        Database AcadDb = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("ECDCreate")]
        public void Create()
        {
            AcadApplication acadApp = null;
            AcadDocument doc = null;
            AcadTable myTable = null;

            acadApp = (AcadApplication)Marshal.GetActiveObject("AutoCAD.Application");
            doc = acadApp.ActiveDocument;

            PromptPointOptions ppOps = new PromptPointOptions("请选择表格插入位置\n");

            PromptPointResult ppRes = AcadEd.GetPoint(ppOps);

            double[] vertices = new double[3];
            vertices[0] = 0;
            vertices[1] = 0;
            vertices[2] = 0;

            if (ppRes.Status == PromptStatus.OK)
            {

                vertices[0] = ppRes.Value[0];
                vertices[1] = ppRes.Value[1];
                vertices[2] = ppRes.Value[2];

            }
            AcRowType acRowType = new AcRowType();
            /*acUnknownRow = 0,
              acDataRow = 1,
              acTitleRow = 2,
              acHeaderRow = 4*/

            myTable = doc.ActiveLayout.Block.AddTable(vertices, 4, 2, 3, 10);
            //设置文字高度
            myTable.SetTextHeight(1, 0.5);
            myTable.SetTextHeight(2, 1.5);
            myTable.SetTextHeight(4, 1);
            //合并单元格
            myTable.MergeCells(1, 2, 0, 0);
            //设置列宽
            myTable.SetColumnWidth(0, 5);
            myTable.SetColumnWidth(1, 25);
            //插入数据
            myTable.SetText(0, 0, "我的表格测试");
            myTable.SetText(1, 0, "Data1");
            myTable.SetText(1, 1, "这是一条数据");
            myTable.SetText(2, 1, "这是一条测试数据");
            myTable.SetText(3, 1, "左边是个块定义");
             
            //设置文字颜色            
            AcadAcCmColor color = new AcadAcCmColor();
            color.ColorIndex = AcColor.acYellow;

            myTable.SetContentColor(2, color);

            //设置单元格中文字颜色
            AcadAcCmColor color2 = new AcadAcCmColor();
            color2.ColorIndex = AcColor.acGreen;

            myTable.SetContentColor2(3, 1, 0, color2);

            //设置单元格对其方式
            myTable.SetAlignment(1, AcCellAlignment.acMiddleCenter);

            PromptEntityOptions propEnt = new PromptEntityOptions("请选择实体\n");

            PromptEntityResult propRes = AcadEd.GetEntity(propEnt);
            
            if (propRes.Status == PromptStatus.OK)
            {
                try
                {

                    //错误
                    // myTable.SetBlockTableRecordId(3, 0, propRes.ObjectId.OldIdPtr.ToInt64(), true);

                    ObjectId oId = propRes.ObjectId;
                    AcadEd.WriteMessage(oId.IsValid.ToString());

                    BlockReference br;
                    using (var trans = AcadDb.TransactionManager.StartTransaction())
                     { 

                         br = trans.GetObject(oId, OpenMode.ForRead) as BlockReference;

                         if (br == null)
                         {
                             Application.ShowAlertDialog("请选择块定义");

                             trans.Commit();

                             return;
                         }


                         trans.Commit();
                     }

                    //错误
                    //br = (BlockReference)oId.GetObject(OpenMode.ForRead);

                    //设置单元格块引用
                    myTable.SetBlockTableRecordId(3, 0, br.BlockTableRecord.OldIdPtr.ToInt64(), true);

                }
                catch (System.Exception e)
                {

                    AcadEd.WriteMessage(e.ToString());
                }
            }
        }
    }
}
