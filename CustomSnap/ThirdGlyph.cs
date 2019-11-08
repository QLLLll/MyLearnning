using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using DotNetARX;

namespace CustomSnap
{
    //自定义对象捕捉靶框
    public class ThirdGlyph : Glyph
    {
        private Point3d _point;
        Point3d pt;//存储自定义对象捕捉靶框的位置
        //public override void SetLocation(Point3d point)
        //{
        //    pt = point;//设置自定义靶框的位置
        //}
        //public override void ViewportDraw(ViewportDraw vd)
        //{
        //    //获取单位正方形内中心点的像素
        //    Point2d glyphSize = vd.Viewport.GetNumPixelsInUnitSquare(pt);
        //    //根据系统靶框的高度调整自定义靶框的高度
        //    double glyphHeight = CustomObjectSnapMode.GlyphSize / glyphSize.Y;
        //    //从DCS坐标系到WCS坐标系的转换矩阵
        //    Matrix3d d2w=vd.Viewport.EyeToWorldTransform;
        //    //创建一个由4点组成的数组，表示自定义靶框的矩形顶点，并将其从DCS坐标系转换到WCS坐标系
        //    Point3d[] pts={new Point3d(pt.X+glyphHeight,pt.Y,pt.Z).TransformBy(d2w),
        //new Point3d(pt.X,pt.Y+glyphHeight,pt.Z).TransformBy(d2w),
        //new Point3d(pt.X-glyphHeight,pt.Y,pt.Z).TransformBy(d2w),
        //new Point3d(pt.X,pt.Y-glyphHeight,pt.Z).TransformBy(d2w)};
        //    //绘制表示自定义靶框的矩形
        //    vd.Geometry.Polygon(new Point3dCollection(pts));
        //    //在自定义靶框矩形的下方绘制文字,方向沿X方向
        //    vd.Geometry.Text(pts[3].PolarPoint(0, glyphHeight), Vector3d.ZAxis, Vector3d.XAxis, glyphHeight, 1.0, 0.0, "1/3");
        //}
        public override void SetLocation(Point3d point)
        {
            _point = point;
        }
        protected override void SubViewportDraw(ViewportDraw vd)
        {
            //Draw a square polygon at snap point
            /*   Point2d gSize = vd.Viewport.GetNumPixelsInUnitSquare(_point);
               double gHeight = CustomObjectSnapMode.GlyphSize / gSize.Y;
               Matrix3d dTOw = vd.Viewport.EyeToWorldTransform;
               Point3d[] gPts = 
                   {
                       new Point3d(_point.X - gHeight/2.0, _point.Y - gHeight/2.0, _point.X).TransformBy(dTOw),
                       new Point3d(_point.X + gHeight/2.0, _point.Y - gHeight/2.0, _point.X).TransformBy(dTOw),
                       new Point3d(_point.X + gHeight/2.0, _point.Y + gHeight/2.0, _point.X).TransformBy(dTOw),
                       new Point3d(_point.X - gHeight/2.0, _point.Y + gHeight/2.0, _point.X).TransformBy(dTOw),
                  };
               vd.Geometry.Polygon(new Point3dCollection(gPts));*/
            ////-----------------------------------------------------------
            ////如果你想画一个圆在提前点,
            ////简单的注释掉的代码和以上
            ////取消下面的代码
            ////-----------------------------------------------------------
            Point2d gSize = vd.Viewport.GetNumPixelsInUnitSquare(_point);
            double dia = CustomObjectSnapMode.GlyphSize / gSize.Y;
            vd.Geometry.Circle(_point, dia, Vector3d.ZAxis);
        }
    }
}
