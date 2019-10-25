﻿using System;
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

        Document doc = Application.DocumentManager.MdiActiveDocument;
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;

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

        int all = 0;

        List<BlockReference> listAllBr = new List<BlockReference>();

        //曲线方向从左到右，从下到上为true，否则为false
        bool splDirection = true;

        PointIsInPolyline PtInPl = new PointIsInPolyline();
        private BlockTableRecord BlkRec;

        DBObjectCollection dbColl = null;

        [CommandMethod("ECDBT")]
        public void Test()
        {
            listAllBr.Clear();

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

                }
                //SecondConditionDown = SecondCondition.Clone() as Curve;
            }

            GetBlockCondition();

            if (BlkRec == null)
            {
                return;
            }


            GetBlkRatioCondtn1();
            #region 保留算法

            //Point3d ptPos = splDirection ? Intersect1 :  Intersect2;

            //BlockReference temp = new BlockReference(ptPos, BlkRec.Id);

            //temp.ScaleFactors = new Scale3d(0.3);

            //Point3d t1 = (Point3d)temp.Bounds?.MinPoint;
            //Point3d t2 = (Point3d)temp.Bounds?.MaxPoint;

            //double blockWidth = Math.Abs(t2.X - t1.X);//块的宽度

            //double blockHigh = Math.Abs(t2.Y - t1.Y);//块的高度

            //double allLength = SecondCondition.GetDistAtPoint(SecondCondition.EndPoint);

            //double piece = blockHigh / 10;

            //temp.Dispose();

            //double countLen = 2 * blockWidth;
            //double countLen1 = countLen;

            //int countAll = (int)(MaxH / blockHigh);

            //Vector3d vecYDown =  -Vector3d.YAxis*1.2* blockHigh;
            //Vector3d vecYUp = Vector3d.YAxis * 1.2 * blockHigh;

            //double factor = 0.5;

            //double scale = 0.3;

            //double scale2 = scale;

            //List<Vector3d> listVec3dDown=null;
            //List<Vector3d> listVec3dUp=null;

            //List<BlockReference> listBr = BlkScaleUp(ref listVec3dUp, scale, countLen1, blockWidth, blockHigh, allLength);
            //List<BlockReference> listBr2 = BlkScaleDown( ref listVec3dDown, scale, countLen, blockWidth, blockHigh, allLength);

            //int firstCount = listBr2.Count;
            //int firstUpCount = listBr.Count;

            //listAllBr.AddRange(listBr2);
            //listAllBr.AddRange(listBr);

            //int q = 1;
            //double s = 0.0;



            //while (q < countAll)
            //{


            //    if (q % 2 == 1)
            //    {
            //        countLen1 += (scale2 + factor * q * RatioW / firstUpCount) * blockWidth;

            //        s += (scale2 + factor * q * RatioW / firstUpCount) * blockWidth;
            //    }
            //    else
            //    {
            //        countLen1 -= s + q * (scale2 + factor * q * RatioW / firstUpCount) * blockWidth;
            //        countLen1 += s;
            //    }
            //    countLen1 = countLen1 < 0 ? 2*blockWidth : countLen1;
            //    countLen1=countLen1>allLength?allLength:countLen1;

            //    listBr2 = BlkScaleUp(ref listVec3dUp, scale, countLen1, (scale2 + factor * q * RatioW / firstUpCount) * blockWidth, blockHigh, allLength);


            //    scale = scale - 0.04;

            //    scale = scale < 0 ? 0.08 : scale;

            //    //listBr2.ForEach((br) => {

            //    //    br.TransformBy(Matrix3d.Displacement(vecYUp));

            //    //});

            //    for (int i = 0; i < listBr2.Count; i++)
            //    {
            //        var v = listVec3dUp[i];

            //        v = v.RotateBy(Math.PI * 0.5, Vector3d.ZAxis).GetNormal();
            //        var br = listBr2[i];

            //        br.TransformBy(Matrix3d.Displacement(v*2*blockHigh*q));



            //    }



            //    vecYUp += Vector3d.YAxis * 2 * blockHigh*q++;

            //    listAllBr.AddRange(listBr2);

            //}

            //q = 1;
            //s = 0.0;
            //scale = scale2;
            //while (/*vecYDown.Length < MaxH*/q < countAll)
            //{

            //    if (q % 2 == 1)
            //    {
            //        countLen += (scale2 +  q * RatioW / firstCount) * blockWidth;

            //        s += (scale2 +  q * RatioW / firstCount) * blockWidth;
            //    }
            //    else
            //    {
            //        countLen -= s + q * (scale2 +  q * RatioW / firstCount) * blockWidth;
            //        countLen += s;
            //    }
            //    countLen = countLen < 0 ? 2 * blockWidth : countLen;
            //    countLen = countLen > allLength ? allLength : countLen;

            //    listBr2 = BlkScaleDown(ref listVec3dDown, scale, countLen, (scale2 +  q * RatioW / firstCount) * blockWidth, blockHigh, allLength);

            //    scale -= 0.04;
            //    scale = scale < 0 ? 0.08 : scale;
            //    //listBr2.ForEach((br) => {

            //    //    br.TransformBy(Matrix3d.Displacement(vecYDown));

            //    //});

            //    for (int i = 0; i < listBr2.Count; i++)
            //    {
            //        var v = listVec3dDown[i];

            //        double angle = Vector3d.XAxis.GetAngleTo(v);



            //        var br = listBr2[i];

            //        if (angle >= Math.PI / 3 && angle <= Math.PI / 1.5)
            //        {

            //            br.TransformBy(Matrix3d.Displacement(vecYDown * 2));
            //            //var v1 = v.RotateBy(Math.PI*-0.5, Vector3d.ZAxis);

            //            //br.TransformBy(Matrix3d.Displacement(v1.GetNormal() * 2 * blockHigh * q));
            //        }
            //        else
            //        {
            //            br.TransformBy(Matrix3d.Displacement(vecYDown));
            //        }

            //    }


            //    vecYDown -= Vector3d.YAxis * 2 * blockHigh * q++;

            //    if (q == 2)
            //    {
            //        continue;
            //    }

            //    listAllBr.AddRange(listBr2);

            //}
            //List<BlockReference> listRemove = new List<BlockReference>();
            //for (int i = firstCount+firstUpCount+1; i < listAllBr.Count; i++)
            //{
            //    var br = listAllBr[i];

            //    for (int j = i + 1; j < listAllBr.Count; j++)
            //    {
            //        var br2 = listAllBr[j];

            //        if (IsRecXjRec(br, br2))
            //        {
            //            //listRemove.Add(br);

            //        }
            //    }
            //}

            //listRemove.ForEach(b => { listAllBr.Remove(b); });


            //listRemove.Clear();

            //foreach (var br in listAllBr)
            //{
            //    var minPt = br.Bounds.Value.MinPoint;
            //    var maxPt = br.Bounds.Value.MaxPoint;

            //    if (PtInPl.PtRelationToPoly(FirstCondition, minPt, 1.0E-4) == -1 || PtInPl.PtRelationToPoly(FirstCondition, maxPt, 1.0E-4) == -1)
            //    {

            //        listRemove.Add(br);
            //    }
            //}

            //listRemove.ForEach(b => { listAllBr.Remove(b); });

            ////listAllBr = listAllBr.Distinct().ToList();

            //listAllBr.ToSpace();


            //// DrawLines(0.2);
            #endregion

            // OffSetAndFill();
            OffSetAndFillFan();
        }

        public void OffSetAndFill()
        {

            if (SecondCondition == null || BlkRec == null)
            {
                return;
            }
            listAllBr.Clear();
            Point3d ptPos = splDirection ? Intersect1 : Intersect2;

            BlockReference temp = new BlockReference(ptPos, BlkRec.Id);

            temp.ScaleFactors = new Scale3d(0.3);

            Point3d t1 = (Point3d)temp.Bounds?.MinPoint;
            Point3d t2 = (Point3d)temp.Bounds?.MaxPoint;

            double blockWidth = Math.Abs(t2.X - t1.X);//块的宽度

            double blockHigh = Math.Abs(t2.Y - t1.Y);//块的高度

            double allLength = SecondCondition.GetDistAtPoint(SecondCondition.EndPoint);

            double piece = blockHigh;

            temp.Dispose();

            double totalCount = MaxH / blockHigh;

            double countLen = 2 * blockWidth;
            double countLen1 = countLen;

            int countAll = (int)(MaxH / blockHigh);

            //double disUp = 60000,disDown=-60000;
            //double addLen = 58000;
            double disUp = (MaxH / 4) + 4 * piece;

            double sumDis = 0;

            if (MaxH / blockHigh < 10)
            {
                disUp = 8 * piece;
            }
            else
            {
                disUp = (MaxH / 10) + 2 * piece;
            }
            double addLen = blockHigh * 0.5;

            disUp = 2 * piece;

            var scale = 0.3;
            var scale2 = scale;

            var minusScale = 2 * blockHigh / MaxH * scale2;

            List<BlockReference> listBrUp = null;


            List<BlockReference> listBr2 = BlkScaleDown(ref listBrUp, scale, countLen, blockWidth, piece, allLength);
            listBr2 = BlkScaleInLine(scale + 0.06, countLen, blockWidth, piece, allLength, SecondCondition);

            listAllBr.AddRange(listBr2);

            int firstCount = listBr2.Count;
            int firstUpCount = listBrUp.Count;// listBr.Count;

            int q = 0;

            double s = 0.0, s1 = 0.0;

            double sum = 0.0;

            var p1 = SecondCondition;
            var p2 = SecondCondition;



            sum = disUp + (1 + q * 2 / totalCount) * addLen;
            sum = sum * 1.3;
            while (sumDis <= MaxH)
            {
                sumDis += disUp + (1 + q * 2 / totalCount) * addLen;

                bool isXJ1 = false;
                bool isXJ2 = false;
                //var pl = OffsetCon2(sumDis,out isXJ1);
                //var pl2 = OffsetCon2(sumDis * -1,out isXJ2);

                //sumDis = sumDis * 1.2;


                var pl = OffsetCon2(p1, sum, out isXJ1);
                var pl2 = OffsetCon2(p2, sum * -1, out isXJ2);

                //sum = sum * 1.2;

                p1 = pl.Clone() as Curve;
                p2 = pl2.Clone() as Curve;


                allLength = pl.GetDistAtPoint(pl.EndPoint);
                double allLength2 = pl2.GetDistAtPoint(pl2.EndPoint);

                //  pl.ToSpace();
                //pl2.ToSpace();

                scale = scale < 0 ? 0.08 : scale;

                if (q % 2 == 0)
                {
                    //countLen += (scale2 + q * RatioW / firstUpCount) * blockWidth;
                    //countLen1 += q / RatioW * blockWidth;
                    //s += (scale2 + q * RatioW / firstUpCount) * blockWidth;
                    //s1+= q / RatioW * blockWidth;
                    s += countLen;
                    s1 += countLen1;

                    countLen *= 1.6;
                    countLen1 *= 1.6;


                    countLen = countLen < 0 ? 2 * blockWidth : countLen;
                    countLen = countLen > allLength ? allLength : countLen;

                    countLen1 = countLen1 < 0 ? 2 * blockWidth : countLen1;
                    countLen1 = countLen1 > allLength2 ? allLength2 : countLen1;

                    if (isXJ1)
                        listBrUp = BlkScaleInLine(scale, countLen, blockWidth, piece, allLength, pl);
                    if (isXJ2)
                        listBr2 = BlkScaleInLine(scale, countLen, blockWidth, piece, allLength2, pl2);

                }
                else
                {
                    //countLen -= s + q * (scale2 + q * RatioW / firstUpCount) * blockWidth;
                    //countLen1 -=s1+q / RatioW * blockWidth;

                    countLen = (countLen - s) / 1.6;
                    countLen1 = (countLen1 - s1) / 1.6;

                    countLen = countLen < 0 ? 2 * blockWidth : countLen;
                    countLen = countLen > allLength ? allLength : countLen;

                    countLen1 = countLen1 < 0 ? 2 * blockWidth : countLen1;
                    countLen1 = countLen1 > allLength2 ? allLength2 : countLen1;
                    if (isXJ1)
                        listBrUp = BlkScaleInLine(scale, countLen, blockWidth, piece, allLength, pl);
                    if (isXJ2)
                        listBr2 = BlkScaleInLine(scale, countLen, blockWidth, piece, allLength2, pl2);

                    countLen += s;
                    countLen1 += s1;
                }
                scale -= 0.02;
                q++;
                listAllBr.AddRange(listBrUp);
                listAllBr.AddRange(listBr2);

                listBrUp.Clear();
                listBr2.Clear();

                if (isXJ1 == false && isXJ2 == false)
                {
                    break;
                }


            }

            List<BlockReference> listRemove = new List<BlockReference>();
            for (int i = firstCount + firstUpCount + 1; i < listAllBr.Count; i++)
            {
                var br = listAllBr[i];

                for (int j = i + 1; j < listAllBr.Count; j++)
                {
                    var br2 = listAllBr[j];

                    if (IsRecXjRec(br, br2))
                    {
                        //listRemove.Add(br);

                    }
                }
            }

            listRemove.ForEach(b => { listAllBr.Remove(b); });

            listRemove.Clear();

            foreach (var br in listAllBr)
            {
                var minPt = br.Bounds.Value.MinPoint;
                var maxPt = br.Bounds.Value.MaxPoint;

                if (PtInPl.PtRelationToPoly(FirstCondition, minPt, 1.0E-4) == -1 || PtInPl.PtRelationToPoly(FirstCondition, maxPt, 1.0E-4) == -1)
                {

                    listRemove.Add(br);
                }
            }

            listRemove.ForEach(b => { listAllBr.Remove(b); });

            listAllBr.ToSpace();

        }

        private void OffSetAndFillFan()
        {
            if (SecondCondition == null || BlkRec == null)
            {
                return;
            }

            listAllBr.Clear();

            Point3d ptPos = splDirection ? Intersect1 : Intersect2;

            BlockReference temp = new BlockReference(ptPos, BlkRec.Id);

            temp.ScaleFactors = new Scale3d(0.3);

            Point3d t1 = (Point3d)temp.Bounds?.MinPoint;
            Point3d t2 = (Point3d)temp.Bounds?.MaxPoint;

            double blockWidth = Math.Abs(t2.X - t1.X);//块的宽度

            double blockHigh = Math.Abs(t2.Y - t1.Y);//块的高度

            double allLength = SecondCondition.GetDistAtPoint(SecondCondition.EndPoint);

            all = (int)(allLength / blockWidth);

            double piece = blockHigh;

            double b1 = blockWidth;

            temp.Dispose();

            double totalCount = MaxH / blockHigh;

            double countLen = 2 * blockWidth;
            double countLen1 = countLen;

            int countAll = (int)(MaxH / blockHigh);
            double disUp = (MaxH / 4) + 4 * piece;

            double sumDis = 0;


            double addLen = blockHigh * 0.5;

            disUp = 2 * piece;

            var scale = 0.16;
            var scale2 = scale;

            var minusScale = 2 * blockHigh / MaxH * scale2;


            blockWidth = (2.0+(1.0*all) / 25) * blockWidth;

            List<BlockReference> listBrUp = null;


            //List<BlockReference> listBr2 = BlkScaleDown(ref listBrUp, scale, countLen, blockWidth, piece, allLength);
            List<BlockReference> listBr2 = BlkScaleInLineFan(scale, countLen, blockWidth, piece, allLength, SecondCondition);

            listAllBr.AddRange(listBr2);

            int firstCount = listBr2.Count;
            int firstUpCount = firstCount;// listBr.Count;

            int q = 0;


            double sum = 0.0;

            var p1 = SecondCondition;
            var p2 = SecondCondition;



            sum = disUp + addLen;
            sum = sum * 1.3;



            while (sumDis <= MaxH)
            {
                sumDis += disUp + (1 + q * 2 / totalCount) * addLen;

                bool isXJ1 = false;
                bool isXJ2 = false;

                var pl = OffsetCon2(p1, sum, out isXJ1);
                var pl2 = OffsetCon2(p2, sum * -1, out isXJ2);

                //sum = sum * 1.2;

                p1 = pl.Clone() as Curve;
                p2 = pl2.Clone() as Curve;


                allLength = pl.GetDistAtPoint(pl.EndPoint);
                double allLength2 = pl2.GetDistAtPoint(pl2.EndPoint);

                pl.ToSpace();
                pl2.ToSpace();

                scale = scale > 0.3 ? 0.3 : scale;

                q++;

                blockWidth = (2 * (all*1.0 - q) / 25) * b1;

                blockWidth = blockWidth < 1.5 * b1 ? 1.5 * b1 : blockWidth;

                if (isXJ1)
                    listBrUp = BlkScaleInLineFan(scale, countLen, blockWidth, piece, allLength, pl);
                if (isXJ2)
                    listBr2 = BlkScaleInLineFan(scale, countLen, blockWidth, piece, allLength2, pl2);


                scale += 0.08;

                listAllBr.AddRange(listBrUp);
                listAllBr.AddRange(listBr2);

                listBrUp.Clear();
                listBr2.Clear();

                if (isXJ1 == false && isXJ2 == false)
                {
                    break;
                }


            }

            List<BlockReference> listRemove = new List<BlockReference>();
            for (int i = firstCount + firstUpCount + 1; i < listAllBr.Count; i++)
            {
                var br = listAllBr[i];

                for (int j = i + 1; j < listAllBr.Count; j++)
                {
                    var br2 = listAllBr[j];

                    if (IsRecXjRec(br, br2))
                    {
                        //listRemove.Add(br);

                    }
                }
            }

            listRemove.ForEach(b => { listAllBr.Remove(b); });

            listRemove.Clear();

            foreach (var br in listAllBr)
            {
                var minPt = br.Bounds.Value.MinPoint;
                var maxPt = br.Bounds.Value.MaxPoint;

                if (PtInPl.PtRelationToPoly(FirstCondition, minPt, 1.0E-4) == -1 || PtInPl.PtRelationToPoly(FirstCondition, maxPt, 1.0E-4) == -1)
                {

                    listRemove.Add(br);
                }
            }

            listRemove.ForEach(b => { listAllBr.Remove(b); });

            listAllBr.ToSpace();

        }

        private Polyline OffsetCon2(Curve curve, double sumDis, out bool isXJ2)
        {
            isXJ2 = true;

            var coll = curve.GetOffsetCurves(sumDis);

            var pl = coll[0] as Polyline;

            for (int i = 1; i < coll.Count; i++)
            {
                pl.JoinEntity(coll[i] as Entity);
            }

            Point3dCollection pt3dColl = new Point3dCollection();

            pl.IntersectWith(FirstCondition, Intersect.ExtendThis, pt3dColl, IntPtr.Zero, IntPtr.Zero);

            //for (int c = 0; c < pt3dColl.Count; c++)
            //{

            //    var pt = pt3dColl[c];


            //    if (pt.Y == MaxPoint.Y||pt.Y==MinPoint.Y)
            //    {
            //        return pl;
            //    }


            //}




            if (pt3dColl.Count >= 2)
            {
                if (pl.StartPoint.X > pt3dColl[0].X)
                    pl.AddVertexAt(0, new Point2d(pt3dColl[0].X, pt3dColl[0].Y), 0, 0, 0);

                if (pl.EndPoint.X < pt3dColl[pt3dColl.Count - 1].X)
                    pl.AddVertexAt(pl.NumberOfVertices, new Point2d(pt3dColl[pt3dColl.Count - 1].X, pt3dColl[pt3dColl.Count - 1].Y), 0, 0, 0);

            }
            else if (pt3dColl.Count == 1)
            {
                if (pl.StartPoint.X > pt3dColl[0].X)
                    pl.AddVertexAt(0, new Point2d(pt3dColl[0].X, pt3dColl[0].Y), 0, 0, 0);
                else if (pl.EndPoint.X < pt3dColl[pt3dColl.Count - 1].X)
                    pl.AddVertexAt(pl.NumberOfVertices, new Point2d(pt3dColl[0].X, pt3dColl[0].Y), 0, 0, 0);

            }
            else
            { isXJ2 = false; }

            return pl;

        }

        private List<BlockReference> BlkScaleInLine(double scale, double countLen, double blockWidth, double piece, double allLength, Curve s)
        {

            List<BlockReference> listBrUPOrg = new List<BlockReference>();

            double q = 0.1;

            while (countLen < allLength)
            {

                var ptPs = s.GetPointAtDist(countLen);

                countLen = countLen + (2 + q) * blockWidth;

                q += 0.1;

                countLen = countLen > allLength ? allLength : countLen;

                BlockReference brUP = new BlockReference(ptPs, BlkRec.Id);

                brUP.ScaleFactors = new Scale3d(scale);

                Point3d p1 = brUP.Bounds.Value.MinPoint;
                Point3d p2 = brUP.Bounds.Value.MaxPoint;

                Point3d ptMoved = brUP.Bounds.Value.MinPoint;
                Point3d ptMoved2 = brUP.Bounds.Value.MaxPoint;

                var ptM4 = new Point3d(ptMoved.X, ptMoved2.Y, 0);

                Point3dCollection p3dColl2 = new Point3dCollection();


                Vector3d v = s.GetFirstDerivative(ptPs);

                var angle = Vector3d.XAxis.GetAngleTo(v);
                if (v.X > 0 && v.Y < 0)
                {

                    angle = Math.PI * 2 - angle;

                }




                //brUP.Rotation = GetRotateMtx(ptPs);

                brUP.Rotation = angle;

                listBrUPOrg.Add(brUP);

            };

            return listBrUPOrg;
        }

        private List<BlockReference> BlkScaleInLineFan(double scale, double countLen, double blockWidth, double piece, double allLength, Curve s)
        {

            List<BlockReference> listBrUPOrg = new List<BlockReference>();


            while (countLen < allLength)
            {

                var ptPs = s.GetPointAtDist(countLen);

                countLen = countLen + blockWidth;

                countLen = countLen > allLength ? allLength : countLen;



                BlockReference brUP = new BlockReference(ptPs, BlkRec.Id);

                brUP.ScaleFactors = new Scale3d(scale);

                Point3d p1 = brUP.Bounds.Value.MinPoint;
                Point3d p2 = brUP.Bounds.Value.MaxPoint;

                Point3d ptMoved = brUP.Bounds.Value.MinPoint;
                Point3d ptMoved2 = brUP.Bounds.Value.MaxPoint;

                var ptM4 = new Point3d(ptMoved.X, ptMoved2.Y, 0);

                Point3dCollection p3dColl2 = new Point3dCollection();


                Vector3d v = s.GetFirstDerivative(ptPs);

                var angle = Vector3d.XAxis.GetAngleTo(v);
                if (v.X > 0 && v.Y < 0)
                {

                    angle = Math.PI * 2 - angle;

                }




                //brUP.Rotation = GetRotateMtx(ptPs);

                brUP.Rotation = angle;

                listBrUPOrg.Add(brUP);

            };

            return listBrUPOrg;
        }

        private List<BlockReference> BlkScaleDown(ref List<BlockReference> listBrUp, double scale, double countLen, double blockWidth, double piece, double allLength)
        {

            List<BlockReference> listBrDownOrg = new List<BlockReference>();
            listBrUp = new List<BlockReference>();

            do
            {

                var ptPs = SecondCondition.GetPointAtDist(countLen);

                countLen += 2 * blockWidth;

                BlockReference brDwn = new BlockReference(ptPs, BlkRec.Id);
                brDwn.ScaleFactors = new Scale3d(scale);

                BlockReference brUp = new BlockReference(ptPs, BlkRec.Id);
                brUp.ScaleFactors = new Scale3d(scale);

                Point3d p1 = brDwn.Bounds.Value.MinPoint;
                Point3d p2 = brDwn.Bounds.Value.MaxPoint;

                Point3d ptMoved = brDwn.Bounds.Value.MinPoint;
                Point3d ptMoved2 = brDwn.Bounds.Value.MaxPoint;

                var ptM4 = new Point3d(ptMoved.X, ptMoved2.Y, 0);

                Line line1 = new Line(ptMoved, ptM4);
                Line line2 = new Line(ptM4, ptMoved);

                Point3dCollection p3dColl2 = new Point3dCollection();
                Point3dCollection p3dColl3 = new Point3dCollection();

                int c1 = 0;
                int c2 = 0;
                do
                {

                    SecondCondition.IntersectWith(brDwn, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);
                    SecondCondition.IntersectWith(brUp, Intersect.OnBothOperands, p3dColl3, IntPtr.Zero, IntPtr.Zero);
                    c1 = p3dColl2.Count;
                    c2 = p3dColl3.Count;
                    brDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * -piece));

                    brUp.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * piece));

                    p3dColl2.Clear();

                    p3dColl3.Clear();

                } while (c1 >= 1 || c2 >= 1);


                Vector3d v = SecondCondition.GetFirstDerivative(ptPs);

                var angle = Vector3d.XAxis.GetAngleTo(v);

                brDwn.Rotation = GetRotateMtx(ptPs);

                brUp.Rotation = GetRotateMtx(ptPs);

                listBrDownOrg.Add(brDwn);
                listBrUp.Add(brUp);

                line1.Dispose();
                line2.Dispose();

            } while (countLen < allLength);




            return listBrDownOrg;
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

        public void GetSecondCondition()
        {


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
        #region 保留算法


        //public void DrawLines(double factor)
        //{
        //    factor = 0.5;
        //    double scale = 0.3;

        //    //宽的间隔
        //    double jgW = (MaxW / RatioW) * factor;

        //    double jgH = (MaxH / RatioH) * factor;

        //    int w = 1, h = 1;

        //    #region 画线
        //    /* 
        //                List<Entity> listLine = new List<Entity>();

        //                //画竖线
        //               while (MinPoint.X + w * jgW <= MaxPoint.X)
        //                {
        //                    Point3d pS = new Point3d(MinPoint.X + w * jgW, MinPoint.Y, 0);
        //                    var pE = new Point3d(MinPoint.X + w * jgW, MaxPoint.Y, 0);

        //                    var line = new Line(pS, pE);

        //                    line.Color = Color.FromColor(System.Drawing.Color.Pink);

        //                    listLine.Add(line);

        //                    w++;
        //                }
        //                //画横线
        //                while (MinPoint.Y + h * jgH <= MaxPoint.Y)
        //                {
        //                    Point3d pS = new Point3d(MinPoint.X, MinPoint.Y + h * jgH, 0);
        //                    var pE = new Point3d(MaxPoint.X, MinPoint.Y + h * jgH, 0);

        //                    var line = new Line(pS, pE);

        //                    line.Color = Color.FromColor(System.Drawing.Color.Pink);

        //                    listLine.Add(line);
        //                    h++;
        //                }
        //               //listLine.ToSpace();
        //               */
        //    #endregion

        //    double sX = MinPoint.X;
        //    double sY = MinPoint.Y;

        //    List<List<Point3d>> listListPt = new List<List<Point3d>>();

        //    List<Line> listDuijiao = new List<Line>();

        //    List<Point3d> listPtCenter = new List<Point3d>();

        //    int totalY = (int)(MaxH / jgH + 1);
        //    int totalX = (int)(MaxW / jgW + 1);

        //    for (int m = 0; m < totalY; m++)
        //    {
        //        List<Point3d> listPt = new List<Point3d>();

        //        listListPt.Add(listPt);
        //        for (int k = 0; k < totalX; k++)
        //        {
        //            var pt = new Point3d(sX + k * jgW, sY + m * jgH, 0);

        //            listPt.Add(pt);
        //        }

        //    }


        //    double freeY = MaxH - (totalY - 1) * jgH;

        //    if (freeY > 0)
        //    {
        //        List<Point3d> list = new List<Point3d>();

        //        listListPt.Add(list);


        //        for (int k = 0; k < totalX; k++)
        //        {
        //            var pt = new Point3d(sX + k * jgW, MaxPoint.Y, 0);

        //            list.Add(pt);
        //        }

        //    }



        //    double freeX = MaxW - (totalX - 1) * jgW;

        //    if (freeX > 0)
        //    {

        //        for (int l = 0; l < listListPt.Count; l++)
        //        {

        //            var list = listListPt[l];

        //            var pt = new Point3d(MaxPoint.X, l * jgH + sY, 0);

        //            list.Add(pt);

        //        }

        //    }

        //    sY = MinPoint.Y;

        //    for (int l1 = 0; l1 < listListPt.Count; l1++)
        //    {
        //        var list = listListPt[l1];

        //        for (int l2 = 0; l2 < list.Count; l2++)
        //        {

        //            Point3d ptS = list[l2];

        //            if (l1 + 1 < listListPt.Count && l2 + 1 < listListPt[l1 + 1].Count)
        //            {


        //                var ptE = listListPt[l1 + 1][l2 + 1];

        //                var ptcenter = new Point3d((ptS.X + ptE.X) / 2, (ptS.Y + ptE.Y) / 2, 0);

        //                listPtCenter.Add(ptcenter);

        //            }

        //        }

        //    }

        //    List<BlockReference> listBr = new List<BlockReference>();
        //    List<double> listLen = new List<double>();
        //    Point3dCollection p3dcoll = new Point3dCollection();

        //    foreach (var center in listPtCenter)
        //    {

        //        BlockReference br = new BlockReference(center, BlkRec.Id);

        //        var line = new Line(new Point3d(center.X, MinPoint.Y, 0), new Point3d(center.X, MaxPoint.Y, 0));

        //        line.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

        //        int count = p3dcoll.Count;

        //        double s = scale;

        //        double length = 0.0;

        //        if (count == 0)
        //        {
        //            line.ToSpace();
        //        }
        //        line.Dispose();

        //        if (count == 1)
        //        {

        //            length = (center - p3dcoll[0]).Length;

        //            s =Math.Abs( scale - (length / MaxH) * 0.3);

        //        }

        //        listLen.Add(length);

        //        s = s > 0.3 ? 0.3 : s;

        //        s = s < 0.08 ? 0.08 : s;

        //        br.ScaleFactors = new Scale3d(s);
        //        p3dcoll.Clear();

        //        br.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

        //        int count1 = p3dcoll.Count;

        //        if (count1 > 0)
        //        {
        //            p3dcoll.Clear();
        //            //br = null;
        //        }
        //        else
        //        {
        //            br.IntersectWith(FirstCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

        //            count1 = p3dcoll.Count;

        //            if (count1 > 0)
        //            {
        //                p3dcoll.Clear();
        //                //br = null;
        //            }

        //        }

        //        listBr.Add(br);
        //        p3dcoll.Clear();

        //    }

        //    BlockReference[,] brArr = new BlockReference[totalY, totalX];
        //    double[,] lenArr = new double[totalY, totalX];

        //    #region 集合转二维数组


        //    int q = 0;
        //    for (int i = 0; i < totalY; i++)
        //    {
        //        for (int j = 0; j < totalX; j++)
        //        {
        //            brArr[i, j] = listBr[q];
        //            lenArr[i, j] = listLen[q++];
        //        }
        //    }
        //    #endregion
        //    //求左起第一个离条件二最近的那个，记录行值
        //    int firstX = 0, firstY = 0;
        //    List<BlockReference> listBrToSpc = new List<BlockReference>();
        //    double l3 = 0.0;
        //    double l4 = 0.0;
        //    while (firstX < totalX)
        //    {
        //        double min = double.MaxValue;

        //        for (int i = 0; i < totalY; i++)
        //        {

        //            if (brArr[i, firstX] != null && lenArr[i, firstX] < min)
        //            {
        //                min = lenArr[i, firstX];
        //                firstY = i;
        //            }

        //        }

        //        var brToSpace = brArr[firstY, firstX];

        //            var PtMin = brToSpace.Bounds.Value.MinPoint;
        //            var ptMax = brToSpace.Bounds.Value.MaxPoint;

        //            l3 = Math.Abs(ptMax.X - PtMin.X);
        //            l4 = Math.Abs(ptMax.Y - PtMin.Y);
        //        p3dcoll.Clear();

        //        Point3d ptJd = Point3d.Origin;

        //        Point3d firstCenter = listPtCenter[firstY * totalX + firstX];

        //        var firstLine = new Line(new Point3d(firstCenter.X, MinPoint.Y, 0), new Point3d(firstCenter.X, MaxPoint.Y, 0));

        //        firstLine.IntersectWith(SecondCondition, Intersect.OnBothOperands, p3dcoll, IntPtr.Zero, IntPtr.Zero);

        //        if (p3dcoll.Count > 0)
        //        {
        //            ptJd = p3dcoll[0];



        //            int upI = firstY;
        //            int loopIndex = 1;
        //            Vector3d vec = Vector3d.XAxis * 0;

        //            double jgHigh = min * 0.8;

        //            double scale1 = 0.3;



        //            if (brToSpace != null) {

        //                if (ptJd.Y < PtMin.Y)
        //                {
        //                    upI = firstY;
        //                }
        //                else if(ptJd.Y>ptMax.Y)
        //                {
        //                    upI = firstY + 1;
        //                }
        //                    }
        //            else
        //            {
        //                upI = firstY + 1;
        //            }

        //            upI += 2;
        //            int oneLine = upI + 1;
        //            while (upI < totalY)
        //            {

        //                var brY = brArr[upI, firstX];

        //                if (brY == null)
        //                {
        //                    upI += 1;
        //                    continue;
        //                }

        //                int m = 0;

        //                double s1 = scale1 - (loopIndex * 1.0) / totalY;

        //                if (firstX == 0 ) {

        //                    vec = Vector3d.XAxis * 0;

        //                    vec +=  Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;

        //                    brY.TransformBy(Matrix3d.Displacement(vec));
        //                }
        //                else
        //                {


        //                if ((upI + 1) % 2 == 0)
        //                {
        //                    vec = Vector3d.XAxis * 0;

        //                        vec += Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY + Vector3d.YAxis *loopIndex* loopIndex * l3 / totalY;

        //                   brY.TransformBy(Matrix3d.Displacement(vec));
        //                }
        //                else
        //                {
        //                    vec = Vector3d.XAxis * 0;

        //                        vec -= Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY;// + Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;
        //                        m = 0;
        //                    brY.TransformBy(Matrix3d.Displacement(vec));
        //                }
        //                }

        //                var PtMin2 = brY.Bounds.Value.MinPoint;
        //                var ptMax2 = brY.Bounds.Value.MaxPoint;

        //                var PointCenterY = new Point3d((PtMin2.X + ptMax2.X) / 2, (PtMin2.Y + ptMax2.Y) / 2, 0);

        //                brY.Rotation=GetRotateMtx(PointCenterY);

        //                loopIndex++;

        //                listBrToSpc.Add(brY);

        //                upI += 1;

        //            }
        //            upI = firstY;

        //            if (brToSpace != null)
        //            {

        //                if (ptJd.Y < PtMin.Y)
        //                {
        //                    upI = firstY;
        //                }
        //                else if (ptJd.Y > ptMax.Y)
        //                {
        //                    upI = firstY - 2;
        //                }
        //            }
        //            else
        //            {
        //                upI = firstY - 1;
        //            }

        //            upI -= 1;
        //            loopIndex = 1;

        //            int oneLine2 = upI;

        //            while (upI >= 0/*false*/ )
        //            {
        //                if (upI - 1 >= 0 && brArr[upI - 1, firstX] != null)
        //                {

        //                    var brY = brArr[upI - 1, firstX];

        //                    int m = 0;
        //                    if (firstX == 0)
        //                    {

        //                        vec = Vector3d.XAxis * 0;

        //                        vec = -Vector3d.YAxis * loopIndex * loopIndex * l3 / totalY;

        //                        brY.TransformBy(Matrix3d.Displacement(vec));
        //                    }
        //                    else
        //                    {
        //                        if ((upI - 1) % 2 == 0)
        //                        {
        //                            vec = Vector3d.XAxis * 0;
        //                            vec = Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY -Vector3d.YAxis * loopIndex * loopIndex * l3 * 1.2/ totalY;

        //                            brY.TransformBy(Matrix3d.Displacement(vec));
        //                        }
        //                        else
        //                        {
        //                            vec = Vector3d.XAxis * 0;
        //                            vec = -Vector3d.XAxis * loopIndex * loopIndex * l3 * 1.4 / totalY-Vector3d.YAxis * loopIndex * loopIndex * l3 * 1.2 / totalY;

        //                            m = 0;
        //                            brY.TransformBy(Matrix3d.Displacement(vec));
        //                        }
        //                    }
        //                    var PtMin1 = brY.Bounds.Value.MinPoint;
        //                    var ptMax1 = brY.Bounds.Value.MaxPoint;

        //                    var PointCenterY = new Point3d((PtMin1.X + ptMax1.X) / 2, (PtMin1.Y + ptMax1.Y) / 2, 0);

        //                    brY.Rotation = GetRotateMtx(PointCenterY);
        //                    loopIndex++;
        //                    listBrToSpc.Add(brY);
        //                }

        //                upI -= 1;

        //            }

        //        }
        //        firstX++;
        //        // break;
        //    }
        //    List<BlockReference> listRemove = new List<BlockReference>();
        //    listAllBr.AddRange(listBrToSpc);

        //    for (int i = 0; i < listBrToSpc.Count; i++)
        //    {
        //        var br = listBrToSpc[i];

        //        for (int j=i+1; j < listBrToSpc.Count; j++)
        //        {
        //            var br2 = listBrToSpc[j];

        //            if (IsRecXjRec(br, br2))
        //            {
        //               listRemove.Add(br);

        //            }
        //        }
        //    }

        //    listRemove.ForEach(b => { listBrToSpc.Remove(b); });


        //    listRemove.Clear();

        //    foreach (var br in listBrToSpc)
        //    {
        //        var minPt = br.Bounds.Value.MinPoint;
        //        var maxPt = br.Bounds.Value.MaxPoint;

        //        if(PtInPl.PtRelationToPoly(FirstCondition,minPt,1.0E-4)==-1|| PtInPl.PtRelationToPoly(FirstCondition, maxPt, 1.0E-4) == -1){

        //            listRemove.Add(br);
        //        }
        //    }

        //    listRemove.ForEach(b => { listBrToSpc.Remove(b); });

        //    listAllBr = listAllBr.Distinct().ToList();

        //    listBrToSpc.ToSpace();

        //}
        #endregion
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
            }
            return 0;
        }
        bool IsRecXjRec(Entity ent1, Entity ent2)
        {
            if (ent1 == null || ent2 == null)
            {
                return false;
            }
            var br1 = ent1 as BlockReference;
            var br2 = ent2 as BlockReference;

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

    }
}
