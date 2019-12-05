using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
[assembly: CommandClass(typeof(TlsCad.LArrow))]
[assembly: ExtensionApplication(typeof(TlsCad.TlsApplication))]
namespace TlsCad
{
    class TlsApplication : IExtensionApplication
    {
        void IExtensionApplication.Initialize()
        {
            LArrow.OverruleStart();
            Overrule.Overruling = true;
        }
        void IExtensionApplication.Terminate()
        {
            LArrow.OverruleEnd();
            Overrule.Overruling = false;
        }
    }
   public class LArrow
    {
        //默认箭头长度
        public static double CurrArrowLength = 5;
        //XData的应用程序注册名
        public readonly static string RegAppName = "TlsCad.Line.Arrow";
        private readonly static Matrix3d _matLeft =
            Matrix3d.Rotation(Math.PI / 6, Vector3d.ZAxis, Point3d.Origin);
        private readonly static Matrix3d _matRight =
            Matrix3d.Rotation(-Math.PI / 6, Vector3d.ZAxis, Point3d.Origin);
        private readonly static TypedValue _tvHead =
            new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName);
        private Line _line;
        private double _arrowLength;
        private double _scale;
        //构造函数,获取直线实体附着的数据
        public LArrow(Line line)
        {
            _line = line;
            ResultBuffer rb = _line.GetXDataForApplication(RegAppName);
            if (rb != null)
            {
                TypedValue[] vals = rb.AsArray();
                _arrowLength = (double)vals[1].Value;
                _scale = (double)vals[2].Value;
            }
        }
        public LArrow(Drawable d)
            : this((Line)d)
        { }
        public LArrow(Entity ent)
            : this((Line)ent)
        { }
        //重定义生效
        //注意这里的实体类型和Overrule子类中必须严格对应
        public static void OverruleStart()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), LArrowDrawOverrule.TheOverrule, false);
            Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), LArrowGripOverrule.TheOverrule, false);
        }
        //重定义失效
        public static void OverruleEnd()
        {
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(Line)), LArrowDrawOverrule.TheOverrule);
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(Line)), LArrowGripOverrule.TheOverrule);
        }
        //绘制箭头
        public void WorldDraw(WorldDraw wd)
        {
            if (_arrowLength > 0)
            {
                Vector3d vec;
                Point3d pnt;
                if (_line.Length == 0)
                {
                    vec = Vector3d.XAxis;
                    pnt = _line.StartPoint;
                }
                else
                {
                    vec = (_line.EndPoint - _line.StartPoint).GetNormal();
                    pnt = _line.GetPointAtParameter(_scale * _line.EndParam);
                }
                Vector3d v1 = vec.TransformBy(_matLeft);
                Vector3d v2 = vec.TransformBy(_matRight);
                //按实体附着的数据显示箭头的实际长度
                wd.Geometry.WorldLine(pnt, pnt + v1 * _arrowLength);
                wd.Geometry.WorldLine(pnt, pnt + v2 * _arrowLength);
            }
        }
        //保存扩展数据
        public void SaveExtendedData()
        {
            ResultBuffer rb = new ResultBuffer();
            rb.Add(_tvHead);
            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, _arrowLength));
            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, _scale));
            _line.XData = rb;
        }
        public Line Line
        {
            get { return _line; }
        }
        public double Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }
        public Point3d Position
        {
            get
            {
                return _line.GetPointAtParameter(_scale * _line.EndParam);
            }
            set
            {
                _scale = _line.GetParameterAtPoint(_line.GetClosestPointTo(value, false)) / _line.EndParam;
            }
        }
        public double ArrowLength
        {
            get { return _arrowLength; }
            set { _arrowLength = value; }
        }
        /*
         * Overrule的实现机理
         * 1、继承相应的Overrule基类,并在子类中定义规则(即函数重载),并设置过滤条件
         * 2、对你想重定义的实体按过滤条件附着数据
         * 3、AutoCad在遇到这类特殊实体时按子类重载的函数去操控它
         */
        [CommandMethod("larr3")]
        public static void DoIt()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptDoubleOptions optDouble = new PromptDoubleOptions("\n请输入箭头长度:");
            optDouble.AllowNone = true;
            optDouble.AllowZero = false;
            optDouble.DefaultValue = CurrArrowLength;
            optDouble.UseDefaultValue = true;
            PromptDoubleResult resDouble = ed.GetDouble(optDouble);
            if (resDouble.Status == PromptStatus.OK)
            {
                //改变箭头长度设定值
                CurrArrowLength = resDouble.Value;
                PromptPointResult resStartPoint = ed.GetPoint("\n请输入起点:");
                if (resStartPoint.Status == PromptStatus.OK)
                {
                    PromptPointOptions optEndPoint = new PromptPointOptions("\n请输入终点:");
                    optEndPoint.BasePoint = resStartPoint.Value;
                    optEndPoint.UseBasePoint = true;
                    PromptPointResult resEndPoint = ed.GetPoint(optEndPoint);
                    if (resEndPoint.Status == PromptStatus.OK)
                    {
                        Point3d startpnt = resStartPoint.Value;
                        Point3d endpnt = resEndPoint.Value;
                        Database db = doc.Database;
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTableRecord btr =
                                (BlockTableRecord)tr.GetObject(
                                    db.CurrentSpaceId,
                                    OpenMode.ForWrite,
                                    false);
                            Line line = new Line(startpnt, endpnt);
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
                            Point3d midpnt = (startpnt + endpnt.GetAsVector()) / 2;
                            LArrow la = new LArrow(line);
                            //附着当前设定的箭头长度
                            la.ArrowLength = CurrArrowLength;
                            la.Scale = 0.5;
                            la.SaveExtendedData();
                            tr.Commit();
                        }
                    }
                }
            }
        }
    }
    //实现特殊的夹点
    class LArrowGripData : GripData
    {
        public LArrowGripData(Point3d position)
        {
            GripPoint = position;
        }
        public void Move(LArrow la, Vector3d vec)
        {
            la.Position += vec;
            la.SaveExtendedData();
        }
    }
    //夹点重定义
    public class LArrowGripOverrule : GripOverrule
    {
        public static LArrowGripOverrule TheOverrule = new LArrowGripOverrule();
        public LArrowGripOverrule()
        {
            SetXDataFilter(LArrow.RegAppName);
        }
        //获取夹点,简单实体应重载该函数以获取更灵活的控制
        public override void GetGripPoints(Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            LArrow la = new LArrow(entity);
            base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
            grips.Remove(grips[2]);
            grips.Add(new LArrowGripData(la.Position));
        }
        //移动夹点
        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            LArrow la = new LArrow(entity);
            foreach (GripData gd in grips)
            {
                if (gd is LArrowGripData)
                {
                    LArrowGripData lagd = (LArrowGripData)gd;
                    lagd.Move(la, offset);
                }
            }
            //排除自定义的夹点移动，让剩下的夹点按默认规则移动
            for (int i = grips.Count - 1; i >= 0; i--)
            {
                if (grips[i] is LArrowGripData)
                {
                    grips.Remove(grips[i]);
                }
            }
            if (grips.Count > 0)
            {
                base.MoveGripPointsAt(entity, grips, offset, bitFlags);
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
            SetXDataFilter(LArrow.RegAppName);
        }
        //显示重载
        public override bool WorldDraw(Drawable drawable, WorldDraw wd)
        {
            LArrow la = new LArrow(drawable);
            la.WorldDraw(wd);
            return base.WorldDraw(drawable, wd);
        }

    }
}