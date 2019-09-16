using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

using Newtonsoft.Json;

using AcAp = Autodesk.AutoCAD.ApplicationServices;
using AcDb = Autodesk.AutoCAD.DatabaseServices;
using AcGe = Autodesk.AutoCAD.Geometry;
using AcGs = Autodesk.AutoCAD.GraphicsSystem;
using AcEi = Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using ArxDotNetLesson;
using System.Drawing;

namespace PluginSample
{
    /// <summary>
    /// BpDesignDialog.xaml 的交互逻辑
    /// </summary>
    public partial class BpDesignDialog : Window
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// 预览图分辨率。
        /// </summary>
        public const int kPreviewImageWidth = 550;
        public const int kPreviewImageHeight = 550;

        // 界面视图。
        public BpDesignView view { get; private set; }

        // 临时存放预览实体信息。
        private AcDb.Entity[] previewEnts;
        private AcDb.Extents3d previewExtents;

        // 预览缩放状态。
        private bool isPreviewImageZooming = false;

        public BpDesignDialog(BpDesignView designView)
        {
            InitializeComponent();

            DataContext = view = designView;
        }

        private static void ShowError(Exception ex)
        {
            Console.WriteLine(ex);
            MessageBox.Show($"{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowImage(Bitmap bitmap)
        {
            var hbitmap = bitmap.GetHbitmap();
            var imgSource = Imaging.CreateBitmapSourceFromHBitmap(
                hbitmap, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hbitmap); // 防止内存泄漏。
            PreviewImage.Source = imgSource;
        }

        private void SaveParams(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveInfo = new SaveFileDialog();
                saveInfo.FileName = "边坡参数";
                saveInfo.Filter = "JSON文件(*.json)|*.json";
                saveInfo.OverwritePrompt = true;
                saveInfo.AddExtension = true;

                if (saveInfo.ShowDialog() == true)
                {
                    using (var fs = new FileStream(saveInfo.FileName, FileMode.Create))
                    {
                        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(view));
                        fs.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadParams(object sender, RoutedEventArgs e)
        {
            try
            {
                var openInfo = new OpenFileDialog();
                openInfo.Filter = "JSON文件(*.json)|*.json";

                if (openInfo.ShowDialog() == true)
                {
                    using (var fs = new FileStream(openInfo.FileName, FileMode.Open, FileAccess.Read))
                    {
                        using (var rs = new StreamReader(fs))
                        {
                            DataContext = view = JsonConvert.DeserializeObject<BpDesignView>(rs.ReadToEnd());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        /// <summary>
        /// 根据参数绘制边坡。
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static AcDb.Entity[] MakeBianpo(BpDesignView param)
        {
            var result = new List<AcDb.Entity>();
            var db = AcAp.Application.DocumentManager.MdiActiveDocument.Database;

            // 边坡顶点。
            var vertexes = new List<Point2d>();

            // 线路顶部。
            vertexes.Add(new Point2d(-param.Lujiankuan * 0.5, 0));
            vertexes.Add(new Point2d(param.Lujiankuan * 0.5, 0));

            // 路肩宽标注。
            result.Add(new AcDb.AlignedDimension(
                new Point3d(-param.Lujiankuan * 0.5, 1, 0),
                new Point3d(param.Lujiankuan * 0.5, 1, 0),
                new Point3d(0, 1.5, 0), null, AcDb.ObjectId.Null));

            // 路肩高程标记。
            var lujianTriangle = EntityHelper.CreatePolygon(new[]
            {
                -param.Lujiankuan * 0.5, 0,
                -param.Lujiankuan * 0.5 + 0.2, 0.4,
                -param.Lujiankuan * 0.5 - 0.2, 0.4,
            });
            result.Add(lujianTriangle);
            result.Add(lujianTriangle.GetTransformedCopy(
                AcGe.Matrix3d.Displacement(Vector3d.XAxis * param.Lujiankuan)));

            // 路肩高程文字。
            var lujianHeight = new AcDb.DBText();
            lujianHeight.SetDatabaseDefaults(db); // 重要！
            lujianHeight.TextString = param.Lujiangaocheng.ToString("0.##");
            lujianHeight.HorizontalMode = AcDb.TextHorizontalMode.TextCenter;
            lujianHeight.AlignmentPoint = new Point3d(-param.Lujiankuan * 0.5, 0.5, 0);
            lujianHeight.AdjustAlignment(db); // 重要！
            result.Add(lujianHeight);
            result.Add(lujianHeight.GetTransformedCopy(
                AcGe.Matrix3d.Displacement(Vector3d.XAxis * param.Lujiankuan)));

            // 一级边坡。
            var yijipogao = param.HasErjibianpo ? param.Yijipogao :
                (param.Lujiangaocheng - param.Churukougaocheng);
            vertexes.Insert(0, vertexes.First() +
                    new Vector2d(-yijipogao * param.Yijipolv, -yijipogao));
            vertexes.Add(vertexes.Last() +
                new Vector2d(yijipogao * param.Yijipolv, -yijipogao));

            // 一级边坡坡率。
            var yjbppl = new AcDb.DBText();
            yjbppl.SetDatabaseDefaults(db); // 重要！
            yjbppl.TextString = "1:" + param.Yijipolv.ToString("0.##");
            yjbppl.HorizontalMode = AcDb.TextHorizontalMode.TextCenter;
            var yjbppl1 = yjbppl.Clone() as AcDb.DBText; // 右侧对称。
            yjbppl.AlignmentPoint = new Point3d(
                -(param.Lujiankuan + yijipogao * param.Yijipolv) * 0.5,
                -yijipogao * 0.5 + 0.1, 0);
            yjbppl.Rotation = Math.Atan2(1, param.Yijipolv);
            yjbppl.AdjustAlignment(db); // 重要！
            result.Add(yjbppl);
            yjbppl1.AlignmentPoint = new Point3d(
                (param.Lujiankuan + yijipogao * param.Yijipolv) * 0.5,
                -yijipogao * 0.5 + 0.1, 0);
            yjbppl1.Rotation = -Math.Atan2(1, param.Yijipolv);
            yjbppl1.AdjustAlignment(db); // 重要！
            result.Add(yjbppl1);

            // 一级边坡宽度标注。
            result.Add(new AcDb.AlignedDimension(
                new Point3d(-param.Lujiankuan * 0.5, 1, 0),
                new Point3d(-param.Lujiankuan * 0.5 - yijipogao * param.Yijipolv, 1, 0),
                new Point3d(0, 1.5, 0), null, AcDb.ObjectId.Null));
            result.Add(new AcDb.AlignedDimension(
                new Point3d(param.Lujiankuan * 0.5, 1, 0),
                new Point3d(param.Lujiankuan * 0.5 + yijipogao * param.Yijipolv, 1, 0),
                new Point3d(0, 1.5, 0), null, AcDb.ObjectId.Null));

            // 二级边坡。
            if (param.HasErjibianpo)
            {
                // 1米加宽平台。
                vertexes.Insert(0, vertexes.First() + new Vector2d(-1, 0));
                vertexes.Add(vertexes.Last() + new Vector2d(1, 0));
                // 标注。
                result.Add(new AcDb.AlignedDimension(
                    new Point3d(vertexes[0].X, vertexes[0].Y, 0),
                    new Point3d(vertexes[1].X, vertexes[1].Y, 0),
                    new Point3d(0, 1.5, 0), null, AcDb.ObjectId.Null));
                result.Add(new AcDb.AlignedDimension(
                    new Point3d(vertexes[vertexes.Count - 1].X, vertexes[vertexes.Count - 1].Y, 0),
                    new Point3d(vertexes[vertexes.Count - 2].X, vertexes[vertexes.Count - 2].Y, 0),
                    new Point3d(0, 1.5, 0), null, AcDb.ObjectId.Null));

                // 二级边坡。
                var erjipogao = param.HasSanjibianpo ? param.Erjipogao :
                    (param.Lujiangaocheng - param.Churukougaocheng - yijipogao);
                vertexes.Insert(0, vertexes.First() +
                        new Vector2d(-erjipogao * param.Erjipolv, -erjipogao));
                vertexes.Add(vertexes.Last() +
                    new Vector2d(erjipogao * param.Erjipolv, -erjipogao));

                // 二级边坡坡率。
                var ejbppl = new AcDb.DBText();
                ejbppl.SetDatabaseDefaults(db); // 重要！
                ejbppl.TextString = "1:" + param.Erjipolv.ToString("0.##");
                ejbppl.HorizontalMode = AcDb.TextHorizontalMode.TextCenter;
                var ejbppl1 = ejbppl.Clone() as AcDb.DBText; // 右侧对称。
                ejbppl.AlignmentPoint = new Point3d(
                    -param.Lujiankuan * 0.5 - yijipogao * param.Yijipolv
                        - 1 - erjipogao * param.Erjipolv * 0.5,
                    -yijipogao - erjipogao * 0.5 + 0.1, 0);
                ejbppl.Rotation = Math.Atan2(1, param.Erjipolv);
                ejbppl.AdjustAlignment(db); // 重要！
                result.Add(ejbppl);
                ejbppl1.AlignmentPoint = new Point3d(
                    param.Lujiankuan * 0.5 + yijipogao * param.Yijipolv
                        + 1 + erjipogao * param.Erjipolv * 0.5,
                    -yijipogao - erjipogao * 0.5 + 0.1, 0);
                ejbppl1.Rotation = -Math.Atan2(1, param.Erjipolv);
                ejbppl1.AdjustAlignment(db); // 重要！
                result.Add(ejbppl1);

                // 二级边坡宽度标注。
                result.Add(new AcDb.AlignedDimension(
                    new Point3d(-param.Lujiankuan * 0.5 - yijipogao * param.Yijipolv - 1, 1, 0),
                    new Point3d(-param.Lujiankuan * 0.5 - yijipogao * param.Yijipolv - 1
                        - erjipogao * param.Erjipolv, 1, 0),
                    new Point3d(0, 1.5, 0), null, AcDb.ObjectId.Null));
                result.Add(new AcDb.AlignedDimension(
                    new Point3d(param.Lujiankuan * 0.5 + yijipogao * param.Yijipolv + 1, 1, 0),
                    new Point3d(param.Lujiankuan * 0.5 + yijipogao * param.Yijipolv + 1
                        + erjipogao * param.Erjipolv, 1, 0),
                    new Point3d(0, 1.5, 0), null, AcDb.ObjectId.Null));
            }

            // TODO 三级边坡。

            result.Add(EntityHelper.CreatePolygon(
                vertexes.SelectMany(p2d => p2d.ToArray()).ToArray(),
                closed: false));

            return result.ToArray();
        }

        /// <summary>
        /// 预览按钮处理。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Preview(object sender, RoutedEventArgs e)
        {
            try
            {
                // 生成边坡实体集合，按实体边界获取截取窗口。
                previewEnts = MakeBianpo(view);
                var extents = new AcDb.Extents3d();
                foreach (var ent in previewEnts.OfType<AcDb.Curve>())
                {
                    extents.AddExtents(ent.GeometricExtents);
                }
                previewExtents = extents;

                // 生成Bitmap，显示到界面的图片上。
                using (var bitmap = GraphicHelper.Snapshort(previewEnts,
                    kPreviewImageWidth, kPreviewImageHeight,
                    previewExtents.MinPoint, previewExtents.MaxPoint))
                {
                    ShowImage(bitmap);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        /// <summary>
        /// 插入按钮处理。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Insert(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = AcAp.Application.DocumentManager.MdiActiveDocument;
                var editor = doc.Editor;

                // 获取用户输入插入点。
                Point3d? insertPt = null;
                using (var interacting = editor.StartUserInteraction(this))
                {
                    var res = editor.GetPoint("请选择插入坐标点：");
                    if (res.Status == AcEi.PromptStatus.OK)
                    {
                        insertPt = res.Value;
                    }

                    interacting.End();
                    this.Focus();
                }

                // 插入边坡图形。
                if (insertPt.HasValue)
                {
                    var ents = MakeBianpo(view);
                    ents.ForEach(ent => ent.TransformBy(
                        AcGe.Matrix3d.Displacement(
                            Point3d.Origin.GetVectorTo(insertPt.Value))));

                    // 从面板启动，操作文档需要加锁。
                    using (var docLock = doc.LockDocument())
                    {
                        ents.ToSpace();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void PreviewImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (previewEnts == null)
                {
                    return;
                }

                // 在鼠标位置放大一倍。
                if (!isPreviewImageZooming && e.Delta > 0)
                {
                    isPreviewImageZooming = true;

                    // 将原预览实体范围扩展为与图片比例相同，以准确定位扩大点。
                    var minPoint = previewExtents.MinPoint;
                    var maxPoint = previewExtents.MaxPoint;
                    var origWidth = maxPoint.X - minPoint.X;
                    var origHeight = maxPoint.Y - minPoint.Y;
                    var origRatio = origHeight > 0 ? (origWidth / origHeight) : 1;
                    var prevImgRatio = kPreviewImageWidth / kPreviewImageHeight;
                    if (origRatio > prevImgRatio) // 过宽。
                    {
                        var heightDiff = origWidth / prevImgRatio - origHeight;
                        minPoint = new Point3d(minPoint.X, minPoint.Y - heightDiff * 0.5, 0);
                        maxPoint = new Point3d(maxPoint.X, maxPoint.Y + heightDiff * 0.5, 0);
                    }
                    else if (origRatio < prevImgRatio) // 过高。
                    {
                        var widthDiff = origHeight * prevImgRatio - origWidth;
                        minPoint = new Point3d(minPoint.X - widthDiff * 0.5, minPoint.Y, 0);
                        maxPoint = new Point3d(maxPoint.X + widthDiff * 0.5, maxPoint.Y, 0);
                    }

                    // 获取缩放后的截取窗口。
                    var zoomPtOnImg = e.GetPosition(PreviewImage);
                    zoomPtOnImg = new System.Windows.Point(
                        zoomPtOnImg.X * kPreviewImageWidth / PreviewImage.ActualWidth,
                        zoomPtOnImg.Y * kPreviewImageHeight / PreviewImage.ActualHeight);
                    var zoomVec = maxPoint - minPoint;
                    var zoomPt = new Point3d(
                        minPoint.X + zoomVec.X * zoomPtOnImg.X / kPreviewImageWidth,
                        maxPoint.Y - zoomVec.X * zoomPtOnImg.Y / kPreviewImageHeight,
                        0);

                    // 生成Bitmap，显示到界面的图片上。
                    using (var bitmap = GraphicHelper.Snapshort(previewEnts,
                        kPreviewImageWidth, kPreviewImageHeight,
                        zoomPt - zoomVec * 0.25, zoomPt + zoomVec * 0.25))
                    {
                        ShowImage(bitmap);
                    }
                }

                // 还原。
                if (isPreviewImageZooming && e.Delta < 0)
                {
                    isPreviewImageZooming = false;

                    // 生成Bitmap，显示到界面的图片上。
                    using (var bitmap = GraphicHelper.Snapshort(previewEnts,
                        kPreviewImageWidth, kPreviewImageHeight,
                        previewExtents.MinPoint, previewExtents.MaxPoint))
                    {
                        ShowImage(bitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
