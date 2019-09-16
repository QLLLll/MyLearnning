using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace HelloWorld
{
    public class HelloWorld
    {
        [CommandMethod("SayHi")]
        public void Sayhello()
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Hi2~");
            Application.ShowAlertDialog("Hi2~");

        }

    }
}
