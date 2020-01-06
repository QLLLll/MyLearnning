using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Linq;

namespace QiangTuoDong
{
    public class MyDrag
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;
        Polyline pl = null;
        bool IsDrag = false;
        [CommandMethod("MyDrag")]
        public void DragIt()
        {
            IsDrag = false;
            PromptEntityOptions entOpts = new PromptEntityOptions("请选择Polyline");

            entOpts.SetRejectMessage("请选择多段线");
            entOpts.AddAllowedClass(typeof(Polyline), true);

            var pEntRes = ed.GetEntity(entOpts);

            if (pEntRes.Status != PromptStatus.OK)
                return;

            Polyline plCo = null;
            using (var trans = db.TransactionManager.StartTransaction())
            {
                
                pl = trans.GetObject(pEntRes.ObjectId, OpenMode.ForWrite) as Polyline;


                //这里如果不复制，直接操作pl，虽然是以写的方式打开的实体，但是会报错说NotOpenForWrite
                plCo = pl.Clone() as Polyline;

                pl.Erase();

                trans.Commit();

            }

            List<LineSegment2d> listL2d = new List<LineSegment2d>();
            for (int i = 0; i < pl.NumberOfVertices - 1; i++)
            {
                listL2d.Add(pl.GetLineSegment2dAt(i));
            }

            var pointRes = ed.GetPoint(new PromptPointOptions("请输入一地个点:\n"));

            if (pointRes.Status != PromptStatus.OK) return;

            Vector2d v2d = new Vector2d(0, 0);

            JigPromptPointOptions jigOpts = new JigPromptPointOptions();

            MyJig myJig = new MyJig();

            myJig.PromptInput(jigOpts, "拖动鼠标");
            myJig.JigEnts.Add(plCo);

            int dir = -1;

            myJig.SetJigUpdate((jig) =>
            {
                if (jig.JigEnts == null || jig.JigEnts.Count == 0)
                {
                    return;
                }

                Polyline p = jig.JigEnts[0] as Polyline;

                var pt1 = pointRes.Value;

                var pt2 = jig.Point;

                var vec = pt2 - pt1;

                /*获取鼠标拖动方向，主要思路
                 *当拖动的距离拖动前按下的那个点的
                 * 距离>1的时候，计算是X轴方向还是Y轴方向
                 * 因为第一次判断，如果距离过下方向不准确。
                 * 并且这个方向一确定，就不在更改。
                 */
                if (!IsDrag)
                {
                    if (vec.Length > 1)
                    {
                        IsDrag = true;

                        if (Math.Abs(vec.X) > Math.Abs(vec.Y))
                        {
                            dir = 0;

                        }
                        else
                        {
                            dir = 1;
                        }
                    }

                }
                //向右或者向左
                if (dir == 0)
                {

                    v2d = new Vector2d(vec.X, 0);

                }
                else//向上或者向下
                {
                    v2d = new Vector2d(0, vec.Y);

                }

                /*
                 * 确定要拖动的边是选择距离鼠标按下的那个点最近的边
                 */
                double minLength = double.MaxValue;

                int index = -1;

                foreach (var i in Enumerable.Range(0, listL2d.Count))
                {
                    var l = listL2d[i];
                    double dis = l.GetDistanceTo(new Point2d(pointRes.Value.X, pointRes.Value.Y));

                    if (dis < minLength)
                    {

                        minLength = dis;
                        index = i;
                    }

                }

                var l2d = listL2d[index];

                Matrix2d mtx2d = Matrix2d.Displacement(v2d);

                var ptGet1 = l2d.StartPoint;
                var ptGet2 = l2d.EndPoint;

                //实时得到变化的点
                var ptStart = ptGet1.TransformBy(mtx2d);
                var ptEnd = ptGet2.TransformBy(mtx2d);

                var vecGet = ptGet2 - ptGet1;

                //判断鼠标移动的方向和被拖动的边是否是在大致的同一方向
                //如果不是，就允许拖动
                if (dir == 0 && (Math.Abs(vecGet.X) < Math.Abs(vecGet.Y)) ||
                dir == 1 && (Math.Abs(vecGet.X) > Math.Abs(vecGet.Y)))
                {
                    p.SetPointAt(index, ptStart);
                    p.SetPointAt(index + 1, ptEnd);

                    //如果polyline是封闭的，要判断被拖动的点是否是闭合位置上的点，
                    //如果是，要一致更改起点和封闭点
                    if (p.Closed)
                    {
                        if (index == 0)
                        {
                            p.SetPointAt(p.NumberOfVertices - 1, ptStart);
                        }
                        if (index + 1 == 0)
                        {
                            p.SetPointAt(p.NumberOfVertices - 1, ptEnd);
                        }
                        if (index == p.NumberOfVertices - 1)
                        {
                            p.SetPointAt(0, ptStart);
                        }
                        if (index + 1 == p.NumberOfVertices - 1)
                        {
                            p.SetPointAt(0, ptEnd);
                        }
                    }
                }
            });

            if (myJig.Drag() != PromptStatus.OK)
            {

                return;
            }

            IsDrag = false;

            //加入到模型空间
            myJig.JigEnts.ToSpace();
            myJig.JigEnts.ForEach(a => a.Dispose());
        }

    }
}
