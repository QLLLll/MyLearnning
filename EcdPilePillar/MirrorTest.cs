using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Runtime;


namespace EcdPilePillar
{
    public class MirrorTest
    {
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        BlockReference blkRef = null;
        List<Entity> list = new List<Entity>();
        string XY = "";

        [CommandMethod("ecdTest")]
        public void GetJingXiangXY()
        {
            DocumentLock m_DocumentLock = null;
            object mirrText = "";
            blkRef = null;
            list.Clear();
            XY = "";
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
                blkRef = br;
                //IdMapping idmap = new IdMapping();


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
                using (Transaction tr = Db.TransactionManager.StartTransaction())

                {

                    if (xYJingXiang == "Y")
                    {

                        Point3d ptEnd = ptPos + Vector3d.YAxis * 100;

                        Line lineY = new Line(ptPos, ptEnd);

                        lineY.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * maxX));
                        XY = "Y";

                        MyMirror(listEnt, lineY, "Y");


                    }
                    else if (xYJingXiang == "X")
                    {

                        Point3d ptEnd = ptPos + Vector3d.XAxis * 100;

                        Line lineX = new Line(ptPos, ptEnd);

                        lineX.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * maxY));

                        XY = "X";
                        MyMirror(listEnt, lineX, "X");

                    }

                    tr.Commit();

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

        private void MyMirror(List<Entity> listEnt, Line line, string xY)
        {

            if (listEnt == null || line == null) return;

            Line3d line3d = new Line3d(line.StartPoint, line.EndPoint);

            double rotation = 0;

            for (int i = 0; i < listEnt.Count; i++)
            {
                var entity = listEnt[i];

                Entity ent = entity;
                if (ent is DBText || ent is MText)
                {
                    ent = entity;
                    ent.ToSpace();
                }
                else
                {
                    ent = entity.GetTransformedCopy(Matrix3d.Mirroring(line3d));
                    list.Add(ent);

                    continue;
                }
                //Entity ent = entity.GetTransformedCopy(Matrix3d.Mirroring(line3d));

                var dbText = ent as DBText;

                if (dbText != null)
                {
                    rotation = dbText.Rotation;
                    //Ed.WriteMessage((rotation * 180 / Math.PI).ToString() + "\n");
                }

                var dim = ent as Dimension;

                var ptMin = ent.Bounds.Value.MinPoint;

                var ptMax = ent.Bounds.Value.MaxPoint;

                var w = Math.Abs(ptMax.X - ptMin.X);
                var h = Math.Abs(ptMax.Y - ptMin.Y);

                var ptCenter = new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0);
                if (ent is DBText)
                {
                    var a = ent as DBText;
                    MirrorText(a, line3d);
                    continue;
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
                        //dimV = dimV.RotateBy(Math.PI / 2, Vector3d.ZAxis);

                        //p = new Plane(dim.TextPosition, Vector3d.YAxis);

                        //ent = dim.GetTransformedCopy(Matrix3d.Mirroring(p));

                        //dynamic blockref_com = dim.AcadObject;
                        //double[] pt1_com = new Point3d(0, 0, 0).ToArray();
                        //double[] pt2_com = new Point3d(0, -1, 0).ToArray();
                        //Vector3d v = new Vector3d(0, -1, -1);
                        p = new Plane(dim.TextPosition, dimV);

                        //ent = dim.GetTransformedCopy(Matrix3d.Mirroring(p));
                        //blockref_com.Mirror(pt1_com, pt2_com);

                        double d = ptMin.X - dimV.X;
                        double d2 = ptMax.X - dimV.X;
                        double d3 = d + d2;
                        Point3d source = new Point3d(d3, 0, 0);
                        Vector3d V = source.GetVectorTo(dim.TextPosition);
                        dim.TransformBy(Matrix3d.Displacement(V));
                    }


                }

                else if (ent is MText)
                {
                    var a = ent as MText;

                    MirrorText(a, line3d);
                    continue;
                }

                list.Add(ent);
            }

            list.ToSpace();

            list.ForEach(ent => ent.Dispose());

            listEnt.ForEach(ent => ent.Dispose());

        }

        void MirrorText(DBText ent, Line3d mirrorLine)

        {

            Database db = ent.ObjectId.Database;



            using (Transaction tr = db.TransactionManager.StartTransaction())

            {

                BlockTableRecord btr = tr.GetObject(

                    db.CurrentSpaceId,

                    OpenMode.ForWrite) as BlockTableRecord;



                // Get text entity

                DBText dbText = tr.GetObject(ent.ObjectId, OpenMode.ForRead)

                    as DBText;



                // Clone original entity

                DBText mirroredTxt = dbText.Clone() as DBText;



                // Create a mirror matrix

                Matrix3d mirrorMatrix = Matrix3d.Mirroring(mirrorLine);



                // Do a geometric mirror on the cloned text

                mirroredTxt.TransformBy(mirrorMatrix);



                // Get text bounding box

                Point3d pt1, pt2, pt3, pt4;

                GetTextBoxCorners(

                    dbText,

                    out pt1,

                    out pt2,

                    out pt3,

                    out pt4);



                // Get the perpendicular direction to the original text

                Vector3d rotDir =

                    pt4.Subtract(pt1.GetAsVector()).GetAsVector();



                // Get the colinear direction to the original text

                Vector3d linDir =

                    pt3.Subtract(pt1.GetAsVector()).GetAsVector();



                // Compute mirrored directions

                Vector3d mirRotDir = rotDir.TransformBy(mirrorMatrix);

                Vector3d mirLinDir = linDir.TransformBy(mirrorMatrix);



                //Check if we need to mirror in Y or in X

                if (Math.Abs(mirrorLine.Direction.Y) >

                    Math.Abs(mirrorLine.Direction.X))

                {

                    // Handle the case where text is mirrored twice

                    // instead of doing "oMirroredTxt.IsMirroredInX = true"

                    mirroredTxt.IsMirroredInX = !mirroredTxt.IsMirroredInX;

                    mirroredTxt.Position = mirroredTxt.Position + mirLinDir;

                }

                else

                {

                    mirroredTxt.IsMirroredInY = !mirroredTxt.IsMirroredInY;

                    mirroredTxt.Position = mirroredTxt.Position + mirRotDir;

                }



                // Add mirrored text to database

                //btr.AppendEntity(mirroredTxt);

                //tr.AddNewlyCreatedDBObject(mirroredTxt, true);

                list.Add(mirroredTxt);

                tr.Commit();

            }

        }

        void MirrorText(MText ent, Line3d mirrorLine)

        {

            Database db = ent.ObjectId.Database;

            MText mText = new MText();


            using (Transaction tr = db.TransactionManager.StartTransaction())

            {

                BlockTableRecord btr = tr.GetObject(

                    db.CurrentSpaceId,

                    OpenMode.ForWrite) as BlockTableRecord;



                // Get text entity

                MText dbText = tr.GetObject(ent.ObjectId, OpenMode.ForRead)

                    as MText;



                // Clone original entity



                DBObjectCollection dbColl = new DBObjectCollection();

                dbText.Explode(dbColl);

                
                // Create a mirror matrix

                Matrix3d mirrorMatrix = Matrix3d.Mirroring(mirrorLine);

                // Do a geometric mirror on the cloned text

                
                var location = ent.Location.TransformBy(mirrorMatrix);
                mText.Location = location;
                //mText.Rotation = Math.PI * 2 - ent.Rotation;
                double rot = Math.PI * 2 - ent.Rotation;

                
                foreach (DBText txt in dbColl)
                {
                    DBText mirroredTxt = txt.Clone() as DBText;
                    mirroredTxt.TransformBy(mirrorMatrix);
                    // Get text bounding box

                    Point3d pt1, pt2, pt3, pt4;

                    GetTextBoxCorners(

                        dbText,

                        out pt1,

                        out pt2,

                        out pt3,

                        out pt4);



                    // Get the perpendicular direction to the original text

                    Vector3d rotDir =

                        pt4.Subtract(pt1.GetAsVector()).GetAsVector();



                    // Get the colinear direction to the original text

                    Vector3d linDir =

                        pt3.Subtract(pt1.GetAsVector()).GetAsVector();



                    // Compute mirrored directions

                    Vector3d mirRotDir = rotDir.TransformBy(mirrorMatrix);

                    Vector3d mirLinDir = linDir.TransformBy(mirrorMatrix);



                    //Check if we need to mirror in Y or in X

                    if (Math.Abs(mirrorLine.Direction.Y) >

                        Math.Abs(mirrorLine.Direction.X))

                    {

                        // Handle the case where text is mirrored twice

                        // instead of doing "oMirroredTxt.IsMirroredInX = true"

                        mirroredTxt.IsMirroredInX = !mirroredTxt.IsMirroredInX;

                        mirroredTxt.Position = mirroredTxt.Position + mirLinDir;

                    }

                    else

                    {

                        mirroredTxt.IsMirroredInY = !mirroredTxt.IsMirroredInY;

                        mirroredTxt.Position = mirroredTxt.Position + mirRotDir;

                    }
                    mText.TextStyleId = mirroredTxt.TextStyleId;
                    mText.Contents += mirroredTxt.TextString + "\\P";

                    list.Add(mirroredTxt);
                }
                
                
                mText.TextHeight = ent.TextHeight;
                mText.LineSpaceDistance = ent.LineSpaceDistance;
                mText.LineSpacingFactor = ent.LineSpacingFactor;
                mText.LinetypeId = ent.LinetypeId;
                
                var ptMin = mText.Bounds.Value.MinPoint;
                var ptMax = mText.Bounds.Value.MaxPoint;
                var ptCenter = new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0);
                mText.Location = ptCenter;
                mText.TransformBy(Matrix3d.Rotation(rot, Vector3d.ZAxis, ptCenter));
                

                tr.Commit();

            }

        }

        public struct ads_name

        {

            public IntPtr a;

            public IntPtr b;

        };



        // Exported function names valid only for R19



        [DllImport("acdb22.dll",

            CallingConvention = CallingConvention.Cdecl,

            EntryPoint = "?acdbGetAdsName@@YA?AW4ErrorStatus@Acad@@AAY01JVAcDbObjectId@@@Z")]

        public static extern int acdbGetAdsName32(

            ref ads_name name,

            ObjectId objId);



        [DllImport("acdb22.dll",

            CallingConvention = CallingConvention.Cdecl,

            EntryPoint = "?acdbGetAdsName@@YA?AW4ErrorStatus@Acad@@AEAY01_JVAcDbObjectId@@@Z")]

        public static extern int acdbGetAdsName64(

            ref ads_name name,

            ObjectId objId);



        public static int acdbGetAdsName(ref ads_name name, ObjectId objId)

        {

            if (Marshal.SizeOf(IntPtr.Zero) > 4)

                return acdbGetAdsName64(ref name, objId);



            return acdbGetAdsName32(ref name, objId);

        }



        [DllImport("accore.dll",

            CharSet = CharSet.Unicode,

            CallingConvention = CallingConvention.Cdecl,

            EntryPoint = "acdbEntGet")]

        public static extern System.IntPtr acdbEntGet(

            ref ads_name ename);



        [DllImport("accore.dll",

            CharSet = CharSet.Unicode,

            CallingConvention = CallingConvention.Cdecl,

            EntryPoint = "acedTextBox")]

        public static extern System.IntPtr acedTextBox(

            IntPtr rb,

            double[] point1,

            double[] point2);



        void GetTextBoxCorners(DBText dbText, out Point3d pt1, out Point3d pt2, out Point3d pt3, out Point3d pt4)

        {

            ads_name name = new ads_name();



            int result = acdbGetAdsName(

                ref name,

                dbText.ObjectId);



            ResultBuffer rb = new ResultBuffer();



            Interop.AttachUnmanagedObject(

                rb,

                acdbEntGet(ref name), true);



            double[] point1 = new double[3];

            double[] point2 = new double[3];



            // Call imported arx function

            acedTextBox(rb.UnmanagedObject, point1, point2);



            pt1 = new Point3d(point1);

            pt2 = new Point3d(point2);



            // Create rotation matrix

            Matrix3d rotMat = Matrix3d.Rotation(

                dbText.Rotation,

                dbText.Normal,

                pt1);



            // The returned points from acedTextBox need

            // to be transformed as follow

            pt1 = pt1.TransformBy(rotMat).Add(dbText.Position.GetAsVector());

            pt2 = pt2.TransformBy(rotMat).Add(dbText.Position.GetAsVector());



            Vector3d rotDir = new Vector3d(

                -Math.Sin(dbText.Rotation),

                Math.Cos(dbText.Rotation), 0);



            Vector3d linDir = rotDir.CrossProduct(dbText.Normal);



            double actualWidth =

                Math.Abs((pt2.GetAsVector() - pt1.GetAsVector())

                    .DotProduct(linDir));



            pt3 = pt1.Add(linDir * actualWidth);

            pt4 = pt2.Subtract(linDir * actualWidth);

        }


        void GetTextBoxCorners(MText dbText, out Point3d pt1, out Point3d pt2, out Point3d pt3, out Point3d pt4)

        {

            ads_name name = new ads_name();



            int result = acdbGetAdsName(

                ref name,

                dbText.ObjectId);



            ResultBuffer rb = new ResultBuffer();



            Interop.AttachUnmanagedObject(

                rb,

                acdbEntGet(ref name), true);



            double[] point1 = new double[3];

            double[] point2 = new double[3];



            // Call imported arx function

            acedTextBox(rb.UnmanagedObject, point1, point2);



            pt1 = new Point3d(point1);

            pt2 = new Point3d(point2);



            // Create rotation matrix

            Matrix3d rotMat = Matrix3d.Rotation(

                dbText.Rotation,

                dbText.Normal,

                pt1);



            // The returned points from acedTextBox need

            // to be transformed as follow

            pt1 = pt1.TransformBy(rotMat).Add(dbText.Location.GetAsVector());

            pt2 = pt2.TransformBy(rotMat).Add(dbText.Location.GetAsVector());



            Vector3d rotDir = new Vector3d(

                -Math.Sin(dbText.Rotation),

                Math.Cos(dbText.Rotation), 0);



            Vector3d linDir = rotDir.CrossProduct(dbText.Normal);



            double actualWidth =

                Math.Abs((pt2.GetAsVector() - pt1.GetAsVector())

                    .DotProduct(linDir));



            pt3 = pt1.Add(linDir * actualWidth);

            pt4 = pt2.Subtract(linDir * actualWidth);

        }
    }
}
