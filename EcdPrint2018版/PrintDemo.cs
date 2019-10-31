using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;


namespace PrintTest
{
    class PrintDemo
    {
        

            /// <summary>
            /// 设置打印信息
            /// </summary>
            /// <param name="layoutId">布局ID</param>
            /// <param name="plotArea">该布局中的一个区域</param>
            /// <param name="plotDevice">打印设备名</param>
            /// <param name="plotCanonicalMeida">标准打印介质名</param>
            /// <param name="plotStyle">打印样式</param>
            /// <param name="isSinglePage">是否只打印单页</param>
            /// <returns></returns>
      private static PlotInfo SetPlotInfo(Layout lo, Extents2d plotArea,string plotDevice, string plotCanonicalMeida, string plotStyle, bool isSinglePage)
        {
            PlotInfo pi = new PlotInfo();
            pi.Layout = lo.Id;

            //获取当前布局的打印信息
            PlotSettings ps = new PlotSettings(lo.ModelType);//是否模型空间
            ps.CopyFrom(lo);

            //着色打印选项，设置按线框进行打印
            ps.ShadePlot = PlotSettingsShadePlotType.Wireframe;

            //获取当前打印设置校验器
            PlotSettingsValidator psv = PlotSettingsValidator.Current;

            #region 以下这些设置请不要改变顺序！！！
            //以下2句顺序不能换！
            psv.SetPlotWindowArea(ps, plotArea);//设置打印区域            
            psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);//设置为窗口打印模式

            //设置布满图纸打印
            psv.SetUseStandardScale(ps, true);//需要?
            psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);//布满图纸

            //设置居中打印
            psv.SetPlotCentered(ps, true);

            //设置打印样式
            try
            {
                psv.SetCurrentStyleSheet(ps, plotStyle);//设置打印样式(笔宽等)(为什么有时会出错？PS:不能与原样式形同？！！！)
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
               // MessageBox.Show(string.Format("{0}\n当前打印样式:{1}\n设置打印样式:{2}", e.Message, ps.CurrentStyleSheet, plotStyle), "设置打印样式出错");
            }

            //配置打印机和打印介质
            psv.SetPlotConfigurationName(ps, plotDevice, plotCanonicalMeida);
            psv.RefreshLists(ps);

            //设置打印单位
            try
            {
                psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);//(为什么有时会出错？)            
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                //MessageBox.Show(string.Format("{0}\n当前尺寸单位:{1}\n设置单位:{2}", e.Message, ps.PlotPaperUnits, PlotPaperUnit.Millimeters), "设置尺寸单位出错");
            }

            //设置旋转角度（打印到同一文档时必须设置为同一旋转角）
            if (isSinglePage)
            {
                if ((plotArea.MaxPoint.X - plotArea.MinPoint.X) > (plotArea.MaxPoint.Y - plotArea.MinPoint.Y))
                {
                    if (ps.PlotPaperSize.X > ps.PlotPaperSize.Y)
                    {
                        psv.SetPlotRotation(ps, PlotRotation.Degrees000);
                    }
                    else
                    {
                        psv.SetPlotRotation(ps, PlotRotation.Degrees090);
                    }
                }
                else
                {
                    if (ps.PlotPaperSize.X > ps.PlotPaperSize.Y)
                    {
                        psv.SetPlotRotation(ps, PlotRotation.Degrees090);
                    }
                    else
                    {
                        psv.SetPlotRotation(ps, PlotRotation.Degrees000);
                    }
                }
            }
            else
            {
                //多页打印必须设置为统一旋转角度（否则打印会出错，出错信息：eValidePlotInfo！特别注意！！！）
                psv.SetPlotRotation(ps, PlotRotation.Degrees000);
            }
            #endregion

            pi.OverrideSettings = ps;//将PlotSetting与PlotInfo关联

            PlotInfoValidator piv = new PlotInfoValidator();
            piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
            piv.Validate(pi);//激活打印设置

            ps.Dispose();

            return pi;
        }
        /// <summary>
        /// 打印预览
        /// </summary>
        /// <returns></returns>
        public static bool Preview( Dictionary<ObjectId, List<Extents2d>> plotAreaDict, string plotDevice, string plotCanonicalMeida, string plotStyle, string saveFileName, bool isPlotSingle//是否每页单独保存
)
        {
            
            Database db = HostApplicationServices.WorkingDatabase;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            bool ret = false;

            if (plotAreaDict.Count == 0) return true;

            #region 准备打印区域列表 PlotList
            Dictionary<Extents2d, ObjectId> PlotList = new Dictionary<Extents2d, ObjectId>();
            foreach (KeyValuePair<ObjectId, List<Extents2d>> kv in plotAreaDict)
            {
                foreach (Extents2d plotArea in kv.Value)
                {
                    PlotList.Add(plotArea, kv.Key);
                }
            }
            #endregion

            if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
            {
                #region 设置为前台打印
                //保存App的原参数
                short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
                //设定为前台打印，加快打印速度
                Application.SetSystemVariable("BACKGROUNDPLOT", 0);
                //object BackGroundPlotVar = mFun.m_GetSystemVar("BACKGROUNDPLOT");
                //mFun.m_SetSystemVar("BACKGROUNDPLOT", 0);//一定要设置成0(前台打印)，否则保存PDF文件中一个布局只会有一张图！！！！
                #endregion

                int sheetNum = 0;
                bool isFinished = false;//预览是否结束
                bool isReadyForPlot = false;//是否准备好打印

                while (!isFinished)
                {
                    PreviewEngineFlags flags = PreviewEngineFlags.Plot;
                    if (sheetNum > 0)
                        flags |= PreviewEngineFlags.PreviousSheet;
                    if (sheetNum < PlotList.Count - 1)
                        flags |= PreviewEngineFlags.NextSheet;

                    using (PlotEngine pe = PlotFactory.CreatePreviewEngine((int)flags))
                    {
                    PreviewEndPlotStatus stat = MultiPlotOrPreview( pe, true, PlotList, sheetNum, plotAreaDict.Count, plotDevice, plotCanonicalMeida, plotStyle, saveFileName);

                        if (stat == PreviewEndPlotStatus.Next)
                        {
                            sheetNum++;
                        }
                        else if (stat == PreviewEndPlotStatus.Previous)
                        {
                            sheetNum--;
                        }
                        else if (stat == PreviewEndPlotStatus.Normal ||
                                stat == PreviewEndPlotStatus.Cancel)
                        {
                            isFinished = true;
                        }
                        else if (stat == PreviewEndPlotStatus.Plot)
                        {
                            isFinished = true;
                            isReadyForPlot = true;

                            ret = true;//结束
                        }
                    }
                }

                // If the plot button was used to exit the preview...

                if (isReadyForPlot)
                {
                    if (!isPlotSingle)
                    {
                        using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                        {
                            PreviewEndPlotStatus stat = MultiPlotOrPreview( pe, false, PlotList, -1, plotAreaDict.Count,
                                plotDevice, plotCanonicalMeida, plotStyle, saveFileName
                              );

                            ret = stat == PreviewEndPlotStatus.Cancel ? false : true;
                        }
                    }
                    else
                    {
                        #region 每页打印成一个PDF文件

                        
                        foreach (KeyValuePair<ObjectId, List<Extents2d>> kv in plotAreaDict)
                        {
                            int i = 1;
                            foreach (Extents2d plotArea in kv.Value)
                            {
                              PlotSinglePage(
                                    kv.Key, plotArea, plotDevice, plotCanonicalMeida, plotStyle,
                                    string.Format("{0}-{1}({2})", saveFileName, wmLayout.GetLayoutName(kv.Key), i++));
                            }
                        }
                        #endregion
                    }
                }

                //恢复变量
                Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);
            }
            else
            {
                ed.WriteMessage("\n其他打印正在进行中！");
            }

            return ret;
        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="plotAreaDict"></param>
        /// <param name="plotDevice"></param>
        /// <param name="plotCanonicalMeida"></param>
        /// <param name="plotStyle"></param>
        /// <param name="saveFileName"></param>
        /// <returns></returns>
        public static bool Plot(
            Dictionary<ObjectId, List<Extents2d>> plotAreaDict,
            string plotDevice,
            string plotCanonicalMeida,
            string plotStyle,
            string saveFileName
            )
        {
            bool ret = true;

            if (plotAreaDict.Count == 0) return true;

            #region 准备打印区域列表 PlotList
            Dictionary<Extents2d, ObjectId> PlotList = new Dictionary<Extents2d, ObjectId>();
            foreach (KeyValuePair<ObjectId, List<Extents2d>> kv in plotAreaDict)
            {
                foreach (Extents2d plotArea in kv.Value)
                {
                    PlotList.Add(plotArea, kv.Key);
                }
            }
            #endregion

            if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
            {
                #region 设置为前台打印
                object BackGroundPlotVar = mFun.m_GetSystemVar("BACKGROUNDPLOT");
                mFun.m_SetSystemVar("BACKGROUNDPLOT", 0);//一定要设置成0(前台打印)，否则保存PDF文件中一个布局只会有一张图！！！！
                #endregion

                using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                {
                    PreviewEndPlotStatus stat = MultiPlotOrPreview(
                        pe, false, PlotList, -1, plotAreaDict.Count,
                        plotDevice, plotCanonicalMeida, plotStyle, saveFileName
                      );

                    ret = stat == PreviewEndPlotStatus.Cancel ? false : true;
                }

                mFun.m_SetSystemVar("BACKGROUNDPLOT", BackGroundPlotVar);
            }
            else
            {
                mCommands.m_ed.WriteMessage("\n其他打印正在进行中！");
            }

            return ret;
        }
        /// <summary>
        /// 单页预览
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="plotArea"></param>
        /// <param name="plotDevice"></param>
        /// <param name="plotCanonicalMeida"></param>
        /// <param name="plotStyle"></param>
        public static void PreviewSinglePage(
            ObjectId layoutId,
            Extents2d plotArea,
            string plotDevice,
            string plotCanonicalMeida,
            string plotStyle
            )
        {
            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
            {
                MessageBox.Show($"当前不能预览，另一个打印任务正在进行中！");
                return;
            }

            using (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Layout lo = mFun.m_OpenEntity(layoutId) as Layout;

                if (LayoutManager.Current.CurrentLayout != lo.LayoutName)
                {
                    LayoutManager.Current.CurrentLayout = lo.LayoutName;
                }

                using (PlotProgressDialog ppd = new PlotProgressDialog(true, 1, false))
                {
                    #region 设置进度条信息

                    #endregion 

                    using (PlotEngine pe = PlotFactory.CreatePreviewEngine(0))
                    {
                        //设置打印配置参数
                        PlotInfo pi = SetPlotInfo(lo, plotArea, plotDevice, plotCanonicalMeida, plotStyle, true);

                        pe.BeginPlot(ppd, null);

                        pe.BeginDocument(pi, mCommands.Doc.Name, null, 1, false, "");

                        pe.BeginPage(new PlotPageInfo(), pi, true, null);

                        pe.BeginGenerateGraphics(null);

                        pe.EndGenerateGraphics(null);

                        PreviewEndPlotInfo pepi = new PreviewEndPlotInfo();
                        pe.EndPage(pepi);//结束本页打印，返回预览状态

                        pe.EndDocument(null);

                        pe.EndPlot(null);
                    }
                }
            }
        }

        /// <summary>
        /// 多页打印/预览函数
        /// </summary>
        /// <param name="pe"></param>
        /// <param name="isPreview"></param>
        /// <param name="plotList"></param>
        /// <param name="sheetNumIfPreview"></param>
        /// <param name="layoutCount"></param>
        /// <param name="plotDevice"></param>
        /// <param name="plotCanonicalMeida"></param>
        /// <param name="plotStyle"></param>
        /// <param name="saveFileName"></param>
        /// <returns></returns>
        private static PreviewEndPlotStatus MultiPlotOrPreview(
        PlotEngine pe,
        bool isPreview,
        Dictionary<Extents2d, ObjectId> plotList,
        int sheetNumIfPreview,
        int layoutCount,//布局总数
        string plotDevice,
        string plotCanonicalMeida,
        string plotStyle,
        string saveFileName
        )
        {
            PreviewEndPlotStatus ret = PreviewEndPlotStatus.Cancel;

            string DocName = mCommands.Doc.Name;
            DocName = DocName.Substring(DocName.LastIndexOf("\") + 1);
            DocName = DocName.Substring(0, DocName.LastIndexOf("."));

            #region 准备打印区域列表plotAreaList
            Dictionary<Extents2d, ObjectId> plotAreaList = new Dictionary<Extents2d, ObjectId>();
            if (isPreview && sheetNumIfPreview >= 0)
            {
                KeyValuePair<Extents2d, ObjectId> kv = plotList.ElementAt(sheetNumIfPreview);
                plotAreaList.Add(kv.Key, kv.Value);//预览只能一个区域 
            }
            else
            {
                plotAreaList = plotList;//打印全部区域
            }
            #endregion

            using (Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                try
                {
                    using (PlotProgressDialog ppd = new PlotProgressDialog(isPreview, plotAreaList.Count, false))
                    {
                        #region 设置进度条显示信息                                    
                        ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "转换进度");
                        ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "本布局转换进度：__/__");
                        ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "总转换进度: __/__");

                        ppd.LowerPlotProgressRange = 0;
                        ppd.UpperPlotProgressRange = plotAreaList.Count;

                        //显示进度条对话框
                        ppd.OnBeginPlot();
                        ppd.IsVisible = true;
                        #endregion

                        int pageNum = 0;//布局打印页计数
                        int layoutPageNum = 0;//当前布局总打印页数(区域数)
                        int sheetNum = 0;//所有打印页计数(打印总区域数)
                        ObjectId layoutId = ObjectId.Null;//当前布局Id

                        Layout lo = null;
                        foreach (KeyValuePair<Extents2d, ObjectId> kv in plotAreaList)
                        {
                            if (kv.Value != layoutId)
                            {
                                layoutId = kv.Value;

                                lo = mFun.m_OpenEntity(layoutId) as Layout;
                                LayoutManager.Current.CurrentLayout = lo.LayoutName;//切换为当前布局,是否必须?!!

                                pageNum = 0;//重置布局页计数

                                layoutPageNum = plotAreaList.Count(a => a.Value == layoutId);

                                ppd.LowerSheetProgressRange = 0;
                                ppd.UpperSheetProgressRange = layoutPageNum;
                            }

                            pageNum++;//布局页计数+1
                            sheetNum++;//总打印区域计数+1                    

                            ppd.set_PlotMsgString(PlotMessageIndex.SheetName, $"{DocName}-{lo.LayoutName}");//打印文件名-布局名

                            //设置打印配置参数
                            PlotInfo pi = SetPlotInfo(lo, kv.Key, plotDevice, plotCanonicalMeida, plotStyle, isPreview);

                            #region 启动打印
                            if (sheetNum == 1)
                            {
                                pe.BeginPlot(ppd, null);

                                pe.BeginDocument(
                                    pi,                                     //打印信息
                                    mCommands.Doc.Name,                     //当前图纸名
                                    null,
                                    1,                                      //打印份数
                                    !isPreview,                             //是否打印至文件
                                    isPreview ? "" : saveFileName           //保存文件名
                                    );
                            }
                            #endregion

                            #region 开始打印
                            ppd.OnBeginSheet();

                            ppd.SheetProgressPos = pageNum;
                            ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, $"本布局转换进度：{pageNum}/{layoutPageNum}");

                            ppd.PlotProgressPos = sheetNum;
                            ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, $"总转换进度：sheetNum}/{plotAreaList.Count}");

                            pe.BeginPage(
                                new PlotPageInfo(),
                                pi,                                 //打印信息
                                sheetNum == plotAreaList.Count,     //是否最后一页
                                null
                                );

                            pe.BeginGenerateGraphics(null);
                            pe.EndGenerateGraphics(null);

                            PreviewEndPlotInfo pepi = new PreviewEndPlotInfo();
                            pe.EndPage(pepi);//结束本页打印，返回预览状态
                            ret = pepi.Status;
                            #endregion
                        }

                        #region 结束打印
                        ppd.OnEndSheet();
                        pe.EndDocument(null);

                        ppd.OnEndPlot();
                        pe.EndPlot(null);
                        #endregion
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception e)
                {
                    MessageBox.Show($"转换为单个PDF文档出错: {e.Message}！\n请选择【单幅分别保存】进行转换。", "转换出错", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    ret = PreviewEndPlotStatus.Cancel;
                }
            }

            return ret;
        }
        /// <summary>
        /// 单页打印
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="plotArea"></param>
        /// <param name="plotDevice"></param>
        /// <param name="plotCanonicalMeida"></param>
        /// <param name="plotStyle"></param>
        /// <param name="saveFilName"></param>
        public static void PlotSinglePage(
            ObjectId layoutId,
            Extents2d plotArea,
            string plotDevice,
            string plotCanonicalMeida,
            string plotStyle,
            string saveFilName)
        {
            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
            {
                MessageBox.Show($"当前不能打印，另一个打印任务正在进行中！");
                return;
            }

            using (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Layout lo = mFun.m_OpenEntity(layoutId) as Layout;

                if (LayoutManager.Current.CurrentLayout != lo.LayoutName)
                {
                    LayoutManager.Current.CurrentLayout = lo.LayoutName;
                }

                using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, false))
                {
                    #region 设置进度条信息

                    #endregion 

                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        //设置打印配置参数
                        PlotInfo pi = SetPlotInfo(lo, plotArea, plotDevice, plotCanonicalMeida, plotStyle, true);

                        pe.BeginPlot(ppd, null);

                        pe.BeginDocument(pi, mCommands.Doc.Name, null, 1, true, saveFilName);

                        pe.BeginPage(new PlotPageInfo(), pi, true, null);

                        pe.BeginGenerateGraphics(null);

                        pe.EndGenerateGraphics(null);

                        pe.EndPage(null);

                        pe.EndDocument(null);

                        pe.EndPlot(null);
                    }
                }
            }
        }
    }
}