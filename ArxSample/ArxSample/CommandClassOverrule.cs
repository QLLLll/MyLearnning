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
    public partial class CommandClass
    {
        public const string AppName = "ArxSample";

        [CommandMethod("ovcmd1")]
        public void ovcmd1()
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
                //circle1.ToSpace();
                //circle2.ToSpace();
                //hatch.ToSpace();
                circle1.AttachXData(AppName, new[] {
                    new TypedValue((int)DxfCode.ExtendedDataReal, circle2.Radius),
                });
                circle1.ToSpace();
            }
        }

        [CommandMethod("ovcmd2")]
        public void ovcmd2()
        {
            var drawRule = new DonutDrawRule();
            drawRule.SetXDataFilter(AppName);
            Overrule.AddOverrule(RXClass.GetClass(typeof(Circle)), drawRule, false);

            Overrule.Overruling = true;
        }

        [CommandMethod("ovcmd3")]
        public void ovcmd3()
        {
            var gripRule = new DonutGripRule();
            gripRule.SetXDataFilter(AppName);
            Overrule.AddOverrule(RXClass.GetClass(typeof(Circle)), gripRule, false);

            Overrule.Overruling = true;
        }

        [CommandMethod("ovcmd4")]
        public void ovcmd4()
        {
            var transRule = new DonutTransformRule();
            transRule.SetXDataFilter(AppName);
            Overrule.AddOverrule(RXClass.GetClass(typeof(Circle)), transRule, false);

            Overrule.Overruling = true;
        }
    }

    public class DonutDrawRule : AcGi.DrawableOverrule
    {
        public override bool WorldDraw(AcGi.Drawable drawable, AcGi.WorldDraw wd)
        {
            var donut = drawable as Circle;
            if (donut != null)
            {
                var xdata = donut.GetXData(CommandClass.AppName);
                if (xdata != null && xdata.Length > 0)
                {
                    var outRad = (double)xdata[0].Value;
                    var outCircle = new Circle(donut.Center, donut.Normal, outRad);
                    outCircle.WorldDraw(wd);

                    var hatch = new Hatch();
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "BOX");
                    hatch.AppendLoop(HatchLoopTypes.Default, new Curve2dCollection()
                    {
                        new CircularArc2d(new Point2d(donut.Center.X, donut.Center.Y),
                                          donut.Radius),
                    }, new IntegerCollection() { 0 });
                    hatch.AppendLoop(HatchLoopTypes.Default, new Curve2dCollection()
                    {
                        new CircularArc2d(new Point2d(outCircle.Center.X, outCircle.Center.Y),
                                          outCircle.Radius),
                    }, new IntegerCollection() { 0 });
                    hatch.WorldDraw(wd);

                    base.WorldDraw(drawable, wd);
                }
            }

            return false;
        }
    }

    public class DonutGripRule : GripOverrule
    {
        public class DonutOutGripData : GripData
        {

        }

        public class DonutInGripData : GripData
        {

        }

        public override void GetGripPoints(Entity entity,
            GripDataCollection grips, double curViewUnitSize,
            int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            var donut = entity as Circle;
            if (donut != null)
            {
                var xdata = donut.GetXData(CommandClass.AppName);
                if (xdata != null && xdata.Length > 0)
                {
                    var outRad = (double)xdata[0].Value;

                    grips.Add(new DonutOutGripData()
                    {
                        GripPoint = donut.Center + (Vector3d.XAxis * outRad).RotateBy(Math.PI / 4, Vector3d.ZAxis)
                    });
                    grips.Add(new DonutOutGripData()
                    {
                        GripPoint = donut.Center - (Vector3d.XAxis * outRad).RotateBy(Math.PI / 4, Vector3d.ZAxis)
                    });
                    grips.Add(new DonutOutGripData()
                    {
                        GripPoint = donut.Center + (Vector3d.YAxis * outRad).RotateBy(Math.PI / 4, Vector3d.ZAxis)
                    });
                    grips.Add(new DonutOutGripData()
                    {
                        GripPoint = donut.Center - (Vector3d.YAxis * outRad).RotateBy(Math.PI / 4, Vector3d.ZAxis)
                    });
/*
                    grips.Add(new DonutInGripData()
                    {
                        GripPoint = donut.Center + (Vector3d.XAxis * donut.Radius)
                    });
                    grips.Add(new DonutInGripData()
                    {
                        GripPoint = donut.Center - (Vector3d.XAxis * donut.Radius)
                    });
                    grips.Add(new DonutInGripData()
                    {
                        GripPoint = donut.Center + (Vector3d.YAxis * donut.Radius)
                    });
                    grips.Add(new DonutInGripData()
                    {
                        GripPoint = donut.Center - (Vector3d.YAxis * donut.Radius)
                    });*/
                }
            }

            base.GetGripPoints(entity, grips, curViewUnitSize, gripSize,
               curViewDir, bitFlags);
        }

        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips,
            Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            var donut = entity as Circle;
            if (donut != null)
            {
                var xdata = donut.GetXData(CommandClass.AppName);
                if (xdata != null && xdata.Length > 0)
                {
                    foreach (var grip in grips)
                    {
                        var outGrip = grip as DonutOutGripData;
                        if (outGrip != null)
                        {
                            donut.SetXData(CommandClass.AppName, 0,
                                new TypedValue((int)DxfCode.ExtendedDataReal,
                                    (outGrip.GripPoint + offset - donut.Center).Length));
                        }

                        var inGrip = grip as DonutInGripData;
                        if (inGrip != null)
                        {
                            donut.Radius = (inGrip.GripPoint + offset - donut.Center).Length;
                            //donut.SetXData(CommandClass.AppName, 0,
                            //    new TypedValue((int)DxfCode.ExtendedDataReal,
                            //        (outGrip.GripPoint + offset - donut.Center).Length));
                        }
                    }
                }
            }

            if (grips.OfType<DonutOutGripData>().Count() < grips.Count)
            {
                base.MoveGripPointsAt(entity, grips, offset, bitFlags);
            }
        }
    }

    public class DonutTransformRule : TransformOverrule
    {
        public override void TransformBy(Entity entity, Matrix3d transform)
        {
            var donut = entity as Circle;
            if (donut != null)
            {
                var xdata = donut.GetXData(CommandClass.AppName);
                if (xdata != null && xdata.Length > 0)
                {
                    var newCenter = donut.Center.TransformBy(transform);
                    donut.Center = new Point3d(
                        Math.Max(0, newCenter.X), Math.Max(0, newCenter.Y), 0);
                }
            }
        }
    }
}
