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


namespace LearnningGeometry
{
    public class MyGeometry
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
                /*Point3d p1 = new Point3d(1, 2, 3);
                Application.ShowAlertDialog($"{p1 * 3}");

                Matrix3d matrx3d = Matrix3d.Displacement(new Vector3d(1, 2, 3));
                Application.ShowAlertDialog($"{matrx3d * p1}");

                var mtx = Matrix3d.Displacement(new Vector3d(1, 2, 3));
                var mtx1 = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
                Application.ShowAlertDialog($"{mtx*mtx1 * p1},{mtx1*mtx*p1}");

                Application.ShowAlertDialog($"{new Vector3d(1, 1, 1).Length}");

                //叉乘
                Application.ShowAlertDialog($"{Vector3d.XAxis.CrossProduct(Vector3d.YAxis)}");
                Application.ShowAlertDialog($"{new Vector3d(1,2,0).CrossProduct(new Vector3d(2,1,0))}");

                //点乘
                Application.ShowAlertDialog($"{new Vector3d(1, 2, 0).DotProduct(new Vector3d(2, 1, 0))}");

                //单位向量
                Application.ShowAlertDialog($"{new Vector3d(1, 2, 0).GetNormal()},{new Vector3d(1, 2, 0).GetNormal().Length}");

                //镜像矩阵
                var mtx3 = Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                Application.ShowAlertDialog($"{mtx3*new Vector3d(1, 2, 0)}");

                Application.ShowAlertDialog($"{mtx3.Inverse()*mtx3 * new Vector3d(1, 2, 0)}");

                var mtx4 = Matrix3d.Mirroring(Point3d.Origin);
                Application.ShowAlertDialog($"{string.Join(",", mtx4.ToArray())}");


                //tolerance
                var old = Tolerance.Global;

                try
                {
                    Tolerance.Global = new Tolerance(0, 0.1);
                    Application.ShowAlertDialog($"{Point3d.Origin == new Point3d(0.0001, 0, 0)}");
                }
                finally
                {
                    Tolerance.Global = old;
                }*/


                acEd.WriteMessage(Vector2d.XAxis.RotateBy(Math.PI*-0.5).ToString());

                foreach (var i in Enumerable.Range(0, 60))
                {
                    var lineVec = -Vector3d.YAxis * (i % 5 == 0 ? 1 : 0.5);
                    var mtx = Matrix3d.Rotation(i * Math.PI / 30, Vector3d.ZAxis, Point3d.Origin) *
                        Matrix3d.Displacement(Vector3d.YAxis * 10);

                    var line = new Line(Point3d.Origin, Point3d.Origin + lineVec);
                    line.TransformBy(mtx);
                    mdlSpc.AppendEntity(line);
                    acTrans.AddNewlyCreatedDBObject(line, true);

                }
                acTrans.Commit();
            }
        }

    }
}
