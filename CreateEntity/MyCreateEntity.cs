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

namespace CreateEntity
{
    public class MyCreateEntity
    {
        [CommandMethod("MakeLine")]
        public void MoveEntity()
        {


            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            Editor acEd = acDoc.Editor;

            

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline acPolyline = new Polyline(2);

                acPolyline.AddVertexAt(acPolyline.NumberOfVertices, Point2d.Origin,  Math.Tan(0.25 * Math.PI), 0.5, 0.5);
                acPolyline.AddVertexAt(acPolyline.NumberOfVertices, Point2d.Origin+Vector2d.XAxis*0.5, Math.Tan(0.25 * Math.PI), 0.5, 0.5);

                acPolyline.Closed = true;


                acBlkTblRec.AppendEntity(acPolyline);
                acTrans.AddNewlyCreatedDBObject(acPolyline, true);

                /*Ellipse acElli = new Ellipse(Point3d.Origin, Vector3d.ZAxis, Vector3d.XAxis * 2, 0.5, 0, 0);

                acBlkTblRec.AppendEntity(acElli);
                acTrans.AddNewlyCreatedDBObject(acElli, true);*/

                 acTrans.Commit();
            }
        }


    }
}
