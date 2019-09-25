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

namespace ArxSample
{
    /// <summary>
    /// TypedValue序列生成辅助工具类。
    /// </summary>
    public static partial class AcTv
    {
        public static IEnumerable<TypedValue> Line
        {
            get { yield return new TypedValue((int)DxfCode.Start, "LINE"); }
        }

        public static IEnumerable<TypedValue> Circle
        {
            get { yield return new TypedValue((int)DxfCode.Start, "CIRCLE"); }
        }

        public static IEnumerable<TypedValue> Polyline
        {
            get { yield return new TypedValue((int)DxfCode.Start, "POLYLINE"); }
        }

        public static IEnumerable<TypedValue> Text
        {
            get { yield return new TypedValue((int)DxfCode.Start, "TEXT"); }
        }

        public static IEnumerable<TypedValue> Or(params IEnumerable<TypedValue>[] tv)
        {
            yield return new TypedValue((int)DxfCode.Operator, "<OR");
            foreach (var te in tv)
            {
                foreach (var t in te)
                {
                    yield return t;
                }
            }
            yield return new TypedValue((int)DxfCode.Operator, "OR>");
        }

        public static IEnumerable<TypedValue> And(params IEnumerable<TypedValue>[] tv)
        {
            yield return new TypedValue((int)DxfCode.Operator, "<AND");
            foreach (var te in tv)
            {
                foreach (var t in te)
                {
                    yield return t;
                }
            }
            yield return new TypedValue((int)DxfCode.Operator, "AND>");
        }

        public static IEnumerable<TypedValue> Layer(string name)
        {
            yield return new TypedValue((int)DxfCode.LayerName, name);
        }

        public static IEnumerable<TypedValue> CircleRadius(double min = 0, double max = double.MaxValue)
        {
            yield return new TypedValue((int)DxfCode.Operator, "<AND");
            yield return new TypedValue((int)DxfCode.Start, "CIRCLE");
            yield return new TypedValue((int)DxfCode.Operator, ">=");
            yield return new TypedValue((int)DxfCode.Real, min);
            yield return new TypedValue((int)DxfCode.Start, "CIRCLE");
            yield return new TypedValue((int)DxfCode.Operator, "<=");
            yield return new TypedValue((int)DxfCode.Real, max);
            yield return new TypedValue((int)DxfCode.Operator, "AND>");
        }
    }

    public static class EidtorHelper
    {
        /// <summary>
        /// 遍历选择集。
        /// </summary>
        /// <param name="selected"></param>
        /// <param name="act"></param>
        /// <param name="db"></param>
        public static void Foreach(this SelectionSet selected, Action<Entity> act,
            Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                foreach (var id in selected.GetObjectIds())
                {
                    var ent = trans.GetObject(id, OpenMode.ForWrite) as Entity;
                    act.Invoke(ent);
                }

                trans.Commit();
            }
        }
    }

    public class JigHelper : DrawJig
    {
        private Func<JigPrompts, SamplerStatus> inputFunc;
        private Action<JigHelper> updateFunc;
        private Entity[] entities;

        public Point3d Point { get; set; }
        public double Distance { get; set; }
        public double Angle { get; set; }

        public void PrapareForNextInput(JigPromptPointOptions opt, string message)
        {
            inputFunc = (prompts) =>
            {
                opt.Message = message;

                var res = prompts.AcquirePoint(opt);
                if (res.Value != Point)
                {
                    Point = res.Value;
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
            };
        }

        public void PrapareForNextInput(JigPromptDistanceOptions opt, string message)
        {
            inputFunc = (prompts) =>
            {
                opt.Message = message;

                var res = prompts.AcquireDistance(opt);
                if (res.Value != Distance)
                {
                    Distance = res.Value;
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
            };
        }

        public void PrapareForNextInput(JigPromptAngleOptions opt, string message)
        {
            inputFunc = (prompts) =>
            {
                opt.Message = message;

                var res = prompts.AcquireAngle(opt);
                if (res.Value != Angle)
                {
                    Angle = res.Value;
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
            };
        }

        public void SetEntities(IEnumerable<Entity> ents)
        {
            entities = ents?.ToArray();
        }

        public void SetUpdate(Action<JigHelper> upd)
        {
            updateFunc = upd;
        }

        public PromptStatus Drag()
        {
            return Application.DocumentManager.MdiActiveDocument.Editor
                .Drag(this).Status;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            return inputFunc.Invoke(prompts);
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (entities != null)
            {
                updateFunc(this);
                foreach (var ent in entities)
                {
                    ent.WorldDraw(draw);
                }
            }
            return true;
        }
    }
}
