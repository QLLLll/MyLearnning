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

namespace JigLineOperation20
{
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
