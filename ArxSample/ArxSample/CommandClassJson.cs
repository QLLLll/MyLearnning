using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;

using ArxDotNetLesson;

namespace ArxSample
{
    public partial class CommandClass
    {
        [CommandMethod("jscmd1")]
        public void jscmd1()
        {

        }

        void JsonInitialize()
        {
            foreach (Document doc in Application.DocumentManager)
            {
                using (var docLoc = doc.LockDocument())
                {
                    using (var trans = doc.TransactionManager.StartTransaction())
                    {
                        var nod = trans.GetObject(doc.Database.NamedObjectsDictionaryId,
                            OpenMode.ForRead) as DBDictionary;
                        if (!nod.Contains(AppName))
                        {
                            continue;
                        }
                    }
                }
            }
        }
    }
}
