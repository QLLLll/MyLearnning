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

namespace MyDimention
{
    public class LearnningDimension
    {
        [CommandMethod("TestDim")]
        public void TestDimension()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline poly0 = new Polyline();

                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis, 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis + Vector2d.YAxis*0.8, 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.8 + Vector2d.YAxis , 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.5 + Vector2d.YAxis, Math.Tan(0.25 * -1 * Math.PI), 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.5 + Vector2d.YAxis, 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -1 + Vector2d.YAxis, 0, 0, 0);
                poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -1, 0, 0, 0);
                poly0.Closed = true;

                ObjectId poly0Id = acBlkTblRec.AppendEntity(poly0);
                acTrans.AddNewlyCreatedDBObject(poly0, true);


                Polyline poly1 = new Polyline();

                poly1.AddVertexAt(poly1.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.4 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
                poly1.AddVertexAt(poly1.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.6 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
                poly1.Closed = true;

                ObjectId poly1Id = acBlkTblRec.AppendEntity(poly1);
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
                acHatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { poly0Id });
                acHatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { poly1Id });
                acHatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { poly2Id });

                //对齐(框线)标注
                AlignedDimension acAligDimens = new AlignedDimension(
                   Point3d.Origin - Vector3d.XAxis,
                   Point3d.Origin + Vector3d.XAxis,
                   Point3d.Origin + Vector3d.YAxis * -0.25,
                   null,
                   ObjectId.Null
                    );

                acBlkTblRec.AppendEntity(acAligDimens);
                acTrans.AddNewlyCreatedDBObject(acAligDimens, true);

                RotatedDimension acRoDimens = new RotatedDimension(
                    0.0,
                    Point3d.Origin + Vector3d.XAxis * 0.8 + Vector3d.YAxis,
                    Point3d.Origin + Vector3d.XAxis + Vector3d.YAxis * 0.8,
                    Point3d.Origin + Vector3d.XAxis * 0.8 + Vector3d.YAxis * 1.5,
                    null,
                    ObjectId.Null
                    );

                acBlkTblRec.AppendEntity(acRoDimens);
                acTrans.AddNewlyCreatedDBObject(acRoDimens, true);





                acTrans.Commit();
            }

        }
    }
}
