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

        [CommandMethod("NumSort0")]
        public void NumSort()
        {

            ObjectId lyId = ObjectId.Null;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {

                var lyTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                //var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                LayerTableRecord lyTblRec = null;

                if (lyTbl.Has("编号"))
                {

                    lyTblRec = trans.GetObject(lyTbl["编号"], OpenMode.ForRead) as LayerTableRecord;

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


                PromptSelectionResult sRes = ed.GetSelection(sOpts, sf);

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

        [CommandMethod("NumSort1")]
        public void NumSort1()
        {
            index = 1;

            var propEnt = new PromptEntityOptions("请选择要编号的一个块\n");

            var propRes = ed.GetEntity(propEnt);

            if (propRes.Status != PromptStatus.OK)
            {
                return;
            }

            var oId = propRes.ObjectId;

            ObjectIdCollection objIds = null;
            List<DBText> listDBText = new List<DBText>();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
               var blkRef = trans.GetObject(oId, OpenMode.ForRead) as BlockReference;



                if (blkRef == null)
                {
                    Application.ShowAlertDialog("请选择块定义");
                    return;
                }

                var recId = blkRef.BlockTableRecord;

                var blkTblRec = trans.GetObject(recId, OpenMode.ForRead) as BlockTableRecord;

                objIds = blkTblRec.GetBlockReferenceIds(true, false);

                

                var txtStlTbl = trans.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

                var txtstyleId = txtStlTbl["Standard"];

                List<BlockReference> listBr = new List<BlockReference>();

                foreach (ObjectId objectId in objIds)
                {
                   var blkTempRef = trans.GetObject(objectId, OpenMode.ForRead) as BlockReference;

                    listBr.Add(blkTempRef);

                    
                }

                listBr.OrderByDescending(b => b.Position.Y).ToList().ForEach(blkTempRef =>
                {

                    DBText dbText = new DBText();
                    dbText.TextString = str + "_" + index++;
                    dbText.TextStyleId = txtstyleId;

                    var pointMin = blkTempRef.Bounds.Value.MinPoint;
                    var pointMax = blkTempRef.Bounds.Value.MaxPoint;
                    dbText.HorizontalMode = TextHorizontalMode.TextMid;
                    dbText.AlignmentPoint = pointMin + Vector3d.YAxis * 2 + Vector3d.XAxis * Math.Abs(pointMax.X - pointMin.X) / 2;

                    listDBText.Add(dbText);

                });

                trans.Commit();
            }

            listDBText.ToSpace();

        }
    }
}
