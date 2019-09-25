using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace ArxDotNetLesson
{
    public static class EntityHelper
    {
        /// <summary>
        /// 创建多边形。
        /// </summary>
        /// <param name="locations">{ x0, y0, x1, y1, ... }</param>
        /// <param name="bulges"></param>
        /// <param name="startWidth"></param>
        /// <param name="endWidth"></param>
        /// <param name="closed"></param>
        /// <returns></returns>
        public static Polyline CreatePolygon(
            double[] locations,
            Tuple<int, double>[] bulges = null,
            Tuple<int, double>[] startWidth = null,
            Tuple<int, double>[] endWidth = null,
            bool closed = true)
        {
            var poly = new Polyline(locations.Length / 2);
            for (var i = 0; i < locations.Length; i += 2)
            {
                poly.AddVertexAt(poly.NumberOfVertices,
                    new Point2d(locations[i], locations[i + 1]),
                    0, 0, 0);
            }

            if (bulges != null)
            {
                foreach (var b in bulges)
                {
                    poly.SetBulgeAt(b.Item1, b.Item2);
                }
            }

            if (startWidth != null)
            {
                foreach (var s in startWidth)
                {
                    poly.SetStartWidthAt(s.Item1, s.Item2);
                }
            }

            if (endWidth != null)
            {
                foreach (var e in endWidth)
                {
                    poly.SetEndWidthAt(e.Item1, e.Item2);
                }
            }

            poly.Closed = closed;

            return poly;
        }

        /// <summary>
        /// 创建矩形。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static Polyline CreateRect(double x, double y, double w, double h)
        {
            return CreatePolygon(new[]
            {
                x, y, x + w, y, x + w, y + h, x, y + h,
            });
        }

        /// <summary>
        /// 创建实心矩形。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static Polyline CreateSolidRect(double x, double y, double w, double h)
        {
            return CreatePolygon(new[]
                {
                    x, y + h * 0.5, x + w, y + h * 0.5,
                },
                startWidth: new[] { Tuple.Create(0, h) },
                endWidth: new[] { Tuple.Create(0, h) });
        }

        /// <summary>
        /// 创建圆形。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Polyline CreateCircle(double x, double y, double r)
        {
            return CreatePolygon(
                new[]
                {
                    x + r, y, x - r, y,
                },
                new[]
                {
                    Tuple.Create(0, Math.Tan(Math.PI / 4)),
                    Tuple.Create(1, Math.Tan(Math.PI / 4)),
                });
        }

        /// <summary>
        /// 创建实心圆。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Polyline CreateSolidCircle(double x, double y, double r)
        {
            return CreatePolygon(
                new[]
                {
                    x + r * 0.5, y, x - r * 0.5, y,
                },
                new[]
                {
                    Tuple.Create(0, Math.Tan(Math.PI / 4)),
                    Tuple.Create(1, Math.Tan(Math.PI / 4)),
                },
                new[]
                {
                    Tuple.Create(0, r),
                    Tuple.Create(1, r),
                },
                new[]
                {
                    Tuple.Create(0, r),
                    Tuple.Create(1, r),
                });
        }

        /// <summary>
        /// 创建填充。
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="loops"></param>
        /// <param name="type"></param>
        /// <param name="scale"></param>
        /// <param name="otherSetting"></param>
        /// <param name="db"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static ObjectId CreateHatch(string pattern, Polyline[] loops,
            HatchPatternType type = HatchPatternType.PreDefined, double scale = 1,
            Action<Hatch> otherSetting = null, Database db = null, string space = null)
        {
            var hatch = new Hatch();
            var id = hatch.ToSpace(db, space);

            using (var trans = id.Database.TransactionManager.StartTransaction())
            {
                hatch = trans.GetObject(id, OpenMode.ForWrite) as Hatch;

                hatch.PatternScale = scale;
                hatch.SetHatchPattern(type, pattern);

                foreach (var loop in loops)
                {
                    var hatchLoop = new HatchLoop(HatchLoopTypes.Polyline);
                    for (var i = 0; i < loop.NumberOfVertices; i++)
                    {
                        hatchLoop.Polyline.Add(
                            new BulgeVertex(loop.GetPoint2dAt(i), loop.GetBulgeAt(i)));
                    }
                    hatch.AppendLoop(hatchLoop);
                }

                otherSetting?.Invoke(hatch);
                trans.Commit();
            }

            return id;
        }

        /// <summary>
        /// 创建填充。
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="loops"></param>
        /// <param name="type"></param>
        /// <param name="scale"></param>
        /// <param name="otherSetting"></param>
        /// <param name="db"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static ObjectId CreateHatch(string pattern, ObjectIdCollection loops,
            HatchPatternType type = HatchPatternType.PreDefined, double scale = 1,
            Action<Hatch> otherSetting = null, string space = null)
        {
            var hatch = new Hatch();
            var id = hatch.ToSpace(loops[0].Database, space);

            using (var trans = id.Database.TransactionManager.StartTransaction())
            {
                hatch = trans.GetObject(id, OpenMode.ForWrite) as Hatch;

                hatch.PatternScale = scale;
                hatch.SetHatchPattern(type, pattern);

                foreach (ObjectId loopId in loops)
                {
                    hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { loopId });
                }

                otherSetting?.Invoke(hatch);
                trans.Commit();
            }

            return id;
        }

        /// <summary>
        /// 创建组。
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="ids"></param>
        /// <param name="setting"></param>
        public static void MakeGroup(string groupName, ObjectIdCollection ids, Action<Group> setting = null)
        {
            using (var trans = ids[0].Database.TransactionManager.StartTransaction())
            {
                var groupDict = trans.GetObject(ids[0].Database.GroupDictionaryId,
                    OpenMode.ForWrite) as DBDictionary;

                Group group;
                if (groupDict.Contains(groupName))
                {
                    group = trans.GetObject(groupDict.GetAt(groupName), OpenMode.ForWrite) as Group;
                }
                else
                {
                    group = new Group();
                    groupDict.SetAt(groupName, group);
                    trans.AddNewlyCreatedDBObject(group, true);
                }

                group.Append(ids);
                setting?.Invoke(group);

                trans.Commit();
            }

            return;
        }

        /// <summary>
        /// 向实体添加注释比例。
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="db"></param>
        public static void Annotationscale(this Entity ent, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            ent.Annotative = AnnotativeStates.True;
            var objCtxMng = db.ObjectContextManager;
            var objCtxCol = objCtxMng.GetContextCollection("ACDB_ANNOTATIONSCALES");
            foreach (var objCtx in objCtxCol)
            {
                ent.AddContext(objCtx);
            }
        }
    }
}
