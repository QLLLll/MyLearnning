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

namespace PickSetMy
{
    public class Class1 : IExtensionApplication
    {
        public void Initialize()
        {
            var pick = Application.GetSystemVariable("PICKFIRST");

           

            if (null==pick || !pick.Equals("1"))
            {
                Application.SetSystemVariable("PICKFIRST", "1");
            }

            
        }

        public void Terminate()
        {
            
        }
    }
}
