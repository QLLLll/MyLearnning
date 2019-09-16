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

namespace LearnningMatrix3d
{
    public class LearnningMatrix3d
    {
        [CommandMethod("TestMT")]
        public void TestMatrix3d()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            Editor acEd = acDoc.Editor;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                for (int i = 0; i < 8; i++)
                {

                    Entity[] entities = GetEntities();

                    Matrix3d matrx1 = Matrix3d.Scaling(0.5, Point3d.Origin);
                    Matrix3d matrx2=Matrix3d.Displacement(Point3d.Origin+Vector3d.XAxis*3*Math.Cos(Math.PI*0.25*i)+
                        Vector3d.YAxis*3*Math.Sin(Math.PI*0.25*i)-Point3d.Origin);
                    Matrix3d matrx3 = Matrix3d.Rotation(Math.PI * 0.5 + i * Math.PI * 0.25, Vector3d.ZAxis, Point3d.Origin);


                    foreach (var item in entities)
                    {

                      Entity ent=  item.GetTransformedCopy(matrx2*matrx3 * matrx1);

                        acBlkTblRec.AppendEntity(ent);
                        acTrans.AddNewlyCreatedDBObject(ent, true);

                    }

                }

                acTrans.Commit();



                //acBlkTblRec.AppendEntity(acPolyline);
                //acTrans.AddNewlyCreatedDBObject(acPolyline, true);

                //acTrans.Commit();
            }


        }


        public Entity[] GetEntities()
        {
            Polyline poly0 = new Polyline();
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis + Vector2d.YAxis * 0.8, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.8 + Vector2d.YAxis, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.5 + Vector2d.YAxis, Math.Tan(0.25 * -1 * Math.PI), 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.5 + Vector2d.YAxis, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -1 + Vector2d.YAxis, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -1, 0, 0, 0);
            poly0.Closed = true;

            Polyline poly1 = new Polyline();
            poly1.AddVertexAt(poly1.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.4 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
            poly1.AddVertexAt(poly1.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.6 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
            poly1.Closed = true;

            Polyline poly2 = new Polyline();
            poly2.AddVertexAt(poly2.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.4 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
            poly2.AddVertexAt(poly2.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.6 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
            poly2.Closed = true;

            return new Polyline[]{ poly0, poly1, poly2 };
        }

    }
}
