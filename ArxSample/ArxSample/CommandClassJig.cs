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
using AcGi = Autodesk.AutoCAD.GraphicsInterface;

using ArxDotNetLesson;

namespace ArxSample
{
    public partial class CommandClassJig
    {
        [CommandMethod("jigcmd1")]
        public void jigcmd1()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            var basePt = doc.Editor.GetPoint("\n请输入圆心");
            if (basePt.Status == PromptStatus.OK)
            {
                var donutJig = new MyDonut(basePt.Value);
                foreach (var step in Enumerable.Range(1, 2))
                {
                    donutJig.Step = step;
                    if (doc.Editor.Drag(donutJig).Status != PromptStatus.OK)
                    {
                        return;
                    }
                }

                donutJig.Circ1.ToSpace();
                donutJig.Circ2.ToSpace();
                donutJig.Hatch.ToSpace();
            }
        }

        public class MyDonut : DrawJig
        {
            public int Step { get; set; }
            public Circle Circ1 { get; } = new Circle();
            public Circle Circ2 { get; } = new Circle();
            public Hatch Hatch { get; } = new Hatch();

            public MyDonut(Point3d center)
            {
                Circ1.Center = Circ2.Center = center;

                Hatch.SetHatchPattern(HatchPatternType.PreDefined, "BOX");
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                var jigOpt = new JigPromptDistanceOptions();
                jigOpt.BasePoint = Circ1.Center;
                jigOpt.UseBasePoint = true;

                if (Step == 1)
                {
                    jigOpt.Message = "\n请确定第一圈";
                    var res = prompts.AcquireDistance(jigOpt);
                    if (res.Value != Circ1.Radius)
                    {
                        if (res.Value == 0)
                        {
                            Circ1.Radius = 0.1;
                        }
                        else
                        {
                            Circ1.Radius = res.Value;
                        }
                    }
                    else
                    {
                        return SamplerStatus.NoChange;
                    }

                    if (res.Status == PromptStatus.OK)
                    {
                        return SamplerStatus.OK;
                    }
                    else
                    {
                        return SamplerStatus.Cancel;
                    }
                }

                if (Step == 2)
                {
                    jigOpt.Message = "\n请确定第二圈";
                    var res = prompts.AcquireDistance(jigOpt);
                    if (res.Value != Circ2.Radius)
                    {
                        if (res.Value == 0)
                        {
                            Circ2.Radius = 0.1;
                        }
                        else
                        {
                            Circ2.Radius = res.Value;
                        }
                    }
                    else
                    {
                        return SamplerStatus.NoChange;
                    }

                    if (res.Status == PromptStatus.OK)
                    {
                        return SamplerStatus.OK;
                    }
                    else
                    {
                        return SamplerStatus.Cancel;
                    }
                }

                return SamplerStatus.Cancel;
            }

            protected override bool WorldDraw(AcGi.WorldDraw draw)
            {
                if (Step == 1)
                {
                    Circ1.WorldDraw(draw);
                    return true;
                }

                if (Step == 2)
                {
                    Circ1.WorldDraw(draw);
                    Circ2.WorldDraw(draw);

                    while (Hatch.NumberOfLoops > 0)
                    {
                        Hatch.RemoveLoopAt(0);
                    }

                    Hatch.AppendLoop(HatchLoopTypes.Default, new Curve2dCollection()
                    {
                        new CircularArc2d(new Point2d(Circ1.Center.X, Circ1.Center.Y),
                                          Circ1.Radius),
                    }, new IntegerCollection() { 0 });
                    Hatch.AppendLoop(HatchLoopTypes.Default, new Curve2dCollection()
                    {
                        new CircularArc2d(new Point2d(Circ2.Center.X, Circ2.Center.Y),
                                          Circ2.Radius),
                    }, new IntegerCollection() { 0 });

                    Hatch.WorldDraw(draw);

                    return true;
                }

                return false;
            }
        }

        [CommandMethod("jigcmd2")]
        public void jigcmd2()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            var basePt = doc.Editor.GetPoint("\n请输入圆心");
            if (basePt.Status == PromptStatus.OK)
            {
                var jigOpt = new JigPromptDistanceOptions();
                jigOpt.BasePoint = basePt.Value;
                jigOpt.UseBasePoint = true;

                var circle1 = new Circle() { Center = basePt.Value };
                var circle2 = new Circle() { Center = basePt.Value };
                var hatch = new Hatch();
                hatch.SetHatchPattern(HatchPatternType.PreDefined, "BOX");

                var donut = new JigHelper();

                // Step 1
                donut.PrapareForNextInput(jigOpt, "\n请确定第一圈");
                donut.SetEntities(new Entity[] { circle1 });
                donut.SetUpdate(jig => circle1.Radius = Math.Max(0.1, jig.Distance));
                if (donut.Drag() != PromptStatus.OK)
                {
                    return;
                }

                // Step 2
                donut.PrapareForNextInput(jigOpt, "\n请确定第二圈");
                donut.SetEntities(new Entity[] { circle1, circle2, hatch });
                donut.SetUpdate(jig =>
                {
                    circle2.Radius = Math.Max(0.1, jig.Distance);
                    while (hatch.NumberOfLoops > 0)
                    {
                        hatch.RemoveLoopAt(0);
                    }
                    hatch.AppendLoop(HatchLoopTypes.Default, new Curve2dCollection()
                    {
                        new CircularArc2d(new Point2d(circle1.Center.X, circle1.Center.Y),
                                          circle1.Radius),
                    }, new IntegerCollection() { 0 });
                    hatch.AppendLoop(HatchLoopTypes.Default, new Curve2dCollection()
                    {
                        new CircularArc2d(new Point2d(circle2.Center.X, circle2.Center.Y),
                                          circle2.Radius),
                    }, new IntegerCollection() { 0 });
                });
                if (donut.Drag() != PromptStatus.OK)
                {
                    return;
                }

                // Final
                circle1.ToSpace();
                circle2.ToSpace();
                hatch.ToSpace();
            }
        }

        [CommandMethod("jigcmd3")]
        public void jigcmd3()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            var firstPt = doc.Editor.GetPoint("\n请输入起始坐标点");
            if (firstPt.Status == PromptStatus.OK)
            {
                var poly = new Polyline();
                //var dim = new AlignedDimension();
                //var aDim = new Point3AngularDimension();
                poly.AddVertexAt(poly.NumberOfVertices,
                    new Point2d(firstPt.Value.X, firstPt.Value.Y), 0, 0, 0);

                var jigOpt = new JigPromptPointOptions();
                var polyJig = new JigHelper();
                polyJig.SetEntities(new Entity[] { poly });

                for (var i = 1; i < int.MaxValue; i++)
                {
                    polyJig.PrapareForNextInput(jigOpt, "\n请输入下一个点");
                    polyJig.SetUpdate(jig =>
                    {
                        if (poly.NumberOfVertices == i)
                        {
                            poly.AddVertexAt(poly.NumberOfVertices,
                                new Point2d(jig.Point.X, jig.Point.Y), 0, 0, 0);
                        }
                        else
                        {
                            poly.SetPointAt(i, new Point2d(jig.Point.X, jig.Point.Y));
                        }
                    });

                    var stat = polyJig.Drag();
                    if (stat == PromptStatus.Cancel)
                    {
                        break;
                    }
                    else if (stat != PromptStatus.OK)
                    {
                        return;
                    }
                }

                poly.ToSpace();
            }
        }
    }
}
