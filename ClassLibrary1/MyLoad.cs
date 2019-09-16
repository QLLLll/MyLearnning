using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace ClassLibrary1
{
    public class MyLoad
    {
        [CommandMethod("NL")]
        public void NetLoad()
        {

            Editor acEd = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = @"D:\C#learnningCode\MyLearnning";
            ofd.Filter = "*.dll|*.dll";

            string fileName = string.Empty;

            if (ofd.ShowDialog() == DialogResult.OK)
            {

                fileName = ofd.FileName;

            }

            acEd.WriteMessage(fileName);

            Assembly.Load(System.IO.File.ReadAllBytes(fileName));

        }

    }
}
