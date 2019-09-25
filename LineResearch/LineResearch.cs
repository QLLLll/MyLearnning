using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var intOpts = new PromptIntegerOptions("\n请输入每隔多少厘米进行点的合并");

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
                List<Arc> listArc = new List<Arc>();

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

                    var vertex1 = pit1 - pitMid;
                    var vertex2 = pit3 - pitMid;

                    //if (vertex1.GetAngleTo(vertex2) != 0 || vertex1.GetAngleTo(vertex2) != Math.PI)
                    //{
                    //    Point2d pitCenter = Point2d.Origin;

                    //    double x = 0.0;
                    //    double y = 0.0;

                    //    GetArcCenter(pit1.X, pit1.Y, pitMid.X, pitMid.Y, pit3.X, pit3.Y, out x, out y);

                    //    pitCenter = new Point2d(x, y);

                    //    double radius = (pitCenter - pit1).Length;

                    //    double startAngle = (pit3 - Point2d.Origin).GetAngleTo(Vector2d.XAxis);

                    //    double endAngle = (pit1 - Point2d.Origin).GetAngleTo(Vector2d.XAxis);


                    //    Arc arc = new Arc(new Point3d(pitCenter.X, pitCenter.Y, 0), radius, startAngle, endAngle);


                    //    listArc.Add(arc);
                    //}

                }
                pline.Closed = true;
                pline.ColorIndex = pl3d1.ColorIndex;

                Spline sPline = new Spline(p3dColl2, 4, 1000);

                var newDoc = Application.DocumentManager.Add("");
                using (var lock1 = newDoc.LockDocument())
                {
                    var newDb = newDoc.Database;

                    pline.ToSpace(newDb);
                    //listArc.ToSpace();
                }

            }

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

        public void GetArcCenter(double a1, double b1, double a2, double b2, double a3, double b3, out double p, out double q)
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



    }
}