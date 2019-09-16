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

namespace HatchFill
{
    public class Class1
    {
        [CommandMethod("FillPL")]
        public void Fill()
        {

            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline poly0 = new Polyline();

                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis, 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis+Vector2d.YAxis, 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis*0.5+Vector2d.YAxis, Math.Tan(0.25 * -1 * Math.PI), 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis*-0.5+Vector2d.YAxis, 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis*-1 + Vector2d.YAxis, 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis*-1, 0, 0, 0);
                poly0.Closed = true;

               ObjectId poly0Id= acBlkTblRec.AppendEntity(poly0);
                acTrans.AddNewlyCreatedDBObject(poly0,true);

                
                Polyline poly1 = new Polyline();

                poly1.AddVertexAt(poly1.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.4 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
                poly1.AddVertexAt(poly1.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.6 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
                poly1.Closed = true;

                ObjectId poly1Id=acBlkTblRec.AppendEntity(poly1);
                acTrans.AddNewlyCreatedDBObject(poly1, true);

                Polyline poly2 = new Polyline();

                poly2.AddVertexAt(poly2.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.4 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
                poly2.AddVertexAt(poly2.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.6 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
                poly2.Closed = true;

                ObjectId poly2Id = acBlkTblRec.AppendEntity(poly2);
                acTrans.AddNewlyCreatedDBObject(poly2, true);
                
                Hatch acHatch = new Hatch();

               acBlkTblRec.AppendEntity(acHatch);
                acTrans.AddNewlyCreatedDBObject(acHatch, true);

                acHatch.PatternScale = 0.01;
                acHatch.SetHatchPattern(HatchPatternType.PreDefined, "BOX");
                acHatch.Associative = true;
                //acHatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { poly0Id });
                //acHatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { poly1Id });
                //acHatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { poly2Id });

                HatchLoop hatchLoop = new HatchLoop(HatchLoopTypes.Polyline);

                foreach (var i in Enumerable.Range(0,poly0.NumberOfVertices))
                {

                    hatchLoop.Polyline.Add(new BulgeVertex(poly0.GetPoint2dAt(i), poly0.GetBulgeAt(i)));

                }

                hatchLoop.Polyline.Add(new BulgeVertex(poly0.GetPoint2dAt(0), poly0.GetBulgeAt(0)));

                HatchLoop hatchLoop1=new HatchLoop(HatchLoopTypes.Polyline);

                for (int i = 0; i < poly1.NumberOfVertices; i++)
                {
                    hatchLoop1.Polyline.Add(new BulgeVertex(poly1.GetPoint2dAt(i), poly1.GetBulgeAt(i)));
                }
                hatchLoop1.Polyline.Add(new BulgeVertex(poly1.GetPoint2dAt(0), poly1.GetBulgeAt(0)));


                HatchLoop hatchLoop2 = new HatchLoop(HatchLoopTypes.Polyline);
                for (int i = 0; i < poly2.NumberOfVertices; i++)
                {
                    hatchLoop2.Polyline.Add(new BulgeVertex(poly2.GetPoint2dAt(i), poly2.GetBulgeAt(i)));
                }
                hatchLoop2.Polyline.Add(new BulgeVertex(poly2.GetPoint2dAt(0), poly2.GetBulgeAt(0)));


                acHatch.AppendLoop(hatchLoop);
                acHatch.AppendLoop(hatchLoop1);
                acHatch.AppendLoop(hatchLoop2);

                acTrans.Commit();
            }

        }





    }
}
