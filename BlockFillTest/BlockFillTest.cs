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

        [CommandMethod("XJ")]

        public void Test2()
        {

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;
            GetSecondCondition();
            GetBlockCondition();

            using (var trans = db.TransactionManager.StartTransaction())
            {
                List<Entity> listEnt = new List<Entity>();
                List<int> listCount = new List<int>();

                int theMax = 0;
                foreach (ObjectId objId in BlkRec)
                {
                    var Enti = trans.GetObject(objId, OpenMode.ForRead) as Entity;
                    listEnt.Add(Enti);
                    Point3dCollection p3dcoll = new Point3dCollection();

                    Enti.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

                    if (p3dcoll.Count > 0)
                    {

                        if (p3dcoll.Count > theMax)
                        {
                            theMax = p3dcoll.Count;
                        }
                        Application.ShowAlertDialog(theMax.ToString());
                    }
                    else
                    {
                        Application.ShowAlertDialog("false");
                    }

                }
                Point3dCollection p3dcoll1 = new Point3dCollection();
                for (int i = 0; i < listEnt.Count; i++)
                {
                    var e1 = listEnt[i];

                    int count = 0;
                    for (int j = i + 1; j < listEnt.Count; j++)
                    {
                        var e2 = listEnt[j];


                        e1.IntersectWith(e2, Intersect.OnBothOperands, p3dcoll1, IntPtr.Zero, IntPtr.Zero);


                        Application.ShowAlertDialog(p3dcoll1.Count.ToString());

                        count += p3dcoll1.Count;
                    }
                    listCount.Add(count);

                }


                //Application.ShowAlertDialog($"{listCount[0]},{listCount[1]}");

                int max = listCount.Max();

                if (max < theMax)
                {
                    Application.ShowAlertDialog("true");
                }


                trans.Commit();
            }

        }

        [CommandMethod("XJ2")]
        public void Test3()
        {
            Polyline p1 = GetFirstCondition1();
            Polyline p2 = GetFirstCondition1();

            if (p1 == null || p2 == null)
            {
                return;
            }

            Point3dCollection p3dC = new Point3dCollection();


            p1.IntersectWith(p2, Intersect.OnBothOperands, p3dC, IntPtr.Zero, IntPtr.Zero);

            Application.ShowAlertDialog(p3dC.Count.ToString());


        }



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

            PlBound = GetBoundsOfCon1(MinPoint,MaxPoint);

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

                    
                    for(int i = 2; i < ent.Length - 1; i++)
                    {

                        curve.JoinEntity(ent[2]);
                    }

                    SecondCondition =curve;

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



            List<Polyline> firstUp=null;
            List<Polyline> firstDown=null;

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

        private Polyline GetBoundsOfCon1(Point3d min , Point3d max)
        {
            var pt1 = new Point2d(min.X,min.Y);
            var pt2 = new Point2d(max.X, min.Y);
            var pt3 = new Point2d(max.X,max.Y);
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

        [CommandMethod("cd2")]
        public void GetSecondCondition()
        {
            //GetFirstCondition();
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

            BlockW= (MaxBlkPt.X - MinBlkPt.X);
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
            listLine.ToSpace();

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

                        var line = new Line(ptS, ptE);

                        line.Color = Color.FromColor(System.Drawing.Color.DarkOrange);

                        listDuijiao.Add(line);
                    }

                }

            }
            //   listDuijiao.ToSpace();

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

                    s = scale - (length / MaxH) * 0.3;

                }
                else if (count > 1)
                {

                    foreach (Point3d pt in p3dcoll)
                    {

                        if (PtInPl.PtRelationToPoly(FirstCondition, pt, 1.0E-4) != -1)
                        {
                            length = (center - p3dcoll[0]).Length;

                            s = scale - (length / MaxH) * 0.3;

                            break;

                        }

                    }


                }

                listLen.Add(length);

                s = s > 0.3 ? 0.3 : s;

                s = s < 0.08 ? 0.08 : s;
                br.ScaleFactors = new Scale3d(s);
                p3dcoll.Clear();

                br.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

                int count1 = p3dcoll.Count;

                if (count1 >0 )
                {
                    p3dcoll.Clear();
                    br = null;
                }
                else
                {
                    p3dcoll.Clear();

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



                //   br.ToSpace();


            }

            BlockReference[,] brArr = new BlockReference[totalY, totalX];
            double[,] lenArr = new double[totalY, totalX];

            #region 集合转二维数组


            int q = 0;
            for (int i = 0; i < totalY; i++)
            {
                for (int j = 0; j < totalX; j++)
                {
                    brArr[i, j] = listBr[q++];
                }
            }

            q = 0;
            for (int i = 0; i < totalY; i++)
            {
                for (int j = 0; j < totalX; j++)
                {
                    lenArr[i, j] = listLen[q++];
                }
            }
            #endregion

            /*foreach (var itemBr in listBr)
            {

                itemBr.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

                int count = p3dcoll.Count;

                if (count > 1)
                {
                    p3dcoll.Clear();
                    continue;
                }
                else
                {

                    itemBr.IntersectWith(FirstCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

                    count = p3dcoll.Count;

                    if (count > 0)
                    {
                        p3dcoll.Clear();
                        continue;
                    }

                }
                itemBr.ToSpace();

            }
            */
            //求左起第一个离条件二最近的那个，记录行值
            int firstX = 0, firstY = 0;

            double max = lenArr[0, 0];

            for (int i = 0; i < totalY; i++)
            {

                
                    if (brArr[i,0]!=null&&lenArr[i, 0] < max)
                    {
                    max = lenArr[i, 0];
                        firstY = i;
                    }
                
            }
            p3dcoll.Clear();

            Point3d firstCenter = listPtCenter[firstY * totalX];

            var firstLine = new Line(new Point3d(firstCenter.X, MinPoint.Y, 0), new Point3d(firstCenter.X, MaxPoint.Y, 0));

            firstLine.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

            if (p3dcoll.Count > 0)
            {

                Vector3d vec1 = SecondCondition.GetFirstDerivative(p3dcoll[0]);

                var vec = vec1.X * vec1.Y > 0 ? vec1 : -vec1;
                double angle = vec.GetAngleTo(Vector3d.XAxis);

                var mtxRotate = Matrix3d.Rotation(angle, Vector3d.ZAxis, firstCenter);

                if (brArr[firstY, 0] != null)
                {
                    brArr[firstY, 0].TransformBy(mtxRotate);
                }

                brArr[firstY, 0].ToSpace();
            }
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



            BlockReference preBrUP = null;
            BlockReference preBrDwn = null;

            Polyline prePlUP = null;
            Polyline prePlDwn = null;

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

                    //plUP.TransformBy(mtxRotate);
                    //brUP.TransformBy(mtxRotate);

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

                    while (IsRectXJCon(plUP, SecondCondition))
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

                    while (IsRectXJCon(plDwn, SecondCondition))
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

                    if (IsRectXJCon(plU, FirstCondition))
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

                    if (plU2 != null && IsRecXjRec(plU, plU2) || IsRectXJCon(plU, FirstCondition) || PtInPl.PtRelationToPoly(plU, p, 1.0E-4) != -1)
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

        bool BlkRotateToSpace(ref BlockReference brUP, ref Polyline p, Point3d pt)
        {

            double angle = Math.PI / 18;

            Matrix3d mtx = Matrix3d.Rotation(angle, Vector3d.ZAxis, pt);

            while (IsRectXJCon(brUP, FirstCondition) && IsRectXJCon(brUP, SecondCondition))
            {
                brUP.Rotation = angle;

                mtx = Matrix3d.Rotation(angle, Vector3d.ZAxis, pt);

                angle += Math.PI;

                p.TransformBy(mtx);


                if (angle >= Math.PI * 2)
                {
                    break;
                }
            }

            if (angle > Math.PI * 2)
            {
                return false;
            }
            else
            {
                int i = 0;
                //判断最后的图形是否在条件一内部
                for (i = 0; i < p.NumberOfVertices; i++)
                {

                    Point2d temp = p.GetPoint2dAt(i);

                    if (PtInPl.PtRelationToPoly(FirstCondition, temp, 1.0E-4) == -1)
                    {
                        break;
                    }


                }

                if (i == 4)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }

        bool IsRectXJCon(Entity br, Entity condition)
        {

            Point3dCollection p3dColl2 = new Point3dCollection();
            if (br is Polyline)
            {

                condition.IntersectWith(br, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);
            }
            /* else if(br is BlockReference)
             {
                 var b = br as BlockReference;

                 DBObjectCollection objColl = new DBObjectCollection();

                 b.Explode(objColl);

                 foreach (var obj in objColl)
                 {

                     var ent = obj as Entity;


                     if (ent != null)
                     {

                         ent.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                         if (p3dColl2.Count > 0)
                         {
                             break;
                         }
                     }

                 }
             }*/
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


        bool IsRecXjRec(Entity ent1, Entity ent2)
        {
            if (ent1 == null || ent2 == null)
            {
                return false;
            }

            Point3dCollection p3dColl2 = new Point3dCollection();

            ent1.IntersectWith(ent2, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

            if (p3dColl2.Count > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
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


        private List<BlockReference> BlkScale3(double scale, Point3d min, Point3d max, double jianju,ref List<Polyline> lplUPOrg)
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

                if(PtInPl.PtRelationToPoly(FirstCondition,firstPt, 1.0E-4)==-1
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
