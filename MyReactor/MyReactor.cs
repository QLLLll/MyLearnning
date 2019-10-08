using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace MyReactor
{
    public class MyReactor
    {

        [CommandMethod("MyTest")]
        public void Test()
        {
            Line line = new Line();

            line.StartPoint = Point3d.Origin;
            line.EndPoint = Point3d.Origin + Vector3d.XAxis * 2 + Vector3d.YAxis * 2;

            var db = Application.DocumentManager.MdiActiveDocument.Database;
            //BlockTableRecord blk = new BlockTableRecord();

            //blk.Name = "MyLine";

            //blk.AppendEntity(line);



            AttributeDefinition def = new AttributeDefinition(new Point3d(1, 1, 0), "2", "MyAttr", "The End", ObjectId.Null);


            var defId = DBHelper.ToBlockDefinition(new List<Entity>() { line, def }, "MyLine");

            db.ObjectModified += Db_ObjectModified;

        }

        private void Db_ObjectModified(object sender, ObjectEventArgs e)
        {
            AttributeReference def = e.DBObject as AttributeReference;
            Database db = sender as Database;
            if (def != null)
            {
                string value = def.TextString;

                using (var trans = db.TransactionManager?.StartOpenCloseTransaction())
                {

                    var blktbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    var tblRec = trans.GetObject(blktbl["MyLine"], OpenMode.ForRead) as BlockTableRecord;

                    Line line2 = new Line();

                    line2.StartPoint = Point3d.Origin;
                    line2.EndPoint = Point3d.Origin + Vector3d.XAxis * 2 + Vector3d.YAxis * 2;



                    var tblRec2 = trans.GetObject(blktbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    tblRec2.AppendEntity(line2);

                    trans.AddNewlyCreatedDBObject(line2, true);


                    /*foreach (ObjectId id in tblRec)
                    {

                        Line line = trans.GetObject(id, OpenMode.ForRead) as Line;

                        if (line != null)
                        {



                            Line line22 = new Line();

                            line2.StartPoint = Point3d.Origin;
                            line2.EndPoint = Point3d.Origin + Vector3d.XAxis * 2 + Vector3d.YAxis * 2;



                            var tblRec2 = trans.GetObject(blktbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                            tblRec2.AppendEntity(line2);

                            trans.AddNewlyCreatedDBObject(line2, true);

                            break;
                        }
                        

                    }*/

                    trans.Commit();
                }

            }


        }
    }
}
