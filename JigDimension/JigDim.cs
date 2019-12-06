using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace JigDimension
{
    public class JigDim
    {
        [CommandMethod("myjig")]
        public void MyJigDimension()
        {
            var Doc = Application.DocumentManager.MdiActiveDocument;
            var Ed = Doc.Editor;

            var pointRes = Ed.GetPoint(new PromptPointOptions("请输入一地个点:\n"));

            if (pointRes.Status != PromptStatus.OK) return;

            Line line = new Line() { StartPoint = pointRes.Value };
            AlignedDimension dim = new AlignedDimension() { XLine1Point = line.StartPoint, DimensionStyle = ObjectId.Null };

            JigPromptPointOptions jigOpts = new JigPromptPointOptions();

            jigOpts.BasePoint = pointRes.Value;
            jigOpts.UseBasePoint = true;

            MyJig myJig = new MyJig();

            myJig.PromptInput(jigOpts, "输入第二个点");
            myJig.JigEnts.Add(line);
            myJig.JigEnts.Add(dim);
            myJig.SetJigUpdate((jig) =>
            {
                line.EndPoint = jig.Point;
                dim.XLine2Point = jig.Point;
                dim.DimensionText = line.Length.ToString() ;

                var centerPt = new Point3d((line.StartPoint.X + jig.Point.X) / 2, (line.StartPoint.Y + jig.Point.Y) / 2, (line.StartPoint.Z + jig.Point.Z) / 2);

                dim.DimLinePoint = centerPt+ (jig.Point-line.StartPoint).GetNormal().RotateBy(Math.PI / 2, Vector3d.ZAxis) * 10;
                
            });

            if (myJig.Drag() != PromptStatus.OK)
            {
                return;
            }

            

            myJig.JigEnts.ToSpace();

        }

    }
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

        public void PromptInput(JigPromptPointOptions pointOpts,string msg)
        {
            InputFunc = (prmpts) =>
            {

                pointOpts.Message = msg;

                var res = prmpts.AcquirePoint(pointOpts);

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
