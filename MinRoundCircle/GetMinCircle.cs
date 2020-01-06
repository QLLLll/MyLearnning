using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Linq;
namespace MinRoundCircle
{


    public class GetMinCircle
    {
        //Document Doc = Application.DocumentManager.MdiActiveDocument;
        //Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;
       //所有的点集
        List<Point3d> listPts = new List<Point3d>();
        List<double> listRadius = new List<double>();

        [CommandMethod("GetMinC")]
        public void GetCircle()
        {
            listPts.Clear();
            listRadius.Clear();

            GetAllPts();
            
            Circle minCircle = null;
            if (listPts.Count >= 3)
            {
                Circle c= GetFirstCircle();

                for (int i = 0; i < listPts.Count; i++)
                {
                    var pt = listPts[i];

                    var len = c.Radius;

                    var cCen = c.Center;

                    var len2 = (pt - cCen).Length;

                    //如果pt在圆内，继续下一个点
                    if (len > len2)
                    {
                        continue;
                    }
                    else
                    {
                        //求圆心和pt点构成的直线和圆的交点，
                        //并求出pt点离圆最远的那个点pt1或者是Pt2，最后用这两个点构成一个新的圆，继续循环，直到所有的点遍历完
                        var line = new Line(pt, cCen);

                        Point3dCollection pt3Coll = new Point3dCollection();

                        c.IntersectWith(line, Intersect.ExtendBoth, pt3Coll, IntPtr.Zero, IntPtr.Zero);

                        var pt1 = pt3Coll[0];
                        var pt2 = pt3Coll[1];

                        var l1 = (pt1 - pt).Length;
                        var l2 = (pt2 - pt).Length;

                        if (l1 > l2)
                        {
                            var center = new Point3d((pt1.X + pt.X) / 2, (pt1.Y + pt.Y) / 2, 0);

                            c = new Circle(center, Vector3d.ZAxis, l1/2);
                        }
                        else
                        {
                            var center = new Point3d((pt2.X + pt.X) / 2, (pt2.Y + pt.Y) / 2, 0);

                            c = new Circle(center, Vector3d.ZAxis, l2 / 2);
                        }
                    }
                }
                minCircle = c;
            }
            else
            {
                minCircle = GetFirstCircle();
            }
            if (minCircle != null)
                //加入模型空间
                minCircle.ToSpace();
            minCircle.Dispose();
        }

        public void GetAllPts()
        {

            using (var trans = Db.TransactionManager.StartTransaction())
            {

                BlockTable blkTbl = (BlockTable)trans.GetObject(Db.BlockTableId, OpenMode.ForRead);

                foreach (ObjectId oId in blkTbl)
                {

                    var rec = trans.GetObject(oId, OpenMode.ForRead) as BlockTableRecord;

                    if (rec != null)
                    {
                        //块参照
                        if (rec.Bounds.HasValue)
                        {
                            var ptMin = rec.Bounds.Value.MinPoint;
                            var ptMax = rec.Bounds.Value.MaxPoint;
                            var radius = (ptMax - ptMin).Length / 2.0;
                            listPts.Add(new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0));
                            listRadius.Add(radius);
                        }
                        //实体
                        foreach (ObjectId entId in rec)
                        {
                            var ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;

                            //在计算边界属性时，dimension的不准确，我就跳过了
                            if ((ent as Dimension) != null)
                            {
                                continue;
                            }

                            if (ent != null)
                            {
                                var ptMin = ent.GeometricExtents.MinPoint;
                                var ptMax = ent.GeometricExtents.MaxPoint;

                                var radius = (ptMax - ptMin).Length / 2.0;

                                listPts.Add(new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0));
                                listRadius.Add(radius);
                            }
                        }
                    }
                }
                listPts = listPts.Distinct<Point3d>().ToList();
                trans.Commit();
            }
        }
        public Circle GetFirstCircle()
        {
            //如果只有一个图，就直接返回这个图元的边界圆
            if (listPts.Count == 1)
            {
                Circle c = new Circle(listPts[0], Vector3d.ZAxis,  listRadius[0]);
                return c;
            }
            else if (listPts.Count == 2)
            {
                var ptMin = listPts[0];
                var ptMax = listPts[1];
                var radius = (ptMax - ptMin).Length / 2.0;
                var ptCenter = new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0);

                Circle c = new Circle(ptCenter, Vector3d.ZAxis, radius);

                return c;

            }
            else
            {
                var ptMin = listPts[0];
                var ptMax = listPts[listPts.Count / 2];
                var radius = (ptMax - ptMin).Length / 2.0;
                var ptCenter = new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0);

                Circle c = new Circle(ptCenter, Vector3d.ZAxis, radius);

                listPts.Remove(ptMin);
                listPts.Remove(ptMax);

                return c;
            }
        }

    }
}
