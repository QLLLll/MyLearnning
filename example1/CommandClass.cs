using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcAg = Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace example1
{
    public class CommandClass
    {
        Entity[] MakeGouJian(Param param)
        {
            Point2d[] p2ds = new Point2d[]
                {
                   Point2d.Origin,
                Point2d.Origin+Vector2d.YAxis*param.A,
                Point2d.Origin+Vector2d.YAxis*param.A-Vector2d.XAxis*param.D,
                Point2d.Origin+Vector2d.YAxis*param.B-Vector2d.XAxis*(param.C-param.E),
                Point2d.Origin+Vector2d.YAxis*param.B-Vector2d.XAxis*param.C,
                 Point2d.Origin-Vector2d.XAxis*param.C
                };

            Vector2d vec1 = p2ds[1] - p2ds[2];
            Vector2d vec2 = p2ds[3] - p2ds[2];
            //倒角
            Point2d[] AnglePoint = GetAnglePoint(p2ds[2], vec1, vec2, 0.1);

            Vector2d vec3 = p2ds[0] - p2ds[1];
            Vector2d vec4 = p2ds[2] - p2ds[1];

            Point2d[] AnglePoint0 = GetAnglePoint(p2ds[1], vec3, vec4, 0.1);



            Polyline pline0 = EntityHelper.CreatePolygon(
                    new double[]
                    {
                       p2ds[0].X,p2ds[0].Y,
                       AnglePoint0[1].X,AnglePoint0[1].Y,
                       AnglePoint0[0].X,AnglePoint0[0].Y,
                       p2ds[1].X-param.D*0.5, p2ds[1].Y,
                       AnglePoint[1].X,AnglePoint[1].Y,
                       AnglePoint[0].X,AnglePoint[0].Y,
                       p2ds[3].X,p2ds[3].Y,
                       p2ds[4].X,p2ds[4].Y,
                       p2ds[5].X,p2ds[5].Y,
                    },
                  new Tuple<int, double>[]{
                  Tuple.Create(4,Math.Tan((Math.PI-vec2.GetAngleTo(vec1))/4)),
                  Tuple.Create(1,Math.Tan((Math.PI-vec4.GetAngleTo(vec3))/4))
                  },
                  null,
                  null
                    );

            AlignedDimension alignDim1 = new AlignedDimension(Point3d.Origin, Point3d.Origin + Vector3d.YAxis * param.A,
                Point3d.Origin + Vector3d.XAxis * 0.5, null, ObjectId.Null);

            AlignedDimension alignDim2 = new AlignedDimension(Point3d.Origin + Vector3d.YAxis * param.A,
                Point3d.Origin + Vector3d.YAxis * param.A - Vector3d.XAxis * param.D,
                Point3d.Origin + Vector3d.YAxis * 3.5, null, ObjectId.Null);

            AlignedDimension alignDim3 = new AlignedDimension(Point3d.Origin + Vector3d.YAxis * param.B - Vector3d.XAxis * (param.C - param.E),
                Point3d.Origin + Vector3d.YAxis * param.B - Vector3d.XAxis * param.C, Point3d.Origin + Vector3d.YAxis * (param.B + 0.5),
                null, ObjectId.Null);

            AlignedDimension alignDim4 = new AlignedDimension(Point3d.Origin + Vector3d.YAxis * param.B - Vector3d.XAxis * param.C,
                Point3d.Origin - Vector3d.XAxis * param.C, Point3d.Origin - Vector3d.XAxis * (param.C + 0.5), null, ObjectId.Null);

            AlignedDimension alignDim5 = new AlignedDimension(Point3d.Origin - Vector3d.XAxis * param.C, Point3d.Origin,
                Point3d.Origin - Vector3d.YAxis * 0.5, null, ObjectId.Null);

            LineAngularDimension2 langularDim = new LineAngularDimension2(new Point3d(p2ds[3].X, p2ds[3].Y, 0), new Point3d(p2ds[2].X, p2ds[2].Y, 0),
                new Point3d(p2ds[3].X, p2ds[3].Y, 0), new Point3d(p2ds[4].X, p2ds[4].Y, 0),
                new Point3d(p2ds[3].X - 0.8, p2ds[3].Y + 0.8, 0), null, ObjectId.Null
                );

            Xline xline = new Xline()
            {
                BasePoint = new Point3d(0, param.A * 0.5, 0),
                UnitDir = Vector3d.XAxis
            };

            DBText d1 = new DBText()
            {
                TextString = "A",
                Position = new Point3d(-0.15, param.A * 0.5, 0)
            };

            HatchLoop hLoop = new HatchLoop(HatchLoopTypes.Polyline);

            foreach (var i in Enumerable.Range(0,pline0.NumberOfVertices))
            {
                hLoop.Polyline.Add(new BulgeVertex(pline0.GetPoint2dAt(i), pline0.GetBulgeAt(i)));
            }


            Hatch hatch = new Hatch();

            hatch.SetHatchPattern(HatchPatternType.PreDefined, "BOX");
            hatch.AppendLoop(hLoop);



            return new Entity[]
            {
                pline0,alignDim1,alignDim2,alignDim3,alignDim4,alignDim5,d1,hatch,langularDim,xline
            };

        }
        //倒角
        public Point2d[] GetAnglePoint(Point2d p, Vector2d vec1, Vector2d vec2, double rad)
        {

            Vector2d v1Normal = vec1.GetNormal();
            Vector2d v2Normal = vec2.GetNormal();

            Vector2d v3 = (v1Normal + v2Normal).GetNormal();

            Vector2d v3Center = v3 * rad /
                Math.Sin(Math.Min(v3.GetAngleTo(v1Normal), v3.GetAngleTo(v2Normal)));

            Point2d p1 = p + (v1Normal * v1Normal.DotProduct(v3Center));
            Point2d p2 = p + (v2Normal * v2Normal.DotProduct(v3Center));

            return new List<Point2d> { p1, p2 }.OrderBy(po => po.X).ThenBy(po => po.Y).ToArray();
        }


        [CommandMethod("mycmd1")]
        public void MyCmd()
        {

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            UserDialog uDialog = new UserDialog();
            if (uDialog.ShowDialog() == true)
            {

                Entity[] entites = MakeGouJian(uDialog.DataContext as Param);

                var pointRes = ed.GetPoint("请选择要把构建插入的点");

                if (pointRes.Status == PromptStatus.OK)
                {

                    foreach (Entity entity in entites)
                    {
                        entity.TransformBy(Matrix3d.Displacement(pointRes.Value - Point3d.Origin));

                        var dText = entity as DBText;

                        if (dText != null)
                        {
                            dText.HorizontalMode = TextHorizontalMode.TextMid;
                            dText.VerticalMode = TextVerticalMode.TextVerticalMid;
                            dText.AlignmentPoint = dText.Position;
                        }
                    }

                    var coll = new Point3dCollection();
                    //求交点
                    entites[0].IntersectWith(entites[entites.Length - 1], Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                }
                entites.ToSpace();
            }
        }
    }


}
