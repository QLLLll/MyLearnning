using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
[assembly: CommandClass(typeof(显示规则重定义.Helper))]
[assembly: ExtensionApplication(typeof(显示规则重定义.TlsApplication))]
namespace 显示规则重定义
{
    class TlsApplication : IExtensionApplication
    {
        //初始例程,重定义生效
        void IExtensionApplication.Initialize()
        {
            Helper.OverruleStart();
            Overrule.Overruling = true;
        }
        //终止例程,重定义失效
        void IExtensionApplication.Terminate()
        {
            Helper.OverruleEnd();
            Overrule.Overruling = false;
        }
    }
    //静态类,存放常用的参数
    static class Helper
    {
        //XData的应用程序注册名
        public readonly static string RegAppName = "TlsCad.Arrow";
        //箭头长度,暂时无法更改,可扩展功能
        public static double ArrowLength = 5;
        //重定义生效
        //注意这里的实体类型和Overrule子类中必须严格对应
        public static void OverruleStart()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), LArrowDrawOverrule.TheOverrule, false);
        }
        //重定义失效
        public static void OverruleEnd()
        {
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(Line)), LArrowDrawOverrule.TheOverrule);
        }
        //让特定的实体附着XData,以便重定义重载可以过滤到该实体
        public static void SetTo(Line line)
        {
            ResultBuffer rb =
                new ResultBuffer(
                    new TypedValue[] {
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                        new TypedValue((int)DxfCode.ExtendedDataReal, ArrowLength) });
            line.XData = rb;
        }
        /*Overrule的实现机理
         * 1、继承相应的Overrule基类,并在子类中定义规则(即函数重载),并设置过滤条件
         * 2、对你想重定义的实体按过滤条件附着数据
         * 3、AutoCad在遇到这类特殊实体时按子类重载的函数去操控它
         */
        [CommandMethod("larr1")]
        public static void LArrow()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptDoubleOptions optDouble = new PromptDoubleOptions("\n请输入箭头长度:");
            optDouble.AllowNone = true;
            optDouble.AllowZero = false;
            optDouble.DefaultValue = ArrowLength;
            optDouble.UseDefaultValue = true;

            PromptDoubleResult resDouble = ed.GetDouble(optDouble);

            if (resDouble.Status != PromptStatus.OK) return;

            ArrowLength = resDouble.Value;

            PromptPointResult res1 = ed.GetPoint("\n请输入起点:");

            if (res1.Status == PromptStatus.OK)
            {
                PromptPointOptions opts = new PromptPointOptions("\n请输入终点:");
                //实现类似jig的操作
                opts.BasePoint = res1.Value;
                opts.UseBasePoint = true;

                PromptPointResult res2 = ed.GetPoint(opts);
                if (res2.Status == PromptStatus.OK)
                {
                    Database db = doc.Database;
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {

                        BlockTableRecord btr =
                            (BlockTableRecord)tr.GetObject(
                                db.CurrentSpaceId,
                                OpenMode.ForWrite,
                                false);
                        Line line = new Line(res1.Value, res2.Value);
                        btr.AppendEntity(line);
                        tr.AddNewlyCreatedDBObject(line, true);
                        RegAppTable rat =
                          (RegAppTable)tr.GetObject(
                              db.RegAppTableId,
                              OpenMode.ForRead,
                              false);
                        if (!rat.Has(RegAppName))
                        {
                            rat.UpgradeOpen();
                            RegAppTableRecord regapp = new RegAppTableRecord();
                            regapp.Name = RegAppName;
                            rat.Add(regapp);
                            tr.AddNewlyCreatedDBObject(regapp, true);
                        }
                        SetTo(line);
                        tr.Commit();
                    }
                }
            }
        }
    }
    //显示重定义
    public class LArrowDrawOverrule : DrawableOverrule
    {
        public static LArrowDrawOverrule TheOverrule = new LArrowDrawOverrule();
        //设置重定义的过滤条件
        public LArrowDrawOverrule()
        {
            SetXDataFilter(Helper.RegAppName);
        }
        //显示重载
        public override bool WorldDraw(Drawable drawable, WorldDraw wd)
        {
            Line line = (Line)drawable;
            Vector3d vec;
            Point3d pnt;

            double arrowLen = (double)line.XData.AsArray()[1].Value;

            if (line.Length == 0)
            {
                vec = Vector3d.XAxis;
                pnt = line.StartPoint;
            }
            else
            {
                double cenpar = (line.StartParam + line.EndParam) / 2;
                vec = line.GetFirstDerivative(cenpar).GetNormal();
                pnt = line.GetPointAtParameter(cenpar);
            }
            Vector3d v1 = vec.TransformBy(Matrix3d.Rotation(Math.PI / 6, Vector3d.ZAxis, Point3d.Origin));
            Vector3d v2 = vec.TransformBy(Matrix3d.Rotation(-Math.PI / 6, Vector3d.ZAxis, Point3d.Origin));
            wd.Geometry.Draw(new Line(pnt, pnt + v1 * arrowLen));
            wd.Geometry.Draw(new Line(pnt, pnt + v2 * arrowLen));
            return base.WorldDraw(drawable, wd);
        }
    }
}

