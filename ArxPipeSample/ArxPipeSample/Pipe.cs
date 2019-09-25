using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.Windows;

using ArxDotNetLesson;

namespace ArxPipeSample
{
    /// <summary>
    /// 管道类。
    /// </summary>
    public static class Pipe
    {
        /// <summary>
        /// 箭头尺寸。
        /// </summary>
        public const double kArrow1Width = 5;
        public const double kArrow1Length = 10;

        /// <summary>
        /// 多段线类型识别信息，存放到扩展数据里。
        /// </summary>
        public const string kPipeType1 = "pipe1";

        /// <summary>
        /// 多段线类型。1表示第一类，2依次类推。
        /// </summary>
        private static int pipeType;
        /// <summary>
        /// 记录由PLINE命令创建的多段线实体。
        /// </summary>
        private static Polyline pipeEntity;

        /// <summary>
        /// 管道创建后处理。
        /// 仅当由本插件调用的PLINE命令结束时，才调用本方法。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void EndPipe(object sender, CommandEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            doc.Editor.WriteMessage($"\nEnd executing {e.GlobalCommandName}.");
            if (e.GlobalCommandName.ToLower().EndsWith("pline") && e.GlobalCommandName.Length <= 6)
            {
                // 注销事件。
                doc.CommandEnded -= EndPipe;
                doc.CommandCancelled -= EndPipe;
                doc.CommandFailed -= EndPipe;
                db.ObjectAppended -= PipeAdded;

                // 当生成的多段线满足要求时，执行操作。
                if (pipeEntity != null && pipeEntity.NumberOfVertices > 1)
                {
                    if (pipeType == 1) // 根据管道类型设置箭头。
                    {
                        // 拷贝原多段线。
                        var pe = pipeEntity.Clone() as Polyline;

                        // 删除由PLINE命令生成的多段线。
                        using (var trans = db.TransactionManager.StartOpenCloseTransaction())
                        {
                            trans.GetObject(pipeEntity.ObjectId, OpenMode.ForWrite).Erase();
                            pipeEntity = null;
                            trans.Commit();
                        }

                        // 根据最末两点计算箭头朝向。
                        var lastPt1 = pe.GetPoint2dAt(pe.NumberOfVertices - 1);
                        var lastPt2 = pe.GetPoint2dAt(pe.NumberOfVertices - 2);
                        doc.Editor.WriteMessage($"\nLast two points: {lastPt1}, {lastPt2}.");

                        var lastVec = (lastPt1 - lastPt2).GetNormal();

                        // 设置箭头尺寸。
                        var lastPt = lastPt1 + lastVec * kArrow1Length;
                        var arrowWidth = kArrow1Width;

                        // 向管道添加箭头顶点，设置宽度。
                        pe.SetStartWidthAt(pe.NumberOfVertices - 1, arrowWidth);
                        pe.AddVertexAt(pe.NumberOfVertices, lastPt, 0, 0, 0);

                        // 向管道实体注册识别信息。
                        pe.AttachXData(PipeSample.kAppName,
                            new[] { new TypedValue((int)DxfCode.ExtendedDataAsciiString, kPipeType1) });

                        // 将复制后的多段线添加到模型空间。
                        pe.ToSpace();
                    }
                }
            }
        }

        /// <summary>
        /// 当有实体被添加时，记录管道实体。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PipeAdded(object sender, ObjectEventArgs e)
        {
            pipeEntity = (e.DBObject as Polyline) ?? pipeEntity;
        }

        /// <summary>
        /// 创建管道类型1。
        /// </summary>
        public static void MakePipeType1()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            // 注册命令结束事件以及实体添加事件。
            doc.CommandEnded += EndPipe;
            doc.CommandCancelled += EndPipe;
            doc.CommandFailed += EndPipe;
            db.ObjectAppended += PipeAdded;

            // 参数初始化。
            pipeType = 1;
            pipeEntity = null;

            // 向文档发出PLINE命令。
            doc.SendStringToExecute("pline ", true, false, true);
        }
    }
}
