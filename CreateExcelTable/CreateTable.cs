using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            double[] vertices = new double[3];
            vertices[0] = 0;
            vertices[1] = 0;
            vertices[2] = 0;


            myTable = doc.ActiveLayout.Block.AddTable(vertices, 4, 2, 3, 10);
            myTable.SetCellTextHeight(0, 0, 2);

            myTable.SetTextHeight(5, 1.5);
            myTable.SetColumnWidth(0, 5);
            myTable.SetColumnWidth(1, 50);

            PromptEntityOptions propEnt = new PromptEntityOptions("请选择实体\n");

            PromptEntityResult propRes = AcadEd.GetEntity(propEnt);
            AcRowType acRowType = new AcRowType();
            if (propRes.Status == PromptStatus.OK)
            {
                try
                {


                    // myTable.SetBlockTableRecordId(3, 0, propRes.ObjectId.OldIdPtr.ToInt64(), true);

                    ObjectId oId = propRes.ObjectId;
                    AcadEd.WriteMessage(oId.IsValid.ToString());

                  
                    BlockReference br;
                    using (var trans = AcadDb.TransactionManager.StartTransaction())
                    { 

                        br = trans.GetObject(oId, OpenMode.ForRead) as BlockReference;

                        if (br == null)
                        {
                            Application.ShowAlertDialog("请先把要填充的块变为块定义，\n并指定块定义的基点为块的中心点。");

                            trans.Commit();

                            return;
                        }

                        
                        trans.Commit();
                    }
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
