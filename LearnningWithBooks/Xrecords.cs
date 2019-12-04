using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DotNetARX;


namespace LearnningWithBooks
{
  public  class Xrecords
    {
        [CommandMethod("AddXrec")]
        public void AddXrec()
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using(Transaction trans = db.TransactionManager.StartTransaction())
            {

                ed.WriteMessage("\n请选择表示房间、椅子的块和员工姓名文本");

                List<Entity> ents = db.GetSelection();

                var blocks = from ent in ents
                             where ent is BlockReference
                             select ent;

                var room = (from BlockReference b in blocks
                            where b.Name == "RMNUM"
                            select b).FirstOrDefault();

                var chairs = (from BlockReference b in blocks
                              where b.Name == "CHAIR7"
                              select b).Count();

                var txt = (from ent in ents
                           where ent is MText
                           select ent).FirstOrDefault();

                if (room == null && txt == null) return;

                string roomNum = room.ObjectId.GetAttributeInBlockReference("NUMBER");

                string employeeType = chairs > 1 ? "管理人员" : "普通员工";

                TypedValueList values = new TypedValueList();
                values.Add(DxfCode.Text, employeeType);
                values.Add(DxfCode.Text, roomNum);
                txt.ObjectId.AddXrecord("员工", values);
                trans.Commit();

            }


        }

        [CommandMethod("AddDict")]
        public static void AddDict()
        {
            Database db = HostApplicationServices.WorkingDatabase;

            using(Transaction trans = db.TransactionManager.StartTransaction())
            {

                ObjectId id = db.AddNamedDictionary("管理人员");

                if (id.IsNull)
                {
                    Application.ShowAlertDialog("已经有管理人员记录了");
                    return;
                }

                TypedValueList values = new TypedValueList();

                values.Add(DxfCode.Real, 500000.0);
                id.AddXrecord("年薪", values);

                id = db.AddNamedDictionary("普通员工");

                if (id.IsNull)
                {
                    Application.ShowAlertDialog("已经有普通员工记录了");
                    return;
                }

                values.Clear();
                values.Add(DxfCode.Real, 100000.0);
                id.AddXrecord("年薪", values);
                trans.Commit();

            }



        }

        [CommandMethod("ListXrec")]
        public void ListXrec()
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptPointResult pointResult = ed.GetPoint("\n请选择表放置的位置：");

            if (pointResult.Status != PromptStatus.OK) return;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {

                DBDictionary dicts = (DBDictionary)trans.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);

                var mtexts = (from m in db.GetEntsInModelSpace<MText>()
                              let xrecord = m.ObjectId.GetXrecord("员工")
                              where xrecord != null
                              let Position = xrecord[0].Value.ToString()
                              let RoomNumber = xrecord[1].Value.ToString()
                              let Name = m.Text.Replace("\r\n", " ")
                              let Salary = string.Format("{0:c0}", dicts.GetAt(Position).GetXrecord("年薪").First().Value)
                              orderby RoomNumber
                              select new { RoomNumber, Position, Name, Salary }).ToList();

                mtexts.Insert(0, new { RoomNumber = "员工明细表", Position = "", Name = "", Salary = "" });
                mtexts.Insert(1, new { RoomNumber = "房间号", Position = "职位", Name = "姓名", Salary = "年薪" });

                Table tb = new Table();

                tb.TableStyle = db.Tablestyle;

                //tb.NumRows = mtexts.Count();
                //tb.NumColumns = 4;
                tb.SetSize(mtexts.Count(), 4);
                tb.SetRowHeight(3);
                tb.SetColumnWidth(15);
                tb.SetTextHeight(1);

                tb.SetAlignment(CellAlignment.MiddleCenter, RowType.DataRow);

                tb.Position = pointResult.Value;

                for (int i = 0; i < mtexts.Count; i++)
                {

                    var mtext = mtexts[i];

                    tb.SetRowTextString(i, mtext.RoomNumber, mtext.Name, mtext.Position, mtext.Salary);

                }
                tb.GenerateLayout();
                db.AddToModelSpace(tb);
                trans.Commit();

            }

        }

    }
}
