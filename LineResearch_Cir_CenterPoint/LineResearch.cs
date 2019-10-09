using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace LineResearch_Cir_CenterPoint
{
    public class LineResearch
    {
        //容差
        static double rongCha = 50;

        //多段线合并范围（直径只差/直径<=range&&圆心距离只差/最小半径<=range）
        static double range = 0.3;

        //多段线合并长度 单位毫米
        static int intCmeter = 2000;



        [CommandMethod("GetLine")]
        public static void GetLine()
        {

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var intOpts = new PromptIntegerOptions("\n请输入每隔多少毫米进行点的合并");

            var intRes = ed.GetInteger(intOpts);

            if (intRes.Status == PromptStatus.OK)
            {

                intCmeter = intRes.Value;

            }
            PromptDoubleOptions pkDbOpts = new PromptDoubleOptions("请输入容差");

            //不允许输入负数
            pkDbOpts.AllowNegative = false;

            var keyDoubleRes = ed.GetDouble(pkDbOpts);


            if (keyDoubleRes.Status == PromptStatus.OK)
            {

                rongCha = keyDoubleRes.Value;

                rongCha = rongCha > 500 ? 500 : rongCha;

            }
            var selectRes = ed.GetSelection(/*new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "POLYLINE") })*/);

            if (selectRes.Status == PromptStatus.OK)
            {

                var selectSet = selectRes.Value;

                

                List<Polyline3d> listPlold = MyForeach(selectSet);
                //Polyline polyy = MyForeach2(selectSet);

                /*Point2dCollection p3dcoll = new Point2dCollection();

                for (int i = 0; i < polyy.NumberOfVertices; i++)
                {
                    p3dcoll.Add(polyy.GetPoint2dAt(i));
                }*/

                if (null == listPlold || listPlold.Count < 1)
                {
                    return;
                }

                //var pl3d1 = listPlold[0];
                //   Point3dCollection p3dcoll = new Point3dCollection();

                List<Point3dCollection> listP3dColl = new List<Point3dCollection>();

                foreach (var pl3d in listPlold)
                {
                    Point3dCollection p3dcoll1 = new Point3dCollection();

                    if (pl3d != null)
                    {
                        listP3dColl.Add(p3dcoll1);

                        using (var trans = db.TransactionManager.StartTransaction())
                        {

                            foreach (ObjectId objId in pl3d)
                            {

                                var vertex3d = trans.GetObject(objId, OpenMode.ForRead) as PolylineVertex3d;

                                p3dcoll1.Add(vertex3d.Position);

                            }
                        }
                    }

                }

                // Polyline pline = new Polyline();
                //Point3dCollection p3dColl2 = new Point3dCollection();

                PromptResult re = null;
                bool isChoise = false;
                foreach (var p3dcoll in listP3dColl)
                {

                    /*
                       * 在多段线合并的时候，超过合并范围，会取消合并，
                       * 当取消合并为最小的3个点之后还是会超过合并范围，就直接做成polyline
                       * 有一个这样的polyline，就记录下它在listEntity下的索引
                        */
                    List<int> isPolineIndex = new List<int>();

                    List<Entity> listEntity = new List<Entity>();
                    List<Entity> listEntity2 = new List<Entity>();

                    List<Entity> listEntity3 = new List<Entity>();//等距弧长多段线

                    //没合并一个Arc 就产生一个CirculaArc2d
                    List<CircularArc2d> listC2d = new List<CircularArc2d>();

                    for (int i = 0; i < p3dcoll.Count; i++)
                    {

                        int startIndex = i;

                        Point2d pit1 = new Point2d(p3dcoll[i].X, p3dcoll[i].Y);
                        Point2d pit2 = Point2d.Origin;
                        Point2d pit3 = Point2d.Origin;

                        if (i + 1 < p3dcoll.Count)
                        {
                            pit2 = new Point2d(p3dcoll[i + 1].X, p3dcoll[i + 1].Y);
                            i += 1;

                        }

                        if (i + 1 < p3dcoll.Count)
                        {
                            pit3 = new Point2d(p3dcoll[i + 1].X, p3dcoll[i + 1].Y);
                            i += 1;
                        }

                        double length = (pit2 - pit1).Length + (pit3 - pit2).Length;

                        //记录循环的点的个数
                        int mid = 0;

                        //当两个点的长度小于要合并的长度就继续循环下一个点
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
                            i += 1;

                            mid++;

                            length = (pit2 - pit1).Length + (pit3 - pit2).Length;
                        }
                        Point2d pitMid = Point2d.Origin;

                        //寻找这些要合并的点的中间的个点
                        if (mid / 2 > 0)
                        {
                            pitMid = new Point2d(p3dcoll[startIndex + mid / 2].X, p3dcoll[startIndex + mid / 2].Y);
                        }
                        else
                        {
                            pitMid = pit2;
                        }

                        // pline.AddVertexAt(pline.NumberOfVertices, pit1, 0, 0, 0);

                        // p3dColl2.Add(new Point3d(pit1.X, pit1.Y, 0));

                        //if (i < p3dcoll.Count)
                        //{
                        //pline.AddVertexAt(pline.NumberOfVertices, pitMid, 0, 0, 0);
                        // pline.AddVertexAt(pline.NumberOfVertices, pit3, 0, 0, 0);

                        // p3dColl2.Add(new Point3d(pitMid.X, pitMid.Y, 0));
                        // p3dColl2.Add(new Point3d(pit3.X, pit3.Y, 0));

                        //}

                        if (i < p3dcoll.Count)
                        {
                            //合并成圆弧
                            Arc arc = GetArc(pit1, pitMid, pit3);
                            CircularArc2d c2d = null;
                            Arc arc2 = GetArc2(pit1, pitMid, pit3, ref c2d);


                            Arc arc3 = GetArc(pit1, pitMid, pit3);

                            //不计算容差的等距圆弧
                            listEntity3.Add(arc3);

                            //计算容差
                            if (!CalRongCha(c2d, startIndex, mid + 2, p3dcoll,listEntity,listEntity2,listC2d,isPolineIndex))
                            {

                                listEntity.Add(arc);
                                listEntity2.Add(arc2);
                                listC2d.Add(c2d);
                            }
                        }

                        if (i == p3dcoll.Count - 1)
                        {
                            break;
                        }
                        i -= 1;

                    }
                    //pline.Closed = true;
                    //pline.ColorIndex = pl3d1.ColorIndex;


                    if (isChoise == false)
                    {
                        PromptKeywordOptions pkOpts = new PromptKeywordOptions("请输入是否进行弧长优化[Y/N]", "Y N");

                       re= ed.GetKeywords(pkOpts);

                        isChoise = true;
                    }
                    List<Entity> listEntsOptimize = new List<Entity>();
                    List<Entity> listEntsOptimize2 = new List<Entity>();

                    if (re.Status == PromptStatus.OK && re.StringResult == "Y")
                    {

                        ed.WriteMessage("进行弧长优化");

                        /* PromptDoubleOptions pkDbOpts2 = new PromptDoubleOptions("请输入优化圆弧的范围");

                         pkDbOpts2.AllowNegative = false;

                         var keyDoubleRes2 = ed.GetDouble(pkDbOpts2);


                         if (keyDoubleRes2.Status == PromptStatus.OK)
                         {

                             range = keyDoubleRes2.Value;

                         }*/


                        for (int i = 0; i < listEntity2.Count; i++)
                        {
                            try
                            {

                                //如果是多段线，直接添加
                                if (isPolineIndex.Contains(i))
                                {
                                    listEntsOptimize.Add(listEntity2[i]);
                                }
                                else
                                {

                                    Arc arc = listEntity2[i] as Arc;

                                    Arc arc2 = null;

                                    //先把arc添加进去，如果进入while循环，在移除
                                    if (arc != null && !listEntsOptimize.Contains(arc))
                                        listEntsOptimize.Add(arc);

                                    if (i + 1 < listEntity2.Count)
                                    {
                                        arc2 = listEntity2[i + 1] as Arc;

                                        if (arc2 == null)
                                        {
                                            listEntsOptimize.Add(listEntity2[i + 1]);
                                            continue;
                                        }

                                        if (arc2 != null && !listEntsOptimize.Contains(arc2))
                                            listEntsOptimize.Add(arc2);

                                        i += 1;
                                    }

                                    //要合并优化的圆弧
                                    List<CircularArc2d> tempArc = new List<CircularArc2d>();

                                    if (arc != null && arc2 != null)
                                    {

                                        // double angle1 = arc.EndAngle - arc.StartAngle;

                                        //double angle2 = arc2.EndAngle - arc.StartAngle;

                                        Point3d pt1 = arc.Center;
                                        Point3d pt2 = arc2.Center;

                                        double diffRad = Math.Abs(arc.Radius - arc2.Radius) / Math.Max(arc.Radius, arc2.Radius);

                                        double diffLength = (pt2 - pt1).Length / Math.Min(arc.Radius, arc2.Radius);

                                        //while (Math.Abs(angle1 - angle2) <= Math.PI * (30.0 / 180))

                                        while (diffRad <= range && diffLength <= range)
                                        {
                                            if (listEntsOptimize.Contains(arc))
                                                listEntsOptimize.Remove(arc);

                                            if (listEntsOptimize.Contains(arc2))
                                                listEntsOptimize.Remove(arc2);

                                            int index = listEntity2.IndexOf(arc);

                                            int index2 = listEntity2.IndexOf(arc2);
                                            if (index != -1)
                                                tempArc.Add(listC2d[index]);
                                            if (index2 != -1)
                                                tempArc.Add(listC2d[index2]);

                                            if (i + 1 < listEntity2.Count)
                                            {
                                                arc2 = listEntity2[i + 1] as Arc;
                                                var po = listEntity2[i + 1] as Polyline;
                                                if (arc2 == null && po != null)
                                                {
                                                    listEntsOptimize.Add(listEntity2[i + 1]);
                                                    i += 1;
                                                    continue;
                                                }
                                                else
                                                    i += 1;
                                            }
                                            else
                                            {
                                                arc2 = null;
                                                break;
                                            }

                                            // angle1 = arc.EndAngle - arc.StartAngle;

                                            // angle2 = arc2.EndAngle - arc.StartAngle;
                                            pt1 = arc.Center;
                                            pt2 = arc2.Center;
                                            diffRad = Math.Abs(arc.Radius - arc2.Radius) / Math.Max(arc.Radius, arc2.Radius);
                                            diffLength = (pt2 - pt1).Length / Math.Min(arc.Radius, arc2.Radius);

                                        }

                                    }
                                    //List<Polyline> listpolytemp = new List<Polyline>();

                                    //进行圆弧优化
                                    if (tempArc.Count > 1)
                                    {
                                        Arc newTempArc = null;
                                        Point2d startPoint = tempArc[0].StartPoint;
                                        Point2d endPoint = tempArc[tempArc.Count - 1].EndPoint;
                                        if (tempArc.Count == 2)
                                        {

                                            Point2d centerPoint = tempArc[0].EndPoint;

                                            newTempArc = GetArc(startPoint,
                                                centerPoint, endPoint);
                                        }
                                        else
                                        {
                                            Point2d centerPoint = tempArc[1].EndPoint;

                                            newTempArc = GetArc(startPoint,
                                                centerPoint, endPoint);
                                        }
                                        newTempArc.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red);

                                        listEntsOptimize.Add(newTempArc);

                                    }

                                    if (i == listEntity2.Count - 1)
                                    {
                                        break;
                                    }
                                    i -= 1;
                                }

                            }
                            catch (System.Exception e)
                            {

                                throw;
                            }
                        }
                    }

                    try
                    {


                        //List<Polyline> listPoly = ArcToPolyline(listEntity3);

                        //List<Polyline> listPoly2 = ArcToPolyline(listEntsOptimize);




                        List<Polyline> listPoly3 = ArcToStraightLine(listEntsOptimize);

                        //Polyline poly = GetPolyline(listPoly);//等距多段线
                        // poly.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.DeepPink);

                        //Polyline poly2 = GetPolyline(listPoly2);

                        //poly2.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.DeepPink);
                        listEntity3.ForEach(p => { p.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.DeepPink); });
                        listEntsOptimize.ForEach(p => { p.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red); });
                        listPoly3.ForEach(p => { p.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.DarkGreen); });





                        //Polyline poly3 = GetPolyline(listpoly3);//不等距优化后的多段直线
                        //poly3.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.DarkGreen);


                        if (re.Status == PromptStatus.OK && re.StringResult == "Y")
                        {
                            //poly.ToSpace();
                            //poly2.ToSpace();
                            //   poly3.ToSpace();
                            //listEntity3.ToSpace();
                            listEntsOptimize.ToSpace();
                            //listPoly3.ToSpace();
                            //doc.SendStringToExecute("PEit\nm\nJ\n", true, false, false);


                            //listEntsOptimize.ToSpace();
                        }
                        //listEntity2.ToSpace();
                        else
                        {
                            // poly.ToSpace();
                        }

                        //poly.ToSpace();

                        //List<Polyline> listpolyOptimize = ArcToPolyline(listEntsOptimize);

                        // Polyline poly = GetPolyline(listpolyOptimize);


                        //var newDoc = Application.DocumentManager.Add("");
                        //using (var lock1 = newDoc.LockDocument())
                        //{
                        //    var newDb = newDoc.Database;

                        //    if (keyRes.Status == PromptStatus.OK && keyRes.StringResult == "Y")
                        //        poly2.ToSpace(newDb);
                        //    else
                        //        poly.ToSpace(newDb);


                        //}

                    }

                    catch (System.Exception e)
                    {

                        throw;
                    }
                    finally
                    {
                        listEntity.Clear();
                        listEntity2.Clear();
                        listC2d.Clear();
                        listEntity3.Clear();
                        isPolineIndex.Clear();
                    }
                }
            }
        }
        private static bool CalRongCha(CircularArc2d c2d, int startIndex, int mid, Point3dCollection p3dcoll,
            List<Entity> listEntity, List<Entity> listEntity2, List<CircularArc2d> listC2d, List<int> isPolineIndex)
        {

            Point2d cPt = c2d.Center;

            double radius = c2d.Radius;

            bool flag = false;

            for (int i = startIndex; i <= startIndex + mid; i++)
            {
                Point2d ptTemp = new Point2d(p3dcoll[i].X, p3dcoll[i].Y);

                double diff = Math.Abs(radius - (cPt - ptTemp).Length);

                if (diff > rongCha)
                {
                    flag = true;
                    break;
                }
            }

            int count = startIndex + mid + 1;

            if (flag == true && count > 2)
            {

                if (count % 2 == 1)
                {

                    for (int i = startIndex; i < count; i += 2)
                    {

                        Point2d pit1 = new Point2d(p3dcoll[i].X, p3dcoll[i].Y);
                        Point2d pit2 = new Point2d(p3dcoll[i + 1].X, p3dcoll[i + 1].Y);
                        Point2d pit3 = new Point2d(p3dcoll[i + 2].X, p3dcoll[i + 2].Y);

                        Arc arc = GetArc(pit1, pit2, pit3);
                        CircularArc2d c2Temp = null;
                        Arc arc2 = GetArc2(pit1, pit2, pit3, ref c2Temp);


                        Point2d cPt2 = c2Temp.Center;

                        double radius2 = c2Temp.Radius;

                        double diff1 = Math.Abs(radius2 - (cPt2 - pit1).Length);
                        double diff2 = Math.Abs(radius2 - (cPt2 - pit2).Length);
                        double diff3 = Math.Abs(radius2 - (cPt2 - pit3).Length);

                        if (diff1 > rongCha || diff2 > rongCha || diff3 > rongCha)
                        {
                            Polyline p = new Polyline(3);

                            p.AddVertexAt(p.NumberOfVertices, pit1, 0, 0, 0);
                            p.AddVertexAt(p.NumberOfVertices, pit2, 0, 0, 0);
                            p.AddVertexAt(p.NumberOfVertices, pit3, 0, 0, 0);

                            listEntity.Add(p);
                            listEntity2.Add(p);
                            listC2d.Add(new CircularArc2d());
                            isPolineIndex.Add(listEntity.Count - 1);
                        }
                        else
                        {

                            listEntity.Add(arc);
                            listEntity2.Add(arc2);
                            listC2d.Add(c2Temp);
                        }


                    }

                }
                else
                {
                    int endFour = count - 4;

                    for (int i = startIndex; i < endFour; i += 2)
                    {

                        Point2d pit11 = new Point2d(p3dcoll[i].X, p3dcoll[i].Y);
                        Point2d pit22 = new Point2d(p3dcoll[i + 1].X, p3dcoll[i + 1].Y);
                        Point2d pit33 = new Point2d(p3dcoll[i + 2].X, p3dcoll[i + 2].Y);

                        Arc arc = GetArc(pit11, pit22, pit33);
                        CircularArc2d c2Temp = null;
                        Arc arc2 = GetArc2(pit11, pit22, pit33, ref c2Temp);

                        Point2d cPt22 = c2Temp.Center;

                        double radius22 = c2Temp.Radius;

                        double diff11 = Math.Abs(radius22 - (cPt22 - pit11).Length);
                        double diff22 = Math.Abs(radius22 - (cPt22 - pit22).Length);
                        double diff33 = Math.Abs(radius22 - (cPt22 - pit33).Length);

                        if (diff11 > rongCha || diff22 > rongCha || diff33 > rongCha)
                        {
                            Polyline p = new Polyline(3);

                            p.AddVertexAt(p.NumberOfVertices, pit11, 0, 0, 0);
                            p.AddVertexAt(p.NumberOfVertices, pit22, 0, 0, 0);
                            p.AddVertexAt(p.NumberOfVertices, pit33, 0, 0, 0);

                            listEntity.Add(p);
                            listEntity2.Add(p);
                            listC2d.Add(new CircularArc2d());
                            isPolineIndex.Add(listEntity.Count - 1);
                        }
                        else
                        {

                            listEntity.Add(arc);
                            listEntity2.Add(arc2);
                            listC2d.Add(c2Temp);
                        }


                    }

                    Point2d pit1 = new Point2d(p3dcoll[endFour].X, p3dcoll[endFour].Y);
                    Point2d pit2 = new Point2d(p3dcoll[endFour + 1].X, p3dcoll[endFour + 1].Y);
                    Point2d pit3 = new Point2d(p3dcoll[endFour + 2].X, p3dcoll[endFour + 2].Y);
                    Point2d pit4 = new Point2d(p3dcoll[endFour + 3].X, p3dcoll[endFour + 3].Y);

                    Arc arc3 = GetArc(pit1, pit2, pit4);
                    CircularArc2d c2Temp2 = null;
                    Arc arc22 = GetArc2(pit1, pit2, pit4, ref c2Temp2);


                    Point2d cPt2 = c2Temp2.Center;

                    double radius2 = c2Temp2.Radius;



                    double diff1 = Math.Abs(radius2 - (cPt2 - pit1).Length);
                    double diff2 = Math.Abs(radius2 - (cPt2 - pit2).Length);
                    double diff3 = Math.Abs(radius2 - (cPt2 - pit4).Length);
                    if (diff1 > rongCha || diff2 > rongCha || diff3 > rongCha)
                    {
                        Polyline p = new Polyline(4);

                        p.AddVertexAt(p.NumberOfVertices, pit1, 0, 0, 0);
                        p.AddVertexAt(p.NumberOfVertices, pit2, 0, 0, 0);
                        p.AddVertexAt(p.NumberOfVertices, pit3, 0, 0, 0);
                        p.AddVertexAt(p.NumberOfVertices, pit4, 0, 0, 0);
                        listEntity.Add(p);
                        listEntity2.Add(p);
                        listC2d.Add(new CircularArc2d());
                        isPolineIndex.Add(listEntity.Count - 1);
                    }
                    else
                    {

                        listEntity.Add(arc3);
                        listEntity2.Add(arc22);
                        listC2d.Add(c2Temp2);
                    }


                }
            }
            else
            {

            }

            return flag;
        }

        public static List<Polyline3d> MyForeach(SelectionSet selected,
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
        public static Polyline MyForeach2(SelectionSet selected,
                   Database db = null)
        {

            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            Polyline ent = null;
            using (var trans = db.TransactionManager.StartTransaction())
            {
                foreach (var id in selected.GetObjectIds())
                {
                    ent = trans.GetObject(id, OpenMode.ForRead) as Polyline;
                    break;
                }

                trans.Commit();
            }

            return ent;
        }

        public static Arc GetArc(Point2d pit1, Point2d pit2, Point2d pit3)
        {
            CircularArc2d arc2d = new CircularArc2d(pit1, pit2, pit3);

            Point2d pitCenter = arc2d.Center;

            double radius = arc2d.Radius;

            double startAngle = AngleFromXAxis(pit1, pitCenter);

            double endAngle = AngleFromXAxis(pit3, pitCenter);

            //double sangle = (startAngle / (2 * Math.PI * radius) * 360);
            //double eangle = (endAngle / (2 * Math.PI * radius) * 360);

            double temp = 0;

            if ((endAngle - startAngle) >= Math.PI || (startAngle > endAngle && Math.Abs(startAngle - endAngle) < Math.PI))
            {
                temp = startAngle;

                startAngle = endAngle;

                endAngle = temp;
            }

            Arc arc = new Arc(new Point3d(pitCenter.X, pitCenter.Y, 0), radius, startAngle, endAngle);

            return arc;

        }

        public static Arc GetArc2(Point2d pit1, Point2d pit2, Point2d pit3, ref CircularArc2d arc2d)
        {

            arc2d = new CircularArc2d(pit1, pit2, pit3);

            Point2d pitCenter = arc2d.Center;

            double radius = arc2d.Radius;

            double startAngle = AngleFromXAxis(pit1, pitCenter);

            double endAngle = AngleFromXAxis(pit3, pitCenter);

            //double sangle = (startAngle / (2 * Math.PI * radius) * 360);
            //double eangle = (endAngle / (2 * Math.PI * radius) * 360);

            double temp = 0;

            if ((endAngle - startAngle) >= Math.PI || (startAngle > endAngle && Math.Abs(startAngle - endAngle) < Math.PI))
            {
                temp = startAngle;

                startAngle = endAngle;

                endAngle = temp;
            }

            Arc arc = new Arc(new Point3d(pitCenter.X, pitCenter.Y, 0), radius, startAngle, endAngle);

            return arc;

        }

        public static double AngleFromXAxis(Point2d pt1, Point2d pt2)
        {

            Vector2d vector = new Vector2d(pt1.X - pt2.X, pt1.Y - pt2.Y);

            return vector.Angle;

        }


        private static List<Polyline> ArcToStraightLine(List<Entity> listEntsOptimize)
        {
            List<Polyline> listPoly = new List<Polyline>();

            foreach (var ent in listEntsOptimize)
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
                    //Double L = p1.GetDistanceTo(p2);
                    //double H = R - Math.Sqrt(R * R - L * L / 4);
                    Polyline poly = new Polyline();

                    poly.AddVertexAt(0, p1, 0, 0, 0);
                    poly.AddVertexAt(1, p2, 0, 0, 0);
                    // poly.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red);

                    listPoly.Add(poly);
                }
                else if (ent is Polyline)
                {
                    Polyline p = ent as Polyline;


                    if (p.NumberOfVertices > 2)
                    {
                        for (int i = 0; i < p.NumberOfVertices; i++)
                        {

                            Polyline tempP = new Polyline();

                            tempP.AddVertexAt(0, p.GetPoint2dAt(i), 0, 0, 0);

                            if (i + 1 < p.NumberOfVertices)
                            {
                                tempP.AddVertexAt(1, p.GetPoint2dAt(i + 1), 0, 0, 0);

                                listPoly.Add(tempP);
                            }

                        }
                    }
                    else
                    {
                        listPoly.Add(p);
                    }

                }
            }
            return listPoly;
        }

        private static List<Polyline> ArcToPolyline(List<Entity> list)
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
                    //poly.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red);

                    listPoly.Add(poly);
                }
                else if (ent is Polyline)
                {
                    Polyline p = ent as Polyline;


                    if (p.NumberOfVertices > 2)
                    {
                        for (int i = 0; i < p.NumberOfVertices; i++)
                        {

                            Polyline tempP = new Polyline();

                            tempP.AddVertexAt(0, p.GetPoint2dAt(i), 0, 0, 0);

                            if (i + 1 < p.NumberOfVertices)
                            {
                                tempP.AddVertexAt(1, p.GetPoint2dAt(i + 1), 0, 0, 0);

                                listPoly.Add(tempP);
                            }

                        }
                    }
                    else
                    {
                        listPoly.Add(p);
                    }

                }
            }
            return listPoly;
        }

        private static Polyline GetPolyline(List<Polyline> list)
        {
            if (list.Count < 1)
            {
                return null;
            }
            Polyline poly = new Polyline();


            Dictionary<Point2d, double> dicDouble = new Dictionary<Point2d, double>();

            List<Point2d> list2d = new List<Point2d>();
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    poly.AddVertexAt(poly.NumberOfVertices, new Point2d(list[i].StartPoint.X, list[i].StartPoint.Y), list[i].GetBulgeAt(0), 0, 0);
                }
            }
            catch (System.Exception e)
            {

                throw;
            }

            return poly;
        }

        /*   private static List<Polyline> GetPolyline(List<Polyline> list)
           {
               if (list.Count < 1)
               {
                   return null;
               }

               List<Polyline> listPoly = new List<Polyline>();


               int breakPoint = 0;
               int i = 0;
               try
               {
                   while (i < list.Count && breakPoint < list.Count)
                   {


                       Polyline poly = list[breakPoint];



                       for (i = breakPoint + 1; i < list.Count; i++)
                       {

                           if (poly.EndPoint == list[i].StartPoint ||
                               poly.EndPoint == list[i].EndPoint ||
                               poly.StartPoint == list[i].StartPoint ||
                               poly.StartPoint == list[i].EndPoint)
                           {
                               poly.JoinEntity(list[i]);
                           }
                           else
                           {

                               breakPoint = i;

                               break;

                               //Polyline[] plNew = new Polyline[4]
                               //{new Polyline(),new Polyline(),new Polyline(),new Polyline() };

                               //plNew[0].AddVertexAt(plNew[0].NumberOfVertices, new Point2d(list[i - 1].EndPoint.X, list[i - 1].EndPoint.Y), 0, 0, 0);
                               //plNew[0].AddVertexAt(plNew[0].NumberOfVertices, new Point2d(list[i].StartPoint.X, list[i].StartPoint.Y), 0, 0, 0);

                               //plNew[1].AddVertexAt(plNew[1].NumberOfVertices, new Point2d(list[i - 1].EndPoint.X, list[i - 1].EndPoint.Y), 0, 0, 0);
                               //plNew[1].AddVertexAt(plNew[1].NumberOfVertices, new Point2d(list[i].EndPoint.X, list[i].EndPoint.Y), 0, 0, 0);

                               //plNew[2].AddVertexAt(plNew[2].NumberOfVertices, new Point2d(list[i - 1].StartPoint.X, list[i - 1].StartPoint.Y), 0, 0, 0);
                               //plNew[2].AddVertexAt(plNew[2].NumberOfVertices, new Point2d(list[i].StartPoint.X, list[i].StartPoint.Y), 0, 0, 0);

                               //plNew[3].AddVertexAt(plNew[3].NumberOfVertices, new Point2d(list[i - 1].StartPoint.X, list[i - 1].StartPoint.Y), 0, 0, 0);
                               //plNew[3].AddVertexAt(plNew[3].NumberOfVertices, new Point2d(list[i].EndPoint.X, list[i].EndPoint.Y), 0, 0, 0);

                               //for(int j=0;j<plNew.Length;j++)
                               //{

                               //    if ((poly.EndPoint == plNew[j].StartPoint&& (list[i].EndPoint == plNew[j].EndPoint|| list[i].StartPoint == plNew[j].EndPoint)) ||
                               //           (poly.EndPoint == plNew[j].EndPoint&&(list[i].EndPoint == plNew[j].StartPoint|| list[i].StartPoint == plNew[j].StartPoint)) ||
                               //           (poly.StartPoint == plNew[j].StartPoint&&(list[i].EndPoint == plNew[j].EndPoint || list[i].StartPoint == plNew[j].EndPoint)) ||
                               //           (poly.StartPoint == plNew[j].EndPoint&& (list[i].EndPoint == plNew[j].StartPoint || list[i].StartPoint == plNew[j].StartPoint)))
                               //    {

                               //            poly.JoinEntity(plNew[j]);
                               //            break;

                               //    }
                               //}


                           }
                           listPoly.Add(poly);
                       }
                   }
               }
               catch (System.Exception)
               {

                   throw;
               }

               return listPoly;
           }*/
        /*  private void GetArcCenter(double a1, double b1, double a2, double b2, double a3, double b3, out double p, out double q)
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

                }*/

    }
}