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
    public class TextAndPlMirror
    {
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        List<Entity> list = new List<Entity>();
        List<ObjectId> listOId = new List<ObjectId>();

        [CommandMethod("EceShiTiJX")]
        public void MirrorTextAndPl()
        {
            DocumentLock docLock = null;

            list.Clear();
            listOId.Clear();

            try
            {

                docLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
                var res = Ed.GetSelection();

                if (res.Status != PromptStatus.OK) return;

                var listOids = res.Value.GetObjectIds().ToList();

                List<Entity> listEnt = new List<Entity>();
                List<Point3d> listPt = new List<Point3d>();

                using (var trans = Db.TransactionManager.StartTransaction())
                {

                    List<Point3d[]> listPtArr = new List<Point3d[]>();

                    foreach (var oId in listOids)
                    {

                        var ent = trans.GetObject(oId, OpenMode.ForWrite) as Entity;

                        listPtArr.Add(GetSizePt(ent));

                        if (ent is DBText)
                        {

                            var dbText = ent as DBText;

                            listPt.Add(dbText.Position);

                        }
                        else if (ent is MText)
                        {
                            var mText = ent as MText;

                            listPt.Add(mText.Location);

                        }

                        listEnt.Add(ent);
                    }



                    Point3d[] ptArr = new Point3d[2];

                    var listPtSort = listPt.OrderBy(p => p.X).ToList();

                    ptArr[0] = listPtSort.First();

                    ptArr[1] = listPtSort[listPtSort.Count - 1];

                    Line lPt = new Line(ptArr[0], ptArr[1]);

                    lPt.ToSpace();

                    Point3d centerPt = new Point3d((ptArr[1].X + ptArr[0].X) / 2, ptArr[0].Y, ptArr[0].Z);

                    var ptEnd = centerPt + Vector3d.YAxis * 100;

                    var line = new Line(centerPt, ptEnd);

                    line.ColorIndex = 20;

                    line.ToSpace();

                    /*Line3d l3d = new Line3d(centerPt, ptEnd);

                    var mtrix = Matrix3d.Mirroring(l3d);*/



                    foreach (var i in Enumerable.Range(0, listEnt.Count))
                    {

                        var ent = listEnt[i];

                        var cPt = listPtArr[i][2];

                        var cic = new Circle(cPt, Vector3d.ZAxis, 1000);

                        cic.ColorIndex = 6;
                        cic.ToSpace();

                        double len1 = Math.Abs(ptArr[0].X - ptArr[1].X);

                        if (ent is DBText || ent is MText)
                        {
                            var pt = Point3d.Origin;
                            var ptTxt = Point3d.Origin;
                            if (ent is DBText)
                                ptTxt = ((DBText)ent).Position;
                            else
                                ptTxt = ((MText)ent).Location;
                            pt = line.GetClosestPointTo(ptTxt, true);

                            var vec = pt - ptTxt;

                            if (vec.X > 0)
                                ent.TransformBy(Matrix3d.Displacement(vec.GetNormal() * len1));
                            else
                            {
                                double txtWidth = (Math.Abs(listPtArr[i][0].X - listPtArr[i][1].X));

                                ent.TransformBy(Matrix3d.Displacement(vec.GetNormal() * len1 + vec.GetNormal() *txtWidth/3));

                            }
                            //if (ent is DBText)
                            //    MirrorText((DBText)ent, l3d);
                            //else
                            //    MirrorText((MText)ent, l3d);
                        }
                        else
                        {
                            var ptE = cPt + Vector3d.YAxis * 10000;

                            var line2 = new Line(cPt, ptE);

                            line2.ColorIndex = 3;

                            line2.ToSpace();

                            Line3d l3d2 = new Line3d(cPt, ptE);

                            var mtrix2 = Matrix3d.Mirroring(l3d2);

                            ent.TransformBy(mtrix2);
                        }
                        ent.DowngradeOpen();
                    }
                    trans.Commit();
                }


            }
            catch (System.Exception e)
            {

                Ed.WriteMessage(e.ToString());

            }
            finally
            {
                docLock.Dispose();
            }
        }

        Point3d[] GetSizePt(Entity ent)
        {
            var ptMin = ent.GeometricExtents.MinPoint;
            var ptMax = ent.GeometricExtents.MaxPoint;

            var ptCenter = new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, (ptMin.Z + ptMax.Z) / 2);
            return new[] { ptMin, ptMax, ptCenter };
        }


        void MirrorText(DBText ent, Line3d mirrorLine)

        {

            Database db = ent.ObjectId.Database;



            using (Transaction tr = db.TransactionManager.StartTransaction())

            {

                // Get text entity

                DBText dbText = tr.GetObject(ent.ObjectId, OpenMode.ForRead)

                    as DBText;



                // Clone original entity

                DBText mirroredTxt = dbText;



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

                tr.Commit();

            }

        }

        void MirrorText(MText ent, Line3d mirrorLine)

        {

            Database db = ent.ObjectId.Database;

            MText mText = new MText();


            using (Transaction tr = db.TransactionManager.StartTransaction())

            {

                MText dbText = tr.GetObject(ent.ObjectId, OpenMode.ForWrite)

                    as MText;

                DBObjectCollection dbColl = new DBObjectCollection();

                dbText.Explode(dbColl);

                Matrix3d mirrorMatrix = Matrix3d.Mirroring(mirrorLine);

                var location = ent.Location.TransformBy(mirrorMatrix);
                mText.Location = location;

                double rot = Math.PI * 2 - ent.Rotation;


                foreach (DBText txt in dbColl)
                {

                    listOId.Add(txt.ToSpace());

                    DBText mirroredTxt = txt.Clone() as DBText;
                    //DBText mirroredTxt = txt;

                    mirroredTxt.TransformBy(mirrorMatrix);
                    // Get text bounding box

                    Point3d pt1, pt2, pt3, pt4;

                    GetTextBoxCorners(

                        txt,

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
                    //mText.TextStyleId = mirroredTxt.TextStyleId;
                    //mText.Contents += mirroredTxt.TextString + "\\P";

                    //list.Add(mirroredTxt);
                }


                //mText.TextHeight = ent.TextHeight;
                //mText.LineSpaceDistance = ent.LineSpaceDistance;
                //mText.LineSpacingFactor = ent.LineSpacingFactor;
                //mText.LinetypeId = ent.LinetypeId;

                //var ptMin = mText.Bounds.Value.MinPoint;
                //var ptMax = mText.Bounds.Value.MaxPoint;
                //var ptCenter = new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0);
                //mText.Location = ptCenter;
                //mText.TransformBy(Matrix3d.Rotation(rot, Vector3d.ZAxis, ptCenter));


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

        #endregion

    }
}
