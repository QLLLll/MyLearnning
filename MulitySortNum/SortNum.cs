using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;

namespace MulitySortNum
{
    public class SortNum
    {
        private Document doc = Application.DocumentManager.MdiActiveDocument;
        private Database db = Application.DocumentManager.MdiActiveDocument.Database;
        private Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

        private string str = "LL";
        public int index = 1;

        [CommandMethod("NumSort")]
        public void NumSort() {

            ObjectId lyId = ObjectId.Null;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {

                var lyBlk = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                var tblBlk = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                LayerTableRecord lyTblRec = null;

                if (lyBlk.Has("编号"))
                {

                    lyTblRec = trans.GetObject(lyBlk["编号"], OpenMode.ForRead) as LayerTableRecord;

                }

                lyId = lyTblRec.Id;



                /* PromptEntityOptions propEntOpts = new PromptEntityOptions("请选择块\n");

                 PromptEntityResult propEntRes = ed.GetEntity(propEntOpts);

                 if (propEntRes.Status == PromptStatus.OK)
                 {

                     ObjectId entId = propEntRes.ObjectId;

                     Entity ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;

                     ed.WriteMessage($"{lyId} {ent.LayerId}");

                 }*/

                PromptSelectionOptions sOpts = new PromptSelectionOptions();


                TypedValue typedValue = new TypedValue((int)DxfCode.LayerName, "编号");


                SelectionFilter sf = new SelectionFilter(new[] { typedValue });
                 

                PromptSelectionResult sRes = ed.GetSelection(sOpts,sf);

                ObjectIdCollection ids = new ObjectIdCollection();

                if (sRes.Status == PromptStatus.OK)
                {

                    sRes.Value.GetObjectIds().ToList().ForEach(id => ids.Add(id));


                }

                foreach (ObjectId id in ids)
                {
                    BlockReference br = trans.GetObject(id, OpenMode.ForRead) as BlockReference;

                    if (br == null)
                    {
                        Application.ShowAlertDialog("br 为空");
                        trans.Commit();
                        return;
                    }

                    Point3d pt = br.Position;

                    DBText dbText = new DBText();

                    dbText.TextString = str + "_" + index++;

                    dbText.Position = new Point3d(pt.X, pt.Y - 100, pt.Z);

                    DBHelper.ToSpace(dbText);



                }







                trans.Commit();

            }



        }



    }
}
