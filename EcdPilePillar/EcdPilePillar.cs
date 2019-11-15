using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;

namespace EcdPilePillar
{
    public class EcdPilePillar
    {


        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("ECDZhuangzhu")]
        public void CreateZhuangZhu()
        {


            var propDbOps = new PromptDoubleOptions("请输入半径:\n");

            propDbOps.AllowNegative = false;
            propDbOps.AllowZero = false;
            propDbOps.AllowNone = false;

            var propDbRes = Ed.GetDouble(propDbOps);

            if (propDbRes.Status != PromptStatus.OK) return;


            double r = propDbRes.Value;

            while (true)
            {

                var propPtOps = new PromptPointOptions("请输入桩柱插入点：\n");

                var propPtRes = Ed.GetPoint(propPtOps);

                if (propPtRes.Status != PromptStatus.OK) return;

                var centerPt = propPtRes.Value;

                Circle c = new Circle(centerPt, Vector3d.ZAxis, r);

                c.Color = Color.FromColor(System.Drawing.Color.Yellow);

                Point3d ptHStart = centerPt - Vector3d.XAxis * 200;
                Point3d ptHEnd = centerPt + Vector3d.XAxis * 200;

                Line hLine = new Line(ptHStart, ptHEnd);


                Point3d ptZStart = centerPt - Vector3d.YAxis * 200;
                Point3d ptZEnd = centerPt + Vector3d.YAxis * 200;

                Line zLine = new Line(ptZStart, ptZEnd);


                hLine.LineWeight = LineWeight.LineWeight030;
                zLine.LineWeight = LineWeight.LineWeight030;


                Point3d ptYx1 = centerPt + Vector3d.XAxis * (r + 800);

                ptYx1 = ptYx1.RotateBy(Math.PI * (70.0 / 180), Vector3d.ZAxis, centerPt);

                Point3d ptYx2 = ptYx1 + Vector3d.XAxis * 700;

                Polyline plYx = new Polyline();

                plYx.Color = Color.FromColor(System.Drawing.Color.Green);

                plYx.AddVertexAt(plYx.NumberOfVertices, new Point2d(centerPt.X, centerPt.Y), 0, 0, 0);
                plYx.AddVertexAt(plYx.NumberOfVertices, new Point2d(ptYx1.X, ptYx1.Y), 0, 0, 0);
                plYx.AddVertexAt(plYx.NumberOfVertices, new Point2d(ptYx2.X, ptYx2.Y), 0, 0, 0);


                DBText dbText = new DBText();

                dbText.TextString = "ZH1";

                ObjectId styleId = ObjectId.Null;

                dbText.HorizontalMode = TextHorizontalMode.TextMid;

                dbText.Height = 135;
                dbText.Color = Color.FromColor(System.Drawing.Color.Red);

                //dbText.AdjustAlignment(Db);

                dbText.AlignmentPoint = new Point3d((ptYx2.X + ptYx1.X) / 2, (ptYx2.Y + ptYx1.Y) / 2 + 100, 0);

                List<Entity> listEnti = new List<Entity>();

                listEnti.Add(c);
                listEnti.Add(hLine);
                listEnti.Add(zLine);
                listEnti.Add(plYx);
                listEnti.Add(dbText);


                ObjectId layer1 = AddLayer(Db, "ECD桩柱01", System.Drawing.Color.White);
                ObjectId layer2 = AddLayer(Db, "ECD桩柱02", System.Drawing.Color.Yellow);
                ObjectId layer3 = AddLayer(Db, "ECD桩柱03", System.Drawing.Color.White);

                hLine.LayerId = layer1;
                zLine.LayerId = layer1;
                c.LayerId = layer2;
                plYx.LayerId = layer3;
                dbText.LayerId = layer3;

                AddBlock(Db, listEnti);
            }
        }
        public void AddBlock(Database db, List<Entity> entis)
        {

            var space = db.CurrentSpaceId;

            using (var trans = db.TransactionManager.StartTransaction())
            {

                var tblBlk = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;



                BlockTableRecord rec = trans.GetObject(space, OpenMode.ForWrite) as BlockTableRecord;



                entis.ForEach(enti => { rec.AppendEntity(enti); trans.AddNewlyCreatedDBObject(enti, true); });

                trans.Commit();

            }


        }

        public ObjectId AddLayer(Database db, string layerName, System.Drawing.Color color)
        {
            ObjectId oId = ObjectId.Null;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var lyerTbl = db.LayerTableId.GetObject(OpenMode.ForWrite) as LayerTable;

                if (lyerTbl.Has(layerName))
                {
                    trans.Commit();
                    return lyerTbl[layerName];
                }
                var lyerTblRec = new LayerTableRecord();
                lyerTblRec.Name = layerName;

                lyerTblRec.Color = Color.FromColor(color);

                lyerTbl.Add(lyerTblRec);
                trans.AddNewlyCreatedDBObject(lyerTblRec, true);

                lyerTbl.DowngradeOpen();

                trans.Commit();

                return lyerTbl[layerName];
            }
        }

    }
}
