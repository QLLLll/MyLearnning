using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;


namespace MyDatabaseHelp
{
    public class UseDbHelp
    {

        [CommandMethod("cmd1")]
        public void Cmd1()
        {

            var line = new Line(Point3d.Origin - Vector3d.XAxis + Vector3d.YAxis * 2, Point3d.Origin + Vector3d.XAxis + Vector3d.YAxis * 2);


            line.ToSpace();

            var db = Application.DocumentManager.MdiActiveDocument.Database;

            ObjectId styleId = DBHelper.GetSymbol(db.DimStyleTableId, "LL");

            AlignedDimension dimAlign = new AlignedDimension(line.StartPoint, line.EndPoint,Point3d.Origin+Vector3d.YAxis*1.5, null, styleId);

            dimAlign.ToSpace();

        }

    }
}
