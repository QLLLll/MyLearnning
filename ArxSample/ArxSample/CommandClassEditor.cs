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
        [CommandMethod("edcmd1")]
        public void edcmd1()
        {
            var editor = Application.DocumentManager.MdiActiveDocument.Editor;

            var tv =
                AcTv.Or(
                    AcTv.And(
                        AcTv.Layer("test1"),
                        AcTv.Line
                    ),
                    AcTv.And(
                        AcTv.Layer("test2"),
                        AcTv.Or(AcTv.Line, AcTv.Circle)
                    ),
                    AcTv.And(
                        AcTv.Layer("test3"),
                        AcTv.CircleRadius(max: 2)
                    )
                ).ToArray();
            editor.WriteMessage($"{string.Join(Environment.NewLine, tv)}");

            var res = editor.GetSelection(new SelectionFilter(tv));
            if (res.Status == PromptStatus.OK)
            {
                res.Value.Foreach(e =>
                {
                    switch (e.Layer)
                    {
                        case "test1":
                            e.ColorIndex = 1;
                            break;
                        case "test2":
                            e.ColorIndex = 2;
                            break;
                        case "test3":
                            e.ColorIndex = 3;
                            break;
                        default:
                            e.ColorIndex = 4;
                            break;
                    }
                });
            }
        }

        [CommandMethod("edcmd2")]
        public void edcmd2()
        {
            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            var res = editor.GetPoint("\n请输入第一个点");
            if (res.Status == PromptStatus.OK)
            {
                MyRectJig.Make(res.Value)?.ToSpace();
            }
        }

        [CommandMethod("edcmd3")]
        public void edcmd3()
        {
            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            editor.PointMonitor += (o, e) =>
            {
                var paths = e.Context.GetPickedEntities();
                if (paths.Length > 0)
                {
                    e.AppendToolTipText(string.Join(Environment.NewLine,
                        paths.SelectMany(p => p.GetObjectIds())
                            .Select(id => id.ObjectClass.Name)));
                }
            };
        }

        [CommandMethod("edcmd4")]
        public void edcmd4()
        {
            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            editor.PointMonitor += (o, e) =>
            {
                var paths = e.Context.GetPickedEntities();
                if (paths.Length > 0)
                {
                    e.AppendToolTipText(string.Join(Environment.NewLine,
                        paths.SelectMany(p => p.GetObjectIds())
                            .Select(id => id.ObjectClass.Name)));
                }
            };
        }

        public class MyRectJig : EntityJig
        {
            public Polyline rect { get { return Entity as Polyline; } }
            public int Step { get; set; }

            public Point3d BasePoint { get; set; }
            public Point3d SecondPoint { get; set; }
            public Point3d FinalPoint { get; set; }

            public MyRectJig(Entity rect, Point3d basePt) : base(rect)
            {
                BasePoint = basePt;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                var jigOpt = new JigPromptPointOptions();

                if (Step == 1)
                {
                    jigOpt.Message = "\n请输入第二个点";
                    var res = prompts.AcquirePoint(jigOpt);
                    if (res.Value != SecondPoint)
                    {
                        SecondPoint = FinalPoint = res.Value;
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
                    jigOpt.Message = "\n请输入第三个点";
                    var res = prompts.AcquirePoint(jigOpt);
                    if (res.Value != FinalPoint)
                    {
                        FinalPoint = res.Value;
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

            protected override bool Update()
            {
                try
                {
                    rect.SetPointAt(0, new Point2d(BasePoint.X, BasePoint.Y));
                    rect.SetPointAt(1, new Point2d(SecondPoint.X, SecondPoint.Y));

                    var baseToSec = SecondPoint - BasePoint;
                    var baseToFin = FinalPoint - BasePoint;

                    var unitVec = baseToSec.RotateBy(Math.PI / 2, Vector3d.ZAxis).GetNormal();

                    var cross = baseToSec.CrossProduct(baseToFin);
                    if (cross.Z < 0)
                    {
                        unitVec = unitVec.Negate();
                    }

                    var vec2d = new Vector2d(unitVec.X, unitVec.Y) * (cross.Length / baseToSec.Length);
                    rect.SetPointAt(2, rect.GetPoint2dAt(1) + vec2d);
                    rect.SetPointAt(3, rect.GetPoint2dAt(0) + vec2d);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            public static Polyline Make(Point3d basePt)
            {
                var editor = Application.DocumentManager.MdiActiveDocument.Editor;
                var jig = new MyRectJig(EntityHelper.CreateRect(
                    basePt.X, basePt.Y, 0, 0), basePt);

                foreach (var step in Enumerable.Range(1, 2))
                {
                    jig.Step = step;
                    if (editor.Drag(jig).Status != PromptStatus.OK)
                    {
                        return null;
                    }
                }

                return jig.rect;
            }
        }
    }
}
