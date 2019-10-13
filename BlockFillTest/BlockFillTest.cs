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


namespace BlockFillTest
{
    public class BlockFillTest
    {

        Polyline FirstCondition;

        Curve SecondCondition;

        Point3d Intersect1;
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

        Polyline plTrangle = new Polyline(4);

        DBObjectCollection dbColl = null;

        [CommandMethod("BlockTest")]
        public void Test()
        {

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            GetFirstCondition();
            GetSecondCondition();

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

                var list = p3dcl.Cast<Point3d>().OrderBy(pt=>pt.X).ThenBy(pt=>pt.Y).ToList();


                Intersect1 = list.First();

                Intersect2 =list[list.Count-1];

                //如果只有一个交点
                if (Intersect1 == Intersect2)
                {
                    if(Intersect1== SecondCondition.EndPoint)
                    {
                        Intersect2= SecondCondition.StartPoint;
                    }
                    else
                    {
                        Intersect2 = SecondCondition.EndPoint;
                    }
                }
                Point3dCollection temp3dCol = new Point3dCollection();
                //分割曲线
                if (list.Count == 2) {
                   
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

            }

            //ed.WriteMessage($"坐标：[{Intersect1.X},{Intersect1.Y},{Intersect1.Z}]\n");
            // ed.WriteMessage($"坐标：[{Intersect2.X},{Intersect2.Y},{Intersect1.Z}]\n");

            GetBlockCondition();

            GetBlkRatioCondtn1();

            // blkDiagonal.StartPoint = MinBlkPt;
            // blkDiagonal.EndPoint = MaxBlkPt;
            BlkScale(0.3, MinBlkPt, MaxBlkPt);
          
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

                if (SecondCondition.StartPoint.X < SecondCondition.EndPoint.Y)
                {
                    splDirection = true;
                }
                else
                {
                    splDirection = false;

                }
                if (SecondCondition.StartPoint.X == SecondCondition.EndPoint.X)

                {
                    if (SecondCondition.StartPoint.Y < SecondCondition.EndPoint.Y){
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
        [CommandMethod("cd3")]
        public void GetBlockCondition()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;


            PromptStringOptions strOpts = new PromptStringOptions("请输入块名称,区分大小写");

            string blockName = string.Empty;

            var strRes = ed.GetString(strOpts);

            if (strRes.Status == PromptStatus.OK)
            {

                blockName = strRes.StringResult;

            }

            if (String.IsNullOrEmpty(blockName))
            {
                Application.ShowAlertDialog("请输入正确的块名称");
                return;
            }

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlkRec = trans.GetObject(blkTbl[blockName], OpenMode.ForRead) as BlockTableRecord;
                
                trans.Commit();
            }
            
            GetBlockMinMaxPoint(blockName);

            GetMinTrangle();
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

        private void BlkScale( double scale,Point3d min,Point3d max ) {

            BlockReference br = new BlockReference(Intersect1, BlkRec.Id);
            
            br.ScaleFactors = new Scale3d(0.3);

            Polyline plCopy = plTrangle.GetTransformedCopy(Matrix3d.Scaling(0.3, Point3d.Origin)) as Polyline;

            var matrix = Matrix3d.Displacement(Intersect1 - Point3d.Origin);


            

            Point3d p1 = (Point3d)br.Bounds?.MinPoint;
            Point3d p2 = (Point3d)br.Bounds?.MaxPoint;

            Point3d center = new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, 0);

            var matrix2 = Matrix3d.Displacement(p2 - center);
            br.TransformBy(matrix2);
            br.Rotation = Math.PI / 4;
            br.ToSpace();
        }


         void GetMinTrangle()
         {
            Point2d p2 = new Point2d(MaxBlkPt.X, MinBlkPt.Y);

            Point2d p4 = new Point2d(MinBlkPt.X, MaxBlkPt.Y);

            plTrangle.AddVertexAt(plTrangle.NumberOfVertices, new Point2d(MinBlkPt.X,MinBlkPt.Y), 0, 0, 0);

            plTrangle.AddVertexAt(plTrangle.NumberOfVertices, p2, 0, 0, 0);
            plTrangle.AddVertexAt(plTrangle.NumberOfVertices, new Point2d(MaxBlkPt.X, MaxBlkPt.Y), 0, 0, 0);

            plTrangle.AddVertexAt(plTrangle.NumberOfVertices, p4, 0, 0, 0);

            plTrangle.Closed = true;

           

            plTrangle.ToSpace();
        }

        [CommandMethod("Test")]
        public void Test2()
        {
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
                    if (returnBlock.Name == "MyBlock1")
                    {
                        returnBlock.GetBoundingBox(out min, out max);
                        double[] arr = min as double[];
                        double[] arr2 = max as double[];
                        listMin.Add(arr);
                        listMax.Add(arr2);
                    }

                }
            }
            listMin.OrderBy(min => min[0]);
            double[] minMin = listMin.First();

            listMax.OrderBy(max => max[0]);
            double[] maxMax = listMax[listMax.Count - 1];

            MinBlkPt = new Point3d(minMin[0], minMin[1], minMin[2]);
            MaxBlkPt = new Point3d(maxMax[0], maxMax[1], maxMax[2]);

        }
    }
}
