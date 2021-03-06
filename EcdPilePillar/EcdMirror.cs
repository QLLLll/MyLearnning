﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Interop;

namespace EcdPilePillar
{
    public class EcdMirror
    {
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("ECDJingXiang", CommandFlags.Session)]
        public void GetJingXiang()
        {


            var mirrText = Application.GetSystemVariable("MIRRTEXT");

            if (mirrText != null && mirrText.ToString() == "1")
            {

                Application.SetSystemVariable("MIRRTEXT", 0);

            }

            DocumentLock m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();

            //eLockViolation
            BlockReference br = GetBlockCondition("请选择要镜像的块") as BlockReference;

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

            var oIdColl = listEnt.ToSpace();

            using (var trans = Db.TransactionManager.StartTransaction())
            {

                var blkRef = trans.GetObject(br.ObjectId, OpenMode.ForWrite) as BlockReference;

                blkRef.Visible = false;

                blkRef.DowngradeOpen();

                trans.Commit();
            }

            app.ActiveDocument.SendCommand("mirror ");


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
                        if(flag)
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

           // var vec = br.Position - ptPosC;

            //brEnt.TransformBy(Matrix3d.Displacement(vec * 2.5));

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

    }


}
