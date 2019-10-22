using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;

namespace BlockFillTest
{
    public class BlockFillTest
    {

        Polyline FirstCondition;

        Polyline PlBound;

        Curve SecondCondition;
        //左边相交的点
        Point3d Intersect1;
        //右边相交的点
        Point3d Intersect2;

        Point3d MinPoint;
        Point3d MaxPoint;

        Point3d MinBlkPt;
        Point3d MaxBlkPt;

        double Ratio;
        double RatioW;
        double RatioH;

        double MaxW, MaxH;
        double BlockW, BlockH;

        //Line blkDiagonal=new Line();

        //曲线方向从左到右，从下到上为true，否则为false
        bool splDirection = true;

        PointIsInPolyline PtInPl = new PointIsInPolyline();
        private BlockTableRecord BlkRec;

        DBObjectCollection dbColl = null;

        [CommandMethod("BT")]
        public void Test()
        {

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            GetFirstCondition();

            if (FirstCondition == null)
            {
                return;
            }

            GetSecondCondition();

            if (SecondCondition == null)
            {
                return;
            }

            PlBound = GetBoundsOfCon1(MinPoint, MaxPoint);

            PlBound.Color = Color.FromColor(System.Drawing.Color.Red);

            PlBound.ToSpace();

            Point3dCollection p3dcl = new Point3dCollection();

            FirstCondition.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcl, IntPtr.Zero, IntPtr.Zero);

            //求Condition1与Condition2的交点
            if (p3dcl.Count <= 0)
            {
                if (splDirection)
                {
                    Intersect1 = SecondCondition.StartPoint;

                    Intersect2 = SecondCondition.EndPoint;
                }
                else
                {
                    Intersect1 = SecondCondition.EndPoint;

                    Intersect2 = SecondCondition.StartPoint;
                }
            }
            else
            {

                var list = p3dcl.Cast<Point3d>().OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();


                Intersect1 = list.First();

                Intersect2 = list[list.Count - 1];

                //如果只有一个交点
                if (Intersect1 == Intersect2)
                {
                    if (Intersect1 == SecondCondition.EndPoint)
                    {
                        Intersect2 = SecondCondition.StartPoint;
                    }
                    else
                    {
                        Intersect2 = SecondCondition.EndPoint;
                    }
                }
                Point3dCollection temp3dCol = new Point3dCollection();
                //分割曲线
                if (list.Count >= 2)
                {

                    if (splDirection)
                    {

                        temp3dCol.Add(Intersect1);
                        temp3dCol.Add(Intersect2);
                        dbColl = SecondCondition.GetSplitCurves(temp3dCol);

                    }
                    else
                    {
                        temp3dCol.Add(Intersect2);
                        temp3dCol.Add(Intersect1);
                        dbColl = SecondCondition.GetSplitCurves(temp3dCol);
                    }
                }
                else if (list.Count == 1)
                {

                    temp3dCol.Add(Intersect1);
                    dbColl = SecondCondition.GetSplitCurves(temp3dCol);
                }
                var ent = GetsplitCurve();

                if (ent != null && ent.Length > 1)
                {
                    var curve = ent[1] as Curve;


                    for (int i = 2; i < ent.Length - 1; i++)
                    {

                        curve.JoinEntity(ent[2]);
                    }

                    SecondCondition = curve;

                    SecondCondition.ToSpace();

                }

            }

            GetBlockCondition();

            if (BlkRec == null)
            {
                return;
            }


            GetBlkRatioCondtn1();

            DrawLines(0.2);



            List<Polyline> firstUp = null;
            List<Polyline> firstDown = null;

            //List<BlockReference> listBr = BlkScale3(0.3, MinBlkPt, MaxBlkPt, 0.1,ref firstUp);

            // BlkScaleDown(0.3, MinBlkPt, MaxBlkPt, 0.1,ref firstDown);

            /* Polyline pl = new Polyline(listBr.Count);

             foreach (var br in listBr)
             {

                 Point3d maxPt = br.Bounds.Value.MaxPoint;

                 pl.AddVertexAt(pl.NumberOfVertices, new Point2d(maxPt.X, maxPt.Y), 0, 0, 0);


             }

             pl.ToSpace();*/



        }

        private Polyline GetBoundsOfCon1(Point3d min, Point3d max)
        {
            var pt1 = new Point2d(min.X, min.Y);
            var pt2 = new Point2d(max.X, min.Y);
            var pt3 = new Point2d(max.X, max.Y);
            var pt4 = new Point2d(min.X, max.Y);

            Polyline poly = new Polyline(4);

            poly.AddVertexAt(poly.NumberOfVertices, pt1, 0, 0, 0);
            poly.AddVertexAt(poly.NumberOfVertices, pt2, 0, 0, 0);
            poly.AddVertexAt(poly.NumberOfVertices, pt3, 0, 0, 0);
            poly.AddVertexAt(poly.NumberOfVertices, pt4, 0, 0, 0);

            poly.Closed = true;

            return poly;



        }

        public void GetFirstCondition()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var entOpts = new PromptEntityOptions("请选择封闭多段线\n");

            entOpts.SetRejectMessage("未选择正确");

            entOpts.AddAllowedClass(typeof(Polyline), true);

            var entRes = ed.GetEntity(entOpts);

            ObjectId entId = ObjectId.Null;
            if (entRes.Status == PromptStatus.OK)
            {
                entId = entRes.ObjectId;

            }

            if (entId == ObjectId.Null)
            {
                return;
            }

            Entity ent = null;

            using (var trans = db.TransactionManager.StartTransaction())
            {

                ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;

                trans.Commit();
            }

            if (ent != null)
            {

                FirstCondition = ent as Polyline;

                MinPoint = FirstCondition.Bounds.Value.MinPoint;
                MaxPoint = FirstCondition.Bounds.Value.MaxPoint;

            }
        }

        public Polyline GetFirstCondition1()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var entOpts = new PromptEntityOptions("请选择封闭多段线\n");

            entOpts.SetRejectMessage("未选择正确");

            entOpts.AddAllowedClass(typeof(Polyline), true);

            var entRes = ed.GetEntity(entOpts);

            ObjectId entId = ObjectId.Null;
            if (entRes.Status == PromptStatus.OK)
            {
                entId = entRes.ObjectId;

            }

            if (entId == ObjectId.Null)
            {
                return null;
            }

            Entity ent = null;

            using (var trans = db.TransactionManager.StartTransaction())
            {

                ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;

                trans.Commit();
            }

            if (ent != null)
            {

                return (ent as Polyline);

            }
            return null;
        }
  
        public void GetSecondCondition()
        {          
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var entOpts = new PromptEntityOptions("请选择曲线\n");

            entOpts.SetRejectMessage("未选择正确");

            entOpts.AddAllowedClass(typeof(Curve), false);

            var entRes = ed.GetEntity(entOpts);

            ObjectId entId = ObjectId.Null;
            if (entRes.Status == PromptStatus.OK)
            {
                entId = entRes.ObjectId;

            }

            if (entId == ObjectId.Null)
            {
                return;
            }


            Entity ent = null;

            using (var trans = db.TransactionManager.StartTransaction())
            {

                ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;

                trans.Commit();
            }

            if (ent != null)
            {

                SecondCondition = ent as Curve;

                if (SecondCondition.StartPoint.X < SecondCondition.EndPoint.X)
                {
                    splDirection = true;
                }
                else
                {
                    splDirection = false;

                }
                if (SecondCondition.StartPoint.X == SecondCondition.EndPoint.X)

                {
                    if (SecondCondition.StartPoint.Y < SecondCondition.EndPoint.Y)
                    {
                        splDirection = true;
                    }
                    else
                    {
                        splDirection = false;
                    }
                }

                //  int i=  PtInPl.PtRelationToPoly(FirstCondition, SecondCondition.StartPoint, 1.0E-4);
            }
        }

        public Entity[] GetsplitCurve()
        {
            if (dbColl.Count <= 0)
            {
                return null;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Entity[] entity = new Entity[dbColl.Count];


            int i = 0;
            foreach (Entity ent in dbColl)
            {

                entity[i++] = ent;

            }

            if (entity.Length == 3)
            {

                entity[0].Color = Color.FromColor(System.Drawing.Color.Red);
                entity[1].Color = Color.FromColor(System.Drawing.Color.Yellow);
                entity[2].Color = Color.FromColor(System.Drawing.Color.Blue);

            }
            else if (entity.Length == 2)
            {
                entity[0].Color = Color.FromColor(System.Drawing.Color.Red);
                entity[1].Color = Color.FromColor(System.Drawing.Color.Yellow);
            }

            return entity;

        }

        public void GetBlockCondition()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var entOpts = new PromptEntityOptions("请选择块\n");

            var entRes = ed.GetEntity(entOpts);

            ObjectId entId = ObjectId.Null;
            if (entRes.Status == PromptStatus.OK)
            {
                entId = entRes.ObjectId;

            }

            if (entId == ObjectId.Null)
            {
                return;
            }

            string blockName = string.Empty;
            BlockReference br;
            using (var trans = db.TransactionManager.StartTransaction())
            {

                br = trans.GetObject(entId, OpenMode.ForRead) as BlockReference;

                blockName = br.Name;

                trans.Commit();
            }

            if (String.IsNullOrEmpty(blockName))
            {
                Application.ShowAlertDialog("请输入正确的块名称");
                return;
            }

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!blkTbl.Has(blockName))
                {
                    Application.ShowAlertDialog("请输入正确的块名称");
                    return;

                }

                BlkRec = trans.GetObject(blkTbl[blockName], OpenMode.ForRead) as BlockTableRecord;

                trans.Commit();
            }

            if (BlkRec == null)
            {
                return;
            }
            if (br.Bounds == null || !br.Bounds.HasValue)
            {
                GetBlockMinMaxPoint(blockName);
            }
            else
            {
                MinBlkPt = br.Bounds.Value.MinPoint;
                MaxBlkPt = br.Bounds.Value.MaxPoint;
            }

        }

        private void GetBlockMinMaxPoint(string blockName)
        {
            if (String.IsNullOrEmpty(blockName))
            {
                return;
            }

            AcadDocument doc = Application.DocumentManager.MdiActiveDocument.GetAcadDocument() as AcadDocument;


            DBObjectCollection dbcll = new DBObjectCollection();

            List<double[]> listMin = new List<double[]>();
            List<double[]> listMax = new List<double[]>();


            foreach (AcadEntity entity in doc.ModelSpace)
            {
                if (entity.EntityName == "AcDbBlockReference")
                {
                    AcadBlockReference returnBlock = (AcadBlockReference)entity;
                    object min = null, max = null;
                    if (returnBlock.Name == blockName)
                    {
                        returnBlock.GetBoundingBox(out min, out max);
                        double[] arr = min as double[];
                        double[] arr2 = max as double[];
                        listMin.Add(arr);
                        listMax.Add(arr2);
                    }

                }
            }

            if (listMin.Count == 0 || listMax.Count == 0)
            {
                return;
            }

            listMin.Sort((min1, min2) => { return min1[0].CompareTo(min2[0]); });
            double[] minMin = listMin.First();

            listMax.Sort((min1, min2) => { return min1[0].CompareTo(min2[0]); });
            double[] maxMax = listMax[listMax.Count - 1];

            MinBlkPt = new Point3d(minMin[0], minMin[1], minMin[2]);
            MaxBlkPt = new Point3d(maxMax[0], maxMax[1], maxMax[2]);
        }

        private void GetBlkRatioCondtn1()
        {

            Ratio = (MaxPoint - MinPoint).Length / (MaxBlkPt - MinBlkPt).Length;

            MaxW = (MaxPoint.X - MinPoint.X);
            MaxH = (MaxPoint.Y - MinPoint.Y);

            BlockW = (MaxBlkPt.X - MinBlkPt.X);
            BlockH = (MaxBlkPt.Y - MinBlkPt.Y);

            RatioW = MaxW / BlockW;
            RatioH = MaxH / BlockH;

        }

        public void DrawLines(double factor)
        {
            factor = 0.5;
            double scale = 0.3;

            //宽的间隔
            double jgW = (MaxW / RatioW) * factor;

            double jgH = (MaxH / RatioH) * factor;

            int w = 1, h = 1;

            #region 画线
            /* 
                        List<Entity> listLine = new List<Entity>();

                        //画竖线
                       while (MinPoint.X + w * jgW <= MaxPoint.X)
                        {
                            Point3d pS = new Point3d(MinPoint.X + w * jgW, MinPoint.Y, 0);
                            var pE = new Point3d(MinPoint.X + w * jgW, MaxPoint.Y, 0);

                            var line = new Line(pS, pE);

                            line.Color = Color.FromColor(System.Drawing.Color.Pink);

                            listLine.Add(line);

                            w++;
                        }
                        //画横线
                        while (MinPoint.Y + h * jgH <= MaxPoint.Y)
                        {
                            Point3d pS = new Point3d(MinPoint.X, MinPoint.Y + h * jgH, 0);
                            var pE = new Point3d(MaxPoint.X, MinPoint.Y + h * jgH, 0);

                            var line = new Line(pS, pE);

                            line.Color = Color.FromColor(System.Drawing.Color.Pink);

                            listLine.Add(line);
                            h++;
                        }
                       //listLine.ToSpace();
                       */
            #endregion

            double sX = MinPoint.X;
            double sY = MinPoint.Y;

            List<List<Point3d>> listListPt = new List<List<Point3d>>();

            List<Line> listDuijiao = new List<Line>();

            List<Point3d> listPtCenter = new List<Point3d>();

            int totalY = (int)(MaxH / jgH + 1);
            int totalX = (int)(MaxW / jgW + 1);

            for (int m = 0; m < totalY; m++)
            {
                List<Point3d> listPt = new List<Point3d>();

                listListPt.Add(listPt);
                for (int k = 0; k < totalX; k++)
                {
                    var pt = new Point3d(sX + k * jgW, sY + m * jgH, 0);

                    listPt.Add(pt);
                }

            }


            double freeY = MaxH - (totalY - 1) * jgH;

            if (freeY > 0)
            {
                List<Point3d> list = new List<Point3d>();

                listListPt.Add(list);


                for (int k = 0; k < totalX; k++)
                {
                    var pt = new Point3d(sX + k * jgW, MaxPoint.Y, 0);

                    list.Add(pt);
                }

            }



            double freeX = MaxW - (totalX - 1) * jgW;

            if (freeX > 0)
            {

                for (int l = 0; l < listListPt.Count; l++)
                {

                    var list = listListPt[l];

                    var pt = new Point3d(MaxPoint.X, l * jgH + sY, 0);



                    list.Add(pt);

                }

            }

            sY = MinPoint.Y;

            for (int l1 = 0; l1 < listListPt.Count; l1++)
            {
                var list = listListPt[l1];

                for (int l2 = 0; l2 < list.Count; l2++)
                {

                    Point3d ptS = list[l2];

                    if (l1 + 1 < listListPt.Count && l2 + 1 < listListPt[l1 + 1].Count)
                    {


                        var ptE = listListPt[l1 + 1][l2 + 1];

                        var ptcenter = new Point3d((ptS.X + ptE.X) / 2, (ptS.Y + ptE.Y) / 2, 0);

                        listPtCenter.Add(ptcenter);

                        //var lineDuijiao = new Line(ptS, ptE);

                     //   lineDuijiao.ToSpace();

                    }

                }

            }

            List<BlockReference> listBr = new List<BlockReference>();
            List<double> listLen = new List<double>();
            Point3dCollection p3dcoll = new Point3dCollection();

            foreach (var center in listPtCenter)
            {

                BlockReference br = new BlockReference(center, BlkRec.Id);

                var line = new Line(new Point3d(center.X, MinPoint.Y, 0), new Point3d(center.X, MaxPoint.Y, 0));

                line.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);



                int count = p3dcoll.Count;

                double s = scale;

                double length = 0.0;

                if (count == 0)
                {
                    line.ToSpace();
                }
                line.Dispose();

                if (count == 1)
                {

                    length = (center - p3dcoll[0]).Length;

                    s =Math.Abs( scale - (length / MaxH) * 0.3);

                }

                listLen.Add(length);

                s = s > 0.3 ? 0.3 : s;

                s = s < 0.08 ? 0.08 : s;
                //s = 0.3;
                br.ScaleFactors = new Scale3d(s);
                p3dcoll.Clear();

                br.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

                int count1 = p3dcoll.Count;

                if (count1 > 0)
                {
                    p3dcoll.Clear();
                    br = null;
                }
                else
                {
                    br.IntersectWith(FirstCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

                    count1 = p3dcoll.Count;

                    if (count1 > 0)
                    {
                        p3dcoll.Clear();
                        br = null;
                    }

                }

                listBr.Add(br);
                p3dcoll.Clear();

            }

            BlockReference[,] brArr = new BlockReference[totalY, totalX];
            double[,] lenArr = new double[totalY, totalX];

            #region 集合转二维数组


            int q = 0;
            for (int i = 0; i < totalY; i++)
            {
                for (int j = 0; j < totalX; j++)
                {
                    brArr[i, j] = listBr[q];
                    lenArr[i, j] = listLen[q++];
                }
            }
            #endregion
            //求左起第一个离条件二最近的那个，记录行值
            int firstX = 0, firstY = 0;
            List<BlockReference> listBrToSpc = new List<BlockReference>();
            double l3 = 0.0;
            double l4 = 0.0;
            while (firstX < totalX)
            {
                double min = lenArr[firstY, firstX];

                for (int i = 0; i < totalY; i++)
                {

                    if (brArr[i, firstX] != null && lenArr[i, firstX] < min)
                    {
                        min = lenArr[i, firstX];
                        firstY = i;
                    }

                }

                var brTemp = brArr[firstY, firstX];
                if (firstX == 0)
                {


                    var PtMin = brTemp.Bounds.Value.MinPoint;
                    var ptMax = brTemp.Bounds.Value.MaxPoint;

                    l3 = Math.Abs(ptMax.X - PtMin.X);
                    l4 = Math.Abs(ptMax.Y - PtMin.Y);

                }
                if (brTemp != null)
                {
                    var PtMin = brTemp.Bounds.Value.MinPoint;
                    var ptMax = brTemp.Bounds.Value.MaxPoint;

                    l3 = Math.Abs(ptMax.X - PtMin.X);
                    l4 = Math.Abs(ptMax.Y - PtMin.Y);
                }

                var brToSpace = brTemp;

                p3dcoll.Clear();

                Point3d ptJd = Point3d.Origin;

                Point3d firstCenter = listPtCenter[firstY * totalX + firstX];

                var firstLine = new Line(new Point3d(firstCenter.X, MinPoint.Y, 0), new Point3d(firstCenter.X, MaxPoint.Y, 0));

                firstLine.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

                if (p3dcoll.Count > 0)
                {
                    ptJd = p3dcoll[0];



                    int upI = firstY;
                    int loopIndex = 1;
                    Vector3d vec = Vector3d.XAxis * 0;

                    double jgHigh = min * 0.8;

                    double scale1 = 0.3;

                    while (upI < totalY)
                    {

                        var brY = brArr[upI, firstX];

                        if (brY == null)
                        {
                            upI += 1;
                            continue;
                        }

                        //var PtMin = brY.Bounds.Value.MinPoint;
                        //var ptMax = brY.Bounds.Value.MaxPoint;

                        //var PointCenter = new Point3d((PtMin.X + ptMax.X) / 2, (PtMin.Y + ptMax.Y) / 2, 0);
                        //var moveY = (PointCenter - ptJd).Length - Math.Abs(ptMax.Y - PtMin.Y) / 2;

                        //var moveY = Math.Abs(min - Math.Abs((ptMax.Y - PtMin.Y) / 2));



                     //   if (PtMin.Y > ptJd.Y)
                     //       brY.TransformBy(Matrix3d.Displacement(-Vector3d.YAxis * moveY * 0.8 + Vector3d.YAxis * jgH * (loopIndex * 1.0 / totalY) * 6));
                     //   else
                     //       brY.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * moveY * 0.8 - Vector3d.YAxis * jgH * (loopIndex * 1.0 / totalY) * 6));


                        int m = 0;

                        double s1 = scale1 - (loopIndex * 1.0) / totalY;

                        //brY.ScaleFactors = new Scale3d(s1);

                        if (loopIndex == 1 || loopIndex == 2) {

                            vec = Vector3d.XAxis * 0;

                            vec +=  Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;

                            //brY.TransformBy(Matrix3d.Displacement(vec));
                        }
                        else
                        {

                        
                        if ((upI + 1) % 2 == 0)
                        {
                            vec = Vector3d.XAxis * 0;

                                //while (m < loopIndex)
                                //{
                                //    vec += Vector3d.XAxis * loopIndex * l3*1.3/ totalY+Vector3d.YAxis*loopIndex*l3/totalY;

                                //    m++;
                                //}

                                vec += Vector3d.XAxis * loopIndex*loopIndex * l3 * 1.4 / totalY + Vector3d.YAxis *loopIndex* loopIndex * l3 / totalY;
                                //m = 0;

                            brY.TransformBy(Matrix3d.Displacement(vec));
                        }
                        else
                        {
                            vec = Vector3d.XAxis * 0;
                            //while (m < loopIndex)
                            //{
                            //    vec -= Vector3d.XAxis * loopIndex* loopIndex * l3 * 1.3 / totalY + Vector3d.YAxis * loopIndex* loopIndex * l3 / totalY;
                            //    m++;
                            //}
                                vec -= Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY + Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;
                                m = 0;
                            brY.TransformBy(Matrix3d.Displacement(vec));
                        }
                        }

                        var PtMin2 = brY.Bounds.Value.MinPoint;
                        var ptMax2 = brY.Bounds.Value.MaxPoint;

                        var PointCenterY = new Point3d((PtMin2.X + ptMax2.X) / 2, (PtMin2.Y + ptMax2.Y) / 2, 0);

                        brY.Rotation=GetRotateMtx(PointCenterY);

                        loopIndex++;

                        listBrToSpc.Add(brY);

                        upI += 1;

                    }
                    upI = firstY;

                    loopIndex = 1;

                    while (upI >= 0)
                    {
                        if (upI - 1 >= 0 && brArr[upI - 1, firstX] != null)
                        {

                            var brY = brArr[upI - 1, firstX];
                            //if (PtMin.Y > ptJd.Y)
                           //     brY.TransformBy(Matrix3d.Displacement(-Vector3d.YAxis * moveY * 0.8 + Vector3d.YAxis * jgH * (loopIndex * 1.0 / totalY) * 6));
                        //    else
                          //      brY.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * moveY * 0.8 - Vector3d.YAxis * jgH * (loopIndex * 1.0 / totalY) * 6));

                            int m = 0;

                            if ((upI - 1) % 2 == 0)
                            {
                                vec = Vector3d.XAxis * 0;
                                vec += Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY + Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;
                                
                                //while (m < loopIndex)
                                //{
                                //    vec += Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY + Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;
                                //    m++;
                                //}
                                //m = 0;
                                brY.TransformBy(Matrix3d.Displacement(vec));
                            }
                            else
                            {
                                vec = Vector3d.XAxis * 0;
                                vec -= Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY + Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;
                                //while (m < loopIndex)
                                //{
                                //    vec -= Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY + Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;
                                //    m++;
                                //}
                                m = 0;
                                brY.TransformBy(Matrix3d.Displacement(vec));
                            }

                            var PtMin = brY.Bounds.Value.MinPoint;
                            var ptMax = brY.Bounds.Value.MaxPoint;

                            var PointCenterY = new Point3d((PtMin.X + ptMax.X) / 2, (PtMin.Y + ptMax.Y) / 2, 0);

                            //brY.TransformBy(GetRotateMtx(PointCenterY));
                            brY.Rotation = GetRotateMtx(PointCenterY);
                            loopIndex++;
                            listBrToSpc.Add(brY);
                        }

                        upI -= 1;


                    }

                }
                firstX++;
                // break;
            }
            #region ZHUSHI 多线程


            /* TaskFactory tf = new TaskFactory();

             Parallel.For(0, totalX, (firstX) =>
             {
                 double min = lenArr[firstY, firstX];



                 if (firstX == 2)
                 {
                     int a = 10;
                 }

                 for (int i = 0; i < totalY; i++)
                 {


                     if (brArr[i, firstX] != null && lenArr[i, firstX] < min)
                     {
                         min = lenArr[i, firstX];
                         firstY = i;
                     }

                 }

                 if (firstX == 0)
                 {
                     var brTemp = brArr[firstY, firstX];

                     var PtMin = brTemp.Bounds.Value.MinPoint;
                     var ptMax = brTemp.Bounds.Value.MaxPoint;

                     ll2 = ptMax.X - PtMin.X;
                     l3 = ll2;

                 }

                 var brToSpace = brArr[firstY, firstX];
                 p3dcoll.Clear();

                 Point3d firstCenter = listPtCenter[firstY * totalX + firstX];

                 var firstLine = new Line(new Point3d(firstCenter.X, MinPoint.Y, 0), new Point3d(firstCenter.X, MaxPoint.Y, 0));

                 firstLine.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);



                 if (p3dcoll.Count > 0)
                 {

                     if (brToSpace != null)
                     {
                         brToSpace.TransformBy(GetRotateMtx(firstCenter));

                         listBrToSpc.Add(brToSpace);
                     }

                     int upI = firstY;

                     while (upI < totalY)
                     {
                         if (upI + 1 < totalY && brArr[upI + 1, firstX] != null)
                         {

                             var brY = brArr[upI + 1, firstX];

                             var PointCenterY = listPtCenter[(upI + 1) * totalX + firstX];

                             brY.TransformBy(GetRotateMtx(PointCenterY));


                             double m = 0;
                             if ((upI + 1) % 2 == 0)
                             {
                                 while (m < l3 && !IsRectXJCon(brY, SecondCondition) && !IsRectXJCon(brY, FirstCondition))
                                 {

                                     brY.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * 0.2 * l3));

                                     m += 0.5 * l3;
                                 }
                             }
                             else
                             {
                                 while (m < l3 && !IsRectXJCon(brY, SecondCondition) && !IsRectXJCon(brY, FirstCondition))
                                 {

                                     brY.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * -0.2 * l3));

                                     m += 0.5 * l3;
                                 }

                             }

                             ll2 += l3;


                             m = 0;

                             listBrToSpc.Add(brY);

                         }
                         upI += 1;


                     }
                     upI = firstY;

                     while (upI >= 0)
                     {
                         if (upI - 1 >= 0 && brArr[upI - 1, firstX] != null)
                         {

                             var brY = brArr[upI - 1, firstX];

                             var PointCenterY = listPtCenter[(upI - 1) * totalX + firstX];

                             brY.TransformBy(GetRotateMtx(PointCenterY));

                             double m = 0.0;

                             if ((upI - 1) % 2 == 0)
                             {
                                 while (m < l3 && !IsRectXJCon(brY, SecondCondition) && !IsRectXJCon(brY, FirstCondition))
                                 {

                                     brY.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * 0.2 * l3));

                                     m += 0.5 * l3;
                                 }
                             }
                             else
                             {
                                 while (m < l3 && !IsRectXJCon(brY, SecondCondition) && !IsRectXJCon(brY, FirstCondition))
                                 {

                                     brY.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * -0.2 * l3));

                                     m += 0.5 * l3;
                                 }

                             }
                             listBrToSpc.Add(brY);
                         }

                         upI -= 1;


                     }

                 }
                 firstX++;
                 ll2 = l3;



             });*/

            #endregion

            List<BlockReference> listRemove = new List<BlockReference>();


            for (int i = 0; i < listBrToSpc.Count; i++)
            {
                var br = listBrToSpc[i];

                for (int j=i+1; j < listBrToSpc.Count; j++)
                {
                    var br2 = listBrToSpc[j];

                    if (IsRecXjRec(br, br2))
                    {
                    //    listRemove.Add(br);

                    }
                }
            }

            listRemove.ForEach(b => { listBrToSpc.Remove(b); });


            listRemove.Clear();

            foreach (var br in listBrToSpc)
            {
                var minPt = br.Bounds.Value.MinPoint;
                var maxPt = br.Bounds.Value.MaxPoint;

                if(PtInPl.PtRelationToPoly(FirstCondition,minPt,1.0E-4)==-1|| PtInPl.PtRelationToPoly(FirstCondition, maxPt, 1.0E-4) == -1){

                    listRemove.Add(br);
                }
            }

            listRemove.ForEach(b => { listBrToSpc.Remove(b); });

            listBrToSpc.ToSpace();

        }


        private double GetRotateMtx(Point3d firstCenter)
        {
            Point3dCollection p3dcoll = new Point3dCollection();
            var firstLine = new Line(new Point3d(firstCenter.X, MinPoint.Y, 0), new Point3d(firstCenter.X, MaxPoint.Y, 0));

            firstLine.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);



            if (p3dcoll.Count > 0)
            {

                Vector3d vec1 = SecondCondition.GetFirstDerivative(p3dcoll[0]);

                var vec = vec1.X * vec1.Y > 0 ? vec1 : -vec1;

                return vec.GetAngleTo(Vector3d.XAxis);
               // return Matrix3d.Rotation(vec.GetAngleTo(Vector3d.XAxis), Vector3d.ZAxis, firstCenter);

            }
            return 0;
            //return Matrix3d.Rotation(0, Vector3d.ZAxis, firstCenter); ;
        }


        private bool point3dEqual(Point3d p1, Point3d p2)
        {

            if (p1.X.ToString("f9") == p2.X.ToString("f9") && p1.Y.ToString("f9") == p2.Y.ToString("f9") && p1.Z.ToString("f9") == p2.Z.ToString("f9"))
                return true;
            return false;
        }

        private List<BlockReference> BlkScale2(double scale, Point3d min, Point3d max)
        {


            List<BlockReference> listBrUPOrg = new List<BlockReference>();
            List<BlockReference> listBrUPDel = new List<BlockReference>();

            List<Polyline> lplUPOrg = new List<Polyline>();
            List<Polyline> lplUPDel = new List<Polyline>();

            List<Point3d> listPt = new List<Point3d>();

            Point3d ptPos = splDirection ? ptPos = Intersect1 : ptPos = Intersect2;

            BlockReference temp = new BlockReference(ptPos, BlkRec.Id);

            temp.ScaleFactors = new Scale3d(scale);

            Point3d t1 = (Point3d)temp.Bounds?.MinPoint;
            Point3d t2 = (Point3d)temp.Bounds?.MaxPoint;

            var bC = FirstCondition.Bounds;


            int count = (int)(Math.Abs(bC.Value.MaxPoint.X - bC.Value.MinPoint.X) / Math.Abs(t2.X - t1.X));

            double moveHigh = Math.Abs(t2.Y - t1.Y);

            double allLength = SecondCondition.GetDistAtPoint(SecondCondition.EndPoint);

            double oneLength = allLength / count;

            oneLength += oneLength / 3;

            int i = 0;
            try
            {
                do
                {

                    BlockReference brUP = new BlockReference(ptPos, BlkRec.Id);
                    BlockReference brDwn = new BlockReference(ptPos, BlkRec.Id);

                    brUP.ScaleFactors = new Scale3d(scale);
                    brDwn.ScaleFactors = new Scale3d(scale);

                    Point3d p1 = (Point3d)brUP.Bounds?.MinPoint;
                    Point3d p2 = (Point3d)brUP.Bounds?.MaxPoint;

                    Polyline plUP = GetMinRect(p1, p2);
                    Polyline plDwn = GetMinRect(p1, p2);




                    Point3d center = new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, 0);


                    Point3d movePt = Point3d.Origin;

                    if (splDirection)
                    {
                        movePt = new Point3d(center.X + Math.Abs((p2.X - p1.X)) / 2, center.Y, center.Z);
                    }
                    else
                    {
                        movePt = new Point3d(center.X - Math.Abs((p2.X - p1.X)) / 2, center.Y, center.Z);
                    }





                    var matrix2 = Matrix3d.Displacement(movePt - center);
                    Point3d movePt2 = SecondCondition.GetPointAtDist(oneLength * i);

                    listPt.Add(movePt2);

                    var vec1 = SecondCondition.GetFirstDerivative(movePt2);
                    var vec = vec1.X * vec1.Y > 0 ? vec1 : -vec1;
                    double angle = vec.GetAngleTo(Vector3d.XAxis);

                    var mtxRotate = Matrix3d.Rotation(angle, Vector3d.ZAxis, center);

                    if (i > 0 && !point3dEqual(movePt2, SecondCondition.EndPoint))
                    {
                        matrix2 = Matrix3d.Displacement(movePt2 - movePt) * matrix2;
                    }

                    brUP.TransformBy(matrix2);
                    plUP.TransformBy(matrix2);

                    brDwn.TransformBy(matrix2);
                    plDwn.TransformBy(matrix2);


                    /*  Vector3d vec = Vector3d.XAxis * 0;

                      Point3dCollection p3dColl2 = new Point3dCollection();

                      SecondCondition.IntersectWith(plUP, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                      if (p3dColl2.Count <= 1)
                      {


                      }
                      else
                      {

                          var list = p3dColl2.Cast<Point3d>().OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();

                          Point3d p3d1 = list.First();

                          Point3d p3d2 = list[list.Count - 1];

                          vec = splDirection ? p3d2 - p3d1 : p3d1 - p3d2;

                      }



                      plUP.TransformBy(mtxRotate);
                      brUP.TransformBy(mtxRotate);

                      brDwn.TransformBy(mtxRotate);
                      plDwn.TransformBy(mtxRotate);*/

                    double rito1 = 0.005, rito2 = 0.005;
                    double five = 0.5;

                    if ((vec.X > 0 && vec.Y > 0) || (vec.X > 0 && vec.Y < 0))
                    {
                        five = 0.5;
                    }
                    else if ((vec.X < 0 && vec.Y > 0) || (vec.X < 0 && vec.Y < 0))
                    {
                        five = -0.5;
                    }

                    Matrix3d mtxDisUP = splDirection ? Matrix3d.Displacement(vec.RotateBy(Math.PI * five, Vector3d.ZAxis)) :
                        Matrix3d.Displacement(vec.RotateBy(Math.PI * -1 * five, Vector3d.ZAxis));

                    Matrix3d mtxDisDwn = splDirection == false ? Matrix3d.Displacement(vec.RotateBy(Math.PI * five, Vector3d.ZAxis)) :
                        Matrix3d.Displacement(vec.RotateBy(Math.PI * -1 * five, Vector3d.ZAxis));

                    while (true/*IsRectXJCon(plUP, SecondCondition)*/)
                    {

                        mtxDisUP = splDirection ? Matrix3d.Displacement(vec.RotateBy(Math.PI * five, Vector3d.ZAxis) * rito1) :
                        Matrix3d.Displacement(vec.RotateBy(Math.PI * -five, Vector3d.ZAxis) * rito1);

                        plUP.TransformBy(mtxDisUP);
                        brUP.TransformBy(mtxDisUP);

                        rito1 += 0.005;

                        if (rito1 > 2)
                        {
                            break;
                        }

                    }

                    while (/*IsRectXJCon(plDwn, SecondCondition)*/true)
                    {

                        mtxDisDwn = splDirection == false ? Matrix3d.Displacement(vec.RotateBy(Math.PI * five, Vector3d.ZAxis)) :
                         Matrix3d.Displacement(vec.RotateBy(Math.PI * -five, Vector3d.ZAxis));

                        brDwn.TransformBy(mtxDisDwn);
                        plDwn.TransformBy(mtxDisDwn);

                        rito2 += 0.005;
                        if (rito2 > 2)
                        {
                            break;
                        }
                    }

                    //判断是否和条件一相交

                    /*      if (BlkRotateToSpace(ref brUP, ref plUP, center))
                          {



                              if (!IsRecXjRec(preBrUP, brUP))
                              {

                                  brUP.ToSpace();
                              }

                              if (!IsRecXjRec(prePlUP, plUP))
                              {

                                  plUP.ToSpace();
                              }

                          };
                          if (BlkRotateToSpace(ref brDwn, ref plDwn, center))
                          {
                              //if (!IsRecXjRec(prePlDwn, brDwn))
                              //{

                                  brDwn.ToSpace();
                              //}

                              //if (!IsRecXjRec(prePlDwn, plDwn))
                              //{

                                  plDwn.ToSpace();
                           //   }


                          }



                          preBrUP = brUP;
                          preBrDwn = brDwn;

                          prePlUP = plUP;
                          prePlDwn = plDwn;*/
                    listBrUPOrg.Add(brUP);
                    lplUPOrg.Add(plUP);

                    i++;

                } while (oneLength * i < allLength);

                foreach (var j in Enumerable.Range(0, listBrUPOrg.Count))
                {

                    BlockReference brU = listBrUPOrg[j];
                    BlockReference brU2 = null;

                    Polyline plU = lplUPOrg[j];
                    Polyline plU2 = null;

                    Point3d p = listPt[j];


                    if (j + 1 < listBrUPOrg.Count)
                    {
                        brU2 = listBrUPOrg[j + 1];
                        plU2 = lplUPOrg[j + 1];
                    }

                    if (/*IsRectXJCon(plU, FirstCondition)*/true)
                    {
                        continue;
                    }
                    double left = 1;
                    double up = 1;
                    int m = 1;
                    if (j == listBrUPOrg.Count - 1)
                    {
                        plU.Color = Color.FromColor(System.Drawing.Color.Pink);
                        plU2 = lplUPOrg[j - 1];
                    }
                    while ((plU2 == null && j == listBrUPOrg.Count - 1 && PtInPl.PtRelationToPoly(plU, p, 1.0E-4) != -1 && m <= 3 * moveHigh) || (plU2 != null && IsRecXjRec(plU, plU2)) && m <= 3 * moveHigh)
                    {

                        brU.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * -left * m + Vector3d.YAxis * up * m));
                        plU.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * -left * m + Vector3d.YAxis * up * m));
                        m++;
                    }

                    if (plU2 != null && IsRecXjRec(plU, plU2) ||  true||/*IsRectXJCon(plU, FirstCondition) ||*/ PtInPl.PtRelationToPoly(plU, p, 1.0E-4) != -1)
                    {
                        continue;
                    }

                    listBrUPDel.Add(brU);
                    lplUPDel.Add(plU);

                }
            }
            catch (System.Exception r)
            {

                throw;
            }




            listBrUPDel.ToSpace();
            lplUPDel.ToSpace();


            return listBrUPOrg;


        }
        bool IsRectXJCon(BlockReference br, Polyline condition)
        {

            Point3dCollection p3dColl2 = new Point3dCollection();

            condition.IntersectWith(br, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

            if (p3dColl2.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        bool IsRectXJCon2(Entity br)
        {

            Point3dCollection p3dColl2 = new Point3dCollection();

            FirstCondition.IntersectWith(br, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

            if (p3dColl2.Count > 1)
            {
                return true;
            }
            else
            {
                return false;
            }

        }


        public static bool   GreateThan(Point3d p1,Point3d p2)
        {

            return (p1.X > p2.X && p1.Y > p2.Y);

        }

        public static bool  lowerThan(Point3d p1, Point3d p2)
        {

            return (p1.X < p2.X && p1.Y < p2.Y);

        }

        bool IsRecXjRec(Entity ent1, Entity ent2)
        {
            if (ent1 == null || ent2 == null)
            {
                return false;
            }

            //return false;
            var br1 = ent1 as BlockReference;
            var br2 = ent2 as BlockReference;

            var angle1 = br1.Rotation;
            var angle2 = br2.Rotation;

            var min1 = br1.Bounds.Value.MinPoint;
            var max1 = br1.Bounds.Value.MaxPoint;

            var min2 = br2.Bounds.Value.MinPoint;
            var max2 = br2.Bounds.Value.MaxPoint;

            var line1 = new Line(min1, max1);
            var line2 = new Line(min2, max2);


            //Point3d[] pt3Arr = GetPoint3d(min1, max1, angle1);

            //Point3d[] pt3Arr2 = GetPoint3d(min2, max2, angle2);

            //Polyline p1 = GetPolyline(min1, max1, pt3Arr);

            //p1.ToSpace();
            //Polyline p2 = GetPolyline(min2, max2, pt3Arr2);

            /*var left1 = min1.X > pt3Arr[0].X ? pt3Arr[0] : min1;
            var left2 = min2.X > pt3Arr2[0].X ? pt3Arr2[0] : min2;

            var right1 = max1.X > pt3Arr[1].X ? max1 : pt3Arr[1];
            var right2 = max2.X > pt3Arr2[1].X ? max2 : pt3Arr2[1];

            var top1 = max1.Y < pt3Arr[0].Y ? pt3Arr[0] : max1;
            var top2 = max2.Y < pt3Arr2[0].Y ? pt3Arr2[0] : max2;

            var bottom1 = min1.Y > pt3Arr[1].Y ? pt3Arr[1] : min1;
            var bottom2 = min2.Y > pt3Arr2[1].Y ? pt3Arr2[1] : min2;*/


            //List<Point3d> listA = new List<Point3d>();
            //listA.Add(min1);
            //listA.Add(max1);
            //listA.Add(pt3Arr[0]);
            //listA.Add(pt3Arr[1]);

            //var left1 = listA.OrderBy(p => p.X).First();
            //var right1 = listA.OrderBy(p => p.X).ToList()[3];
            //var top1 = listA.OrderByDescending(p => p.Y).First();
            //var bottom1 = listA.OrderBy(p => p.Y).First();


            //List<Point3d> listB = new List<Point3d>();
            //listB.Add(min2);
            //listB.Add(max2);
            //listB.Add(pt3Arr2[0]);
            //listB.Add(pt3Arr2[1]);

            //var left2 = listB.OrderBy(p => p.X).First();
            //var right2 = listB.OrderByDescending(p => p.Y).First();
            //var top2 = listB.OrderByDescending(p => p.Y).First();
            //var bottom2 = listB.OrderBy(p => p.Y).First();

            //return !((lowerThan(max1 , min2) || GreateThan(pt3Arr[1] , pt3Arr2[0])) ||
            //  (lowerThan(max2 , min1) || GreateThan(pt3Arr2[1] , pt3Arr[0]))
            //);
            //return !(((right1.X < left2.X) || (bottom1.Y > top2.Y)) ||
            //  ((right2.X < left1.X) || (bottom2.Y > top1.Y)));


            Point3dCollection p3dColl2 = new Point3dCollection();

            br1.IntersectWith(br2, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

            //p1.Dispose();
            //p2.Dispose();

            if (p3dColl2.Count > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        private Polyline GetPolyline(Point3d min1, Point3d max1, Point3d[] pt3Arr)
        {
            Polyline p = new Polyline(4);

            p.AddVertexAt(p.NumberOfVertices, new Point2d(min1.X, min1.Y), 0, 0, 0);
            p.AddVertexAt(p.NumberOfVertices, new Point2d(pt3Arr[0].X, pt3Arr[0].Y), 0, 0, 0);
            p.AddVertexAt(p.NumberOfVertices, new Point2d(max1.X, max1.Y), 0, 0, 0);
            p.AddVertexAt(p.NumberOfVertices, new Point2d(pt3Arr[1].X, pt3Arr[1].Y), 0, 0, 0);

            p.Closed = true;

            return p;
        }

        private Point3d[] GetPoint3d(Point3d min, Point3d max, double r)
        {
            Point3d[] p3dArr = new Point3d[2];

            var x1 = min.X;
            var y1 = min.Y;

            var x2 = max.X;
            var y2 = max.Y;

            var x = x2 - (x2 - x1) / (Math.Cos(r) * Math.Cos(r));
            var y = y1 + Math.Tan(r) * (x2 - x1);

            p3dArr[0] = new Point3d(x, y, 0);

            var y4 = (y2 - y) * Math.Sin(r) * Math.Sin(r) + y;
            var x4 = x2 + (y2 - y) * Math.Sin(r) * Math.Cos(r);

            p3dArr[1] = new Point3d(x4, y4,0);


            return p3dArr;

        }

        Polyline GetMinRect(Point3d min, Point3d max)
        {
            Polyline pl = new Polyline(4);

            Point2d p2 = new Point2d(max.X, min.Y);

            Point2d p4 = new Point2d(min.X, max.Y);

            pl.AddVertexAt(pl.NumberOfVertices, new Point2d(min.X, min.Y), 0, 0, 0);

            pl.AddVertexAt(pl.NumberOfVertices, p2, 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, new Point2d(max.X, max.Y), 0, 0, 0);

            pl.AddVertexAt(pl.NumberOfVertices, p4, 0, 0, 0);

            pl.Closed = true;
            Polyline3d pppp = new Polyline3d();

            return pl;
        }

        private List<BlockReference> BlkScale3(double scale, Point3d min, Point3d max, double jianju, ref List<Polyline> lplUPOrg)
        {

            List<BlockReference> listBrUPOrg = new List<BlockReference>();

            lplUPOrg = new List<Polyline>();

            Point3d ptPos = splDirection ? ptPos = Intersect1 : ptPos = Intersect2;

            BlockReference temp = new BlockReference(ptPos, BlkRec.Id);

            temp.ScaleFactors = new Scale3d(scale);

            Point3d t1 = (Point3d)temp.Bounds?.MinPoint;
            Point3d t2 = (Point3d)temp.Bounds?.MaxPoint;

            var bC = FirstCondition.Bounds;
            var c1Width = Math.Abs(bC.Value.MaxPoint.X - bC.Value.MinPoint.X);
            double blockWidth = Math.Abs(t2.X - t1.X);//块的宽度

            var c1Height = Math.Abs(bC.Value.MaxPoint.Y - bC.Value.MinPoint.Y);
            int count = (int)(c1Width / blockWidth);



            double moveHigh = Math.Abs(t2.Y - t1.Y);//块的高度

            double allLength = SecondCondition.GetDistAtPoint(SecondCondition.EndPoint);

            double oneLength = allLength / count;

            oneLength += oneLength / 3;

            int i = 0;

            Vector3d vec3d = Vector3d.XAxis;

            do
            {

                BlockReference brUP = new BlockReference(ptPos, BlkRec.Id);

                brUP.ScaleFactors = new Scale3d(scale);

                Point3d p1 = brUP.Bounds.Value.MinPoint;
                Point3d p2 = brUP.Bounds.Value.MaxPoint;

                Polyline plUP = GetMinRect(p1, p2);


                Point3d center = new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, 0);

                //移动到边界处
                Point3d movePt = splDirection == true ? new Point3d(center.X + Math.Abs((p2.X - p1.X)) / 2, center.Y, 0)
                    : new Point3d(center.X - Math.Abs((p2.X - p1.X)) / 2, center.Y, 0);


                var matrix2 = Matrix3d.Displacement(movePt - center);

                brUP.TransformBy(matrix2);
                plUP.TransformBy(matrix2);

                vec3d = Vector3d.XAxis * i * blockWidth;
                vec3d += Vector3d.XAxis * jianju * i * blockWidth;

                brUP.TransformBy(Matrix3d.Displacement(vec3d));
                plUP.TransformBy(Matrix3d.Displacement(vec3d));

                Point3d ptMoved = brUP.Bounds.Value.MinPoint;
                Point3d ptMoved2 = brUP.Bounds.Value.MaxPoint;

                var ptM4 = new Point3d(ptMoved.X, ptMoved2.Y, 0);

                Line line1 = new Line(ptMoved, ptM4);
                Line line2 = new Line(ptM4, ptMoved);

                if (i == 8)
                {
                    int a = 54;

                }

                Point3dCollection p3dColl2 = new Point3dCollection();
                // 如果块直接和条件一相交就舍去
                FirstCondition.IntersectWith(plUP, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                if (p3dColl2.Count > 1)
                {
                    p3dColl2.Clear();
                    i++;
                    continue;
                }

                int c = 0;

                int c1 = 0;
                int c2 = 0;

                double piece = moveHigh / 10;

                SecondCondition.IntersectWith(plUP, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                c = p3dColl2.Count;

                Point3dCollection p3dColl4 = new Point3dCollection();

                int m = 1;
                if (c == 0)
                {
                    do
                    {
                        line1.IntersectWith(SecondCondition, Intersect.ExtendThis, p3dColl4, IntPtr.Zero, IntPtr.Zero);

                        if (line1.Length * m++ > c1Height)
                        {
                            break;
                        }

                    } while (p3dColl4.Count < 1);// (false);//while (c2 < 1&&c3<1);

                    if (p3dColl4.Count != 0)
                    {
                        if (p3dColl4[0].Y - ptM4.Y > 0)
                        {
                            brUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * Math.Abs(p3dColl4[0].Y - ptM4.Y)));
                            plUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * Math.Abs(p3dColl4[0].Y - ptM4.Y)));
                            c1 = 1;
                        }
                        m = 0;
                        p3dColl4.Clear();

                    }
                }

                if (c == 0 && c1 != 1)
                {
                    do
                    {
                        line2.IntersectWith(SecondCondition, Intersect.ExtendThis, p3dColl4, IntPtr.Zero, IntPtr.Zero);

                        if (line2.Length * m++ > c1Height)
                        {
                            break;
                        }

                    } while (p3dColl4.Count < 1);// (false);//while (c2 < 1&&c3<1);

                    if (p3dColl4.Count != 0)
                    {
                        if (p3dColl4[0].Y - ptMoved.Y < 0)
                        {
                            brUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * -Math.Abs(p3dColl4[0].Y - ptMoved.Y)));
                            plUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * -Math.Abs(p3dColl4[0].Y - ptMoved.Y)));
                            c2 = 0;
                            //var line = new Line(p3dColl4[0], ptMoved);
                            //line.ToSpace();
                            p3dColl4.Clear();
                        }
                        c1 = 0;
                    }
                }

                do
                {

                    SecondCondition.IntersectWith(plUP, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                    c = p3dColl2.Count;

                    brUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * piece));
                    plUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * piece));

                    p3dColl2.Clear();

                } while (c >= 1);

                // 如果块直接和条件一相交就舍去
                FirstCondition.IntersectWith(plUP, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                if (p3dColl2.Count > 1)
                {
                    p3dColl2.Clear();
                    i++;
                    continue;
                }

                var firstPt = plUP.GetPoint3dAt(0);
                var secondPt = plUP.GetPoint3dAt(1);
                var thridPt = plUP.GetPoint3dAt(2);
                var firthPt = plUP.GetPoint3dAt(3);

                if (PtInPl.PtRelationToPoly(FirstCondition, firstPt, 1.0E-4) == -1
                    || PtInPl.PtRelationToPoly(FirstCondition, secondPt, 1.0E-4) == -1
                    || PtInPl.PtRelationToPoly(FirstCondition, thridPt, 1.0E-4) == -1
                    || PtInPl.PtRelationToPoly(FirstCondition, firthPt, 1.0E-4) == -1
                    )
                {
                    i++;

                    continue;

                }



                listBrUPOrg.Add(brUP);
                lplUPOrg.Add(plUP);
                i++;
            } while (i < count);

            listBrUPOrg.ToSpace();
            lplUPOrg.ToSpace();


            return listBrUPOrg;
        }

        private List<BlockReference> BlkScaleDown(double scale, Point3d min, Point3d max, double jianju, ref List<Polyline> lplDownOrg)
        {

            List<BlockReference> listBrDownOrg = new List<BlockReference>();

            lplDownOrg = new List<Polyline>();

            Point3d ptPos = splDirection ? ptPos = Intersect1 : ptPos = Intersect2;

            BlockReference temp = new BlockReference(ptPos, BlkRec.Id);

            temp.ScaleFactors = new Scale3d(scale);

            Point3d t1 = (Point3d)temp.Bounds?.MinPoint;
            Point3d t2 = (Point3d)temp.Bounds?.MaxPoint;

            var bC = FirstCondition.Bounds;
            var c1Width = Math.Abs(bC.Value.MaxPoint.X - bC.Value.MinPoint.X);
            double blockWidth = Math.Abs(t2.X - t1.X);//块的宽度

            var c1Height = Math.Abs(bC.Value.MaxPoint.Y - bC.Value.MinPoint.Y);
            int count = (int)(c1Width / blockWidth);

            double moveHigh = Math.Abs(t2.Y - t1.Y);//块的高度

            double allLength = SecondCondition.GetDistAtPoint(SecondCondition.EndPoint);

            double oneLength = allLength / count;

            oneLength += oneLength / 3;

            int i = 0;

            Vector3d vec3d;

            do
            {
                BlockReference brDwn = new BlockReference(ptPos, BlkRec.Id);
                brDwn.ScaleFactors = new Scale3d(scale);

                Point3d p1 = brDwn.Bounds.Value.MinPoint;
                Point3d p2 = brDwn.Bounds.Value.MaxPoint;

                Polyline plDwn = GetMinRect(p1, p2);

                Point3d center = new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, 0);

                //移动到边界处
                Point3d movePt = splDirection == true ? new Point3d(center.X + Math.Abs((p2.X - p1.X)) / 2, center.Y, 0)
                    : new Point3d(center.X - Math.Abs((p2.X - p1.X)) / 2, center.Y, 0);


                var matrix2 = Matrix3d.Displacement(movePt - center);

                brDwn.TransformBy(matrix2);
                plDwn.TransformBy(matrix2);

                vec3d = Vector3d.XAxis * i * blockWidth;
                vec3d += Vector3d.XAxis * jianju * i * blockWidth;

                brDwn.TransformBy(Matrix3d.Displacement(vec3d));
                plDwn.TransformBy(Matrix3d.Displacement(vec3d));

                if (i == 6)
                {
                    plDwn.Color = Color.FromColor(System.Drawing.Color.Blue);
                }
                if (i == 7)
                {
                    plDwn.Color = Color.FromColor(System.Drawing.Color.Blue);
                }

                Point3d ptMoved = brDwn.Bounds.Value.MinPoint;
                Point3d ptMoved2 = brDwn.Bounds.Value.MaxPoint;

                var ptM4 = new Point3d(ptMoved.X, ptMoved2.Y, 0);

                Line line1 = new Line(ptMoved, ptM4);
                Line line2 = new Line(ptM4, ptMoved);



                Point3dCollection p3dColl2 = new Point3dCollection();
                // 如果块直接和条件一相交就舍去
                FirstCondition.IntersectWith(plDwn, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                if (p3dColl2.Count > 1)
                {
                    p3dColl2.Clear();
                    i++;
                    continue;
                }

                int c = 0;

                int c1 = 0;
                int c2 = 0;

                double piece = moveHigh / 10;

                SecondCondition.IntersectWith(plDwn, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                c = p3dColl2.Count;

                Point3dCollection p3dColl4 = new Point3dCollection();

                int m = 1;
                if (c == 0)
                {
                    do
                    {
                        line1.IntersectWith(SecondCondition, Intersect.ExtendThis, p3dColl4, IntPtr.Zero, IntPtr.Zero);

                        if (line1.Length * m++ > c1Height)
                        {
                            break;
                        }

                    } while (p3dColl4.Count < 1);// (false);//while (c2 < 1&&c3<1);

                    if (p3dColl4.Count != 0)
                    {
                        if (p3dColl4[0].Y - ptM4.Y > 0)
                        {
                            brDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * Math.Abs(p3dColl4[0].Y - ptM4.Y)));
                            plDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * Math.Abs(p3dColl4[0].Y - ptM4.Y)));
                            c1 = 1;
                        }
                        m = 0;
                        p3dColl4.Clear();


                    }
                }

                if (c == 0 && c1 != 1)
                {
                    do
                    {
                        line2.IntersectWith(SecondCondition, Intersect.ExtendThis, p3dColl4, IntPtr.Zero, IntPtr.Zero);

                        if (line2.Length * m++ > c1Height)
                        {
                            break;
                        }

                    } while (p3dColl4.Count < 1);// (false);//while (c2 < 1&&c3<1);

                    if (p3dColl4.Count != 0)
                    {
                        if (p3dColl4[0].Y - ptMoved.Y < 0)
                        {
                            brDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * -Math.Abs(p3dColl4[0].Y - ptMoved.Y)));
                            plDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * -Math.Abs(p3dColl4[0].Y - ptMoved.Y)));
                            c2 = 0;
                            var line = new Line(p3dColl4[0], ptMoved);
                            line.ToSpace();
                            p3dColl4.Clear();
                        }
                        c1 = 0;
                    }
                }

                do
                {

                    SecondCondition.IntersectWith(plDwn, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                    c = p3dColl2.Count;

                    brDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * -piece));
                    plDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * -piece));

                    p3dColl2.Clear();

                } while (c >= 1);


                // 如果块直接和条件一相交就舍去
                FirstCondition.IntersectWith(plDwn, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                if (p3dColl2.Count > 1)
                {
                    p3dColl2.Clear();
                    i++;
                    continue;
                }


                var firstPt = plDwn.GetPoint3dAt(0);
                var secondPt = plDwn.GetPoint3dAt(1);
                var thridPt = plDwn.GetPoint3dAt(2);
                var ptEnd = plDwn.GetPoint3dAt(3);

                if (PtInPl.PtRelationToPoly(FirstCondition, firstPt, 1.0E-4) == -1
                    || PtInPl.PtRelationToPoly(FirstCondition, secondPt, 1.0E-4) == -1
                    || PtInPl.PtRelationToPoly(FirstCondition, thridPt, 1.0E-4) == -1
                    || PtInPl.PtRelationToPoly(FirstCondition, ptEnd, 1.0E-4) == -1
                    )
                {
                    i++;

                    continue;

                }


                listBrDownOrg.Add(brDwn);
                lplDownOrg.Add(plDwn);

                i++;

            } while (i < count);

            listBrDownOrg.ToSpace();
            lplDownOrg.ToSpace();


            return listBrDownOrg;
        }
    }
}
