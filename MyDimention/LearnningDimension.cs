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

using DotNetARX;

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


        [CommandMethod("Dimtest")]
        public void Dimtest()
        {


            //创建要标注的图形 
            Line line1 = new Line(new Point3d(30, 20, 0), new Point3d(120, 20, 0));
            Line line2 = new Line(new Point3d(120, 20, 0), new Point3d(120, 40, 0));
            Line line3 = new Line(new Point3d(120, 40, 0), new Point3d(90, 80, 0));
            Line line4 = new Line(new Point3d(90, 80, 0), new Point3d(30, 80, 0));
            Arc arc = new Arc(new Point3d(30, 50, 0), 30, Math.PI / 2, Math.PI * 3 / 2);
            Circle cir1 = new Circle(new Point3d(30, 50, 0), Vector3d.ZAxis, 15);
            Circle cir2 = new Circle(new Point3d(70, 50, 0), Vector3d.ZAxis, 10);

            Entity[] entities = new Entity[] { line1, line2, line3, line4, arc, cir1, cir2 };

            List<Dimension> dims = new List<Dimension>();

            RotatedDimension dimRotated1 = new RotatedDimension();

            dimRotated1.XLine1Point = line1.StartPoint;
            dimRotated1.XLine2Point = line1.EndPoint;
            dimRotated1.DimLinePoint = GeTools.MidPoint(line1.StartPoint, line1.EndPoint)
                .PolarPoint(-Math.PI / 2, 10);
            dimRotated1.DimensionText = "<>mm";
            dims.Add(dimRotated1);

            //转角标注(垂直)
            RotatedDimension dimRotated2 = new RotatedDimension();
            dimRotated2.Rotation = Math.PI / 2;
            dimRotated2.XLine1Point = line2.StartPoint;
            dimRotated2.XLine2Point = line2.EndPoint;
            dimRotated2.DimLinePoint = GeTools.MidPoint(
                line2.StartPoint, line2.EndPoint)
                .PolarPoint(0, 10);
            dims.Add(dimRotated2);

            //转角标注(尺寸公差标注)
            RotatedDimension dimRotated3 = new RotatedDimension();
            dimRotated3.XLine1Point = line4.StartPoint;
            dimRotated3.XLine2Point = line4.EndPoint;
            dimRotated3.DimLinePoint = GeTools.MidPoint(
                line4.StartPoint, line4.EndPoint).PolarPoint(
                Math.PI / 2, 10);
            dimRotated3.DimensionText = TextTools.StackText(
                "<>", "+0.026", "-0.025", StackType.Tolerance, 0.7);
            dims.Add(dimRotated3);


            entities.ToSpace();

        }
    }
}
