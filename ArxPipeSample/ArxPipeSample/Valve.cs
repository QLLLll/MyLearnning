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
using Autodesk.AutoCAD.Windows;
using Autodesk.Windows;

using ArxDotNetLesson;

namespace ArxPipeSample
{
    public static class Valve
    {
        /// <summary>
        /// 阀门类型1块名。
        /// </summary>
        public const string kValve1BlkName = "valvetype1";

        /// <summary>
        /// 阀门尺寸。
        /// </summary>
        public const double kValve1Length = 15;
        public const double kValve1Width = 8;

        /// <summary>
        /// 创建阀门块类型1。
        /// </summary>
        private static void DefValveType1()
        {
            new[] { EntityHelper.CreatePolygon(new[] {
                kValve1Length * 0.5, kValve1Width * 0.5,
                kValve1Length * 0.5, -kValve1Width * 0.5,
                -kValve1Length * 0.5, kValve1Width * 0.5,
                -kValve1Length * 0.5, -kValve1Width * 0.5,
            }) }.ToBlockDefinition(kValve1BlkName);
        }

        /// <summary>
        /// 测试阀门是否在管道上。
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="insPt"></param>
        /// <returns></returns>
        private static bool IsValveType1OnPipe(Entity ent, Point3d insPt)
        {
            var poly = ent as Polyline;
            if (poly != null)
            {
                // 测试多段线是否为管道。
                var xdata = ent.GetXData(PipeSample.kAppName);
                if (xdata == null || xdata.Length == 0)
                {
                    return false;
                }

                // 对多段线求交点。
                var iptCol = new Point3dCollection();

                Circle cir = new Circle();

                cir.Radius = kValve1Length * 0.5;

                cir.Center = insPt;

                cir.ToSpace();

                ent.IntersectWith(cir,
                            Intersect.OnBothOperands, iptCol, IntPtr.Zero, IntPtr.Zero);

                // 交点必须有两个。
                // ！这里并未处理有2个以上交点的情况，处理起来比较复杂。
                 if (iptCol.Count != 2)
                 {
                    return false;
                }

                var pt1 = iptCol[0];
                var pt2 = iptCol[1];

                // 交点必须满足与圆心共线。
                if (Math.Round(Math.Abs((pt1 - insPt).GetNormal()
                    .DotProduct((pt1 - pt2).GetNormal())), 4) != 1)
                {
                    return false;
                }

                // 交点和圆心必须都在同一条线段上。
                for (var i = 0; i < poly.NumberOfVertices - 1; i++)
                {
                    if (poly.OnSegmentAt(i, new Point2d(insPt.X, insPt.Y), 0) &&
                        poly.OnSegmentAt(i, new Point2d(pt1.X, pt1.Y), 0) &&
                        poly.OnSegmentAt(i, new Point2d(pt2.X, pt2.Y), 0))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
         }

        /// <summary>
        /// 使用阀门1打断管道。
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="insPt"></param>
        private static void BreakPipeWithValveType1(Polyline poly, Point3d insPt)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            for (var i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                if (poly.OnSegmentAt(i, new Point2d(insPt.X, insPt.Y), 0))
                {
                    var lineSeg = poly.GetLineSegmentAt(i);

                    var angle = Vector3d.XAxis.GetAngleTo(lineSeg.Direction);
                    if (Vector3d.XAxis.CrossProduct(lineSeg.Direction).Z < 0)
                    {
                        angle = -angle;
                    }

                    // 块坐标变换。
                    var transform = Matrix3d.Displacement(insPt.GetAsVector()) *
                        Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);

                    // 将阀门插入到指定位置。
                    var valveId = DBHelper.Insert(DBHelper.GetSymbol(db.BlockTableId, kValve1BlkName), transform);

                    // 插入阀门顶点，隐藏原始线条。
                    var insIdx = i + 1;
                    // 保存原有线条宽度。
                    var sw = poly.GetStartWidthAt(i);
                    var ew = poly.GetEndWidthAt(i);
                    // 求插入顶点。
                    var insPts = new[]
                    {
                        new Point3d(-kValve1Length * 0.5, 0, 0).TransformBy(transform),
                        new Point3d(-kValve1Length * 0.5, -kValve1Width * 0.5, 0).TransformBy(transform),
                        new Point3d(kValve1Length * 0.5, kValve1Width * 0.5, 0).TransformBy(transform),
                        new Point3d(kValve1Length * 0.5, 0, 0).TransformBy(transform),
                    };
                    // 将插入点顺序调整至与线段朝向相反。（插入时按insIdx插入会再次反向）
                    if (lineSeg.Direction.DotProduct(insPts[insPts.Length - 1] - insPts[0]) > 0)
                    {
                        Array.Reverse(insPts);
                    }

                    poly.AddVertexAt(insIdx, new Point2d(insPts[0].X, insPts[0].Y), 0, 0, 0);
                    poly.AddVertexAt(insIdx, new Point2d(insPts[1].X, insPts[1].Y), 0, 0, 0);
                    poly.AddVertexAt(insIdx, new Point2d(insPts[2].X, insPts[2].Y), 0, 0, 0);
                    poly.AddVertexAt(insIdx, new Point2d(insPts[3].X, insPts[3].Y), 0, sw, ew);

                    break;
                }
            }
        }

        /// <summary>
        /// 还原被阀门1打断的管道。
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="insPt"></param>
        private static void ConnectPipeWithValveType1(Polyline poly, Point3d insPt)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            for (var i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                if (poly.OnSegmentAt(i, new Point2d(insPt.X, insPt.Y), 0))
                {
                    poly.RemoveVertexAt(i + 1);
                    poly.RemoveVertexAt(i + 1);
                    poly.RemoveVertexAt(i - 1);
                    poly.RemoveVertexAt(i - 1);
                    break;
                }
            }
        }

        /// <summary>
        /// 当阀门删除时，接上被打断的管道。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void ValveRemoved(object sender, ObjectErasedEventArgs e)
        {
            var valve = e.DBObject as BlockReference;
            if (e.Erased == true && valve != null)
            {
                if (valve.Name == Valve.kValve1BlkName)
                {
                    DBHelper.ForEach(ent =>
                    {
                        var poly = ent as Polyline;
                        if (poly != null)
                        {
                            var xdata = poly.GetXData(PipeSample.kAppName);
                            if (xdata == null || xdata.Length == 0)
                            {
                                return;
                            }
                            ConnectPipeWithValveType1(poly, valve.Position);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 创建阀门类型1。
        /// </summary>
        public static void MakeValveType1()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            if (DBHelper.GetSymbol(db.BlockTableId, kValve1BlkName) == ObjectId.Null)
            {
                DefValveType1();
            }

            var insPt = doc.Editor.GetPoint("\n选择阀门插入点");
            if (insPt.Status == PromptStatus.OK)
            {
                DBHelper.ForEach((ent) =>
                {
                    if (IsValveType1OnPipe(ent, insPt.Value))
                    {
                        var poly = ent as Polyline;
                        BreakPipeWithValveType1(poly, insPt.Value);
                    }
                });
            }
        }
    }
}
