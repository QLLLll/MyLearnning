using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(JigOperation.JigOperation))]
[assembly: ExtensionApplication(typeof(JigOperation.JigOperation))]

namespace JigOperation
{
    public class JigOperation : IExtensionApplication
    {
        [CommandMethod("cmd1")]
        public void Cmd1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;

            var bsPointRes = ed.GetPoint(new PromptPointOptions("\n请输入圆心"));

            if (bsPointRes.Status == PromptStatus.OK)
            {

                var jigdistanceOpts = new JigPromptDistanceOptions();

                jigdistanceOpts.BasePoint = bsPointRes.Value;

                jigdistanceOpts.UseBasePoint = true;

                var circle1 = new Circle() { Center = jigdistanceOpts.BasePoint };
                var circle2 = new Circle() { Center = jigdistanceOpts.BasePoint };
                var hatch = new Hatch();

                hatch.SetHatchPattern(HatchPatternType.PreDefined, "BOX");
                hatch.PatternScale = 0.01;

                var donut = new JigHelper();

                donut.PrapareForNextInput(jigdistanceOpts, "\n请输入第一圈");
                donut.SetEntities(new Entity[] { circle1 });
                donut.SetUpdate(jig => { circle1.Radius = Math.Max(0.1, jig.Distance); });
                if (donut.Drag() != PromptStatus.OK)
                {
                    return;
                }


                donut.PrapareForNextInput(jigdistanceOpts, "\n请输入第二圈");
                donut.SetEntities(new Entity[] { circle1, circle2, hatch });
                donut.SetUpdate(jig => { circle2.Radius = Math.Max(0.1, jig.Distance); });

                if (donut.Drag() != PromptStatus.OK)
                {
                    return;
                }

                hatch.AppendLoop(HatchLoopTypes.Default, new Curve2dCollection()
                { new CircularArc2d(new Point2d(circle1.Center.X,circle1.Center.Y),circle1.Radius)},
                new IntegerCollection() { 0 });

                hatch.AppendLoop(HatchLoopTypes.Default, new Curve2dCollection()
                { new CircularArc2d(new Point2d(circle2.Center.X,circle2.Center.Y),circle2.Radius)},
                new IntegerCollection() { 0 });

                if (donut.Drag() != PromptStatus.OK)
                {
                    return;
                }

                circle1.ToSpace();
                circle2.ToSpace();
                hatch.ToSpace();
            }


        }

        [CommandMethod("cmd2")]
        public void Cmd2()
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;

            Autodesk.AutoCAD.DatabaseServices.Polyline pyLine = new Autodesk.AutoCAD.DatabaseServices.Polyline();

            var bsPointRes = ed.GetPoint(new PromptPointOptions("\n请输入圆心"));

            if (bsPointRes.Status == PromptStatus.OK)
            {

                pyLine.AddVertexAt(pyLine.NumberOfVertices, new Point2d(bsPointRes.Value.X, bsPointRes.Value.Y), 0, 0, 0);
                var jigPtOpts = new JigPromptPointOptions();

                var dimAlign = new AlignedDimension();

                var donut = new JigHelper();

                donut.SetEntities(new Entity[] { pyLine, dimAlign });

                for (int i = 1; i < int.MaxValue; i++)
                {

                    donut.PrapareForNextInput(jigPtOpts, "\n请输入下一个点");


                    donut.SetUpdate(jig =>
                    {

                        if (pyLine.NumberOfVertices == i)
                        {
                            pyLine.AddVertexAt(pyLine.NumberOfVertices, new Point2d(jig.Point.X, jig.Point.Y), 0, 0, 0);
                        }
                        else
                        {
                            pyLine.SetPointAt(i, new Point2d(jig.Point.X, jig.Point.Y));
                        }

                        Point3d pt1 = pyLine.GetPoint3dAt(i - 1);
                        Point3d pt2 = pyLine.GetPoint3dAt(i);

                        var vec3d = pt1 - pt2;
                        var vec3d2 = vec3d.RotateBy(Math.PI / 2, Vector3d.ZAxis);



                        Point3d cPoint3d = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
                        //dimAlign.DynamicDimension = true;

                        dimAlign.XLine1Point = pt1;
                        dimAlign.XLine2Point = pt2;



                        dimAlign.DimLinePoint = cPoint3d + vec3d2.GetNormal() * 0.5;

                        dimAlign.DimensionText = null;
                        dimAlign.DimensionStyle = ObjectId.Null;
                        //dimAlign.DimensionStyle = DBHelper.GetSymbol(doc.Database.DimStyleTableId, "Standard");

                    });
                    dimAlign.ToSpace();

                    dimAlign = new AlignedDimension();

                    var status = donut.Drag();
                    if (status == PromptStatus.Cancel)
                    {
                        break;
                    }
                    else if (status != PromptStatus.OK)
                    {
                        return;
                    }

                }

                pyLine.ToSpace();

            }


        }

        public void Initialize()
        {

        }

        public void Terminate()
        {

        }
    }
}
