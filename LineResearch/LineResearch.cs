using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace LineResearch
{
    public class LineResearch
    {
        [CommandMethod("GetLine")]
        public void GetLine()
        {

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var intOpts = new PromptIntegerOptions("\n请输入每隔多少毫米进行点的合并");

            var intRes = ed.GetInteger(intOpts);

            int intCmeter = 2000;

            if (intRes.Status == PromptStatus.OK)
            {

                intCmeter = intRes.Value;

            }

            var selectRes = ed.GetSelection(new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "POLYLINE") }));

            if (selectRes.Status == PromptStatus.OK)
            {

                var selectSet = selectRes.Value;

                List<Polyline3d> listPl = new List<Polyline3d>();

                List<Polyline3d> listPlold = MyForeach(selectSet);

                Point3dCollection p3dcoll = new Point3dCollection();

                if (null == listPlold || listPlold.Count < 1)
                {
                    return;
                }

                var pl3d1 = listPlold[0];

                foreach (var pl3d in listPlold)
                {

                    if (pl3d != null)
                    {

                        using (var trans = db.TransactionManager.StartTransaction())
                        {

                            foreach (ObjectId objId in pl3d)
                            {

                                var vertex3d = trans.GetObject(objId, OpenMode.ForRead) as PolylineVertex3d;

                                p3dcoll.Add(vertex3d.Position);

                            }
                        }
                    }

                }

                Polyline pline = new Polyline();
                Point3dCollection p3dColl2 = new Point3dCollection();
                List<Entity> listEntity = new List<Entity>();
                List<Entity> listEntity2 = new List<Entity>();
                for (int i = 0; i < p3dcoll.Count; i++)
                {

                    int startIndex = i;

                    Point2d pit1 = new Point2d(p3dcoll[i].X, p3dcoll[i].Y);
                    Point2d pit2 = Point2d.Origin;
                    Point2d pit3 = Point2d.Origin;

                    if (i + 1 < p3dcoll.Count)
                    {
                        pit2 = new Point2d(p3dcoll[i + 1].X, p3dcoll[i + 1].Y);
                        i = i + 1;

                    }

                    if (i + 1 < p3dcoll.Count)
                    {
                        pit3 = new Point2d(p3dcoll[i + 1].X, p3dcoll[i + 1].Y);
                        i = i + 1;
                    }

                    double length = (pit2 - pit1).Length + (pit3 - pit2).Length;

                    int mid = 0;

                    while (length < intCmeter)
                    {


                        if (i + 1 < p3dcoll.Count)
                        {
                            pit2 = pit3;
                            pit3 = new Point2d(p3dcoll[i + 1].X, p3dcoll[i + 1].Y);
                        }
                        else
                        {
                            break;
                        }
                        i = i + 1;

                        mid++;

                        length = (pit2 - pit1).Length + (pit3 - pit2).Length;
                    }
                    Point2d pitMid = Point2d.Origin;

                    if (mid / 2 > 0)
                    {
                        pitMid = new Point2d(p3dcoll[startIndex + mid / 2].X, p3dcoll[startIndex + mid / 2].Y);
                    }
                    else
                    {
                        pitMid = pit2;
                    }

                    pline.AddVertexAt(pline.NumberOfVertices, pit1, 0, 0, 0);

                    p3dColl2.Add(new Point3d(pit1.X, pit1.Y, 0));

                    if (i < p3dcoll.Count)
                    {
                        pline.AddVertexAt(pline.NumberOfVertices, pitMid, 0, 0, 0);
                        pline.AddVertexAt(pline.NumberOfVertices, pit3, 0, 0, 0);

                        p3dColl2.Add(new Point3d(pitMid.X, pitMid.Y, 0));
                        p3dColl2.Add(new Point3d(pit3.X, pit3.Y, 0));

                    }

                    if (i < p3dcoll.Count)
                    {
                        Arc arc = GetArc(pit1, pitMid, pit3);
                        Arc arc2 = GetArc(pit1, pitMid, pit3);
                        arc.ColorIndex = 0;
                        arc2.ColorIndex = 0;
                        listEntity.Add(arc);
                        listEntity2.Add(arc2);

                    }

                    if (i == p3dcoll.Count - 1)
                    {
                        break;
                    }
                    i = i - 1;

                }
                pline.Closed = true;
                pline.ColorIndex = pl3d1.ColorIndex;



                PromptKeywordOptions pkOpts = new PromptKeywordOptions("请输入是否进行弧长优化[Y/N]", "Y N");

                var keyRes = ed.GetKeywords(pkOpts);

                List<Entity> listEntsOptimize = new List<Entity>();

                if (keyRes.Status == PromptStatus.OK && keyRes.StringResult == "Y")
                {

                    ed.WriteMessage("进行弧长优化");


                    for (int i = 0; i < listEntity2.Count; i++)
                    {

                        Arc arc = listEntity2[i] as Arc;

                        Arc arc2 = null;
                        if (!listEntsOptimize.Contains(arc))
                            listEntsOptimize.Add(arc);

                        if (i + 1 < listEntity2.Count)
                        {
                            arc2 = listEntity2[i + 1] as Arc;

                            if (!listEntsOptimize.Contains(arc2))
                                listEntsOptimize.Add(arc2);

                            i = i + 1;
                        }




                        List<Arc> tempArc = new List<Arc>();

                        if (arc != null && arc2 != null)
                        {

                            double angle1 = arc.EndAngle - arc.StartAngle;

                            double angle2 = arc2.EndAngle - arc.StartAngle;

                            while (Math.Abs(angle1 - angle2) <= Math.PI * (8.0 / 180))
                            {
                                if (listEntsOptimize.Contains(arc))
                                    listEntsOptimize.Remove(arc);

                                if (listEntsOptimize.Contains(arc2))
                                    listEntsOptimize.Remove(arc2);

                                tempArc.Add(arc);

                                tempArc.Add(arc2);

                                if (i + 1 < listEntity2.Count)
                                {
                                    arc2 = listEntity2[i + 1] as Arc;
                                    i = i + 1;
                                }
                                else
                                {
                                    arc2 = null;
                                    break;
                                }

                                angle1 = arc.EndAngle - arc.StartAngle;

                                angle2 = arc2.EndAngle - arc.StartAngle;


                            }

                        }
                        List<Polyline> listpolytemp = new List<Polyline>();

                        tempArc = tempArc.Distinct<Arc>().ToList();

                        if (tempArc.Count > 1)
                        {
                            Arc mergeArc = GetMergeArc(tempArc);

                            listEntsOptimize.Add(mergeArc);

                            /* for (int j = 0; j < tempArc.Count; j++)
                             {
                                 var ent = tempArc[j];

                                 if (ent is Arc)
                                 {
                                     Arc arct = ent as Arc;
                                     double R = arc.Radius;
                                     Point3d startPoint = arct.StartPoint;
                                     Point3d endPoint = arct.EndPoint;
                                     Point2d p1, p2;
                                     p1 = new Point2d(startPoint.X, startPoint.Y);
                                     p2 = new Point2d(endPoint.X, endPoint.Y);
                                     Double L = p1.GetDistanceTo(p2);
                                     double H = R - Math.Sqrt(R * R - L * L / 4);

                                     Polyline polytemp = new Polyline();

                                     polytemp.AddVertexAt(0, p1, 2 * H / L, 0, 0);
                                     polytemp.AddVertexAt(1, p2, 0, 0, 0);
                                     polytemp.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red);

                                     listpolytemp.Add(polytemp);
                                 }

                             }*/

                            //listpolytemp = listpolytemp.Distinct().ToList();

                           // Polyline polyArc = GetPolyline(listpolytemp);

                            //polyArc.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red);
                            //listEntsOptimize.Add(polyArc);
                        }
                        listEntsOptimize = listEntsOptimize.Distinct<Entity>().ToList();
                        if (i == listEntity2.Count - 1)
                        {
                            break;
                        }
                        i = i - 1;
                    }
                }

                List<Polyline> listPoly = ArcToPolyline(listEntity);
                List<Polyline> listPoly2 = ArcToPolyline(listEntsOptimize);
               
                List<Polyline> listpolyOptimize = ArcToPolyline(listEntsOptimize);

                Polyline poly = GetPolyline(listPoly);
                Polyline poly2 = GetPolyline(listPoly2);
                

                if (keyRes.Status == PromptStatus.OK && keyRes.StringResult == "Y")
                    poly2.ToSpace();
                else
                    poly.ToSpace();

                var newDoc = Application.DocumentManager.Add("");
                using (var lock1 = newDoc.LockDocument())
                {
                    var newDb = newDoc.Database;

                    if (keyRes.Status == PromptStatus.OK && keyRes.StringResult == "Y")
                        listEntsOptimize.ToSpace(newDb);
                    else
                        poly.ToSpace(newDb);


                }

            }

        }

        private Arc GetMergeArc(List<Arc> listArc)
        {



            double startAngle = listArc[0].StartAngle;
            double endAngle = listArc[listArc.Count - 1].EndAngle;



            double temp = 0;

            if ((endAngle - startAngle) > Math.PI || (startAngle > endAngle && Math.Abs(startAngle - endAngle) < Math.PI))
            {
                temp = startAngle;

                startAngle = endAngle;

                endAngle = temp;
            }

            double rad = 0.0;
            double x = 0.0;
            double y = 0.0;
            listArc.ForEach((arc) =>
            {
                rad += arc.Radius;
                x += arc.Center.X;
                y += arc.Center.Y;
            });

            rad = rad / listArc.Count;

            Point3d center = new Point3d(x / listArc.Count, y / listArc.Count, 0);


            return new Arc(center, rad, startAngle, endAngle);
        }

        public List<Polyline3d> MyForeach(SelectionSet selected,
                   Database db = null)
        {

            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            List<Polyline3d> list = new List<Polyline3d>();
            using (var trans = db.TransactionManager.StartTransaction())
            {
                foreach (var id in selected.GetObjectIds())
                {
                    var ent = trans.GetObject(id, OpenMode.ForRead) as Polyline3d;
                    list.Add(ent);
                }

                trans.Commit();
            }

            return list;
        }

        private void GetArcCenter(double a1, double b1, double a2, double b2, double a3, double b3, out double p, out double q)
        {

            double u = (Math.Pow(a1, 2) - Math.Pow(a2, 2)
                + Math.Pow(b1, 2) - Math.Pow(b2, 2))
                / (2 * (a1 - a2));

            double v = (Math.Pow(a1, 2) - Math.Pow(a3, 2)
                + Math.Pow(b1, 2) - Math.Pow(b3, 2))
                / (2 * (a1 - a3));

            double k1 = (b1 - b2) / (a1 - a2);

            double k2 = (b1 - b3) / (a1 - a3);

            q = (u - v) / (k1 - k2);

            p = v - (u - v) * k2 / (k1 - k2);

        }

        public Arc GetArc(Point2d pit1, Point2d pit2, Point2d pit3)
        {
            CircularArc2d arc2d = new CircularArc2d(pit1, pit2, pit3);

            Point2d pitCenter = arc2d.Center;

            double radius = arc2d.Radius;

            double startAngle = AngleFromXAxis(pit1, pitCenter);

            double endAngle = AngleFromXAxis(pit3, pitCenter);

            double sangle = (startAngle / (2 * Math.PI * radius) * 360);
            double eangle = (endAngle / (2 * Math.PI * radius) * 360);

            double temp = 0;

            if ((endAngle - startAngle) > Math.PI || (startAngle > endAngle && Math.Abs(startAngle - endAngle) < Math.PI))
            {
                temp = startAngle;

                startAngle = endAngle;

                endAngle = temp;
            }

            Arc arc = new Arc(new Point3d(pitCenter.X, pitCenter.Y, 0), radius, startAngle, endAngle);

            return arc;

        }

        public double AngleFromXAxis(Point2d pt1, Point2d pt2)
        {

            Vector2d vector = new Vector2d(pt1.X - pt2.X, pt1.Y - pt2.Y);

            return vector.Angle;

        }

        private List<Polyline> ArcToPolyline(List<Entity> list)
        {
            List<Polyline> listPoly = new List<Polyline>();

            foreach (var ent in list)
            {

                //如果实体为圆弧
                if (ent is Arc)
                {
                    Arc arc = ent as Arc;
                    double R = arc.Radius;
                    Point3d startPoint = arc.StartPoint;
                    Point3d endPoint = arc.EndPoint;
                    Point2d p1, p2;
                    p1 = new Point2d(startPoint.X, startPoint.Y);
                    p2 = new Point2d(endPoint.X, endPoint.Y);
                    Double L = p1.GetDistanceTo(p2);
                    double H = R - Math.Sqrt(R * R - L * L / 4);
                    Polyline poly = new Polyline();

                    poly.AddVertexAt(0, p1, 2 * H / L, 0, 0);
                    poly.AddVertexAt(1, p2, 0, 0, 0);
                    poly.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red);

                    listPoly.Add(poly);
                }
                else if (ent is Polyline)
                {
                    var p = ent as Polyline;
                    p.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red);
                    listPoly.Add(p);
                }


            }
            return listPoly;
        }

        private Polyline GetPolyline(List<Polyline> list)
        {
            if (list.Count < 1)
            {
                return null;
            }
            Polyline poly = list[0];

            for (int i = 1; i < list.Count; i++)
            {
                if (poly.EndPoint == list[i].StartPoint || poly.StartPoint == list[i].EndPoint)
                    poly.JoinEntity(list[i]);
            }
            return poly;
        }

    }
}