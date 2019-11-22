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
        List<ObjectId> listOId = new List<ObjectId>();
        string XY = "";

        [CommandMethod("ECDKuaiJX")]
        public void GetJingXiangXY()
        {
            DocumentLock m_DocumentLock = null;
            blkRef = null;
            list.Clear();
            listOId.Clear();
            XY = "";




            try
            {
                m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();

                var br = GetBlockCondition("请选择要镜像的块") as BlockReference;
                string name = br.Name;

                if (br == null)
                {
                    return;
                }
                blkRef = br;

                Circle circle = new Circle(blkRef.Position, Vector3d.ZAxis, 0.001);

                listOId.Add(circle.ToSpace());

                Circle mirrorCircle = null;

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

                using (Transaction tr = blkRef.Database.TransactionManager.StartTransaction())

                {

                    if (xYJingXiang == "Y")
                    {

                        Point3d ptEnd = ptPos + Vector3d.YAxis * 100;

                        Line lineY = new Line(ptPos, ptEnd);

                        lineY.TransformBy(Matrix3d.Displacement(Vector3d.XAxis * maxX));

                        XY = "Y";
                        mirrorCircle = circle.GetTransformedCopy(Matrix3d.Mirroring(new Line3d(lineY.StartPoint, lineY.EndPoint))) as Circle;
                        MyMirror(listEnt, lineY, "Y");

                    }
                    else if (xYJingXiang == "X")
                    {

                        Point3d ptEnd = ptPos + Vector3d.XAxis * 100;

                        Line lineX = new Line(ptPos, ptEnd);

                        lineX.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * maxY));

                        XY = "X";
                        mirrorCircle = circle.GetTransformedCopy(Matrix3d.Mirroring(new Line3d(lineX.StartPoint, lineX.EndPoint))) as Circle;
                        MyMirror(listEnt, lineX, "X");

                    }

                    tr.Commit();

                }

                ObjectId breNewId = ObjectId.Null;

                using (var trans = blkRef.Database.TransactionManager.StartTransaction())
                {

                    var blkTbl = trans.GetObject(blkRef.Database.BlockTableId, OpenMode.ForWrite) as BlockTable;

                    BlockTableRecord blkRec = new BlockTableRecord();

                    blkRec.Units = br.BlockUnit;

                    string blkName = br.Name + "_" + DateTime.Now.ToString("yyyyMMddHHmmssffff");

                    blkRec.Name = blkName;
                    blkRec.Origin = mirrorCircle.Center;

                    for (int i = 0; list != null && i < list.Count; i++)
                    {

                        var ent = list[i];

                        if (ent != null)
                        {
                            //Entity entCopy = ent.Clone() as Entity;

                            // ent.Erase(true);

                            blkRec.AppendEntity(ent);
                            //ent.ToSpace();
                        }
                    }

                    breNewId = blkTbl.Add(blkRec);

                    trans.AddNewlyCreatedDBObject(blkRec, true);

                    foreach (var oId in listOId)
                    {

                        var ent = trans.GetObject(oId, OpenMode.ForWrite) as Entity;

                        ent.Erase(true);

                    }

                    listOId.Clear();

                    trans.Commit();

                }

                list.ForEach(ent => ent.Dispose());

                var brOld = new BlockReference(br.Position, br.BlockTableRecord);

                var brNew = new BlockReference(mirrorCircle.Center, breNewId);

                brNew.ToSpace();

                brOld.ToSpace();

            }
            catch (System.Exception e)
            {

                Ed.WriteMessage(e.ToString());
            }
            finally
            {
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

            for (int i = 0; i < listEnt.Count; i++)
            {

                var entity = listEnt[i];

                Entity ent = entity;

                //if((ent as Dimension) != null)
                //{
                //    continue;
                //}


                if (ent is DBText || ent is MText)
                {

                    listOId.Add(ent.ToSpace(blkRef.Database));
                }
                else
                {
                    ent = entity.GetTransformedCopy(Matrix3d.Mirroring(line3d));

                    if ((ent as Dimension) == null)
                    { list.Add(ent); }

                    //continue;
                }

                /* var ptMin = ent.Bounds.Value.MinPoint;

                 var ptMax = ent.Bounds.Value.MaxPoint;

                 var w = Math.Abs(ptMax.X - ptMin.X);
                 var h = Math.Abs(ptMax.Y - ptMin.Y);

                 var ptCenter = new Point3d((ptMin.X + ptMax.X) / 2, (ptMin.Y + ptMax.Y) / 2, 0);*/
                if (ent is DBText)
                {
                    var a = ent as DBText;
                    MirrorText(a, line3d);

                }
                else if (ent is MText)
                {
                    var a = ent as MText;

                    MirrorText(a, line3d);

                }
                else if ((ent as Dimension) != null)
                {

                    var dim = ent as Dimension;



                    Plane p = null;

                    if (xY == "X")
                    {

                        p = new Plane(dim.TextPosition, dim.Normal);
                        ent = dim.GetTransformedCopy(Matrix3d.Mirroring(p));

                    }
                    else if (xY == "Y")
                    {

                        p = new Plane(dim.TextPosition, dim.Normal);

                        ent = dim.GetTransformedCopy(Matrix3d.Mirroring(p));




                    }
                    if (ent is RotatedDimension)
                    {

                        var rDim = ent as RotatedDimension;

                       var rDim1 = new RotatedDimension(rDim.Rotation, rDim.XLine1Point, rDim.XLine2Point, rDim.DimLinePoint, rDim.DimensionText, rDim.DimensionStyle);
                        Dim2Dim(rDim1, rDim);
                        list.Add(rDim1);

                    }

                    else if (ent is AlignedDimension)
                    {
                        var rDim = ent as AlignedDimension;

                       var rDim1 = new AlignedDimension(rDim.XLine1Point, rDim.XLine2Point, rDim.DimLinePoint, rDim.DimensionText, rDim.DimensionStyle);
                        Dim2Dim(rDim1, rDim);
                        list.Add(rDim1);
                        
                    }
                    else if (ent is ArcDimension)
                    {
                        var rDim = ent as ArcDimension;

                       var rDim1 = new ArcDimension(rDim.CenterPoint, rDim.XLine1Point, rDim.XLine2Point, rDim.ArcPoint, rDim.DimensionText, rDim.DimensionStyle);

                        Dim2Dim(rDim1, rDim);
                        list.Add(rDim1);
                    }
                    else if (ent is DiametricDimension)
                    {
                        var rDim = ent as DiametricDimension;

                       var rDim1 = new DiametricDimension(rDim.ChordPoint, rDim.FarChordPoint, rDim.LeaderLength, rDim.DimensionText, rDim.DimensionStyle);

                        Dim2Dim(rDim1, rDim);
                        list.Add(rDim1);
                    }
                    else if (ent is LineAngularDimension2)
                    {
                        var rDim = ent as LineAngularDimension2;

                       var rDim1 = new LineAngularDimension2(rDim.XLine1Start, rDim.XLine1End, rDim.XLine2Start, rDim.XLine2End, rDim.ArcPoint, rDim.DimensionText, rDim.DimensionStyle);

                        Dim2Dim(rDim1, rDim);
                        list.Add(rDim1);
                    }
                    else if (ent is Point3AngularDimension)
                    {
                        var rDim = ent as Point3AngularDimension;

                      var  rDim1 = new Point3AngularDimension(rDim.CenterPoint, rDim.XLine1Point, rDim.XLine2Point, rDim.ArcPoint, rDim.DimensionText, rDim.DimensionStyle);

                        Dim2Dim(rDim1, rDim);
                        list.Add(rDim1);
                    }
                    else if (ent is RadialDimension)
                    {
                        var rDim = ent as RadialDimension;

                        var rDim1 = new RadialDimension(rDim.Center, rDim.ChordPoint, rDim.LeaderLength, rDim.DimensionText, rDim.DimensionStyle);


                        Dim2Dim(rDim1, rDim);
                        list.Add(rDim1);
                    }
                    else if (ent is RadialDimensionLarge)
                    {
                        var rDim = ent as RadialDimensionLarge;

                       var rDim1= new RadialDimensionLarge(rDim.Center, rDim.ChordPoint, rDim.OverrideCenter, rDim.JogPoint, rDim.JogAngle, rDim.DimensionText, rDim.DimensionStyle);

                        Dim2Dim(rDim1, rDim);
                        list.Add(rDim1);
                    }

                }
            }



            listEnt.ForEach(ent => ent.Dispose());

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


        void MirrorText(DBText ent, Line3d mirrorLine)

        {

            Database db = ent.ObjectId.Database;



            using (Transaction tr = db.TransactionManager.StartTransaction())

            {

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
