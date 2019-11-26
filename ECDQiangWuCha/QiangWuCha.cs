using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;

namespace ECDQiangWuCha
{
    public class QiangWuCha
    {
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("EcdQiangWuCha")]
        public void TZWuCha()
        {

            int length = 100;


            TypedValue[] tv = new TypedValue[2] { new TypedValue((int)DxfCode.Start, "POLYLINE"), new TypedValue((int)DxfCode.Start, "DIMENSION") };

            SelectionFilter sf = new SelectionFilter(tv);

            //var selRes = Ed.GetSelection(sf);
            var selRes = Ed.GetSelection();

            if (selRes.Status != PromptStatus.OK) return;

            var listIds = selRes.Value.GetObjectIds().ToList();


            List<Polyline> listPl = new List<Polyline>();
            List<Dimension> listDim = new List<Dimension>();

            using (var trans = Db.TransactionManager.StartTransaction())
            {

                foreach (ObjectId oId in listIds)
                {

                    var ent = trans.GetObject(oId, OpenMode.ForRead) as Entity;

                    if ((ent as Polyline) != null)
                    {

                        listPl.Add((Polyline)ent);

                    }
                    else if ((ent as Dimension) != null)
                    {
                        listDim.Add((Dimension)ent);
                    }
                }
                trans.Commit();
            }

            var propDbOpts = new PromptDoubleOptions("请输入误差范围:");

            var dbRes = Ed.GetDouble(propDbOpts);

            if (dbRes.Status != PromptStatus.OK) return;

            double lMin = length - dbRes.Value;
            double lMax = length + dbRes.Value;

            Dictionary<Vector3d, double> dicVecDb = new Dictionary<Vector3d, double>();
            Dictionary<Vector3d, Line> dicVecL = new Dictionary<Vector3d, Line>();
            List<Point3d> ptArr = new List<Point3d>();

            Polyline plSF = null;
            Polyline plST = null;

            int indexSF = 0;
            int indexST = 0;
            
            //找到相连的pl组成一组加入到集合中
            foreach (var pl in  listPl)
            {
                ptArr = Get4Pt(pl);
                if (ptArr.Count == 3)
                {
                    plST = pl;
                    indexST = listPl.IndexOf(plST);
                    continue;
                }
                else if (ptArr.Count == 4)
                {
                    for (int i = 0; i < ptArr.Count - 1; i++)
                    {
                        var ptF1 = ptArr[i];
                        var ptF2 = ptArr[i + 1];

                        var vecF = ptF2 - ptF1;

                        dicVecDb.Add(vecF, Math.Round(vecF.Length, 3));

                    }
                    var listVec = dicVecDb.Keys.ToList();

                    var v1 = listVec[0];
                    var v2 = listVec[1];
                    var v3 = listVec[2];


                    if (Math.Abs(dicVecDb[v1] - dicVecDb[v3]) > 2 * dbRes.Value)
                    {

                        plSF = pl;
                        indexSF = listPl.IndexOf(plSF);

                        continue;

                    }


                }

            }

            bool flag = false;

            if (plSF != null && plST != null)
            {

                using (var trans = Db.TransactionManager.StartTransaction())
                {

                    plST = trans.GetObject(plST.ObjectId, OpenMode.ForWrite) as Polyline;
                    plSF = trans.GetObject(plSF.ObjectId, OpenMode.ForWrite) as Polyline;

                    try
                    {
                        plST.JoinEntity(plSF);
                        flag = true;

                    }
                    catch (System.Exception ex)
                    {

                        flag = false;
                        Ed.WriteMessage(ex.ToString());
                    }

                    plST.DowngradeOpen();
                    plSF.DowngradeOpen();

                    trans.Commit();
                }
            }

            if (flag)
            {
                listPl[indexST] = plST;
                listPl.RemoveAt(indexSF);
                flag = false;
                indexST = 0;
                indexSF = 0;
            }

            //分点的个数分别处理
            foreach (Polyline pl in listPl)
            {

                dicVecDb.Clear();
                ptArr.Clear();


                ptArr = Get4Pt(pl);
                if (ptArr.Count == 5)
                {
                    Vector3d v1 = ptArr[1] - ptArr[0];
                    Vector3d v2 = ptArr[2] - ptArr[1];
                    Vector3d v3 = ptArr[3] - ptArr[2];
                    Vector3d v4 = ptArr[4] - ptArr[3];

                    dicVecDb.Add(v1, Math.Round(v1.Length, 3));
                    dicVecDb.Add(v2, Math.Round(v2.Length, 3));
                    dicVecDb.Add(v3, Math.Round(v3.Length, 3));
                    dicVecDb.Add(v4, Math.Round(v4.Length, 3));

                    dicVecL.Add(v1, new Line(ptArr[0], ptArr[1]));
                    dicVecL.Add(v2, new Line(ptArr[1], ptArr[2]));
                    dicVecL.Add(v3, new Line(ptArr[2], ptArr[3]));
                    dicVecL.Add(v4, new Line(ptArr[3], ptArr[4]));

                    int indexPt1 = 0;
                    int indexPt2 = 0;
                    //求有问题得点
                    //v1不是需要计算的误差
                    if (dicVecDb[v1] > lMax || dicVecDb[v1] < lMin)
                    {
                        var v2Db = Math.Abs(length - dicVecDb[v2]);
                        var v4Db = Math.Abs(length - dicVecDb[v4]);
                        if (dicVecDb[v1] < dicVecDb[v3])
                        {

                            if (v2Db > v4Db)
                            {
                                indexPt1 = 1;
                                indexPt2 = 2;
                            }
                            else
                            {

                                indexPt1 = 4;
                                indexPt2 = 3;
                            }

                        }
                        else
                        {
                            if (v2Db > v4Db)
                            {
                                indexPt1 = 2;
                                indexPt2 = 1;
                            }
                            else
                            {

                                indexPt1 = 3;
                                indexPt2 = 4;
                            }
                        }

                    }
                    else
                    {    

                        var v1Db = Math.Abs(length - dicVecDb[v1]);
                        var v3Db = Math.Abs(length - dicVecDb[v3]);
                        if (dicVecDb[v2] < dicVecDb[v4])
                        {

                            if (v1Db > v3Db)
                            {
                                indexPt1 = 1;
                                indexPt2 = 0;
                            }
                            else
                            {
                                indexPt1 = 2;
                                indexPt2 = 3;
                            }
                        }
                        else
                        {
                            if (v1Db > v3Db)
                            {
                                indexPt1 = 0;
                                indexPt2 = 1;
                            }
                            else
                            {
                                indexPt1 = 3;
                                indexPt2 = 2;
                            }
                        }
                    }

                    var pt1 = ptArr[indexPt1];
                    var wrongPt = ptArr[indexPt2];

                    var vec = (wrongPt - pt1).GetNormal();

                    var jzPt = pt1 + vec * length;

                    ptArr[indexPt2] = jzPt;

                    if (indexPt2 == 4)
                    {
                        ptArr[0] = jzPt;
                    }


                    Polyline plJz = new Polyline();

                    foreach (var point in ptArr)
                    {
                        plJz.AddVertexAt(plJz.NumberOfVertices, new Point2d(point.X, point.Y), 0, 0, 0);
                    }

                    //plJz.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * 100));
                    plJz.ColorIndex = 6;

                    Pl2Pl(plJz, pl);

                    plJz.ToSpace();

                    DimToSpace(ptArr, listDim, pt1, wrongPt, indexPt2,true);

                    //Ed.WriteMessage($"{v1.Length}\n,{v2.Length}\n{v3.Length}\n{v4.Length}\n");
                    //Ed.WriteMessage($"{v1.GetAngleTo(v2) * 180 / Math.PI}\n");
                    //Ed.WriteMessage($"{v2.GetAngleTo(v3) * 180 / Math.PI}\n");
                    //Ed.WriteMessage($"{v3.GetAngleTo(v4) * 180 / Math.PI}\n");
                    //Ed.WriteMessage($"{v4.GetAngleTo(v1) * 180 / Math.PI}\n");
                }
                else if (ptArr.Count == 4)
                {
                    for (int i = 0; i < ptArr.Count - 1; i++)
                    {
                        var ptF1 = ptArr[i];
                        var ptF2 = ptArr[i + 1];

                        var vecF = ptF2 - ptF1;

                        dicVecDb.Add(vecF, Math.Round(vecF.Length, 3));

                    }
                    var listVec = dicVecDb.Keys.ToList();

                    var v1 = listVec[0];
                    var v2 = listVec[1];
                    var v3 = listVec[2];

                    int indexPt1 = 0;
                    int indexPt2 = 0;
                    if (dicVecDb[v1] >= lMin && dicVecDb[v1] <= lMax)
                    {
                        indexPt1 = 0;
                        indexPt2 = 1;

                    }
                    else if (dicVecDb[v3] >= lMin && dicVecDb[v3] <= lMax)
                    {

                        indexPt1 = 2;
                        indexPt2 = 3;

                    }
                    var pt1 = ptArr[indexPt1];
                    var wrongPt = ptArr[indexPt2];

                    var vec = (wrongPt - pt1).GetNormal();

                    var jzPt = pt1 + vec * length;

                    ptArr[indexPt2] = jzPt;

                    Polyline plJz = new Polyline();

                    foreach (var point in ptArr)
                    {
                        plJz.AddVertexAt(plJz.NumberOfVertices, new Point2d(point.X, point.Y), 0, 0, 0);
                    }

                    //plJz.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * 100));
                    plJz.ColorIndex = 6;

                    Pl2Pl(plJz, pl);

                    plJz.ToSpace();

                    DimToSpace(ptArr, listDim, pt1, wrongPt, indexPt2,true);

                }
                else if (ptArr.Count == 6 && !pl.Closed)
                {

                    
                    Vector3d v1 = ptArr[2] - ptArr[1];
                    Vector3d v2 = ptArr[3] - ptArr[2];
                    Vector3d v3 = ptArr[4] - ptArr[3];
                    Vector3d v4 = ptArr[4] - ptArr[1];
                    Vector3d v5 = ptArr[0] - ptArr[1];
                    Vector3d v6= ptArr[4] - ptArr[5];
                    dicVecDb.Add(v1, Math.Round(v1.Length, 3));
                    dicVecDb.Add(v2, Math.Round(v2.Length, 3));
                    dicVecDb.Add(v3, Math.Round(v3.Length, 3));
                    dicVecDb.Add(v4, Math.Round(v4.Length, 3));
                    dicVecDb.Add(v5, Math.Round(v5.Length, 3));
                    dicVecDb.Add(v6, Math.Round(v6.Length, 3));

                    int indexPt1 = 0;
                    int indexPt2 = 0;
                    //求有问题得点
                    //v1 不是要求误差的边
                    if (dicVecDb[v1] > lMax || dicVecDb[v1] < lMin)
                    {
                        var v2Db = Math.Abs(length - dicVecDb[v2]);
                        var v4Db = Math.Abs(length - dicVecDb[v4]);
                        if (dicVecDb[v1] < dicVecDb[v3])
                        {

                            if (v2Db > v4Db)
                            {
                                indexPt1 = 2;
                                indexPt2 = 3;
                            }
                            else
                            {

                                indexPt1 = 1;
                                indexPt2 = 4;
                            }

                        }
                        else
                        {
                            if (v2Db > v4Db)
                            {
                                indexPt1 = 3;
                                indexPt2 = 2;
                            }
                            else
                            {

                                indexPt1 = 4;
                                indexPt2 = 1;
                            }
                        }

                    }
                    else
                    {

                        var v1Db = Math.Abs(length - dicVecDb[v1]);
                        var v3Db = Math.Abs(length - dicVecDb[v3]);
                        if (dicVecDb[v2] < dicVecDb[v4])
                        {

                            if (v1Db > v3Db)
                            {
                                indexPt1 = 2;
                                indexPt2 = 1;
                            }
                            else
                            {
                                indexPt1 = 3;
                                indexPt2 = 4;
                            }
                        }
                        else
                        {
                            if (v1Db > v3Db)
                            {
                                indexPt1 = 1;
                                indexPt2 = 2;
                            }
                            else
                            {
                                indexPt1 = 4;
                                indexPt2 = 3;
                            }
                        }
                    }
                    var wrongPt = Point3d.Origin;
                    var pt1 = Point3d.Origin;
                    if ((indexPt1 != 1 && indexPt2 != 4) || (indexPt1 != 4 && indexPt2 != 1))
                    {
                         pt1 = ptArr[indexPt1];
                        wrongPt = ptArr[indexPt2];

                        var vec = (wrongPt - pt1).GetNormal();

                        var jzPt = pt1 + vec * length;

                        ptArr[indexPt2] = jzPt;

                    }
                    else
                    {

                        if(indexPt1 == 1 && indexPt2 == 4)
                        {

                            pt1 = ptArr[5];
                            wrongPt = ptArr[indexPt2];

                            var vec = wrongPt - pt1;

                            var jzPt = pt1 + vec.GetNormal() * (vec.Length + dbRes.Value);

                            ptArr[indexPt2] = jzPt;

                        }
                        if(indexPt1 == 4 && indexPt2 == 1)
                        {
                            pt1 = ptArr[0];
                            wrongPt = ptArr[indexPt2];

                            var vec = wrongPt - pt1;

                            var jzPt = pt1 + vec.GetNormal() * (vec.Length + dbRes.Value);

                            ptArr[indexPt2] = jzPt;

                        }

                    }


                    Polyline plJz = new Polyline();

                    foreach (var point in ptArr)
                    {
                        plJz.AddVertexAt(plJz.NumberOfVertices, new Point2d(point.X, point.Y), 0, 0, 0);
                    }

                    //plJz.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * 100));
                    plJz.ColorIndex = 6;

                    Pl2Pl(plJz, pl);

                    plJz.ToSpace();

                    if ((indexPt1 != 1 && indexPt2 != 4) || (indexPt1 != 4 && indexPt2 != 1))
                    {
                        DimToSpace(ptArr, listDim, pt1, wrongPt, indexPt2,true);

                    }
                    else
                    {
                        if (indexPt1 == 1 && indexPt2 == 4)
                        {
                            DimToSpace(ptArr, listDim, ptArr[1], wrongPt, indexPt2,false);
                        }
                        if (indexPt1 == 4 && indexPt2 == 1)
                        {
                            DimToSpace(ptArr, listDim, ptArr[4], wrongPt, indexPt2,false);
                        }
                    }

                }


                using (var trans = Db.TransactionManager.StartTransaction())
                {


                    var ent2 = trans.GetObject(pl.ObjectId, OpenMode.ForWrite) as Entity;

                    ent2.Erase(true);

                    trans.Commit();

                }
            }


            /* if (plSF != null && plST != null)
             {

                 using (var trans = Db.TransactionManager.StartTransaction())
                 {

                     plST = trans.GetObject(plST.ObjectId, OpenMode.ForWrite) as Polyline;
                     plSF = trans.GetObject(plSF.ObjectId, OpenMode.ForWrite) as Polyline;

                     plST.JoinEntity(plSF);

                     plST.DowngradeOpen();
                     plSF.DowngradeOpen();

                     trans.Commit();
                 }
             }
             */

        }

        private void DimToSpace(List<Point3d> ptArr, List<Dimension> listDim, Point3d pt1, Point3d wrongPt, int indexPt2,bool f)
        {

            RotatedDimension dimOld = null;
            RotatedDimension dimOld2 = null;
            int indexPt = ptArr.IndexOf(pt1);

            Point3d pt3 = Point3d.Origin;
            if (f)
            {
                if (indexPt < indexPt2)
                {
                    pt3 = ptArr[(indexPt2 + 1) % ptArr.Count];
                }
                else
                {
                    pt3 = ptArr[(indexPt2 - 1) % ptArr.Count];
                }
            }
            else
            {
                pt3 = pt1;
            }
            foreach (var d in listDim)
            {

                if ((d as RotatedDimension) != null)
                {

                    dimOld = d as RotatedDimension;


                    if ((PtEqual(dimOld.XLine1Point, pt1) && PtEqual(dimOld.XLine2Point, wrongPt))
                        || (PtEqual(dimOld.XLine1Point, wrongPt) && PtEqual(dimOld.XLine2Point, pt1)))
                    {
                        dimOld = d as RotatedDimension;
                        break;

                    }
                    else
                    {
                        dimOld = null;
                    }

                }

            }

            foreach (var d in listDim)
            {

                if ((d as RotatedDimension) != null)
                {

                    dimOld2 = d as RotatedDimension;


                    if ((PtEqual(dimOld2.XLine1Point, pt3) && PtEqual(dimOld2.XLine2Point, wrongPt)) ||
                        (PtEqual(dimOld2.XLine1Point, wrongPt) && PtEqual(dimOld2.XLine2Point, pt3)))
                    {
                        dimOld2 = d as RotatedDimension;

                        if (dimOld2 != dimOld)
                        {
                            break;
                        }
                        else
                        {
                            dimOld2 = null;
                        }

                    }
                    else
                    {
                        dimOld2 = null;
                    }
                }

            }
            double dimLen = 20d;
            if (null != dimOld)
            {

                var line = new Line(dimOld.XLine1Point, dimOld.XLine2Point);
                var ptDim = dimOld.DimLinePoint;

                var ptDim2 = line.GetClosestPointTo(ptDim, true);

                dimLen = (ptDim2 - ptDim).Length;

                Vector3d v = ptArr[indexPt2] - pt1;

                Vector3d v2 = v.RotateBy(Math.PI / 2, Vector3d.ZAxis);

                var ptGet = pt1 + v2.GetNormal() * dimLen;

                var dimNew = new RotatedDimension(0, pt1, ptArr[indexPt2], ptGet, v.Length.ToString(), dimOld.DimensionStyle);
                Dim2Dim(dimNew, dimOld);
                dimNew.ToSpace();

                using (var trans = Db.TransactionManager.StartTransaction())
                {
                    var ent = trans.GetObject(dimOld.ObjectId, OpenMode.ForWrite) as Entity;

                    ent.Erase(true);

                    trans.Commit();

                }
            }
            if (dimOld2 != null)
            {
                Vector3d v3 = pt3 - ptArr[indexPt2];

                Vector3d v4 = v3.RotateBy(Math.PI / 2, Vector3d.ZAxis);

                var ptGet2 = ptArr[indexPt2] + v4.GetNormal() * dimLen;

                Point3d midPoint = new Point3d((ptArr[indexPt2].X + pt3.X) / 2.0,
                                        (ptArr[indexPt2].Y + pt3.Y) / 2.0,
                                        (ptArr[indexPt2].Z + pt3.Z) / 2.0);

                var pt4 = midPoint + v4.GetNormal() * dimLen;
                //var dimNew2 = new RotatedDimension(0, ptArr[indexPt2], pt3, pt4, v3.Length.ToString(), dimOld2.DimensionStyle);
                var dimNew2 = new AlignedDimension(ptArr[indexPt2], pt3, pt4, v3.Length.ToString("f2"), dimOld2.DimensionStyle);

                Dim2Dim(dimNew2, dimOld2);

                var line = new Line(ptArr[indexPt2], pt3);

                line.ColorIndex = 1;
                line.ToSpace();

                dimNew2.ToSpace();

                using (var trans = Db.TransactionManager.StartTransaction())
                {
                    //var ent = trans.GetObject(dimOld.ObjectId, OpenMode.ForWrite) as Entity;

                    // ent.Erase(true);

                    var ent2 = trans.GetObject(dimOld2.ObjectId, OpenMode.ForWrite) as Entity;

                    ent2.Erase(true);

                    trans.Commit();

                }

            }

        }

        private bool PtEqual(Point3d p1, Point3d p2)
        {

            if (p1.X.ToString("f5") == p2.X.ToString("f5") && p1.Y.ToString("f5") == p2.Y.ToString("f5") && p1.Z.ToString("f5") == p2.Z.ToString("f5"))
                return true;
            return false;



        }
        private void Pl2Pl(Polyline plJz, Polyline pl)
        {
            plJz.LayerId = pl.LayerId;
            plJz.LinetypeId = pl.LinetypeId;
            plJz.Thickness = pl.Thickness;
            plJz.Transparency = pl.Transparency;
            plJz.XData = pl.XData;
            plJz.LineWeight = pl.LineWeight;
            plJz.LinetypeScale = pl.LinetypeScale;
        }

        void Dim2Dim(Dimension rDim1, Dimension rDim)
        {

            rDim1.TextStyleId = rDim.TextStyleId;
            rDim1.TextRotation = rDim.TextRotation;
            rDim1.TextPosition = rDim.TextPosition;
            rDim1.ToleranceSuppressLeadingZeros = rDim.ToleranceSuppressLeadingZeros;
            rDim1.ToleranceSuppressTrailingZeros = rDim.ToleranceSuppressTrailingZeros;
            rDim1.ToleranceSuppressZeroFeet = rDim.ToleranceSuppressZeroFeet;
            rDim1.ToleranceSuppressZeroInches = rDim.ToleranceSuppressZeroInches;
            rDim1.Transparency = rDim.Transparency;
            rDim1.UsingDefaultTextPosition = rDim.UsingDefaultTextPosition;
            rDim1.Visible = rDim.Visible;
            rDim1.VisualStyleId = rDim.VisualStyleId;
            rDim1.XData = rDim.XData;
            rDim1.TextLineSpacingStyle = rDim.TextLineSpacingStyle;
            rDim1.TextLineSpacingFactor = rDim.TextLineSpacingFactor;
            rDim1.TextDefinedSize = rDim.TextDefinedSize;
            rDim1.TextAttachment = rDim.TextAttachment;
            rDim1.SuppressZeroInches = rDim.SuppressZeroInches;
            rDim1.SuppressZeroFeet = rDim.SuppressZeroFeet;
            rDim1.SuppressTrailingZeros = rDim.SuppressTrailingZeros;
            rDim1.SuppressLeadingZeros = rDim.SuppressLeadingZeros;
            rDim1.SuppressAngularTrailingZeros = rDim.SuppressAngularTrailingZeros;
            rDim1.SuppressAngularLeadingZeros = rDim.SuppressAngularLeadingZeros;
            rDim1.Suffix = rDim.Suffix;
            rDim1.Prefix = rDim.Prefix;
            rDim1.PlotStyleNameId = rDim.PlotStyleNameId;



            rDim1.LineWeight = rDim.LineWeight;
            rDim1.LinetypeScale = rDim.LinetypeScale;
            rDim1.Linetype = rDim.Linetype;
            rDim1.LayerId = rDim.LayerId;

            rDim1.HasSaveVersionOverride = rDim.HasSaveVersionOverride;
            rDim1.ForceAnnoAllVisible = rDim.ForceAnnoAllVisible;
            rDim1.FaceStyleId = rDim.FaceStyleId;
            rDim1.Elevation = rDim.Elevation;
            rDim1.EdgeStyleId = rDim.EdgeStyleId;
        }

        private List<Point3d> Get4Pt(Polyline pl)
        {

            List<Point3d> listPt = new List<Point3d>();
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                listPt.Add(pl.GetPoint3dAt(i));
            }
            return listPt;
        }
    }
}
