using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Runtime;

namespace LearnMyMirror
{
    public class MyMirror
    {
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        List<Entity> list = new List<Entity>();
        List<ObjectId> listOId = new List<ObjectId>();

        [CommandMethod("testM")]

        public void MirrorTextCmd()

        {

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;



            //Entity selection

            PromptEntityOptions peo = new PromptEntityOptions(

                "\nSelect a text entity:");



            peo.SetRejectMessage("\nMust be text entity...");

            peo.AddAllowedClass(typeof(DBText), true);



            PromptEntityResult perText = ed.GetEntity(peo);



            if (perText.Status != PromptStatus.OK)

                return;



            peo = new PromptEntityOptions("\nSelect a mirror line:");

            peo.SetRejectMessage("\nMust be a line entity...");

            peo.AddAllowedClass(typeof(Line), true);



            PromptEntityResult perLine = ed.GetEntity(peo);



            if (perLine.Status != PromptStatus.OK)

                return;



            using (Transaction tr = db.TransactionManager.StartTransaction())

            {

                Line line = tr.GetObject(perLine.ObjectId, OpenMode.ForRead)

                    as Line;



                Line3d mirrorLine = new Line3d(

                    line.StartPoint,

                    line.EndPoint);



                MirrorText(perText.ObjectId, mirrorLine);



                tr.Commit();

            }

        }



        void MirrorText(ObjectId oId, Line3d mirrorLine)

        {

            Database db = oId.Database;



            using (Transaction tr = db.TransactionManager.StartTransaction())

            {

                // Get text entity

                DBText dbText = tr.GetObject(oId, OpenMode.ForRead)

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

                //list.Add(mirroredTxt);
                mirroredTxt.ToSpace();
                tr.Commit();

            }

        }
        #region p/Invoke


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

            var ptX = pt1 + Vector3d.XAxis * 40;
            var ptY = pt2 + Vector3d.YAxis * 50;


            var lX = new Line(pt1, ptX);
            var lY = new Line(pt2, ptY);

            lX.Color= Color.FromColor(System.Drawing.Color.Green);
            lY.Color= Color.FromColor(System.Drawing.Color.Orange);


            Line line = new Line(pt1, pt2);

            line.Color = Color.FromColor(System.Drawing.Color.Red);

            line.ToSpace();
            lX.ToSpace();
            lY.ToSpace();

            // Create rotation matrix

            Matrix3d rotMat = Matrix3d.Rotation(

                dbText.Rotation,

                dbText.Normal,

                pt1);



            // The returned points from acedTextBox need

            // to be transformed as follow

            pt1 = pt1.TransformBy(rotMat).Add(dbText.Position.GetAsVector());

            pt2 = pt2.TransformBy(rotMat).Add(dbText.Position.GetAsVector());

            Line linetrans = new Line(pt1, pt2);

            linetrans.Color = Color.FromColor(System.Drawing.Color.Yellow) ;

            linetrans.ToSpace();


            Vector3d rotDir = new Vector3d(

                -Math.Sin(dbText.Rotation),

                Math.Cos(dbText.Rotation), 0);


            //求垂直于rotDir和normal的法向量
            Vector3d linDir = rotDir.CrossProduct(dbText.Normal);



            double actualWidth =

                Math.Abs((pt2.GetAsVector() - pt1.GetAsVector())

                    .DotProduct(linDir));



            pt3 = pt1.Add(linDir * actualWidth);

            pt4 = pt2.Subtract(linDir * actualWidth);

            Line linetrans2 = new Line(pt3, pt4);

            linetrans2.Color = Color.FromColor(System.Drawing.Color.Blue);

            linetrans2.ToSpace();
        }

        #endregion
    }
}
