using System;
using System.Collections.Generic;
using System.Linq;


using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;



namespace EcdPilePillar
{
    public class EcdDrawGirder
    {

        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("ECDLiang")]
        public void DrawGirder()
        {
            Entity ent = GetBlockCondition();

            if (ent == null) { Application.ShowAlertDialog("请选择第一个实体"); return; }

            var extends = ent.GeometricExtents;

            var minPt = extends.MinPoint;
            var maxPt = extends.MaxPoint;

            var centerPt1 = new Point3d((minPt.X + maxPt.X) / 2, (minPt.Y + maxPt.Y) / 2, 0);

            // Ed.WriteMessage($"[{centerPt.X},{centerPt.Y},0]");

            Entity ent2 = GetBlockCondition();

            if (ent2 == null) { Application.ShowAlertDialog("请选择第二个实体"); return; }

            var extends2 = ent2.GeometricExtents;

            var minPt2 = extends2.MinPoint;
            var maxPt2 = extends2.MaxPoint;

            var centerPt2 = new Point3d((minPt2.X + maxPt2.X) / 2, (minPt2.Y + maxPt2.Y) / 2, 0);

            Line line = new Line(centerPt1, centerPt2);

            var dbProp = new PromptDoubleOptions("请输入梁宽：\n");
            dbProp.AllowNegative = false;
            dbProp.AllowNone = false;

            var dbRes = Ed.GetDouble(dbProp);

            if (dbRes.Status != PromptStatus.OK) return;

            double width = 1.0 * dbRes.Value/ 2;

            Vector3d v = (centerPt2 - centerPt1).GetNormal().RotateBy(Math.PI/2,Vector3d.ZAxis);

            Line line1 = line.GetTransformedCopy(Matrix3d.Displacement(v * width)) as Line;
            Line line2 = line.GetTransformedCopy(Matrix3d.Displacement(v * -width)) as Line;

            List<Line> listLine = new List<Line>();
            listLine.Add(line);
            listLine.Add(line1);
            listLine.Add(line2);

            CutLine(ent, ent2, listLine);


            //DBHelper.ToSpace(listLine);


        }

        public void CutLine(Entity ent1, Entity ent2, List<Line> listLines)
        {

            Point3dCollection pt3dColl = new Point3dCollection();

            Point3d[] ptArr1 = new Point3d[3];
            Point3d[] ptArr2 = new Point3d[3];

            //listLines[0].IntersectWith(ent1, Intersect.OnBothOperands,pt3dColl,IntPtr.Zero,IntPtr.Zero);
            //ptArr1[0] = pt3dColl[0];
            //pt3dColl.Clear();

            listLines[1].IntersectWith(ent1, Intersect.OnBothOperands, pt3dColl, IntPtr.Zero, IntPtr.Zero);
            ptArr1[1] = pt3dColl[0];
            pt3dColl.Clear();

            listLines[2].IntersectWith(ent1, Intersect.OnBothOperands, pt3dColl, IntPtr.Zero, IntPtr.Zero);
            ptArr1[2] = pt3dColl[0];
            pt3dColl.Clear();

            ////////////////////////

            //listLines[0].IntersectWith(ent2, Intersect.OnBothOperands, pt3dColl, IntPtr.Zero, IntPtr.Zero);
            //ptArr2[0] = pt3dColl[0];
            //pt3dColl.Clear();

            listLines[1].IntersectWith(ent2, Intersect.OnBothOperands, pt3dColl, IntPtr.Zero, IntPtr.Zero);
            ptArr2[1] = pt3dColl[0];
            pt3dColl.Clear();

            listLines[2].IntersectWith(ent2, Intersect.OnBothOperands, pt3dColl, IntPtr.Zero, IntPtr.Zero);
            ptArr2[2] = pt3dColl[0];
            pt3dColl.Clear();

           


            //Line line1 = new Line(ptArr1[0], ptArr2[0]);
            Line line2 = new Line(ptArr1[1], ptArr2[1]);
            Line line3 = new Line(ptArr1[2], ptArr2[2]);


            List<Entity> list = new List<Entity>();

            list.Add(listLines[0]);
            list.Add(line2);
            list.Add(line3);

            list.ForEach(l => { l.Color = Color.FromColor(System.Drawing.Color.DeepPink); });

            DBHelper.ToSpace(list);
            listLines.ForEach(ll => ll.Dispose());
            list.ForEach(lll => lll.Dispose());
        }

        public Entity GetBlockCondition()
        {


            var entOpts = new PromptEntityOptions("请选择桩柱\n");

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

            Entity ent=null;
            using (var trans = Db.TransactionManager.StartTransaction())
            {

                ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;

                if (ent == null)
                {
                    Application.ShowAlertDialog("请先把要填充的块变为块定义，\n并指定块定义的基点为块的中心点。");

                    trans.Commit();

                    return null;
                }

               

                trans.Commit();
            }

            return ent;
        }
    }
}
