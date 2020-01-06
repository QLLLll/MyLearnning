using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QiangTuoDong
{
    public class MyJig : DrawJig
    {


        public Point3d Point = Point3d.Origin;

        Func<JigPrompts, SamplerStatus> InputFunc;

        public List<Entity> JigEnts = new List<Entity>();
        Action<MyJig> JigUpdateAction;

        public MyJig()
        {
            JigEnts.Clear();
            InputFunc = null;
        }

        public void SetJigUpdate(Action<MyJig> action)
        {
            JigUpdateAction = action;
        }

        public void PromptInput(JigPromptPointOptions pointOpts, string msg)
        {
            InputFunc = (prmpts) =>
            {

                pointOpts.Message = msg;

                var res = prmpts.AcquirePoint(pointOpts);
                //Point就是我们要更新实体数据的点
                if (res.Value == Point)
                {
                    return SamplerStatus.NoChange;
                }
                else if (res.Value != Point)
                {
                    Point = res.Value;
                    return SamplerStatus.OK;
                }
                else
                {
                    return SamplerStatus.Cancel;
                }

            };

        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (InputFunc == null)
            {
                return SamplerStatus.NoChange;
            }

            return InputFunc.Invoke(prompts);
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (JigEnts.Count > 0)
            {
                //这是个委托，主要实现你要如何去更新你的实体
                JigUpdateAction(this);
                foreach (var ent in JigEnts)
                {
                    ent.WorldDraw(draw);
                }
            }
            return true;
        }

        public PromptStatus Drag()
        {
            return Application.DocumentManager.MdiActiveDocument.Editor
                .Drag(this).Status;
        }
    }
}
