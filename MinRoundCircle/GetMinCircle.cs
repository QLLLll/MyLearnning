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
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        List<Entity> listEnts = new List<Entity>();
        List<Point3d> listPts = new List<Point3d>();
        List<double> listRadius = new List<double>();

        [CommandMethod("GetMinC")]
        public void GetCircle()
        {
            listEnts.Clear();
            listPts.Clear();
            listRadius.Clear();

            GetAllPts();
            //Ed.WriteMessage(listEnts.Count.ToString());
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
                minCircle.ToSpace();

        }

        public void GetAllPts()
        {
            List<Entity> listAllEnts = new List<Entity>();

            using (var trans = Db.TransactionManager.StartTransaction())
            {

                BlockTable blkTbl = (BlockTable)trans.GetObject(Db.BlockTableId, OpenMode.ForRead);

                foreach (ObjectId oId in blkTbl)
                {

                    var rec = trans.GetObject(oId, OpenMode.ForRead) as BlockTableRecord;

                    if (rec != null)
                    {
                        if (rec.Bounds.HasValue)
                        {
                            var ptMin = rec.Bounds.Value.MinPoint;
                            var ptMax = rec.Bounds.Value.MaxPoint;
                            var radius = (ptMax - ptMin).Length / 2.0;
                            listPts.Add(new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0));
                            listRadius.Add(radius);
                        }
                        foreach (ObjectId entId in rec)
                        {
                            var ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;

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
