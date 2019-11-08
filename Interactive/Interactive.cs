using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interactive
{
    public class Interactive
    {

        [CommandMethod("T")]
        public void T()
        {

            object obj=Application.GetSystemVariable("DIMBLK");
 
           


        }

        [CommandMethod("AddPoly")]
        public void AddPoly()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            double width = 0; //初始化线宽
            short colorIndex = 0; //初始化颜色索引值
            int index = 2; //初始化多段线顶点数
            ObjectId polyEntId = ObjectId.Null; //声明多段线的ObjectId
            //定义第一个点的用户交互类
            PromptPointOptions optPoint = new PromptPointOptions("\n请输入第一个点<100,200>");
            optPoint.AllowNone = true; //允许用户回车响应
            //返回点的用户提示类
            PromptPointResult resPoint = ed.GetPoint(optPoint);
            //用户按下ESC键，退出
            if (resPoint.Status == PromptStatus.Cancel)
                return;
            Point3d ptStart; //声明第一个输入点
            //用户按回车键
            if (resPoint.Status == PromptStatus.None)
                //得到第一个输入点的默认值
                ptStart = new Point3d(100, 200, 0);
            else
                //得到第一个输入点
                ptStart = resPoint.Value;
            Point3d ptPrevious = ptStart;//保存当前点
            //定义输入下一点的点交互类
            PromptPointOptions optPtKey = new PromptPointOptions("\n请输入下一个点或[线宽(W)/颜色(C)/完成(O)]<O>");
            //为点交互类添加关键字
            optPtKey.Keywords.Add("W");
            optPtKey.Keywords.Add("C");
            optPtKey.Keywords.Add("O");
            optPtKey.Keywords.Default = "O"; //设置默认的关键字
            optPtKey.UseBasePoint = true; //允许使用基准点
            optPtKey.BasePoint = ptPrevious;//设置基准点
            optPtKey.AppendKeywordsToMessage = false;//不将关键字列表添加到提示信息中
            //提示用户输入点
            PromptPointResult resKey = ed.GetPoint(optPtKey);

            Point3d ptNext = Point3d.Origin;
            //如果用户输入点或关键字，则一直循环
            while (resKey.Status == PromptStatus.OK || resKey.Status == PromptStatus.Keyword)
            {
                //声明下一个输入点
                //如果用户输入的是关键字集合对象中的关键字
                if (resKey.Status == PromptStatus.Keyword)
                {
                    switch (resKey.StringResult)
                    {
                        case "W":
                            width = GetWidth();
                            break;
                        case "C":
                            colorIndex = GetColorIndex();
                            break;
                        case "O":
                            return;
                        default:
                            ed.WriteMessage("\n输入了无效关键字");
                            break;
                    }
                }
                else
                {
                    ptNext = resKey.Value;//得到户输入的下一点
                    if (index == 2) //新建多段线
                    {
                        //提取三维点的X、Y坐标值，转化为二维点
                        Point2d pt1 = new Point2d(ptPrevious[0], ptPrevious[1]);
                        Point2d pt2 = new Point2d(ptNext[0], ptNext[1]);
                        Polyline polyEnt = new Polyline();//新建一条多段线
                        //给多段线添加顶点，设置线宽
                        polyEnt.AddVertexAt(0, pt1, 0, width, width);
                        polyEnt.AddVertexAt(1, pt2, 0, width, width);
                        //设置多段线的颜色
                        polyEnt.Color = Color.FromColorIndex(ColorMethod.ByColor, colorIndex);
                        //将多段线添加到图形数据库并返回一个ObjectId(在绘图窗口动态显示多段线)
                        polyEntId = db.AddToModelSpace(polyEnt);
                    }
                    else  //修改多段线，添加最后一个顶点
                    {
                        using (Transaction trans = db.TransactionManager.StartTransaction())
                        {
                            //打开多段线的状态为写
                            Polyline polyEnt = trans.GetObject(polyEntId, OpenMode.ForWrite) as Polyline;
                            if (polyEnt != null)
                            {
                                //继续添加多段线的顶点
                                Point2d ptCurrent = new Point2d(ptNext[0], ptNext[1]);
                                polyEnt.AddVertexAt(index - 1, ptCurrent, 0, width, width);
                                //重新设置多段线的颜色和线宽
                                polyEnt.Color = Color.FromColorIndex(ColorMethod.ByColor, colorIndex);
                                polyEnt.ConstantWidth = width;
                            }
                            trans.Commit();
                        }
                    }
                    index++;
                }
                ptPrevious = ptNext;
                optPtKey.BasePoint = ptPrevious;//重新设置基准点
                resKey = ed.GetPoint(optPtKey); //提示用户输入新的顶点
            }
        }

        // 得到用户输入线宽的函数.
        public double GetWidth()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //定义一个实数的用户交互类.
            PromptDoubleOptions optDou = new PromptDoubleOptions("\n请输入线宽");
            optDou.AllowNegative = false; //不允许输入负数
            optDou.DefaultValue = 0; //设置默认值
            PromptDoubleResult resDou = ed.GetDouble(optDou);
            if (resDou.Status == PromptStatus.OK)
            {
                Double width = resDou.Value;
                return width; //得到用户输入的线宽
            }
            else
                return 0;
        }

        // 得到用户输入颜色索引值的函数.
        public short GetColorIndex()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //定义一个整数的用户交互类
            PromptIntegerOptions optInt = new PromptIntegerOptions("\n请输入颜色索引值(0～256)");
            optInt.DefaultValue = 0; //设置默认值
            //返回一个整数提示类
            PromptIntegerResult resInt = ed.GetInteger(optInt);
            if (resInt.Status == PromptStatus.OK)
            {
                //得到用户输入的颜色索引值
                short colorIndex = (short)resInt.Value;
                if (colorIndex > 256 | colorIndex < 0)
                    return 0;
                else
                    return colorIndex;
            }
            else
                return 0;
        }


        [CommandMethod("TestGetSelect")]
        public static void TestGetSelect()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            //生成三个同心圆并添加到当前模型空间
            Circle cir1 = new Circle(Point3d.Origin, Vector3d.ZAxis, 10);
            Circle cir2 = new Circle(Point3d.Origin, Vector3d.ZAxis, 20);
            Circle cir3 = new Circle(Point3d.Origin, Vector3d.ZAxis, 30);
            db.AddToModelSpace(new Circle[] { cir1, cir2, cir3 });
            //提示用户选择对象
            PromptSelectionResult psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK) return;//如果未选择，则返回
            //获取选择集
            SelectionSet ss = psr.Value;
            //信息提示框，给出选择集中包含实体个数的提示
            Application.ShowAlertDialog("选择集中实体的数量：" + ss.Count.ToString());
        }

        [CommandMethod("MergeSelection")]
        public static void MergeSelection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            //第一次选择
            PromptSelectionResult ss1 = ed.GetSelection();
            if (ss1.Status != PromptStatus.OK) return; //若选择不成功，返回
            Application.ShowAlertDialog("第一个选择集中实体的数量：" + ss1.Value.Count.ToString());
            //第二次选择
            PromptSelectionResult ss2 = ed.GetSelection();
            if (ss2.Status != PromptStatus.OK) return;
            Application.ShowAlertDialog("第二个选择集中实体的数量：" + ss2.Value.Count.ToString());
            //第二个选择集的ObjectId加入到第一个选择集中
            var ss3 = ss1.Value.GetObjectIds().Union(ss2.Value.GetObjectIds());
            Application.ShowAlertDialog("合并后选择集中实体的数量：" + ss3.Count().ToString());
        }

        [CommandMethod("DelFromSelection")]
        public static void DelFromSelection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            //第一次选择
            PromptSelectionResult ss1 = ed.GetSelection();
            if (ss1.Status != PromptStatus.OK) return; //若选择不成功，返回
            Application.ShowAlertDialog("第一个选择集中实体的数量：" + ss1.Value.Count.ToString());
            //第二次选择
            PromptSelectionResult ss2 = ed.GetSelection();
            if (ss2.Status != PromptStatus.OK) return;
            Application.ShowAlertDialog("第二个选择集中实体的数量：" + ss2.Value.Count.ToString());
            //若第二次选择的实体位于第一个选择集中，则删除该实体的ObjectId
            var ss3 = ss1.Value.GetObjectIds().Except(ss2.Value.GetObjectIds());
            Application.ShowAlertDialog("删除第二个选择集后第一个选择集中实体的数量：" + ss3.Count().ToString());
        }

        [CommandMethod("TestPickFirst", CommandFlags.UsePickSet)]
        public static void TestPickFirst()
        {

            object pickFirst = Application.GetSystemVariable("PickFirst");

            if (null != pickFirst && pickFirst.ToString() != "1")
            {
                Application.SetSystemVariable("PickFirst", 1);
            }


            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            //获取当前已选择的实体
            PromptSelectionResult psr = ed.SelectImplied();
            //在命令发出前已有实体被选中
            if (psr.Status == PromptStatus.OK)
            {
                SelectionSet ss1 = psr.Value; //获取选择集
                //显示当前已选择的实体个数
                Application.ShowAlertDialog("PickFirst示例：当前已选择的实体个数：" + ss1.Count.ToString());
                //清空当前选择集
                ed.SetImpliedSelection(new ObjectId[0]);
                psr = ed.GetSelection();//提示用户进行新的选择
                if (psr.Status == PromptStatus.OK)
                {
                    //设置当前已选择的实体
                    ed.SetImpliedSelection(psr.Value.GetObjectIds());
                    SelectionSet ss2 = psr.Value;
                    Application.ShowAlertDialog("PickFirst示例：当前已选择的实体个数：" + ss2.Count.ToString());
                }
            }
            else
            {
                Application.ShowAlertDialog("PickFirst示例：当前已选择的实体个数：0");
            }
            Application.SetSystemVariable("PickFirst", pickFirst);
        }

        [CommandMethod("TestPolygonSelect")]
        public static void TestPolygonSelect()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            //声明一个Point3d类列表对象，用于存储多段线的顶点
            Point3dList pts = new Point3dList();
            //提示用户选择多段线
            PromptEntityResult per = ed.GetEntity("请选择多段线");
            if (per.Status != PromptStatus.OK) return;//选择错误，返回
            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                //转换为Polyline对象
                Polyline pline = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                if (pline != null)
                {
                    //遍历所选多段线的顶点并添加到Point3d类列表
                    for (int i = 0; i < pline.NumberOfVertices; i++)
                    {
                        Point3d point = pline.GetPoint3dAt(i);
                        pts.Add(point);
                    }
                    //窗口选择，仅选择完全位于多边形区域中的对象
                    PromptSelectionResult psr = ed.SelectWindowPolygon(pts);
                    if (psr.Status == PromptStatus.OK)
                    {
                        Application.ShowAlertDialog("选择集中实体的数量：" + psr.Value.Count.ToString());
                    }
                }
                trans.Commit();
            }
        }

        [CommandMethod("TestSelectException")]
        public static void TestSelectException()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Point3d pt1 = Point3d.Origin;
            Point3d pt2 = new Point3d(100, 100, 0);
            //交叉窗口选择，选择由pt1和pt2组成的矩形窗口包围的或相交的对象
            PromptSelectionResult psr = ed.SelectCrossingWindow(pt1, pt2);
            if (psr.Status == PromptStatus.OK)
            {
                Application.ShowAlertDialog("选择集中实体的数量：" + psr.Value.Count.ToString());
            }
        }

        [CommandMethod("TestFilter")]
        public static void TestFilter()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            //创建一个自定义的TypedValue列表对象，用于构建过滤器列表
            TypedValueList values = new TypedValueList();
            //选择图层1上的直线对象
            values.Add(DxfCode.LayerName, "图层1");
            values.Add(typeof(Line));
            //构建过滤器列表，注意这里使用自定义类型转换
            SelectionFilter filter = new SelectionFilter(values);
            //选择图形中所有满足过滤器的对象，即位于图层1上的直线
            PromptSelectionResult psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
            {
                Application.ShowAlertDialog("选择集中实体的数量：" + psr.Value.Count.ToString());
            }
        }
    }
}
