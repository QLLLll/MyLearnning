using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcdPrint
{
    class DBhelp
    {

        /// <summary> 
        /// /// 选择所有对象 
        /// </summary> 
        /// <returns></returns> 
        public static DBObjectCollection All()
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Entity entity = null;
            DBObjectCollection EntityCollection = new DBObjectCollection();
            PromptSelectionResult ents = ed.SelectAll();
            if (ents.Status == PromptStatus.OK)
            {
                using (Transaction transaction = db.TransactionManager.StartTransaction())
                {
                    SelectionSet ss = ents.Value;
                    foreach (ObjectId id in ss.GetObjectIds())
                    {
                        entity = transaction.GetObject(id, OpenMode.ForWrite, true) as Entity;
                        if (entity != null)
                         EntityCollection.Add(entity);
                    }
              

                transaction.Commit();
             }
            }
             return EntityCollection;
        }

}}

