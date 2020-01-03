using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Linq;
namespace GetMinDisOfCurve
{
    public class GetMinDis
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("GetMinDis")]
        public void GetMinDistance()
        {
            Curve curve1 = null;
            Curve curve2 = null;

            var entOpts = new PromptEntityOptions("\n请选择曲线1:\n");

            entOpts.SetRejectMessage("未选择正确");

            entOpts.AddAllowedClass(typeof(Curve), false);

            PromptEntityResult entRes = ed.GetEntity(entOpts);

            if (entRes.Status != PromptStatus.OK)
            {
                return;
            }
            ObjectId id1 = entRes.ObjectId;


            var entOpts2 = new PromptEntityOptions("\n请选择曲线2:");

            entOpts2.SetRejectMessage("未选择正确");

            entOpts2.AddAllowedClass(typeof(Curve), false);

            PromptEntityResult entRes2 = ed.GetEntity(entOpts2);

            if (entRes2.Status != PromptStatus.OK)
            {
                return;
            }
            ObjectId id2 = entRes2.ObjectId;


            using (var trans = db.TransactionManager.StartTransaction())
            {

                curve1 = trans.GetObject(id1, OpenMode.ForRead) as Curve;
                curve2 = trans.GetObject(id2, OpenMode.ForRead) as Curve;

                trans.Commit();
            }

            double jd = 1;

            var line = GetMinLine(curve1, curve2, jd);
            
            line.ColorIndex = 1;

            line.ToSpace();
            line.Dispose();
        }

        public Line GetMinLine(Curve curve1,Curve curve2,double jd)
        {
            List<Curve> lstCurves = GetCurves(curve1, jd);

            double minVal = double.MaxValue;
            Point3d ptMin1 = Point3d.Origin;
            Point3d ptMin2 = Point3d.Origin;
            foreach (var c in lstCurves)
            {
                Point3d pt1 = c.StartPoint;
                Point3d pt2 = c.EndPoint;

                var pt11=curve2.GetClosestPointTo(pt1, false);
                var pt22= curve2.GetClosestPointTo(pt2, false);

                var l1 = pt11.DistanceTo(pt1);
                var l2 = pt22.DistanceTo(pt2);

                if (l1 < minVal)
                {
                    minVal = l1;
                    ptMin1 = pt11;
                    ptMin2 = pt1;
                }
                if (l2 < minVal)
                {
                    minVal = l2;
                    ptMin1 = pt22;
                    ptMin2 = pt2;
                }

            }
            ed.WriteMessage("\n最短距离：" + minVal + "\n");

            return new Line(ptMin1,ptMin2);
        }

        public List<Curve> GetCurves(Curve curve ,double jd)
        {
            List<Curve> lstCurves = new List<Curve>();

            double totalLength = curve.GetDistanceAtParameter(curve.EndParam);

            if (totalLength < jd)
            {
                lstCurves.Add(curve);
                return lstCurves;
            }
            double addLength = 0;

            Point3dCollection pt3dCol = new Point3dCollection();

            while (addLength < totalLength)
            {
                pt3dCol.Add(curve.GetPointAtDist(addLength));
                addLength += jd;

            }
            if (addLength != totalLength)
                pt3dCol.Add(curve.GetPointAtDist(totalLength));


           DBObjectCollection dbObjColl= curve.GetSplitCurves(pt3dCol);

            foreach (var item in dbObjColl)
            {
                lstCurves.Add((Curve)item);
            }

            dbObjColl.Dispose();

            return lstCurves;
        }

    }
}
