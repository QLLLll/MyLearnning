using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EcdPilePillar
{
    public class EcdMirrorCopy
    {
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;


        BlockReference br = null;

        ObjectIdCollection oIdColl = new ObjectIdCollection();
        object mirrText;

        public void GetJingXiang2()
        {

            mirrText = Application.GetSystemVariable("MIRRTEXT");

            if (mirrText != null && mirrText.ToString() == "1")
            {
                Application.SetSystemVariable("MIRRTEXT", 0);
            }

            DocumentLock m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();

            //eLockViolation
            br = GetBlockCondition("请选择要镜像的块") as BlockReference;

            if (br == null)
            {
                return;
            }

            AcadApplication app = Application.AcadApplication as AcadApplication;



            DBObjectCollection coll = new DBObjectCollection();

            br.Explode(coll);

            Circle c = new Circle(br.Position, Vector3d.ZAxis, 0.010324);

            var listEnt = coll.Cast<Entity>().ToList();

            listEnt.Add(c);

            oIdColl = listEnt.ToSpace();

            using (var trans = Db.TransactionManager.StartTransaction())
            {

                var blkRef = trans.GetObject(br.ObjectId, OpenMode.ForWrite) as BlockReference;

                blkRef.Visible = false;

                blkRef.DowngradeOpen();

                trans.Commit();
            }

            m_DocumentLock.Dispose();
        }

        //[CommandMethod("ECDHeBing")]
        public void ActiveDocument_EndCommand()
        {
            DocumentLock m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            using (var trans = Db.TransactionManager.StartTransaction())
            {

                var blkRef = trans.GetObject(br.ObjectId, OpenMode.ForWrite) as BlockReference;

                blkRef.Visible = true;

                blkRef.DowngradeOpen();

                for (int i = 0; oIdColl != null && i < oIdColl.Count; i++)
                {

                    var ent = trans.GetObject(oIdColl[i], OpenMode.ForWrite) as Entity;

                    if (ent != null)
                    {

                        ent.Erase(true);

                    }
                }

                trans.Commit();
            }

            var selRes = Ed.GetSelection();

            if (selRes.Status != PromptStatus.OK) return;

            var list = selRes.Value.GetObjectIds()?.ToList();

            ObjectId recId = ObjectId.Null;
            Point3d ptPosC = Point3d.Origin;
            using (var trans = Db.TransactionManager.StartTransaction())
            {

                var blkTbl = trans.GetObject(Db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                BlockTableRecord blkRec = new BlockTableRecord();

                blkRec.Units = br.BlockUnit;

                string blkName = br.Name + "_" + DateTime.Now.ToString("yyyyMMddHHmmssffff");

                blkRec.Name = blkName;

                for (int i = 0; list != null && i < list.Count; i++)
                {

                    var ent = trans.GetObject(list[i], OpenMode.ForWrite);

                    if (ent != null)
                    {
                        bool flag = true;
                        if (ent is Circle)
                        {
                            var cir = ent as Circle;

                            if (cir.Radius == 0.010324)
                            {

                                ptPosC = cir.Center;

                                cir.Erase(true);
                                flag = false;

                            }


                        }
                        if (flag)
                        {
                            Entity entCopy = ent.Clone() as Entity;

                            ent.Erase(true);

                            blkRec.AppendEntity(entCopy);
                        }

                    }
                }
                blkRec.Origin = ptPosC;
                recId = blkTbl.Add(blkRec);

                trans.AddNewlyCreatedDBObject(blkRec, true);



                trans.Commit();

            }

            BlockReference brEnt = new BlockReference(ptPosC, recId);

            brEnt.ToSpace();

            Application.ShowAlertDialog("OK");
            Application.SetSystemVariable("MIRRTEXT", mirrText);

            m_DocumentLock.Dispose();
        }

        public void GetMirrorEnti()
        {
            BlockReference br = GetBlockCondition("请选择要镜像的块") as BlockReference;

            if (br == null)
            {
                return;
            }

            Point3d ptPos = br.Position;

            Point3d ptFirst = GetPoint("请选择镜像线的第一个点：\n");

            Point3d ptSecond = GetPoint("请选择镜像线的第二个点：\n");

            Line mLine = new Line(ptFirst, ptSecond);



            Vector3d mVec = ptSecond - ptFirst;

            double angle = Vector3d.YAxis.GetAngleTo(mVec);

            var br2 = br.GetTransformedCopy(Matrix3d.Rotation(Math.PI + angle, Vector3d.ZAxis, ptPos)) as BlockReference;

            br2.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.YAxis, br2.Position));

            Vector3d mVecTri = mVec.RotateBy(Math.PI / 2, Vector3d.ZAxis);

            Point3d pt1 = ptPos + mVecTri.GetNormal() * 10;

            Point3d pt2 = mLine.GetClosestPointTo(ptPos, true);

            Line line2 = new Line(ptPos, pt2);

            Point3dCollection ptColl = new Point3dCollection();

            line2.IntersectWith(mLine, Intersect.ExtendBoth, ptColl, IntPtr.Zero, IntPtr.Zero);

            var vec = ptColl[0] - ptPos;

            br2.TransformBy(Matrix3d.Displacement(vec));
            mLine.ToSpace();
            br2.ToSpace();
        }

        public Entity GetBlockCondition(string s)
        {


            var entOpts = new PromptEntityOptions(s + "\n");

            var entRes = Ed.GetEntity(entOpts);

            ObjectId entId = ObjectId.Null;
            if (entRes.Status == PromptStatus.OK)
            {
                entId = entRes.ObjectId;

            }

            if (entId == ObjectId.Null)
            {
                return null;
            }

            Entity br = null;
            using (var trans = Db.TransactionManager.StartTransaction())
            {

                br = trans.GetObject(entId, OpenMode.ForRead) as Entity;

                if (br == null)
                {
                    Application.ShowAlertDialog("一定要选择块定义");

                    trans.Commit();

                    return null;
                }
                br.UpgradeOpen();

                br.Visible = false;

                //br.ColorIndex = 20;

                br.DowngradeOpen();


                trans.Commit();
            }

            return br;
        }

        public Point3d GetPoint(string s)
        {
            PromptPointOptions opts = new PromptPointOptions(s);

            opts.AllowNone = false;

            PromptPointResult res = Ed.GetPoint(opts);


            return res.Value;



        }

        [CommandMethod("ECDJingXiang")]
        public void GetJingXiangXY()
        {
            DocumentLock m_DocumentLock = null;
            object mirrText = "";
            try
            {


                m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();

                mirrText = Application.GetSystemVariable("MIRRTEXT");

                if (mirrText != null && mirrText.ToString() == "1")
                {
                    Application.SetSystemVariable("MIRRTEXT", 0);
                }
                var br = GetBlockCondition("请选择要镜像的块") as BlockReference;
                string name = br.Name;

                if (br == null)
                {
                    return;
                }
                IdMapping idmap = new IdMapping();


                string xYJingXiang = "";

                PromptKeywordOptions pkOpts = new PromptKeywordOptions("请选择镜像方向[X/Y]", "X Y");
                var propRes = Ed.GetKeywords(pkOpts);

                if (propRes.Status != PromptStatus.OK) return;

                xYJingXiang = propRes.StringResult;

                DBObjectCollection coll = new DBObjectCollection();

                br.Explode(coll);

                var listEnt = coll.Cast<Entity>().ToList();


                Point3d ptPos = br.Position;

                Point3d ptMax = br.Bounds.Value.MaxPoint;
                Point3d ptMin = br.Bounds.Value.MinPoint;

                double maxY = Math.Abs(ptMax.Y - ptMin.Y);
                double maxX = Math.Abs(ptMax.X - ptMin.X);

                if (xYJingXiang == "Y")
                {

                    Point3d ptEnd = ptPos + Vector3d.YAxis * 100;

                    Line lineY = new Line(ptPos, ptEnd);

                    lineY.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * maxX));


                    MyMirror(listEnt, lineY, "Y");


                }
                else if (xYJingXiang == "X")
                {

                    Point3d ptEnd = ptPos + Vector3d.XAxis * 100;

                    Line lineX = new Line(ptPos, ptEnd);

                    lineX.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * maxY));

                    MyMirror(listEnt, lineX, "X");
                }

                var brNew = new BlockReference(br.Position, br.BlockTableRecord);

                brNew.ToSpace();

                Application.ShowAlertDialog("OK");

            }
            catch (System.Exception e)
            {

                Ed.WriteMessage(e.ToString());
            }
            finally
            {
                Application.SetSystemVariable("MIRRTEXT", mirrText);
                m_DocumentLock.Dispose();
            }

        }

        private void MyMirror(List<Entity> listEnt, Line line, string xY)
        {
            Application.SetSystemVariable("MIRRTEXT", 0);
            List<Entity> list = new List<Entity>();
            if (listEnt == null || line == null) return;

            Line3d line3d = new Line3d(line.StartPoint, line.EndPoint);

            for (int i = 0; i < listEnt.Count; i++)
            {
                var entity = listEnt[i];

                Entity ent = entity.GetTransformedCopy(Matrix3d.Mirroring(line3d));
                var dim = ent as Dimension;
               
                

                var ptMin = ent.Bounds.Value.MinPoint;

                var ptMax = ent.Bounds.Value.MaxPoint;

                var ptCenter = new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0);
                if (ent is DBText) //|| entity is Dimension || entity is MText)
                {

                    Plane p = null;
                    if (xY == "X")
                        p = new Plane(ptCenter, Vector3d.YAxis);
                    else if (xY == "Y")
                        p = new Plane(ptCenter, Vector3d.XAxis);

                    ent = ent.GetTransformedCopy(Matrix3d.Mirroring(p));

                }
                else if (dim != null)
                {
                    Plane p = null;

                    var dimV = dim.Normal;

                    if (xY == "X")
                    {
                        
                        p = new Plane(dim.TextPosition, dimV);
                        ent = dim.GetTransformedCopy(Matrix3d.Mirroring(p));
                    }
                    else if (xY == "Y")
                    {
                        dimV = dimV.RotateBy(Math.PI / 2, Vector3d.ZAxis);

                        p = new Plane(dim.TextPosition, Vector3d.YAxis);

                        ent = dim.GetTransformedCopy(Matrix3d.Mirroring(p));
                    }
                    

                }

                else if (ent is MText) //|| entity is Dimension || entity is MText)
                {
                    Plane p = null;
                    if (xY == "X")
                    {

                        var ptMT = ptCenter + Vector3d.XAxis * 100;

                        var l3d = new Line3d(ptCenter, ptMT);

                        ent = ent.GetTransformedCopy(Matrix3d.Mirroring(l3d));

                    }
                    else if (xY == "Y")
                    {
                        p = new Plane(ptCenter, Vector3d.ZAxis);

                        ent = ent.GetTransformedCopy(Matrix3d.Mirroring(p));
                    }
                }

                list.Add(ent);
            }

            list.ToSpace();

        }
    }


}
