using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
namespace CustomSnap
{
    public class Command
    {
        [CommandMethod("StartSnap")]
        public void StartSnap()
        {
            //创建自定义对象捕捉模式
            CustomObjectSnapMode mode = new CustomObjectSnapMode("third", "_third", "三分之一", new ThirdGlyph());
            //捕捉模式的实体类型为曲线
            mode.ApplyToEntityType(RXClass.GetClass(typeof(Curve)), CurveSnap);
            //开启自定义对象捕捉模式
            CustomObjectSnapMode.Activate("_third");
        }
        [CommandMethod("StopSnap")]
        public void StopSnap()
        {
            //关闭自定义对象捕捉模式
            CustomObjectSnapMode.Deactivate("_third");
        }
        public void CurveSnap(ObjectSnapContext context, ObjectSnapInfo result)
        {
            Curve curve = (Curve)context.PickedObject;//当前捕捉到的曲线对象
            Point3dCollection snaps = result.SnapPoints;//获取对象的捕捉点集合
            if (curve.Closed) return;//如果为闭合曲线，则返回
            double startParam = curve.StartParam;//曲线的起点参数
            double endParam = curve.EndParam;//曲线的终点参数
            //将与曲线起点的距离为1/3曲线长度的点作为捕捉点
            snaps.Add(curve.GetPointAtParameter(startParam + ((endParam - startParam) / 3)));
            //将与曲线起点的距离为2/3曲线长度的点作为捕捉点
            snaps.Add(curve.GetPointAtParameter(startParam + ((endParam - startParam) * 2 / 3)));
        }
    }
}
