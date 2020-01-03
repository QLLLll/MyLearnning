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
            PromptEntityOptions entOpts = new PromptEntityOptions("请选择polyline");

            entOpts.SetRejectMessage("请选择多段线");
            entOpts.AddAllowedClass(typeof(Polyline), true);

            var pEntRes = ed.GetEntity(entOpts);

            if (pEntRes.Status != PromptStatus.OK)
                return;
            Polyline plCo = null;
            using (var trans = db.TransactionManager.StartTransaction())
            {

                pl = trans.GetObject(pEntRes.ObjectId, OpenMode.ForWrite) as Polyline;

                plCo = pl.Clone() as Polyline;

                pl.Erase();

                trans.Commit();

            }
            
            List<LineSegment2d> listL2d = new List<LineSegment2d>();
            for (int i = 0; i < pl.NumberOfVertices-1; i++)
            {
             //LineSegment2d l2d=   pl.GetLineSegment2dAt(i);
                listL2d.Add(pl.GetLineSegment2dAt(i));
            }

           

            var pointRes =ed.GetPoint(new PromptPointOptions("请输入一地个点:\n"));

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
                //向右向左
                if (dir==0/*Math.Abs(vec.X) > Math.Abs(vec.Y)*/)
                {

                    v2d = new Vector2d(vec.X, 0);
                    
                }
                else if(dir==1||dir==-1/*Math.Abs(vec.X) <= Math.Abs(vec.Y)*/)
                {
                    v2d = new Vector2d(0, vec.Y);
                    
                }
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




                var ptStart = ptGet1.TransformBy(mtx2d);
                var ptEnd = ptGet2.TransformBy(mtx2d);

                var vecGet = ptGet2 - ptGet1;

                

              
                if (dir==0 &&(Math.Abs(vecGet.X)<Math.Abs(vecGet.Y))||
                dir == 1 && (Math.Abs(vecGet.X) > Math.Abs(vecGet.Y))
                )
                //if(true)
                { 
                p.SetPointAt(index, ptStart);
                p.SetPointAt(index + 1, ptEnd);
                if (index==0)
                {
                    p.SetPointAt(p.NumberOfVertices - 1, ptStart);
                }
                if (index + 1 == 0)
                {
                    p.SetPointAt(p.NumberOfVertices - 1, ptEnd);
                }
                if(index== p.NumberOfVertices - 1)
                {
                    p.SetPointAt(0, ptStart);
                }
                if(index+1 == p.NumberOfVertices - 1)
                {
                    p.SetPointAt(0, ptEnd);
                }
                }
            });

            if (myJig.Drag() != PromptStatus.OK)
            {
                
                return;
            }
            IsDrag = false;
            myJig.JigEnts.ToSpace();
           myJig.JigEnts.ForEach(a=>a.Dispose());
        }

    }
}
