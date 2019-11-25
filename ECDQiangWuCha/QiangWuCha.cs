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

            List<Point3d> ptArr = new List<Point3d>();

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

                    int indexPt1 = 0;
                    int indexPt2 = 0;
                    //求有问题得点
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

                    DimToSpace(ptArr, listDim,pt1, wrongPt, indexPt2);

                    Ed.WriteMessage($"{v1.Length}\n,{v2.Length}\n{v3.Length}\n{v4.Length}\n");
                    Ed.WriteMessage($"{v1.GetAngleTo(v2) * 180 / Math.PI}\n");
                    Ed.WriteMessage($"{v2.GetAngleTo(v3) * 180 / Math.PI}\n");
                    Ed.WriteMessage($"{v3.GetAngleTo(v4) * 180 / Math.PI}\n");
                    Ed.WriteMessage($"{v4.GetAngleTo(v1) * 180 / Math.PI}\n");
                }
            }
        }

        private void DimToSpace(List<Point3d> ptArr, List<Dimension> listDim,Point3d pt1,Point3d wrongPt,int indexPt2)
        {

            RotatedDimension dimOld = null;
            RotatedDimension dimOld2 = null;
            int indexPt = ptArr.IndexOf(pt1);

            Point3d pt3 = Point3d.Origin;

            if (indexPt < indexPt2)
            {
                pt3 = ptArr[(indexPt2 + 1) % ptArr.Count];
            }
            else
            {
                pt3= ptArr[(indexPt2 - 1) % ptArr.Count];
            }

            foreach (var d in listDim)
            {
               
                if ((d as RotatedDimension) != null)
                {

                    dimOld = d as RotatedDimension;
                    

                    if((PtEqual(dimOld.XLine1Point, pt1) && PtEqual(dimOld.XLine2Point, wrongPt))
                        ||(PtEqual(dimOld.XLine1Point, wrongPt) && PtEqual(dimOld.XLine2Point, pt1)))
                    {
                        dimOld = d as RotatedDimension;
                        break;

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
                    Vector3d v3 = ptArr[indexPt2] - pt3;

                    Vector3d v4 = v3.RotateBy(Math.PI / 2, Vector3d.ZAxis);

                    var ptGet2 = ptArr[indexPt2] + v4.GetNormal() * dimLen;

                Point3d midPoint = new Point3d((ptArr[indexPt2].X + pt3.X) / 2.0,
                                        (ptArr[indexPt2].Y + pt3.Y) / 2.0,
                                        (ptArr[indexPt2].Z + pt3.Z) / 2.0);

                var pt4 = new Point3d(midPoint.X + dimLen * Math.Cos(-Math.PI / 2), midPoint.Y + dimLen * Math.Sin(-Math.PI / 2), midPoint.Z);
                var dimNew2 = new RotatedDimension(0, ptArr[indexPt2], pt3, pt4, v3.Length.ToString(), dimOld2.DimensionStyle);
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
