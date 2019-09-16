using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly:CommandClass(typeof(MovePolyline.MovePolyline))]
namespace MovePolyline
{
    public class MovePolyline
    {
        [CommandMethod("ML")]
        public void MoveEntity()
        {


            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            Editor acEd = acDoc.Editor;

            PromptPointOptions acPPointOpts = new PromptPointOptions("");
            acPPointOpts.Message = "\n请输入一个点";

            PromptPointResult acPPointRes = acEd.GetPoint(acPPointOpts);

            if (acPPointRes.Status != PromptStatus.OK)
            {
                return;
            }

            PromptAngleOptions acPAngleOpts = new PromptAngleOptions("");
            acPAngleOpts.Message = "\n请指定角度";

            PromptDoubleResult acPDblRes = acEd.GetAngle(acPAngleOpts);

            if (acPDblRes.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline acPolyline = GetPolyline() as Polyline;


                Matrix3d mtrix = Matrix3d.Displacement(acPPointRes.Value - Point3d.Origin) *
                    Matrix3d.Rotation(acPDblRes.Value - Math.PI * 0.5, Vector3d.ZAxis, Point3d.Origin);

                acPolyline.TransformBy(mtrix);

                acBlkTblRec.AppendEntity(acPolyline);
                acTrans.AddNewlyCreatedDBObject(acPolyline, true);

                acTrans.Commit();
            }
        }


        public Entity GetPolyline()
        {

            Polyline acPolyline = new Polyline();

            acPolyline.AddVertexAt(0, Point2d.Origin+Vector2d.YAxis*5, 0, 0, 0);
            acPolyline.AddVertexAt(1, Point2d.Origin + Vector2d.XAxis*2, 0, 0, 0);
            acPolyline.AddVertexAt(2, Point2d.Origin + Vector2d.XAxis * -2, 0, 0, 0);

            acPolyline.Closed = true;

            return acPolyline;

        }
    }
}
