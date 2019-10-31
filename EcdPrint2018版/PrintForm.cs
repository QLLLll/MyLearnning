using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Spire.Pdf;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using System.IO;

namespace EcdPrint
{
    public partial class PrintForm : Form
    {

        PlotSettingsEx ps = null;//声明增强型打印设置对象
        Layout layout = null;//当前布局对象
        public PrintForm()
        {
            InitializeComponent();

            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayoutManager lm = LayoutManager.Current;//获取当前布局管理器
                //获取当前布局
                ObjectId layoutId = lm.GetLayoutId(lm.CurrentLayout);
                layout = (Layout)layoutId.GetObject(OpenMode.ForRead);
                //获取当前布局名称
                string layoutName = layout.ModelType ? "模型" : layout.LayoutName;
                this.Text = "打印 - " + layoutName;//设置窗口标题
                ps = new PlotSettingsEx(layout);//创建增强型打印设置对象
                trans.Commit();
            }
        }

        private void btn_Print_Click(object sender, EventArgs e)
        {
            this.Hide();
            PrintTT();
            
        }

        private void PrintForm_Load(object sender, EventArgs e)
        {
            //绑定图纸尺寸组合框
            this.comboBoxMedia.DataSource = ps.MediaList;//设置“图纸尺寸列表”组合框的数据源
            this.comboBoxMedia.DataBindings.Add("SelectedItem", ps, "CanonicalMediaName", true, DataSourceUpdateMode.OnPropertyChanged);
            //绑定打印样式表组合框
            this.comboBoxStyleSheet.DataSource = ps.ColorDependentPlotStyles;
            this.comboBoxStyleSheet.DataBindings.Add("SelectedItem", ps, "CurrentStyleSheet", true, DataSourceUpdateMode.OnPropertyChanged);
            //绑定图形方向为横向的单选按钮
            this.radioButtonHorizontal.DataBindings.Add("Checked", ps, "PlotHorizontal", true, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void control_ValueChanged(object sender, EventArgs e)
        {
            //当控件的值改变，表示用户修改了打印设置，则“应用到布局”按钮变为可用状态
            LayoutManager lm = LayoutManager.Current;//获取当前布局管理器
                                                     //获取当前布局的ObjectId
            ObjectId layoutId = lm.GetLayoutId(lm.CurrentLayout);
            ps.UpdatePlotSettings(layoutId);//更新当前布局的打印设置
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Dispose();//销毁窗体
        }

        private void comboBox_UpdateDataBinding(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox == null) return;//若控件不是复选框，则返回
            //使用LINQ筛选复选框绑定属性为SelectedItem的绑定对象
            var binds = from Binding b in comboBox.DataBindings
                        where b.PropertyName == "SelectedItem"
                        select b;
            foreach (var bind in binds)
            {
                //读取复选框的“当前选定项”的属性值，将其写入绑定数据源
                bind.WriteValue();
            }
        }

      

        public void PrintTT()
        {

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            List<String> imagelist = new List<String>();

            string directory = @"D:\";//磁盘路径
            string   MediaName = comboBoxMedia.SelectedItem.ToString().Replace(" ", "_").Replace("毫米", "MM").Replace("英寸", "Inches").Replace("像素", "Pixels");
           
            try
            {
                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {

                    using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                    {
                        int flag = 0;
                        //获取当前布局管理器变量
                        LayoutManager acLayoutMgr = LayoutManager.Current;
                        //获取当前布局变量
                        Layout acLayout = (Layout)acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout), OpenMode.ForRead);
                        //Layout acLayout = (Layout)acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout), OpenMode.ForWrite);
                        //获取当前布局的打印信息
                        PlotInfo acPlInfo = new PlotInfo()
                        {
                            Layout = acLayout.ObjectId
                        };




                        //提示用户输入打印窗口的两个角点
                        PromptPointResult resultp = ed.GetPoint("\n指定第一个角点");
                        if (resultp.Status != PromptStatus.OK) return;
                        Point3d basePt = resultp.Value;
                        resultp = ed.GetCorner("指定对角点", basePt);
                        if (resultp.Status != PromptStatus.OK) return;
                        Point3d cornerPt = resultp.Value;

                        //选择实体对象
                        // PromptSelectionOptions result1 = new PromptSelectionOptions();

                        SelectionFilter frameFilter = new SelectionFilter(
                            new TypedValue[]
                            { new TypedValue(0, "LWPOLYLINE"),
                           new TypedValue(90, 4),
                           new TypedValue(70, 1) });


                        PromptSelectionResult selectedFrameResult = ed.SelectWindow(basePt, cornerPt, frameFilter);
                        // PromptSelectionResult selectedFrameResult = ed.GetSelection(result1, frameFilter);
                        PromptSelectionResult selectedFrameResult1 = ed.SelectAll(frameFilter);
                        if (selectedFrameResult.Status == PromptStatus.OK)
                        {
                            List<ObjectId> selectedObjectIds = new List<ObjectId>(selectedFrameResult.Value.GetObjectIds());
                            List<ObjectId> resultObjectIds = new List<ObjectId>(selectedFrameResult.Value.GetObjectIds());
                            RemoveInnerPLine(acTrans, ref selectedObjectIds, ref resultObjectIds);
                            foreach (ObjectId frameId in resultObjectIds)
                            {
                                Polyline framePline = acTrans.GetObject(frameId, OpenMode.ForRead) as Polyline;
                                framePline.Highlight();
                            }


                            PlotSettings acPlSet = new PlotSettings(acLayout.ModelType);
                            acPlSet.CopyFrom(acLayout);
                            //着色打印选项，设置按线框进行打印
                            acPlSet.ShadePlot = PlotSettingsShadePlotType.Wireframe;
                            PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;
                            //打印比例
                            //用户标准打印
                            acPlSetVdr.SetUseStandardScale(acPlSet, true);
                            acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);

                            //居中打印
                            acPlSetVdr.SetPlotCentered(acPlSet, true);
                            //调用GetPlotStyleSheetList之后才可以使用SetCurrentStyleSheet
                            System.Collections.Specialized.StringCollection sc = acPlSetVdr.GetPlotStyleSheetList();
                            //设置打印样式表
                            if (comboBoxStyleSheet.SelectedItem.ToString() == "无")
                            {

                                acPlSetVdr.SetCurrentStyleSheet(acPlSet, "acad.ctb");


                            }

                            else
                            { acPlSetVdr.SetCurrentStyleSheet(acPlSet, comboBoxStyleSheet.SelectedItem.ToString()); }

                            //选择方向
                            if (radioButtonHorizontal.Checked)
                            {
                                //横向
                                acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG To PDF.pc3", MediaName);
                                acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees090);
                            }

                            //竖向
                            if (radioButtonVertical.Checked)
                            {
                                acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG To PDF.pc3", MediaName);//获取打印图纸尺寸ComboxMedia
                                acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);
                            }
                            PlotProgressDialog acPlProgDlg = new PlotProgressDialog(false, resultObjectIds.Count, true);
                            string imagename = "";
                            string[] files = new string[] { };
                            List<string> listfile = new List<string>();
                            foreach (var frame in resultObjectIds)
                            {
                                if (!Directory.Exists(directory))
                                    Directory.CreateDirectory(directory);
                                flag++;

                                Entity ent = acTrans.GetObject(frame, OpenMode.ForRead) as Entity;

                                imagename = string.Format("{0}-{1}.pdf", frame, flag);
                                //imagelist.Add(directory + imagename);
                                listfile.Add(directory + imagename);

                              
                                //设置是否使用打印样式
                                acPlSet.ShowPlotStyles = true;
                                //设置打印区域
                                Extents3d extents3d = ent.GeometricExtents;

                                Extents2d E2d = new Extents2d(extents3d.MinPoint.X, extents3d.MinPoint.Y, extents3d.MaxPoint.X, extents3d.MaxPoint.Y);
                                acPlSetVdr.SetPlotWindowArea(acPlSet, E2d);
                                acPlSetVdr.SetPlotType(acPlSet, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                                //重载和保存打印信息
                                acPlInfo.OverrideSettings = acPlSet;
                                //验证打印信息设置，看是否有误
                                PlotInfoValidator acPlInfoVdr = new PlotInfoValidator();
                                acPlInfoVdr.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                                acPlInfoVdr.Validate(acPlInfo);

                                while (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)

                                    continue;
                                #region BackUpCode

                                //保存App的原参数
                                short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
                                //设定为前台打印，加快打印速度
                                Application.SetSystemVariable("BACKGROUNDPLOT", 0);
                                PlotEngine acPlEng1 = PlotFactory.CreatePublishEngine();
                                // acPlProgDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, "图片输出");
                                // acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "取消输出");
                                //acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "输出进度");
                                acPlProgDlg.LowerPlotProgressRange = 0;
                                acPlProgDlg.UpperPlotProgressRange = 100;
                                acPlProgDlg.PlotProgressPos = 0;
                                acPlProgDlg.OnBeginPlot();
                                acPlProgDlg.IsVisible = true;
                                acPlEng1.BeginPlot(acPlProgDlg, null);
                                acPlEng1.BeginDocument(acPlInfo, "", null, 1, true, directory + imagename);
                                //  acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status, string.Format("正在输出文件"));
                                acPlProgDlg.OnBeginSheet();
                                acPlProgDlg.LowerSheetProgressRange = 0;
                                acPlProgDlg.UpperSheetProgressRange = 100;
                                acPlProgDlg.SheetProgressPos = 0;
                                PlotPageInfo acPlPageInfo = new PlotPageInfo();
                                acPlEng1.BeginPage(acPlPageInfo, acPlInfo, true, null);
                                acPlEng1.BeginGenerateGraphics(null);
                                acPlEng1.EndGenerateGraphics(null);
                                acPlEng1.EndPage(null);
                                acPlProgDlg.SheetProgressPos = 100;
                                acPlProgDlg.OnEndSheet();
                                acPlEng1.EndDocument(null);
                                acPlProgDlg.PlotProgressPos = 100;
                                acPlProgDlg.OnEndPlot();
                                acPlEng1.EndPlot(null);
                                acPlEng1.Dispose();
                                acPlEng1.Destroy();
                                Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);

                                #endregion

                            }


                            files = listfile.ToArray();
                            PlotConfig config = PlotConfigManager.CurrentConfig;
                            //获取去除扩展名后的文件名（不含路径）
                            string fileName = SymbolUtilityServices.GetSymbolNameFromPathName(acDoc.Name, "dwg");
                            //定义保存文件对话框
                            PromptSaveFileOptions opt = new PromptSaveFileOptions("文件名")
                            {
                                //保存文件对话框的文件扩展名列表
                                Filter = "*" + config.DefaultFileExtension + "|*" + config.DefaultFileExtension,
                                DialogCaption = "浏览打印文件",//保存文件对话框的标题
                                InitialDirectory = @"D:\",//缺省保存目录
                                InitialFileName = fileName + "-" + acLayout.LayoutName//缺省保存文件名
                            };
                            //根据保存对话框中用户的选择，获取保存文件名
                            PromptFileNameResult result = ed.GetFileNameForSave(opt);
                            if (result.Status != PromptStatus.OK) return;
                            fileName = result.StringResult;

                            //string fileName = @"D:\输出.pdf";
                            PdfDocumentBase docx = PdfDocument.MergeFiles(files);
                            docx.Save(fileName, FileFormat.PDF);
                            System.Diagnostics.Process.Start(fileName);

                            

                            //保存App的原参数
                            short bgPlot1 = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
                            //设定为前台打印，加快打印速度
                            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
                            PlotEngine acPlEng = PlotFactory.CreatePublishEngine();
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, "图片输出");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "取消输出");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "输出进度");
                            acPlProgDlg.LowerPlotProgressRange = 0;
                            acPlProgDlg.UpperPlotProgressRange = 100;
                            acPlProgDlg.PlotProgressPos = 0;
                            acPlProgDlg.OnBeginPlot();
                            acPlProgDlg.IsVisible = true;
                            acPlEng.BeginPlot(acPlProgDlg, null);
                            acPlEng.BeginDocument(acPlInfo, "", null, 1, true, directory + imagename);
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status, string.Format("正在输出文件"));
                            acPlProgDlg.OnBeginSheet();
                            acPlProgDlg.LowerSheetProgressRange = 0;
                            acPlProgDlg.UpperSheetProgressRange = 100;
                            acPlProgDlg.SheetProgressPos = 0;
                            PlotPageInfo acPlPageInfo1 = new PlotPageInfo();
                            acPlEng.BeginPage(acPlPageInfo1, acPlInfo, true, null);
                            acPlEng.BeginGenerateGraphics(null);
                            acPlEng.EndGenerateGraphics(null);
                            acPlEng.EndPage(null);
                            acPlProgDlg.SheetProgressPos = 100;
                            acPlProgDlg.OnEndSheet();
                            acPlEng.EndDocument(null);
                            acPlProgDlg.PlotProgressPos = 100;
                            acPlProgDlg.OnEndPlot();
                            acPlEng.EndPlot(null);
                            acPlEng.Dispose();
                            acPlEng.Destroy();
                            Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot1);

                            acPlProgDlg.Dispose();
                            acPlProgDlg.Destroy();

                            for (int i = 0; i < files.Length; i++)
                            {
                                File.Delete(files[i]);
                            }



                        }
                        while (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
                            continue;
                        //MessageBox.Show("打印完成！");
                    }
                }
                else
                {
                    ed.WriteMessage("\n另一个打印进程正在进行中.");
                }
            }



            catch (System.Exception)
            {

                throw;
            }


        }

      

        void RemoveInnerPLine(Transaction tr, ref List<ObjectId> selectedObjectIds, ref List<ObjectId> resultObjectIds)
        {
            ObjectId outerPlineId = selectedObjectIds[0];
            selectedObjectIds.RemoveAt(0);
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Polyline outerPline = tr.GetObject(outerPlineId, OpenMode.ForRead) as Polyline;
            SelectionFilter frameFilter = new SelectionFilter(new TypedValue[] { new TypedValue(0, "LWPOLYLINE"), new TypedValue(90, 4), new TypedValue(70, 1) });
            PromptSelectionResult getInnerPlineResult = ed.SelectWindow(outerPline.GetPoint3dAt(0), outerPline.GetPoint3dAt(2), frameFilter);
            if (getInnerPlineResult.Status == PromptStatus.OK)
            {
                List<ObjectId> innerPlineObjectIds = new List<ObjectId>(getInnerPlineResult.Value.GetObjectIds());
                innerPlineObjectIds.Remove(outerPlineId);
                foreach (ObjectId innerPlineObjectId in innerPlineObjectIds)
                {
                    selectedObjectIds.Remove(innerPlineObjectId);
                    resultObjectIds.Remove(innerPlineObjectId);
                }
                if (selectedObjectIds.Count > 0)
                {
                    RemoveInnerPLine(tr, ref selectedObjectIds, ref resultObjectIds);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string MediaName = comboBoxMedia.SelectedItem.ToString().Replace(" ", "_").Replace("毫米", "MM").Replace("英寸", "Inches").Replace("像素", "Pixels");
            MessageBox.Show("尺寸："+MediaName);
        }
    }
}
