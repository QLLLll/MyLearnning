using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;

using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;

namespace EcdPrint
{
    public class Class1
    {

        [DllImport("acad.exe", CallingConvention = CallingConvention.Cdecl,
                 EntryPoint = "acedTrans")
          ]
        static extern int acedTrans(
            double[] point,
            IntPtr fromRb,
            IntPtr toRb,
            int disp,
            double[] result
          );

        [CommandMethod("EcdPrint")]
        public void ShowPlotForm()
        {
            PrintForm plotForm = new PrintForm();//新建对话框
            Application.ShowModelessDialog(plotForm);//显示无模式对话框
        }
        //private static Extents2d Ucs2Dcs(Point3d objStart, Point3d objEnd)
        //{
        //    ResultBuffer rbFrom = new ResultBuffer(new TypedValue(5003, 1)),
        //        rbTo = new ResultBuffer(new TypedValue(5003, 2));

        //    double[] firres = new double[] { 0, 0, 0 };
        //    double[] secres = new double[] { 0, 0, 0 };

        //    acedTrans(
        //        objStart.ToArray(),
        //        rbFrom.UnmanagedObject,
        //        rbTo.UnmanagedObject,
        //        0,
        //        firres
        //    );

        //    acedTrans(
        //        objEnd.ToArray(),
        //        rbFrom.UnmanagedObject,
        //        rbTo.UnmanagedObject,
        //        0,
        //        secres
        //    );


        //    Extents2d window =
        //      new Extents2d(
        //        firres[0],
        //        firres[1],
        //        secres[0],
        //        secres[1]
        //      );
        //    return window;
        //}
        //#region 多段线

        ///// <summary>
        /////首尾相连的线段连接成多段线
        ///// V1.0 by WeltionChen @2011.02.17
        ///// 实现原理：
        ///// 1.选择图面上所有直线段
        ///// 2.选取选集第一条直线作为起始线段，向线段的两个方向搜索与之相连的直线段
        ///// 3.搜索方式采用Editor的SelectCrossingWindow方法通过线段的端点创建选集
        ///// 正常情况下会选到1到2个线段（本程序暂不处理3个线段相交的情况），剔除本身，得到与之相连的直线段
        ///// 4.处理过的直线段将不再作为起始线段，由集合中剔除
        ///// 4.通过递归循环依次搜索，直到末端。
        ///// 5.删除原线段，根据创建多段线
        ///// 6.循环处理所有的线段
        ///// </summary>
        //[CommandMethod("tt5")]
        //public void JionLinesToPline()
        //{
        //    //选择图面上所有直线段
        //    Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
        //    SelectionFilter sf = new SelectionFilter(new TypedValue[] { new TypedValue(0, "Line") });
        //    PromptSelectionResult selectLinesResult = ed.SelectAll(sf);
        //    if (selectLinesResult.Status != PromptStatus.OK)
        //        return;
        //    //需要处理的直线段集合
        //    List<ObjectId> lineObjectIds = new List<ObjectId>(selectLinesResult.Value.GetObjectIds());
        //    using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        //    {
        //        Database db = HostApplicationServices.WorkingDatabase;
        //        BlockTableRecord currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
        //        while (true)
        //        {
        //            //选取选集第一条直线作为起始线段
        //            ObjectId currentLineId = lineObjectIds[0];
        //            //处理过的直线段将不再作为起始线段，由集合中剔除
        //            lineObjectIds.RemoveAt(0);
        //            Line currentLine = tr.GetObject(currentLineId, OpenMode.ForWrite) as Line;
        //            //多段线的顶点集合，由各段相连的直线段的端点组成，初始值为起始线段的端点
        //            List<Point3d> plinePoints = new List<Point3d> { currentLine.StartPoint, currentLine.EndPoint };
        //            //每个直线段有两个方向，由起点向终点方向搜索
        //            JionLinesToPline(ref lineObjectIds, tr, ref plinePoints, currentLineId, currentLineId);
        //            //翻转点集
        //            plinePoints.Reverse();
        //            //由终点向起点方向搜索
        //            JionLinesToPline(ref lineObjectIds, tr, ref plinePoints, currentLineId, currentLineId);
        //            //本程序为将相连的直线段转成多段线，所以对孤立的直线段不做处理
        //            if (plinePoints.Count > 2)
        //            {
        //                //创建多段线
        //                Polyline resultPline = new Polyline();
        //                for (int i = 0; i < plinePoints.Count - 1; i++)
        //                {
        //                    //resultPline.AddVertexAt(i, new Point2d(plinePoints.X, plinePoints.Y), 0, 0, 0);
        //                }
        //                if (plinePoints[0] == plinePoints[plinePoints.Count - 1])
        //                {
        //                    resultPline.Closed = true;
        //                }
        //                else
        //                {
        //                    resultPline.AddVertexAt(plinePoints.Count - 1, new Point2d(plinePoints[plinePoints.Count - 1].X, plinePoints[plinePoints.Count - 1].Y), 0, 0, 0);
        //                }
        //                resultPline.Layer = currentLine.Layer;
        //                resultPline.Linetype = currentLine.Linetype;
        //                resultPline.LinetypeScale = currentLine.LinetypeScale;
        //                currentSpace.AppendEntity(resultPline);
        //                tr.AddNewlyCreatedDBObject(resultPline, true);
        //                //删除起始直线段
        //                currentLine.Erase();
        //            }
        //            //处理完毕，跳出循环
        //            if (lineObjectIds.Count == 0)
        //                break;
        //        }
        //        tr.Commit();
        //    }
        //}
        ///// <summary>
        ///// 线段连接成多段线递归循环部分
        ///// V1.0 by WeltionChen @2011.02.17
        ///// </summary>
        ///// <param name="lineObjectIds">线段的objectid集合</param>
        ///// <param name="tr">transaction</param>
        ///// <param name="plinePoints">多段线顶点坐标，也是各线段的端点坐标集合</param>
        ///// <param name="currentLineId">当前线段的objectid</param>
        //void JionLinesToPline(ref List<ObjectId> lineObjectIds, Transaction tr, ref List<Point3d> plinePoints, ObjectId currentLineId, ObjectId startLineId)
        //{
        //    //提取端点
        //    Point3d lastPoint = plinePoints[plinePoints.Count - 1];
        //    Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
        //    SelectionFilter sf = new SelectionFilter(new TypedValue[] { new TypedValue(0, "Line") });
        //    //通过点创建选集
        //    PromptSelectionResult selectLinesResult = ed.SelectCrossingWindow(lastPoint, lastPoint, sf);
        //    if (selectLinesResult.Status == PromptStatus.OK)
        //    {
        //        List<ObjectId> selectedLinesId = new List<ObjectId>(selectLinesResult.Value.GetObjectIds());
        //        //剔除本身
        //        selectedLinesId.Remove(currentLineId);
        //        //处理相连的直线段
        //        if (selectedLinesId.Count == 1)
        //        {
        //            ObjectId selectedLineId = selectedLinesId[0];
        //            //处理过的直线段将不再作为起始线段，由集合中剔除
        //            if (selectedLineId != startLineId)
        //            {
        //                lineObjectIds.Remove(selectedLineId);
        //                Line selectedLine = tr.GetObject(selectedLineId, OpenMode.ForWrite) as Line;
        //                //添加顶点
        //                if (selectedLine.StartPoint == lastPoint)
        //                {
        //                    plinePoints.Add(selectedLine.EndPoint);
        //                }
        //                else
        //                {
        //                    plinePoints.Add(selectedLine.StartPoint);
        //                }
        //                //递归继续搜索
        //                JionLinesToPline(ref lineObjectIds, tr, ref plinePoints, selectedLineId, startLineId);
        //                //删除中间线段
        //                selectedLine.Erase();
        //            }
        //        }
        //    }

        //}
        //#endregion
        //#region 单个实体打印
        ///// <summary>
        ///// 搜索图框
        ///// V1.0 by WeltionChen@2011.02.24
        ///// </summary>
        //[CommandMethod("PrintSim")]
        //public void GetDrawingFrames()
        //{
        //    using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        //    {
        //        //缩放到全屏操作
        //        Database db = HostApplicationServices.WorkingDatabase;
        //        Document doc = Application.DocumentManager.MdiActiveDocument;
        //        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        //        int num = 1;//定义文档编码
        //        //PromptPointOptions ppo = new PromptPointOptions("\n选择绘图区域第一个点: ")

        //        //{
        //        //    AllowNone = false
        //        //};
        //        //PromptPointResult ppr = ed.GetPoint(ppo);
        //        //if (ppr.Status != PromptStatus.OK)
        //        //    return;
        //        //Point3d first = ppr.Value;
        //        //PromptCornerOptions pco = new PromptCornerOptions("\n选择绘图区域的第二个点: ", first);
        //        //ppr = ed.GetCorner(pco);

        //        //if (ppr.Status != PromptStatus.OK)
        //        //    return;
        //        //Point3d second = ppr.Value;
        //        //Point3d minPoint = first;
        //        //Point3d maxPoint = second;
        //        //ed.WriteMessage(@"第一个点" + minPoint + "er:" + maxPoint);

        //        PromptEntityOptions entityOptions = new PromptEntityOptions("请选择实体对象：");
        //        PromptEntityResult promptEntity = ed.GetEntity(entityOptions);
        //        if (promptEntity.Status != PromptStatus.OK)
        //            return;

        //        //获取实体对象
        //        Entity ent = tr.GetObject(promptEntity.ObjectId, OpenMode.ForRead) as Entity;
        //        Extents3d ex = ent.GeometricExtents;

        //        ViewTableRecord currentVP = ed.GetCurrentView() as ViewTableRecord;
        //        ViewTableRecord zoomVP = new ViewTableRecord();


        //        //绘制当前布局
        //        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);


        //        Layout lo = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);
        //        // 需要一个链接到布局的PlotInfo对象
        //        PlotInfo pi = new PlotInfo
        //        {
        //            Layout = btr.LayoutId
        //        };
        //        // 我们需要一个基于布局设置的PlotSettings对象，然后对其进行自定义
        //        PlotSettings ps = new PlotSettings(lo.ModelType);

        //        ps.CopyFrom(lo);
        //        // PlotSettingsValidator可帮助创建有效的PlotSettings对象

        //        PlotSettingsValidator psv = PlotSettingsValidator.Current;
        //        // 我们将绘制范围，居中并缩放以适合
        //        //psv.SetPlotWindowArea(ps, Ucs2Dcs(minPoint, maxPoint));
        //        // psv.SetPlotWindowArea(ps, new Extents2d(eExtents.MinPoint.X, eExtents.MinPoint.Y, eExtents.MaxPoint.X, eExtents.MaxPoint.Y));

        //        psv.SetPlotWindowArea(ps, new Extents2d(ex.MinPoint.X, ex.MinPoint.Y, ex.MaxPoint.X, ex.MaxPoint.Y));
        //        psv.SetPlotType(ps, PlotType.Window);


        //        psv.SetUseStandardScale(ps, true);

        //        psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);

        //        psv.SetPlotCentered(ps, true);
        //        // 定义文档样式和打印样式
        //        psv.SetPlotConfigurationName(ps, "DWG To PDF.pc3", "ANSI_A_(8.50_x_11.00_Inches)");
        //        // 将PlotInfo链接到PlotSettings，然后对其进行验证
        //        pi.OverrideSettings = ps;
        //        PlotInfoValidator piv = new PlotInfoValidator
        //        {
        //            MediaMatchingPolicy = MatchingPolicy.MatchEnabled
        //        };
        //        piv.Validate(pi);
        //        // 一个PlotEngine进行实际的绘图（也可以为预览创建一个）
        //        if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
        //        {
        //            using (PlotEngine pe = PlotFactory.CreatePublishEngine())
        //            {
        //                // 创建进度对话框以提供信息并允许用户取消
        //                using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
        //                {

        //                    //保存App的原参数
        //                    short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
        //                    //设定为前台打印，加快打印速度
        //                    Application.SetSystemVariable("BACKGROUNDPLOT", 0);
        //                    ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "预览打印进度");
        //                    ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
        //                    ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "取消");
        //                    ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
        //                    ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "进度");
        //                    ppd.LowerPlotProgressRange = 0;
        //                    ppd.UpperPlotProgressRange = 100;
        //                    ppd.PlotProgressPos = 0;
        //                    // 开始打印
        //                    ppd.OnBeginPlot();
        //                    ppd.IsVisible = true;
        //                    pe.BeginPlot(ppd, null);

        //                    PlotConfig config = PlotConfigManager.CurrentConfig;
        //                    //获取去除扩展名后的文件名（不含路径）
        //                    string fileName = SymbolUtilityServices.GetSymbolNameFromPathName(doc.Name, "dwg");
        //                    //定义保存文件对话框
        //                    PromptSaveFileOptions opt = new PromptSaveFileOptions("文件名")
        //                    {
        //                        //保存文件对话框的文件扩展名列表
        //                        Filter = "*" + config.DefaultFileExtension + "|*" + config.DefaultFileExtension,
        //                        DialogCaption = "浏览打印文件",//保存文件对话框的标题
        //                        InitialDirectory = @"D:\",//缺省保存目录
        //                        InitialFileName = fileName + "-" + lo.LayoutName + "-" + num//缺省保存文件名
        //                    };

        //                    num++;
        //                    //根据保存对话框中用户的选择，获取保存文件名
        //                    PromptFileNameResult result = ed.GetFileNameForSave(opt);
        //                    if (result.Status != PromptStatus.OK) return;
        //                    fileName = result.StringResult;

        //                    // 我们将绘制一个文档
        //                    pe.BeginDocument(pi, doc.Name, null, 1, true, fileName);

        //                    ppd.OnBeginSheet();
        //                    ppd.LowerSheetProgressRange = 0;
        //                    ppd.UpperSheetProgressRange = 100;
        //                    ppd.SheetProgressPos = 0;
        //                    PlotPageInfo ppi = new PlotPageInfo();
        //                    pe.BeginPage(ppi, pi, true, null);
        //                    pe.BeginGenerateGraphics(null);
        //                    pe.EndGenerateGraphics(null);
        //                    pe.EndPage(null);
        //                    ppd.SheetProgressPos = 100;
        //                    ppd.OnEndSheet();
        //                    pe.EndDocument(null);
        //                    ppd.PlotProgressPos = 100;
        //                    ppd.OnEndPlot();
        //                    pe.EndPlot(null);
        //                    pe.Dispose();
        //                    pe.Destroy();
        //                    Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);

        //                }
        //            }
        //        }
        //        else
        //        {
        //            ed.WriteMessage(
        //              "\n另一个打印进程正在进行中."
        //            );
        //        }
        //    }


        //}
        //void RemoveInnerPLine(Transaction tr, ref List<ObjectId> selectedObjectIds, ref List<ObjectId> resultObjectIds)
        //{
        //    ObjectId outerPlineId = selectedObjectIds[0];
        //    selectedObjectIds.RemoveAt(0);
        //    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        //    Polyline outerPline = tr.GetObject(outerPlineId, OpenMode.ForRead) as Polyline;
        //    SelectionFilter frameFilter = new SelectionFilter(new TypedValue[] { new TypedValue(0, "LWPOLYLINE"), new TypedValue(90, 4), new TypedValue(70, 1) });
        //    PromptSelectionResult getInnerPlineResult = ed.SelectWindow(outerPline.GetPoint3dAt(0), outerPline.GetPoint3dAt(2), frameFilter);
        //    if (getInnerPlineResult.Status == PromptStatus.OK)
        //    {
        //        List<ObjectId> innerPlineObjectIds = new List<ObjectId>(getInnerPlineResult.Value.GetObjectIds());
        //        innerPlineObjectIds.Remove(outerPlineId);
        //        foreach (ObjectId innerPlineObjectId in innerPlineObjectIds)
        //        {
        //            selectedObjectIds.Remove(innerPlineObjectId);
        //            resultObjectIds.Remove(innerPlineObjectId);
        //        }
        //        if (selectedObjectIds.Count > 0)
        //        {
        //            RemoveInnerPLine(tr, ref selectedObjectIds, ref resultObjectIds);
        //        }
        //    }
        //}
        //#endregion

        //#region 自动识别标准图幅以及比例

        //[CommandMethod("tt7")]
        //public void GetFrameSizeScale()
        //{
        //    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        //    Point3d pt1 = ed.GetPoint("\n第一个点").Value;
        //    Point3d pt2 = ed.GetPoint("\n对角点").Value;
        //    object[] returnResult;
        //    if (GetFrameSizeScale(new Point2d(pt1.X, pt1.Y), new Point2d(pt2.X, pt2.Y), out returnResult))
        //    {
        //        ed.WriteMessage("\n比例:{0},横向尺寸:{1},竖向尺寸:{2}", returnResult[0], returnResult[1], returnResult[2]);
        //    }
        //    else
        //    {
        //        ed.WriteMessage("\n你所选的不是标准图框");
        //    }
        //}
        //bool GetFrameSizeScale(Point2d pt1, Point2d pt2, out object[] returnResult)
        //{
        //    returnResult = new object[3];
        //    List<int> pageWidths = new List<int> { 841, 594, 419, 293, 210 };
        //    double frameXSize = Math.Abs(pt1.X - pt2.X);
        //    double frameYSize = Math.Abs(pt1.Y - pt2.Y);
        //    double frameWidth = Math.Min(frameXSize, frameYSize);
        //    for (int i = 0; i < pageWidths.Count; i++)
        //    {
        //        int pageWidth = pageWidths[i];
        //        if ((int)frameWidth % pageWidth == 0)
        //        {
        //            double scale = pageWidth / frameWidth;
        //            returnResult[0] = scale;
        //            returnResult[1] = frameXSize * scale;
        //            returnResult[2] = frameYSize * scale;
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //#endregion

        //#region 单表预览打印

        //[CommandMethod("simprev")]
        //static public void SimplePreview()
        //{
        //    Document doc =
        //      Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = doc.Editor;
        //    Database db = doc.Database;
        //    // PlotEngines do the previewing and plotting
        //    if (PlotFactory.ProcessPlotState ==
        //        ProcessPlotState.NotPlotting)
        //    {
        //        // First we preview...
        //        PreviewEndPlotStatus stat;
        //        PlotEngine pre =
        //          PlotFactory.CreatePreviewEngine(
        //            (int)PreviewEngineFlags.Plot
        //          );
        //        using (pre)
        //        {
        //            stat =
        //              PlotOrPreview(
        //                pre,
        //                true,
        //                db.CurrentSpaceId,
        //                ""
        //              );
        //        }
        //        if (stat == PreviewEndPlotStatus.Plot)
        //        {
        //            // And if the user asks, we plot...
        //            PlotEngine ple =
        //              PlotFactory.CreatePublishEngine();
        //            stat =
        //              PlotOrPreview(
        //                ple,
        //                false,
        //                db.CurrentSpaceId,
        //                "c:\\previewed-plot"
        //              );
        //        }
        //    }
        //    else
        //    {
        //        ed.WriteMessage(
        //          "\nAnother plot is in progress."
        //        );
        //    }
        //}
        //static PreviewEndPlotStatus PlotOrPreview(
        //  PlotEngine pe,
        //  bool isPreview,
        //  ObjectId spaceId,
        //  string filename)
        //{
        //    Document doc =
        //      Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = doc.Editor;
        //    Database db = doc.Database;
        //    PreviewEndPlotStatus ret =
        //      PreviewEndPlotStatus.Cancel;
        //    Transaction tr =
        //      db.TransactionManager.StartTransaction();
        //    using (tr)
        //    {
        //        // We'll be plotting the current layout
        //        BlockTableRecord btr =
        //          (BlockTableRecord)tr.GetObject(
        //            spaceId,
        //            OpenMode.ForRead
        //          );
        //        Layout lo =
        //          (Layout)tr.GetObject(
        //            btr.LayoutId,
        //            OpenMode.ForRead
        //          );
        //        // We need a PlotInfo object
        //        // linked to the layout
        //        PlotInfo pi = new PlotInfo();
        //        pi.Layout = btr.LayoutId;
        //        // We need a PlotSettings object
        //        // based on the layout settings
        //        // which we then customize
        //        PlotSettings ps =
        //          new PlotSettings(lo.ModelType);
        //        ps.CopyFrom(lo);
        //        // The PlotSettingsValidator helps
        //        // create a valid PlotSettings object
        //        PlotSettingsValidator psv =
        //          PlotSettingsValidator.Current;
        //        // We'll plot the extents, centered and 
        //        // scaled to fit
        //        psv.SetPlotType(
        //          ps,
        //          Autodesk.AutoCAD.DatabaseServices.PlotType.Extents
        //        );
        //        psv.SetUseStandardScale(ps, true);
        //        psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
        //        psv.SetPlotCentered(ps, true);
        //        // We'll use the standard DWF PC3, as
        //        // for today we're just plotting to file
        //        psv.SetPlotConfigurationName(
        //          ps,
        //          "DWF6 ePlot.pc3",
        //          "ANSI_A_(8.50_x_11.00_Inches)"
        //        );
        //        // We need to link the PlotInfo to the
        //        // PlotSettings and then validate it
        //        pi.OverrideSettings = ps;
        //        PlotInfoValidator piv =
        //          new PlotInfoValidator();
        //        piv.MediaMatchingPolicy =
        //          MatchingPolicy.MatchEnabled;
        //        piv.Validate(pi);
        //        // Create a Progress Dialog to provide info
        //        // and allow thej user to cancel
        //        PlotProgressDialog ppd =
        //          new PlotProgressDialog(isPreview, 1, true);
        //        using (ppd)
        //        {
        //            ppd.set_PlotMsgString(
        //              PlotMessageIndex.DialogTitle,
        //              "Custom Preview Progress"
        //            );
        //            ppd.set_PlotMsgString(
        //              PlotMessageIndex.SheetName,
        //              doc.Name.Substring(
        //                doc.Name.LastIndexOf(@"\") + 1
        //              )
        //            );
        //            ppd.set_PlotMsgString(
        //              PlotMessageIndex.CancelJobButtonMessage,
        //              "Cancel Job"
        //            );
        //            ppd.set_PlotMsgString(
        //              PlotMessageIndex.CancelSheetButtonMessage,
        //              "Cancel Sheet"
        //            );
        //            ppd.set_PlotMsgString(
        //              PlotMessageIndex.SheetSetProgressCaption,
        //              "Sheet Set Progress"
        //            );
        //            ppd.set_PlotMsgString(
        //              PlotMessageIndex.SheetProgressCaption,
        //              "Sheet Progress"
        //            );
        //            ppd.LowerPlotProgressRange = 0;
        //            ppd.UpperPlotProgressRange = 100;
        //            ppd.PlotProgressPos = 0;
        //            // Let's start the plot/preview, at last
        //            ppd.OnBeginPlot();
        //            ppd.IsVisible = true;
        //            pe.BeginPlot(ppd, null);
        //            // We'll be plotting/previewing
        //            // a single document
        //            pe.BeginDocument(
        //              pi,
        //              doc.Name,
        //              null,
        //              1,
        //              !isPreview,
        //              filename
        //            );
        //            // Which contains a single sheet
        //            ppd.OnBeginSheet();
        //            ppd.LowerSheetProgressRange = 0;
        //            ppd.UpperSheetProgressRange = 100;
        //            ppd.SheetProgressPos = 0;
        //            PlotPageInfo ppi = new PlotPageInfo();
        //            pe.BeginPage(
        //              ppi,
        //              pi,
        //              true,
        //              null
        //            );
        //            pe.BeginGenerateGraphics(null);
        //            ppd.SheetProgressPos = 50;
        //            pe.EndGenerateGraphics(null);
        //            // Finish the sheet
        //            PreviewEndPlotInfo pepi =
        //              new PreviewEndPlotInfo();
        //            pe.EndPage(pepi);
        //            ret = pepi.Status;
        //            ppd.SheetProgressPos = 100;
        //            ppd.OnEndSheet();
        //            // Finish the document
        //            pe.EndDocument(null);
        //            // And finish the plot
        //            ppd.PlotProgressPos = 100;
        //            ppd.OnEndPlot();
        //            pe.EndPlot(null);
        //        }
        //        // Committing is cheaper than aborting
        //        tr.Commit();
        //    }
        //    return ret;
        //}
        //#endregion


        //#region 多表预览打印

        //[CommandMethod("mprev")]
        //static public void MultiSheetPreview()
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = doc.Editor;
        //    Database db = doc.Database;
        //    ObjectIdCollection layoutsToPlot = new ObjectIdCollection();

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        // First we need to collect the layouts to
        //        // plot/preview in tab order
        //        SortedDictionary<int, ObjectId> layoutDict = new SortedDictionary<int, ObjectId>();
        //        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        //        foreach (ObjectId btrId in bt)
        //        {
        //            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
        //            if (btr.IsLayout && btr.Name.ToUpper() != BlockTableRecord.ModelSpace.ToUpper())
        //            {
        //                // The dictionary we're using will
        //                // sort on the tab order of the layout
        //                Layout lo = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);
        //                layoutDict.Add(lo.TabOrder, btrId);
        //            }
        //        }
        //        // Let's now get the layout IDs and add them to a
        //        // standard ObjectIdCollection
        //        SortedDictionary<int, ObjectId>.ValueCollection vc = layoutDict.Values;
        //        foreach (ObjectId id in vc)
        //        {
        //            layoutsToPlot.Add(id);
        //        }
        //        // Committing is cheaper than aborting
        //        tr.Commit();
        //    }
        //    // PlotEngines do the previewing and plotting
        //    if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
        //    {
        //        int layoutNum = 0;
        //        bool isFinished = false;
        //        bool isReadyForPlot = false;
        //        while (!isFinished)
        //        {
        //            // Create the preview engine with the appropriate
        //            // buttons enabled - this depends on which
        //            // layout in the list is being previewed
        //            PreviewEngineFlags flags = PreviewEngineFlags.Plot;
        //            if (layoutNum > 0)
        //                flags |= PreviewEngineFlags.PreviousSheet;
        //            if (layoutNum < layoutsToPlot.Count - 1)
        //                flags |= PreviewEngineFlags.NextSheet;
        //            PlotEngine pre = PlotFactory.CreatePreviewEngine((int)flags);
        //            using (pre)
        //            {
        //                PreviewEndPlotStatus stat = MultiplePlotOrPreview(pre, true, layoutsToPlot, layoutNum, "");
        //                // We're not checking the list bounds for
        //                // next/previous as the buttons are only shown
        //                // when they can be used
        //                if (stat == PreviewEndPlotStatus.Next)
        //                {
        //                    layoutNum++;
        //                }
        //                else if (stat == PreviewEndPlotStatus.Previous)
        //                {
        //                    layoutNum--;
        //                }
        //                else if (stat == PreviewEndPlotStatus.Normal ||
        //                        stat == PreviewEndPlotStatus.Cancel)
        //                {
        //                    isFinished = true;
        //                }
        //                else if (stat == PreviewEndPlotStatus.Plot)
        //                {
        //                    isFinished = true;
        //                    isReadyForPlot = true;
        //                }
        //            }
        //        }
        //        // If the plot button was used to exit the preview...
        //        if (isReadyForPlot)
        //        {
        //            PlotEngine ple =
        //              PlotFactory.CreatePublishEngine();
        //            using (ple)
        //            {
        //                PreviewEndPlotStatus stat = MultiplePlotOrPreview(ple, false, layoutsToPlot, -1, "D:\\multisheet-previewed-plot");
        //            }
        //        }
        //    }
        //    else
        //    {
        //        ed.WriteMessage("\nAnother plot is in progress."
        //        );
        //    }
        //}
        //static PreviewEndPlotStatus MultiplePlotOrPreview(PlotEngine pe, bool isPreview, ObjectIdCollection layoutSet, int layoutNumIfPreview, string filename)
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = doc.Editor;
        //    Database db = doc.Database;
        //    PreviewEndPlotStatus ret = PreviewEndPlotStatus.Cancel;
        //    ObjectIdCollection layoutsToPlot;
        //    if (isPreview && layoutNumIfPreview >= 0)
        //    {
        //        // Preview is really pre-sheet, so we reduce the
        //        // sheet collection to contain the one we want
        //        layoutsToPlot = new ObjectIdCollection();
        //        layoutsToPlot.Add(layoutSet[layoutNumIfPreview]);
        //    }
        //    else
        //    {
        //        // If we're plotting we need all the sheets,
        //        // so copy the ObjectIds across
        //        ObjectId[] ids = new ObjectId[layoutSet.Count];
        //        layoutSet.CopyTo(ids, 0);
        //        layoutsToPlot = new ObjectIdCollection(ids);
        //    }

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        // Create a Progress Dialog to provide info
        //        // and allow thej user to cancel
        //        PlotProgressDialog ppd = new PlotProgressDialog(isPreview, layoutsToPlot.Count, true);
        //        using (ppd)
        //        {
        //            int numSheet = 1;
        //            foreach (ObjectId btrId in layoutsToPlot)
        //            {
        //                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
        //                Layout lo = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);
        //                // We need a PlotSettings object
        //                // based on the layout settings
        //                // which we then customize
        //                PlotSettings ps =
        //                  new PlotSettings(lo.ModelType);
        //                ps.CopyFrom(lo);
        //                // The PlotSettingsValidator helps
        //                // create a valid PlotSettings object
        //                PlotSettingsValidator psv = PlotSettingsValidator.Current;
        //                // We'll plot the extents, centered and 
        //                // scaled to fit
        //                psv.SetPlotType(ps, PlotType.Extents);
        //                psv.SetUseStandardScale(ps, true);
        //                psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
        //                psv.SetPlotCentered(ps, true);
        //                // We'll use the standard DWFx PC3, as
        //                // this supports multiple sheets
        //                psv.SetPlotConfigurationName(ps, "DWG TO PDF.pc3", "ANSI_A_(8.50_x_11.00_Inches)");
        //                // We need a PlotInfo object
        //                // linked to the layout
        //                PlotInfo pi = new PlotInfo();
        //                pi.Layout = btr.LayoutId;
        //                // Make the layout we're plotting current
        //                LayoutManager.Current.CurrentLayout = lo.LayoutName;
        //                // We need to link the PlotInfo to the
        //                // PlotSettings and then validate it
        //                pi.OverrideSettings = ps;
        //                PlotInfoValidator piv = new PlotInfoValidator();
        //                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
        //                piv.Validate(pi);
        //                // We set the sheet name per sheet
        //                ppd.set_PlotMsgString(PlotMessageIndex.SheetName, doc.Name.Substring(doc.Name.LastIndexOf(@"\") + 1) + " - " + lo.LayoutName);
        //                if (numSheet == 1)
        //                {
        //                    // All other messages get set once
        //                    ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Custom Preview Progress");
        //                    ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
        //                    ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
        //                    ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
        //                    ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
        //                    ppd.LowerPlotProgressRange = 0;
        //                    ppd.UpperPlotProgressRange = 100;
        //                    ppd.PlotProgressPos = 0;
        //                    // Let's start the plot/preview, at last
        //                    ppd.OnBeginPlot();
        //                    ppd.IsVisible = true;
        //                    pe.BeginPlot(ppd, null);
        //                    // We'll be plotting a single document
        //                    pe.BeginDocument(pi, doc.Name, null, 1, !isPreview, filename);
        //                }
        //                // Which may contains multiple sheets
        //                ppd.LowerSheetProgressRange = 0;
        //                ppd.UpperSheetProgressRange = 100;
        //                ppd.SheetProgressPos = 0;
        //                PlotPageInfo ppi = new PlotPageInfo();
        //                pe.BeginPage(ppi, pi, (numSheet == layoutsToPlot.Count), null);
        //                ppd.OnBeginSheet();
        //                pe.BeginGenerateGraphics(null);
        //                ppd.SheetProgressPos = 50;
        //                pe.EndGenerateGraphics(null);
        //                // Finish the sheet
        //                PreviewEndPlotInfo pepi = new PreviewEndPlotInfo();
        //                pe.EndPage(pepi);
        //                ret = pepi.Status;
        //                ppd.SheetProgressPos = 100;
        //                ppd.OnEndSheet();
        //                numSheet++;
        //                // Update the overall progress
        //                ppd.PlotProgressPos += (100 / layoutsToPlot.Count);
        //            }
        //            // Finish the document
        //            pe.EndDocument(null);
        //            // And finish the plot
        //            ppd.PlotProgressPos = 100;
        //            ppd.OnEndPlot();
        //            pe.EndPlot(null);
        //        }
        //    }
        //    return ret;
        //}
        //#endregion



        //#region 选中多个实体打印
        //[CommandMethod("PrintMul")]
        //public void PrintTT()
        //{

        //    Document acDoc = Application.DocumentManager.MdiActiveDocument;
        //    Database acCurDb = acDoc.Database;
        //    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        //    List<String> imagelist = new List<String>();

        //    string directory = @"D:\";//磁盘路径


        //    try
        //    {
        //        if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
        //        {

        //            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
        //            {
        //                int flag = 0;
        //                //获取当前布局管理器变量
        //                LayoutManager acLayoutMgr = LayoutManager.Current;
        //                //获取当前布局变量
        //                Layout acLayout = (Layout)acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout), OpenMode.ForRead);
        //                //Layout acLayout = (Layout)acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout), OpenMode.ForWrite);
        //                //获取当前布局的打印信息
        //                PlotInfo acPlInfo = new PlotInfo()
        //                {
        //                    Layout = acLayout.ObjectId
        //                };


        //                //提示用户输入打印窗口的两个角点
        //                PromptPointResult resultp = ed.GetPoint("\n指定第一个角点");
        //                if (resultp.Status != PromptStatus.OK) return;
        //                Point3d basePt = resultp.Value;
        //                resultp = ed.GetCorner("指定对角点", basePt);
        //                if (resultp.Status != PromptStatus.OK) return;
        //                Point3d cornerPt = resultp.Value;

        //                //选择实体对象
        //               // PromptSelectionOptions result1 = new PromptSelectionOptions();

        //                SelectionFilter frameFilter = new SelectionFilter(
        //                    new TypedValue[]
        //                    { new TypedValue(0, "LWPOLYLINE"),
        //                   new TypedValue(90, 4),
        //                   new TypedValue(70, 1) });


        //                PromptSelectionResult selectedFrameResult = ed.SelectWindow(basePt, cornerPt, frameFilter);
        //               // PromptSelectionResult selectedFrameResult = ed.GetSelection(result1, frameFilter);
        //                PromptSelectionResult selectedFrameResult1 = ed.SelectAll(frameFilter);
        //                if (selectedFrameResult.Status == PromptStatus.OK)
        //                {
        //                    List<ObjectId> selectedObjectIds = new List<ObjectId>(selectedFrameResult.Value.GetObjectIds());
        //                    List<ObjectId> resultObjectIds = new List<ObjectId>(selectedFrameResult.Value.GetObjectIds());
        //                    RemoveInnerPLine(acTrans, ref selectedObjectIds, ref resultObjectIds);
        //                    foreach (ObjectId frameId in resultObjectIds)
        //                    {
        //                        Polyline framePline = acTrans.GetObject(frameId, OpenMode.ForRead) as Polyline;
        //                        framePline.Highlight();
        //                    }


        //                    PlotSettings acPlSet = new PlotSettings(acLayout.ModelType);
        //                    acPlSet.CopyFrom(acLayout);
        //                    //着色打印选项，设置按线框进行打印
        //                    acPlSet.ShadePlot = PlotSettingsShadePlotType.Wireframe;
        //                    PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;
        //                    //打印比例
        //                    //用户标准打印
        //                    acPlSetVdr.SetUseStandardScale(acPlSet, true);
        //                    acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);

        //                    //居中打印
        //                    acPlSetVdr.SetPlotCentered(acPlSet, true);
        //                    //调用GetPlotStyleSheetList之后才可以使用SetCurrentStyleSheet
        //                    System.Collections.Specialized.StringCollection sc = acPlSetVdr.GetPlotStyleSheetList();
        //                    //设置打印样式表
        //                    acPlSetVdr.SetCurrentStyleSheet(acPlSet, "acad.ctb");

        //                    PlotProgressDialog acPlProgDlg = new PlotProgressDialog(false, resultObjectIds.Count, true);
        //                    string imagename = "";
        //                    string[] files = new string[] { };
        //                    List<string> listfile = new List<string>();
        //                    foreach (var frame in resultObjectIds)
        //                    {
        //                        if (!Directory.Exists(directory))
        //                            Directory.CreateDirectory(directory);
        //                        flag++;

        //                        Entity ent = acTrans.GetObject(frame, OpenMode.ForRead) as Entity;





        //                        //比例
        //                        //string entityscale = frame.Scale;
        //                        //横竖
        //                        // string vertial = frame.IsVertial;
        //                        //图片索引
        //                        //string picindex = frame.ImageIndex;


        //                        //if (frame.ImageType == 0)
        //                        //{
        //                        //    imagename = string.Format("1-{0}-{1}-{2}.pdf", entityscale, vertial, picindex);
        //                        //    totalflatimage = directory + imagename;
        //                        //}

        //                        //opt.InitialFileName = fileName + ent.BlockName + "-" + frame + "-" + flag;//缺省保存文件名



        //                        imagename = string.Format("{0}-{1}.pdf", frame, flag);
        //                        //imagelist.Add(directory + imagename);
        //                        listfile.Add(directory + imagename);


        //                        //打印设备和图纸
        //                        //if (vertial == "0")
        //                        //{
        //                        //    //横向
        //                        //    acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG To PDF.pc3", "ISO_expand_A4_(297.00_x_210.00_mm)");
        //                        //    acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);
        //                        //}

        //                        //竖向
        //                        acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG To PDF.pc3", "ISO_expand_A4_(210.00_x_297.00_mm)");
        //                        acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);

        //                        //设置是否使用打印样式
        //                        acPlSet.ShowPlotStyles = true;
        //                        //设置打印区域
        //                        Extents3d extents3d = ent.GeometricExtents;

        //                        ////单位是米时
        //                        //double h = double.Parse(entityscale) * 6;
        //                        //double x = extents3d.MinPoint.X + unit * 2;
        //                        //double y = extents3d.MaxPoint.Y + h + unit;
        //                        //if (unit == 1)
        //                        //{
        //                        //    x = extents3d.MinPoint.X + unit * 1;
        //                        //    y = extents3d.MaxPoint.Y + double.Parse(entityscale) / 250 + unit * 2;
        //                        //}
        //                        Extents2d E2d = new Extents2d(extents3d.MinPoint.X, extents3d.MinPoint.Y, extents3d.MaxPoint.X, extents3d.MaxPoint.Y);
        //                        acPlSetVdr.SetPlotWindowArea(acPlSet, E2d);
        //                        acPlSetVdr.SetPlotType(acPlSet, PlotType.Window);
        //                        //重载和保存打印信息
        //                        acPlInfo.OverrideSettings = acPlSet;
        //                        //验证打印信息设置，看是否有误
        //                        PlotInfoValidator acPlInfoVdr = new PlotInfoValidator();
        //                        acPlInfoVdr.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
        //                        acPlInfoVdr.Validate(acPlInfo);

        //                        while (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)

        //                            continue;
        //                        #region BackUpCode

        //                        //保存App的原参数
        //                        short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
        //                        //设定为前台打印，加快打印速度
        //                        Application.SetSystemVariable("BACKGROUNDPLOT", 0);
        //                        PlotEngine acPlEng1 = PlotFactory.CreatePublishEngine();
        //                        // acPlProgDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, "图片输出");
        //                        // acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "取消输出");
        //                        //acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "输出进度");
        //                        acPlProgDlg.LowerPlotProgressRange = 0;
        //                        acPlProgDlg.UpperPlotProgressRange = 100;
        //                        acPlProgDlg.PlotProgressPos = 0;
        //                        acPlProgDlg.OnBeginPlot();
        //                        acPlProgDlg.IsVisible = true;
        //                        acPlEng1.BeginPlot(acPlProgDlg, null);
        //                        acPlEng1.BeginDocument(acPlInfo, "", null, 1, true, directory + imagename);
        //                        //  acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status, string.Format("正在输出文件"));
        //                        acPlProgDlg.OnBeginSheet();
        //                        acPlProgDlg.LowerSheetProgressRange = 0;
        //                        acPlProgDlg.UpperSheetProgressRange = 100;
        //                        acPlProgDlg.SheetProgressPos = 0;
        //                        PlotPageInfo acPlPageInfo = new PlotPageInfo();
        //                        acPlEng1.BeginPage(acPlPageInfo, acPlInfo, true, null);
        //                        acPlEng1.BeginGenerateGraphics(null);
        //                        acPlEng1.EndGenerateGraphics(null);
        //                        acPlEng1.EndPage(null);
        //                        acPlProgDlg.SheetProgressPos = 100;
        //                        acPlProgDlg.OnEndSheet();
        //                        acPlEng1.EndDocument(null);
        //                        acPlProgDlg.PlotProgressPos = 100;
        //                        acPlProgDlg.OnEndPlot();
        //                        acPlEng1.EndPlot(null);
        //                        acPlEng1.Dispose();
        //                        acPlEng1.Destroy();
        //                        Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);

        //                        #endregion

        //                    }


        //                    files = listfile.ToArray();
        //                    PlotConfig config = PlotConfigManager.CurrentConfig;
        //                    //获取去除扩展名后的文件名（不含路径）
        //                    string fileName = SymbolUtilityServices.GetSymbolNameFromPathName(acDoc.Name, "dwg");
        //                    //定义保存文件对话框
        //                    PromptSaveFileOptions opt = new PromptSaveFileOptions("文件名")
        //                    {
        //                        //保存文件对话框的文件扩展名列表
        //                        Filter = "*" + config.DefaultFileExtension + "|*" + config.DefaultFileExtension,
        //                        DialogCaption = "浏览打印文件",//保存文件对话框的标题
        //                        InitialDirectory = @"D:\",//缺省保存目录
        //                        InitialFileName = fileName + "-" + acLayout.LayoutName//缺省保存文件名
        //                    };
        //                    //根据保存对话框中用户的选择，获取保存文件名
        //                    PromptFileNameResult result = ed.GetFileNameForSave(opt);
        //                    if (result.Status != PromptStatus.OK) return;
        //                    fileName = result.StringResult;

        //                    //string fileName = @"D:\输出.pdf";
        //                    PdfDocumentBase docx = PdfDocument.MergeFiles(files);
        //                    docx.Save(fileName, FileFormat.PDF);
        //                    System.Diagnostics.Process.Start(fileName);

        //                    ed.WriteMessage(listfile.ToString());

        //                    //保存App的原参数
        //                    short bgPlot1 = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
        //                    //设定为前台打印，加快打印速度
        //                    Application.SetSystemVariable("BACKGROUNDPLOT", 0);
        //                    PlotEngine acPlEng = PlotFactory.CreatePublishEngine();
        //                    acPlProgDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, "图片输出");
        //                    acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "取消输出");
        //                    acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "输出进度");
        //                    acPlProgDlg.LowerPlotProgressRange = 0;
        //                    acPlProgDlg.UpperPlotProgressRange = 100;
        //                    acPlProgDlg.PlotProgressPos = 0;
        //                    acPlProgDlg.OnBeginPlot();
        //                    acPlProgDlg.IsVisible = true;
        //                    acPlEng.BeginPlot(acPlProgDlg, null);
        //                    acPlEng.BeginDocument(acPlInfo, "", null, 1, true, directory + imagename);
        //                    acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status, string.Format("正在输出文件"));
        //                    acPlProgDlg.OnBeginSheet();
        //                    acPlProgDlg.LowerSheetProgressRange = 0;
        //                    acPlProgDlg.UpperSheetProgressRange = 100;
        //                    acPlProgDlg.SheetProgressPos = 0;
        //                    PlotPageInfo acPlPageInfo1 = new PlotPageInfo();
        //                    acPlEng.BeginPage(acPlPageInfo1, acPlInfo, true, null);
        //                    acPlEng.BeginGenerateGraphics(null);
        //                    acPlEng.EndGenerateGraphics(null);
        //                    acPlEng.EndPage(null);
        //                    acPlProgDlg.SheetProgressPos = 100;
        //                    acPlProgDlg.OnEndSheet();
        //                    acPlEng.EndDocument(null);
        //                    acPlProgDlg.PlotProgressPos = 100;
        //                    acPlProgDlg.OnEndPlot();
        //                    acPlEng.EndPlot(null);
        //                    acPlEng.Dispose();
        //                    acPlEng.Destroy();
        //                    Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot1);

        //                    acPlProgDlg.Dispose();
        //                    acPlProgDlg.Destroy();

        //                    for (int i = 0; i  <files.Length; i++)
        //                    {
        //                        File.Delete(files[i]);
        //                    }



        //                }
        //                while (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
        //                    continue;
        //                //MessageBox.Show("打印完成！");
        //            }
        //        }
        //        else
        //        {
        //            ed.WriteMessage( "\n另一个打印进程正在进行中.");
        //        }
        //    }



        //    catch (System.Exception)
        //    {

        //        throw;
        //    }


        //}

        //#endregion




    }




}

