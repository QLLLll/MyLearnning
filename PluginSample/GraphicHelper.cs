using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using System.Windows.Interop;

namespace ArxDotNetLesson
{
    /// <summary>
    /// 绘图系统辅助类。
    /// </summary>
    public static class GraphicHelper
    {
        /// <summary>
        /// 对实体进行离线绘图，生成Bitmap。
        /// </summary>
        /// <param name="ents">实体集合。</param>
        /// <param name="imgWidth">图片分辨率宽度。</param>
        /// <param name="imgHeight">图片分辨率高度。</param>
        /// <param name="lowerLeft">模型空间截取窗口左下。</param>
        /// <param name="upperRight">模型空间截取窗口右上。</param>
        /// <returns></returns>
        public static Bitmap Snapshort(IEnumerable<Entity> ents,
            int imgWidth, int imgHeight, Point3d lowerLeft, Point3d upperRight)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var graMgr = doc.GraphicsManager;

            // 生成绘图用临时视图。
            using (var view = new View())
            {
                var cvport = (short)Application.GetSystemVariable("CVPORT");
                graMgr.SetViewFromViewport(view, cvport);

                // 设置相机相关属性。
                view.SetView(Point3d.Origin + Vector3d.ZAxis, Point3d.Origin,
                    Vector3d.YAxis, imgWidth, imgHeight);

                var descriptor = new KernelDescriptor();
                descriptor.addRequirement(KernelDescriptor.Drawing3D);

                // 生成绘图设备。
                using (var kernel = Manager.AcquireGraphicsKernel(descriptor))
                {
                    using (var device = graMgr.CreateAutoCADOffScreenDevice(kernel))
                    {
                        device.OnSize(new Size(imgWidth, imgHeight));
                        device.DeviceRenderType = RendererType.Default;
                        device.BackgroundColor = Color.Black;
                        device.Add(view);
                        device.Update();

                        // 生成临时模型，添加绘图实体。
                        using (var model = graMgr.CreateAutoCADModel(kernel))
                        {
                            foreach (var ent in ents)
                            {
                                view.Add(ent, model);
                            }
                        }

                        // 对视图按指定截取窗口缩放。
                        view.ZoomExtents(lowerLeft, upperRight);

                        return view.GetSnapshot(
                            new Rectangle(0, 0, imgWidth - 1, imgHeight - 1));
                    }
                }
            }
        }
    }
}
