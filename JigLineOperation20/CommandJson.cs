using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;
using AcGi = Autodesk.AutoCAD.GraphicsInterface;

[assembly:CommandClass(typeof(JigLineOperation20.CommandJson))]

namespace JigLineOperation20
{
    public class CommandJson
    {

        public static string AppName = "ArxSample";

        [CommandMethod("JsCmd")]
        public void JsCmd()
        {

            var doc = Application.DocumentManager.MdiActiveDocument;

            var basePt = doc.Editor.GetPoint("\n请输入起始点：");

            if (basePt.Status == PromptStatus.OK)
            {
                var jigOpt = new JigPromptPointOptions();

                var line = new Line(basePt.Value, basePt.Value);

                var polyLine = new Polyline(4) { Closed = true };

                var hatch = new Hatch();

                hatch.SetHatchPattern(HatchPatternType.PreDefined, "BOX");

                var wall = new JigHelper();

                wall.PrapareForNextInput(jigOpt, "\n请输入墙顶中心坐标：");

                wall.SetEntities(new Entity[] { line });

                wall.SetUpdate(jig =>
                {
                    line.EndPoint = jig.Point;
                });

                if (wall.Drag() != PromptStatus.OK)
                {
                    return;
                }

                wall.PrapareForNextInput(jigOpt, "\n请输入墙面宽度：");

                wall.SetEntities(new Entity[] { polyLine, hatch });

                wall.SetUpdate(jig =>
                {

                    var edgePt = jig.Point;

                    var centerToEdgeLength = (line.StartPoint - line.EndPoint)
                    .CrossProduct(edgePt - line.EndPoint)
                    .Length / line.Length;

                    var centerToEdge = (line.StartPoint - line.EndPoint)
                    .RotateBy(Math.PI / 2, Vector3d.ZAxis)
                    .GetNormal() * centerToEdgeLength;

                    Point3d pt;

                    pt = line.StartPoint - centerToEdge;
                    polyLine.SetPointAt(0, new Point2d(pt.X, pt.Y));

                    pt = line.StartPoint + centerToEdge;
                    polyLine.SetPointAt(1, new Point2d(pt.X, pt.Y));

                    pt = line.EndPoint + centerToEdge;
                    polyLine.SetPointAt(2, new Point2d(pt.X, pt.Y));

                    pt = line.EndPoint - centerToEdge;
                    polyLine.SetPointAt(3, new Point2d(pt.X, pt.Y));

                    var loop = new HatchLoop(HatchLoopTypes.Polyline);

                    for (int i = 0; i < polyLine.NumberOfVertices; i++)
                    {

                        loop.Polyline.Add(new BulgeVertex(polyLine.GetPoint2dAt(i), polyLine.GetBulgeAt(i)));

                    }

                    if (hatch.NumberOfLoops > 0)
                    {

                        hatch.RemoveLoopAt(0);
                    }
                    hatch.AppendLoop(loop);
                });
                if (wall.Drag() != PromptStatus.OK)
                {
                    return;
                }
            }
        }
        public Tuple<Line, Wall> JigWall(string name, string hatchPattern)
        {

            var doc = Application.DocumentManager.MdiActiveDocument;

            var basePt = doc.Editor.GetPoint("\n请输入起始点：");

            if (basePt.Status == PromptStatus.OK)
            {
                var jigOpt = new JigPromptPointOptions();

                var line = new Line(basePt.Value, basePt.Value);

                var polyLine = new Polyline(4) { Closed = true };

                polyLine.AddVertexAt(0, Point2d.Origin, 0, 0, 0);
                polyLine.AddVertexAt(0, Point2d.Origin, 0, 0, 0);
                polyLine.AddVertexAt(0, Point2d.Origin, 0, 0, 0);
                polyLine.AddVertexAt(0, Point2d.Origin, 0, 0, 0);


                var hatch = new Hatch();

                hatch.SetHatchPattern(HatchPatternType.PreDefined, hatchPattern);

                var wall = new JigHelper();

                wall.PrapareForNextInput(jigOpt, "\n请输入墙顶中心坐标：");

                wall.SetEntities(new Entity[] { line });

                wall.SetUpdate(jig =>
                {
                    line.EndPoint = jig.Point;
                });

                if (wall.Drag() != PromptStatus.OK)
                {
                    return null;
                }

                wall.PrapareForNextInput(jigOpt, "\n请输入墙面宽度：");

                wall.SetEntities(new Entity[] { polyLine, hatch });

                wall.SetUpdate(jig =>
                {

                    var edgePt = jig.Point;

                    var centerToEdgeLength = (line.StartPoint - line.EndPoint)
                    .CrossProduct(edgePt - line.EndPoint)
                    .Length / line.Length;

                    var centerToEdge = (line.StartPoint - line.EndPoint)
                    .RotateBy(Math.PI / 2, Vector3d.ZAxis)
                    .GetNormal() * centerToEdgeLength;

                    Point3d pt;

                    pt = line.StartPoint - centerToEdge;
                    polyLine.SetPointAt(0, new Point2d(pt.X, pt.Y));

                    pt = line.StartPoint + centerToEdge;
                    polyLine.SetPointAt(1, new Point2d(pt.X, pt.Y));

                    pt = line.EndPoint + centerToEdge;
                    polyLine.SetPointAt(2, new Point2d(pt.X, pt.Y));

                    pt = line.EndPoint - centerToEdge;
                    polyLine.SetPointAt(3, new Point2d(pt.X, pt.Y));

                    var loop = new HatchLoop(HatchLoopTypes.Polyline);

                    for (int i = 0; i < polyLine.NumberOfVertices; i++)
                    {

                        loop.Polyline.Add(new BulgeVertex(polyLine.GetPoint2dAt(i), polyLine.GetBulgeAt(i)));

                    }

                    if (hatch.NumberOfLoops > 0)
                    {

                        hatch.RemoveLoopAt(0);
                    }
                    hatch.AppendLoop(loop);
                });
                if (wall.Drag() != PromptStatus.OK)
                {
                    return null;
                }

                return Tuple.Create(line, new Wall
                {
                    Name = name,
                    Pos = new Point { X = line.StartPoint.X, Y = line.StartPoint.Y },
                    Dir = new Point
                    {
                        X = line.EndPoint.X - line.StartPoint.X,
                        Y = line.EndPoint.Y - line.StartPoint.Y,
                    },
                    Size = new Size
                    {
                        W = (polyLine.GetPoint2dAt(0) - polyLine.GetPoint2dAt(1)).Length,
                        H = (polyLine.GetPoint2dAt(1) - polyLine.GetPoint2dAt(2)).Length,
                    },
                    Hatch = hatchPattern,
                });

            }
            return null;
        }

        [CommandMethod("jscmd1")]
        public void JsCmd1()
        {

            var wall1 = JigWall("wall1", "BRICK");

            if (wall1 != null)
            {

                wall1.Item2.Tags = new[] { "concrete", "brick" };

                var id = wall1.Item1.ToSpace();

                var data = new ResultBuffer(
                    new TypedValue((int)DxfCode.Text,
                    wall1.Item2.GetType().ToString()),
                    new TypedValue((int)DxfCode.Text,
                    JsonConvert.SerializeObject(wall1.Item2)));

                DBHelper.SetNODData(AppName, id.ToString(), new Xrecord { Data = data });

                id.AttachXData(AppName, new[]
                {
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString,id.ToString())
                });

                Application.DocumentManager.MdiActiveDocument.Editor.Regen();

            }

        }
        [CommandMethod("jscmd2")]
        public void jscmd2()
        {
            var drawRule = new WallDrawRule();
            drawRule.SetXDataFilter(AppName);
            drawRule.SetCustomFilter();
            Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), drawRule, false);

            var gripRule = new WallGripRule();
            gripRule.SetXDataFilter(AppName);
            gripRule.SetCustomFilter();
            Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), gripRule, false);

            Overrule.Overruling = true;

        }
    }

    public class WallDrawRule : AcGi.DrawableOverrule
    {

        public override bool IsApplicable(RXObject overruledSubject)
        {

            var wall = overruledSubject as Line;

            if (wall != null)
            {

                var data = DBHelper.GetNODData(CommandJson.AppName, wall.Id.ToString())?.Data?.AsArray();

                if (data != null && data.Length == 2
                    && data[0].TypeCode == (int)DxfCode.Text
                    && data[0].Value.ToString() == typeof(Wall).ToString())
                {

                    return true;
                }
            }

            return false;
        }

        public override bool WorldDraw(AcGi.Drawable drawable, AcGi.WorldDraw wd)
        {
            var wall = drawable as Line;

            if (wall != null)
            {

                var data = DBHelper.GetNODData(CommandJson.AppName, wall.Id.ToString())?.Data?.AsArray();

                var wallParam = JsonConvert.DeserializeObject<Wall>(
                    data[1].Value.ToString()
                    );

                var dir = new Vector2d(wallParam.Dir.X, wallParam.Dir.Y)
                    .GetNormal() * wallParam.Size.H;

                var vertDir = dir.RotateBy(Math.PI / 2)
                    .GetNormal() * wallParam.Size.W * 0.5;

                var startPt = new Point2d(wallParam.Pos.X, wallParam.Pos.Y);
                var endPt = startPt + dir;

                var poly = new Polyline(4);

                poly.AddVertexAt(poly.NumberOfVertices, startPt + vertDir, 0, 0, 0);
                poly.AddVertexAt(poly.NumberOfVertices, startPt - vertDir, 0, 0, 0);
                poly.AddVertexAt(poly.NumberOfVertices, endPt - vertDir, 0, 0, 0);
                poly.AddVertexAt(poly.NumberOfVertices, endPt + vertDir, 0, 0, 0);
                poly.Closed = true;

                var hatch = new Hatch();

                hatch.SetHatchPattern(HatchPatternType.PreDefined, wallParam.Hatch);
                hatch.PatternAngle = dir.Angle - Math.PI / 2;

                var loop = new HatchLoop(HatchLoopTypes.Polyline);

                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    loop.Polyline.Add(new BulgeVertex(poly.GetPoint2dAt(i), poly.GetBulgeAt(i)));
                }

                hatch.AppendLoop(loop);

                poly.WorldDraw(wd);
                hatch.WorldDraw(wd);
            }
            //return base.WorldDraw(drawable, wd);
            return true;
        }
    }

    public class WallGripRule : GripOverrule
    {

        public class WallGripData : GripData
        {
            public bool IsStartPoint { get; set; }
        }


        public override bool IsApplicable(RXObject overruledSubject)
        {
            var wall = overruledSubject as Line;

            if (wall != null)
            {

                var data = DBHelper.GetNODData(CommandJson.AppName, wall.Id.ToString())?.Data?.AsArray();

                if (data != null && data.Length == 2
                    && data[0].TypeCode == (int)DxfCode.Text
                    && data[0].Value.ToString() == typeof(Wall).ToString())
                {

                    return true;
                }
            }

            return false;
        }

        public override void GetGripPoints(Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            var wall = entity as Line;

            if (wall != null)
            {

                grips.Add(new WallGripData { GripPoint = wall.StartPoint, IsStartPoint = true });
                grips.Add(new WallGripData { GripPoint = wall.EndPoint, IsStartPoint = false });

            }


        }

        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {

            var wall = entity as Line;

            if (wall != null)
            {

                var xdata = wall.GetXData(CommandJson.AppName);

                var data = DBHelper.GetNODData(CommandJson.AppName, xdata[0].Value.ToString())?.Data?.AsArray();

                var wallPara = JsonConvert.DeserializeObject<Wall>(
                    data[1].Value.ToString());

                foreach (var grip in grips)
                {

                    var wallGrip = grip as WallGripData;

                    if (wallGrip != null)
                    {
                        if (wallGrip.IsStartPoint)
                        {
                            wall.StartPoint = wallGrip.GripPoint + offset;
                        }
                        else
                        {
                            wall.EndPoint = wallGrip.GripPoint + offset;
                        }
                    }

                    wallPara.Pos.X = wall.StartPoint.X;
                    wallPara.Pos.Y = wall.StartPoint.Y;
                    wallPara.Dir.X = wall.EndPoint.X - wall.StartPoint.X;
                    wallPara.Dir.Y = wall.EndPoint.Y - wall.StartPoint.Y;
                    wallPara.Size.H = wall.Length;

                    var newData = new ResultBuffer(
                        new TypedValue((int)DxfCode.Text,
                        wallPara.GetType().ToString()),
                        new TypedValue((int)DxfCode.Text,
                        JsonConvert.SerializeObject(wallPara)
                        ));

                    DBHelper.SetNODData(CommandJson.AppName, wall.Id.ToString(),
                        new Xrecord { Data = newData });

                }

            }

        }

    }
}
