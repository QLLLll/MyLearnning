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
using Autodesk.AutoCAD.BoundaryRepresentation;

namespace LearnningBoundaryRepresentation
{
    public class MyBoundaryRepresentation
    {
        [CommandMethod("CMD1")]
        public void Test()
        {

            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            Editor acEd = acDoc.Editor;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                var blkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                var mdlSpc = acTrans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                /* var ent3dArray = Enumerable.Range(0, 8).Select(i => new Solid3d()).ToArray();
                 var box = ent3dArray[0];
                 box.CreateBox(1, 2, 3);
                 //圆锥/圆柱
                 var fru = ent3dArray[1];
                 fru.CreateFrustum(3, 1, 1, 0);

                 //台体
                 var pyr = ent3dArray[2];
                 pyr.CreatePyramid(3, 5, 2, 1);

                 var sph = ent3dArray[3];
                 sph.CreateSphere(2);

                 var tor = ent3dArray[4];
                 tor.CreateTorus(2, 0.5);

                 var wdg = ent3dArray[5];
                 wdg.CreateWedge(1, 2, 3);

                 var poly = new Polyline();

                 poly.AddVertexAt(poly.NumberOfVertices, Point2d.Origin - Vector2d.XAxis, 0, 0, 0);
                 poly.AddVertexAt(poly.NumberOfVertices, Point2d.Origin + Vector2d.XAxis, 0, 0, 0);
                 poly.AddVertexAt(poly.NumberOfVertices, Point2d.Origin + Vector2d.YAxis, 0, 0, 0);
                 poly.Closed = true;

                 var reg=Region.CreateFromCurves(new DBObjectCollection() { poly })[0] as Region;

                 var ext1 = ent3dArray[6];
                 ext1.Extrude(reg, 6, 0);

                 var ext2 = ent3dArray[7];
                 ext2.Revolve(reg, Point3d.Origin + Vector3d.XAxis, Vector3d.YAxis, Math.PI * 2);

                 foreach (var i in Enumerable.Range(0,8))
                 {

                     var ent3d = ent3dArray[i];

                     ent3d.TransformBy(Matrix3d.Displacement((Vector3d.XAxis * 10).RotateBy(i * Math.PI / 4, Vector3d.ZAxis)));

                     mdlSpc.AppendEntity(ent3d);
                     acTrans.AddNewlyCreatedDBObject(ent3d, true);


                 }*/

                var unit0 = new Solid3d();
                unit0.CreateBox(2, 2, 0.2);
                unit0.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * 0.1));

                using(var unit1=new Solid3d())
                {

                    unit1.CreateFrustum(3, 0.9, 0.9, 0);
                    unit1.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * 1.7));

                    unit0.BooleanOperation(BooleanOperationType.BoolUnite, unit1);

                }

                using (var unit2=new Solid3d())
                {
                    unit2.CreateFrustum(3, 0.9, 0.9, 0);
                    unit2.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * 1.5));
                    unit0.BooleanOperation(BooleanOperationType.BoolSubtract, unit2);
                }

                using (var unit3 = new Solid3d())
                {
                    unit3.CreateFrustum(2, 0.6, 0.6, 0);
                    unit3.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * 1));
                    unit0.BooleanOperation(BooleanOperationType.BoolUnite, unit3);
                }

                var brep = new Brep(unit0);

                acEd.WriteMessage(string.Join("\n",
                    new[]
                    {
                        $"Complex:{brep.Complexes.Count()}",
                        $"Shell:{brep.Shells.Count()}",
                        $"Face:{brep.Faces.Count()}",
                        $"Edged:{brep.Edges.Count()}",
                        $"Vertex:{brep.Vertices.Count()}"
                    }
                    ));

                mdlSpc.AppendEntity(unit0);
                acTrans.AddNewlyCreatedDBObject(unit0, true);



                acTrans.Commit();
            }

            }

    }
}
