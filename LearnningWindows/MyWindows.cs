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
using AcGi = Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Windows;

[assembly: ExtensionApplication(typeof(LearnningWindows.MyWindows))]
[assembly: CommandClass(typeof(LearnningWindows.MyWindows))]
namespace LearnningWindows
{
    public class MyWindows : IExtensionApplication
    {
        internal static PaletteSet panels = new PaletteSet("我的面板");
        internal static MyPanel1 myPanel1 = new MyPanel1();
        internal static MyPanel2 myPanel2 = new MyPanel2();
        public class MyWallDrawRule : AcGi.DrawableOverrule
        {

            public override bool WorldDraw(AcGi.Drawable drawable, AcGi.WorldDraw wd)
            {

                var line = drawable as Line;

                if (line != null)
                {
                    //delta,这条直线所代表的三维向量
                    var vec = line.Delta.RotateBy(Math.PI / 2, Vector3d.ZAxis).GetNormal();
                    var pts = new Point3dCollection()
                    {
                        line.StartPoint+vec,line.EndPoint+vec,
                        line.EndPoint-vec,line.StartPoint-vec,
                    };
                    wd.Geometry.Polygon(pts);

                    var hatch = new Hatch();
                    var pts2d = new Point2dCollection();
                    var bulge = new DoubleCollection();

                    foreach (Point3d pt3d in pts)
                    {
                        pts2d.Add(new Point2d(pt3d.X, pt3d.Y));
                        bulge.Add(0);
                    }

                    hatch.AppendLoop(HatchLoopTypes.Default, pts2d, bulge);
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANGLE");

                    hatch.WorldDraw(wd);

                }

                return base.WorldDraw(drawable, wd);
            }

        }

        [CommandMethod("cmd1")]
        public void Test()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            Editor acEd = acDoc.Editor;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                var blkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                var mdlSpc = acTrans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;


                var myWallRule = new MyWallDrawRule();

                myWallRule.SetExtensionDictionaryEntryFilter("MyWallType");

                Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), myWallRule, false);
                Overrule.Overruling = true;


                var line = new Line(Point3d.Origin, Point3d.Origin + Vector3d.XAxis.RotateBy(Math.PI / 6, Vector3d.ZAxis) * 10);

                mdlSpc.AppendEntity(line);

                if (!line.ExtensionDictionary.IsValid)
                {

                    line.CreateExtensionDictionary();
                    var dict = acTrans.GetObject(line.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
                    dict.SetAt("MyWallType", new DataTable());
                }

                acTrans.AddNewlyCreatedDBObject(line,true);

                acTrans.Commit();

            }
        }
        [CommandMethod("CMD2")]
        public void cmd2()
        {
            panels.Opacity = 70;
            panels.Visible = true;
        }
        [CommandMethod("CMD3")]
        public void cmd3()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            doc.ImpliedSelectionChanged += (s, e) =>
            {

                var res = ed.SelectImplied();

                if (res.Status == PromptStatus.OK && res.Value.Count == 1)
                {
                    myPanel1.UpdatePanel(res.Value.GetObjectIds()[0]);
                }
                else
                {
                    myPanel1.UpdatePanel(ObjectId.Null);
                }
            };


        }
        public void Initialize()
        {
            panels.AddVisual("面板一", myPanel1);
            panels.AddVisual("面板二", myPanel2);
        }

        public void Terminate()
        {
            
        }
    }
}
