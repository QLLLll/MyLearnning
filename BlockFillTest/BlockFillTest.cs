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

        Entity BlkEnt = null;

        Point3d Intersect1;
        Point3d Intersect2;

        Point3d MinPoint;
        Point3d MaxPoint;

        Point3d MinBlkPt;
        Point3d MaxBlkPt;

        double Ratio;

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

            //foreach (Point3d item in p3dcl)
            //{
            //    ed.WriteMessage($"坐标：[{item.X},{item.Y},{item.Z}]\n");
            //}

            if (p3dcl.Count == 0)
            {
                Intersect1 = SecondCondition.StartPoint;

                Intersect2 = SecondCondition.EndPoint;

            }
            else if (p3dcl.Count == 1)
            {
                if (SecondCondition.StartPoint.X > MinPoint.X
                    && SecondCondition.StartPoint.Y > MinPoint.Y
                    && SecondCondition.StartPoint.X < MaxPoint.X
                    && SecondCondition.StartPoint.Y < MaxPoint.Y)
                {

                    Intersect1 = SecondCondition.StartPoint;

                    Intersect2 = p3dcl[0];

                }
                else
                {
                    Intersect1 = p3dcl[0];

                    Intersect2 = SecondCondition.StartPoint;
                }


            }
            else if (p3dcl.Count == 2)
            {

                Intersect1 = p3dcl[0];
                Intersect2 = p3dcl[1];
            }
            //ed.WriteMessage($"坐标：[{Intersect1.X},{Intersect1.Y},{Intersect1.Z}]\n");
            // ed.WriteMessage($"坐标：[{Intersect2.X},{Intersect2.Y},{Intersect1.Z}]\n");

            GetBlockCondition();

            GetBlkRatioCondtn1();


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
            listMin.Sort((min1, min2) => { return min1[0].CompareTo(min2[0]); });
            double[] minMin = listMin.First();

            listMax.Sort((min1, min2) => { return min1[0].CompareTo(min2[0]); });
            double[] maxMax = listMax[listMax.Count - 1];

            MinBlkPt = new Point3d(minMin[0], minMin[1], minMin[2]);
            MaxBlkPt = new Point3d(maxMax[0], maxMax[1], maxMax[2]);

        }

        public void GetFirstCondition()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var entOpts = new PromptEntityOptions("请选择封闭线");

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

        public void GetSecondCondition()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var entOpts = new PromptEntityOptions("请选择曲线");

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

            }
        }

        public  void GetBlockCondition()
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
                return ;
            }
           
            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
             var blkRec=  trans.GetObject(blkTbl[blockName], OpenMode.ForRead) as BlockTableRecord;


                List<Entity> listEntis = new List<Entity>();
                foreach (ObjectId objectId in blkRec)
                {
                   var Ent= trans.GetObject(objectId, OpenMode.ForRead) as Entity;

                    listEntis.Add(Ent);
                }
                BlkEnt = listEntis[0];
                trans.Commit();
            }

            GetBlockMinMaxPoint(blockName);



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


        private  bool point3dEqual(Point3d p1, Point3d p2)
        {

            if (p1.X.ToString("f9") == p2.X.ToString("f9") && p1.Y.ToString("f9") == p2.Y.ToString("f9") && p1.Z.ToString("f9") == p2.Z.ToString("f9"))
                return true;
            return false;



        }

       

    }
}
