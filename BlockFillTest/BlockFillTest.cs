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


            MinPoint = FirstCondition.Bounds.Value.MinPoint;
            MaxPoint = FirstCondition.Bounds.Value.MaxPoint;

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
                    SecondCondition = ent[1] as Curve;

                    SecondCondition.ToSpace();

                }

            }

            GetBlockCondition();

            if (BlkRec == null)
            {
                return;
            }


            GetBlkRatioCondtn1();

            // blkDiagonal.StartPoint = MinBlkPt;
            // blkDiagonal.EndPoint = MaxBlkPt;
            List<BlockReference> listBr = BlkScale3(0.3, MinBlkPt, MaxBlkPt,0.1);

            Polyline pl = new Polyline(listBr.Count);

            foreach (var br in listBr)
            {

                Point3d maxPt = br.Bounds.Value.MaxPoint;

                pl.AddVertexAt(pl.NumberOfVertices, new Point2d(maxPt.X, maxPt.Y), 0, 0, 0);


            }

            pl.ToSpace();



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


        [CommandMethod("cd3")]
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


        }


        private bool point3dEqual(Point3d p1, Point3d p2)
        {

            if (p1.X.ToString("f9") == p2.X.ToString("f9") && p1.Y.ToString("f9") == p2.Y.ToString("f9") && p1.Z.ToString("f9") == p2.Z.ToString("f9"))
                return true;
            return false;
        }

        private void BlkScale(double scale, Point3d min, Point3d max)
        {

            Point3d ptPos = splDirection ? ptPos = Intersect1 : ptPos = Intersect2;

            double allLength = SecondCondition.GetDistAtPoint(SecondCondition.EndPoint);

            double countLength = 0;

            BlockReference preBrUP = null;
            BlockReference preBrDwn = null;

            Polyline prePlUP = null;
            Polyline prePlDwn = null;
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

                var lengthX = (plUP.GetPoint2dAt(0) - plUP.GetPoint2dAt(1)).Length;

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

                brUP.TransformBy(matrix2);
                plUP.TransformBy(matrix2);

                brDwn.TransformBy(matrix2);
                plDwn.TransformBy(matrix2);



                Vector3d vec = Vector3d.XAxis * 0;

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

                double rito1 = 0.005, rito2 = 0.005;

                Matrix3d mtxDisUP = splDirection ? Matrix3d.Displacement(vec.RotateBy(Math.PI * 0.5, Vector3d.ZAxis)) :
                    Matrix3d.Displacement(vec.RotateBy(Math.PI * -0.5, Vector3d.ZAxis));

                Matrix3d mtxDisDwn = splDirection == false ? Matrix3d.Displacement(vec.RotateBy(Math.PI * 0.5, Vector3d.ZAxis)) :
                    Matrix3d.Displacement(vec.RotateBy(Math.PI * -0.5, Vector3d.ZAxis));
                var mtxRotate = Matrix3d.Rotation(vec.GetAngleTo(Vector3d.XAxis), Vector3d.ZAxis, center);

                plUP.TransformBy(mtxRotate);
                brUP.TransformBy(mtxRotate);

                brDwn.TransformBy(mtxRotate);
                plDwn.TransformBy(mtxRotate);
                while (IsRectXJCon(brUP, SecondCondition))
                {

                    mtxDisUP = splDirection ? Matrix3d.Displacement(vec.RotateBy(Math.PI * 0.5, Vector3d.ZAxis) * rito1) :
                    Matrix3d.Displacement(vec.RotateBy(Math.PI * -0.5, Vector3d.ZAxis) * rito1);

                    plUP.TransformBy(mtxDisUP);
                    brUP.TransformBy(mtxDisUP);

                    rito1 += 0.005;

                    if (rito1 > 2)
                    {
                        break;
                    }

                }

                while (IsRectXJCon(brDwn, SecondCondition))
                {

                    mtxDisDwn = splDirection == false ? Matrix3d.Displacement(vec.RotateBy(Math.PI * 0.5, Vector3d.ZAxis)) :
                     Matrix3d.Displacement(vec.RotateBy(Math.PI * -0.5, Vector3d.ZAxis));

                    brDwn.TransformBy(mtxDisDwn);
                    plDwn.TransformBy(mtxDisDwn);

                    rito2 += 0.005;
                    if (rito2 > 2)
                    {
                        break;
                    }
                }

                //判断是否和条件一相交

                if (BlkRotateToSpace(ref brUP, ref plUP, center))
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
                    if (!IsRecXjRec(prePlDwn, brDwn))
                    {

                        brDwn.ToSpace();
                    }

                    if (!IsRecXjRec(prePlDwn, plDwn))
                    {

                        plDwn.ToSpace();
                    }


                }

                countLength += lengthX / 10;

                ptPos = SecondCondition.GetPointAtDist(countLength);
                //658231.39122614253

                preBrUP = brUP;
                preBrDwn = brDwn;

                prePlUP = plUP;
                prePlDwn = plDwn;


            } while (countLength <= allLength);


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
                    while ((plU2 == null && j == listBrUPOrg.Count - 1 && PtInPl.PtRelationToPoly(plU, p, 1.0E-4) != -1 && m <= 3 * moveHigh) || (plU2 != null && IsRecXjRec(plU, plU2)) && m <= 3*moveHigh)
                    {

                        brU.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * -left * m + Vector3d.YAxis * up*m));
                        plU.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * -left * m + Vector3d.YAxis * up*m));
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
            if (ent1 == null||ent2==null)
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


        private List<BlockReference> BlkScale3(double scale, Point3d min, Point3d max,double jianju)
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

            double blockWidth = Math.Abs(t2.X - t1.X);//块的宽度

            double moveHigh = Math.Abs(t2.Y - t1.Y);//块的高度

            double allLength = SecondCondition.GetDistAtPoint(SecondCondition.EndPoint);

            double oneLength = allLength / count;

            oneLength += oneLength / 3;

            int i = 0;

            Vector3d vec3d = Vector3d.XAxis;
            try
            {
                do
                {

                    BlockReference brUP = new BlockReference(ptPos, BlkRec.Id);
                    BlockReference brDwn = new BlockReference(ptPos, BlkRec.Id);

                    brUP.ScaleFactors = new Scale3d(scale);
                    brDwn.ScaleFactors = new Scale3d(scale);

                    Point3d p1 = brUP.Bounds.Value.MinPoint;
                    Point3d p2 = brUP.Bounds.Value.MaxPoint;

                    Polyline plUP = GetMinRect(p1, p2);
                    Polyline plDwn = GetMinRect(p1, p2);

                    Point3d center = new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, 0);

                    //移动到边界处
                    Point3d  movePt = splDirection==true ? new Point3d(center.X + Math.Abs((p2.X - p1.X)) / 2, center.Y, 0)
                        : new Point3d(center.X - Math.Abs((p2.X - p1.X)) / 2, center.Y, 0);
                   

                    var matrix2 = Matrix3d.Displacement(movePt - center);

                    brUP.TransformBy(matrix2);
                    plUP.TransformBy(matrix2);

                    brDwn.TransformBy(matrix2);
                    plDwn.TransformBy(matrix2);

                    vec3d = Vector3d.XAxis * i * blockWidth;
                    vec3d += Vector3d.XAxis * jianju *i* blockWidth;

                    brUP.TransformBy(Matrix3d.Displacement(vec3d));
                    plUP.TransformBy(Matrix3d.Displacement(vec3d));


                    brDwn.TransformBy(Matrix3d.Displacement(vec3d));
                    plDwn.TransformBy(Matrix3d.Displacement(vec3d));

                    Point3d ptMoved = brUP.Bounds.Value.MinPoint;
                    Point3d ptMoved2 = brUP.Bounds.Value.MaxPoint;

                    var ptM4 = new Point3d(ptMoved.X, ptMoved2.Y,0);

                    Line line1 = new Line(ptMoved, ptM4);
                    

                    if (i == 8)
                    {
                        int a = 54;
                    }

                    Point3dCollection p3dColl2 = new Point3dCollection();
                    // 如果块直接和条件一相交就舍去
                    /*FirstCondition.IntersectWith(plUP, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                    if (p3dColl2.Count >1)
                    {
                        p3dColl2.Clear();
                        i++;
                        continue;
                    }
                    */
                    int c = 0;

                    double piece = moveHigh / 10;
                    List<Point3d> listColl=null;
                  
                    do
                    {
                        

                        SecondCondition.IntersectWith(plUP, Intersect.OnBothOperands, p3dColl2, IntPtr.Zero, IntPtr.Zero);

                        c = p3dColl2.Count;



                        if (c == 0)
                        {
                            Point3dCollection p3dColl3= new Point3dCollection();
                            
                            int c2 = 0;

                           
                            int mm = 0;
                            do
                            {

                                line1.EndPoint = new Point3d(line1.EndPoint.X, line1.EndPoint.Y + moveHigh, 0);
                                

                                SecondCondition.IntersectWith(line1, Intersect.OnBothOperands, p3dColl3, IntPtr.Zero, IntPtr.Zero);
                                c2 = p3dColl3.Count;
                                if (mm > 10)
                                {
                                    break;
                                }
                                
                                mm++;

                            } while (c2 < 1);// (false);//while (c2 < 1&&c3<1);

                            if (c2 != 0) {

                                brUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * mm* moveHigh));
                                plUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * mm * moveHigh));
                                c2 = 0;
                            }
                            
                        }


                        listColl = p3dColl2.Cast<Point3d>().ToList().OrderBy(p=>p.Y).ToList();

                        brUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis*piece));
                        plUP.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * piece ));


                        brDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * piece ));
                        plDwn.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * piece ));

                        p3dColl2.Clear();

                    } while (c>=1);




                    /*
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

                    listBrUPOrg.Add(brUP);
                    lplUPOrg.Add(plUP);*/

                    i++;

                    plUP.ToSpace();
                    brUP.ToSpace();

                } while (i<count);

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
    }
}
