using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace LineResearch
{
    public class LineResearch
    {
        [CommandMethod("GetLine")]
        public void GetLine()
        {

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;
            ed.WriteMessage("请选择PolyLine\n");

            var selectRes = ed.GetSelection(new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "POLYLINE") }));

            if (selectRes.Status == PromptStatus.OK)
            {

                var selectSet = selectRes.Value;

                List<Polyline3d> listPl = new List<Polyline3d>();

                List<Polyline3d> listPlold= MyForeach(selectSet);

                foreach (var pl3d in listPlold)
                {

                    if (pl3d != null)
                    {

                        Point3dCollection p3dcoll = new Point3dCollection();
                        //System.Collections.IEnumerator enumerator = pl3d.GetEnumerator();

                        //for (int i = 0; i < pl3d.Length; i++)
                        //{

                        //    if (i % 4 == 0)
                        //    {
                        //        enumerator.MoveNext();
                        //        object o = enumerator.Current;

                        //        Point3d p =(Point3d)o;

                        //        p3dcoll.Add(p);
                        //    }
                        //    else
                        //    {
                        //        enumerator.MoveNext();
                        //    }
                        //    p3dcoll.Add(pl3d.EndPoint);
                        //}
                        using (var trans = db.TransactionManager.StartTransaction())
                        {
                            
                            int m = 0;
                            foreach (ObjectId objId in pl3d)
                            {
                                if (m % 4 == 0)
                                {

                                    var vertex3d = trans.GetObject(objId, OpenMode.ForRead) as PolylineVertex3d;

                                    p3dcoll.Add(vertex3d.Position);
                                }
                                m++;

                            }
                        }
                       
                      
                        Polyline3d p3dNew = new Polyline3d(pl3d.PolyType, p3dcoll, pl3d.Closed);
                        //p3dNew.EndPoint = pl3d.EndPoint;
                        listPl.Add(p3dNew);
                    }

                }
            

                var newDoc = Application.DocumentManager.Add("");
                using (var lock1 = newDoc.LockDocument())
                {
                    var newDb = newDoc.Database;

                    listPl.ToSpace(newDb);
                }

            }

        }
        public List<Polyline3d> MyForeach(SelectionSet selected,
                   Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            List<Polyline3d> list = new List<Polyline3d>();
            using (var trans = db.TransactionManager.StartTransaction())
            {
                foreach (var id in selected.GetObjectIds())
                {
                    var ent = trans.GetObject(id, OpenMode.ForRead) as Polyline3d;
                    list.Add(ent);
                }

                trans.Commit();
            }

            return list;
        }
    }
}

