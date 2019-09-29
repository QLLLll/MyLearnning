using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
namespace AutoNetLoad
{
    public partial class FormAutoLoad : Form
    {
        List<RegistryKey> autoCADVersions=new List<RegistryKey>();
        public FormAutoLoad()
        {
            InitializeComponent();
        }

        private void FormAutoLoad_Load(object sender, EventArgs e)
        {
            //获取当前AutoCAD的注册表键名
            string cadKeyName=GetAutoCADKeyName();
            //打开HKEY_LOCAL_MACHINE下当前AutoCAD的注册表键以获得版本号
            RegistryKey keyCAD=GetRegistryKey2(cadKeyName);
            //设置文本框显示当前AutoCAD版本号
            string cadName=keyCAD.GetValue("ProductName").ToString();
            this.textBoxCurCAD.Text = cadName;
            //打开HKEY_CURRENT_USER下当前AutoCAD的Applications注册表键以显示已加载的.NET程序
            RegistryKey keyApplications=Registry.CurrentUser.CreateSubKey(cadKeyName + "\\" + "Applications");
            //遍历Applications下的注册表项
            foreach (var subKeyNameApp in keyApplications.GetSubKeyNames())
            {
                //打开注册表键
                RegistryKey keyApplication=keyApplications.OpenSubKey(subKeyNameApp);
                //如果是.NET程序
                if (keyApplication.GetValue("MANAGED") != null)
                {
                    //在列表框中添加.NET程序的名字和程序路径
                    ListViewItem item=new ListViewItem(subKeyNameApp);
                    item.SubItems.Add(keyApplication.GetValue("LOADER").ToString());
                    this.listViewAssembly.Items.Add(item);
                }
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            //判断用户是否通过文件对话框选择了.NET程序集
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //选择的DLL文件的文件名（含路径）
                this.textBoxFilePath.Text = this.openFileDialog1.FileName;
                //选择的DLL文件的文件名（不含路径与后缀），作为应用程序名
                this.textBoxApp.Text = System.IO.Path.GetFileNameWithoutExtension(this.textBoxFilePath.Text);
                //应用程序描述设置成程序名
                this.textBoxAppDesc.Text = this.textBoxApp.Text;
            }
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string appName=this.textBoxApp.Text; //应用程序名
            string appDesc=this.textBoxAppDesc.Text; //应用描述
            string appPath=this.textBoxFilePath.Text;//应用程序路径
            //添加注册表项到Applications键下
            CreateDemandLoadingEntries(appName, appDesc, appPath, true, false, 2);
            //应用程序的有关信息添加到列表框中
            ListViewItem item=new ListViewItem(appName);
            item.SubItems.Add(appPath);
            this.listViewAssembly.Items.Add(item);
        }


        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Dispose();//销毁窗体
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            //获取列表框中当前选择的项
            string text=this.listViewAssembly.SelectedItems[0].Text;
            if (text != null)
            {
                //显示警告对话框，提示用户是否真的要删除注册表项
                DialogResult result=MessageBox.Show("你确实想删除" + text + "注册表项吗？", "警告", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes) //选择删除
                {
                    if (RemoveDemandLoadingEntries(text, true)) //删除成功
                    {
                        //从列表框中移除对应的注册表项
                        this.listViewAssembly.Items.Remove(listViewAssembly.SelectedItems[0]);
                    }
                }
            }
        }

        public static string GetAutoCADKeyName()
        {
            // 获取HKEY_CURRENT_USER键
             RegistryKey keyCurrentUser = Registry.CurrentUser;
            // 打开AutoCAD所属的注册表键:HKEY_CURRENT_USER\Software\Autodesk\AutoCAD
            // RegistryKey keyAutoCAD =keyCurrentUser.OpenSubKey("Software\\Autodesk\\AutoCAD");

            RegistryKey keyAutoCAD = GetRegistryKey("Software\\Autodesk\\AutoCAD");
            //获得表示当前的AutoCAD版本的注册表键值:R18.2
            string valueCurAutoCAD=keyAutoCAD.GetValue("CurVer").ToString();
            if (valueCurAutoCAD == null) return "";//如果未安装AutoCAD，则返回
            //获取当前的AutoCAD版本的注册表键:HKEY_LOCAL_MACHINE\Software\Autodesk\AutoCAD\R18.2
            RegistryKey keyCurAutoCAD = keyAutoCAD.OpenSubKey(valueCurAutoCAD);
            //获取表示AutoCAD当前语言的注册表键值:ACAD-a001:804
            string language=keyCurAutoCAD.GetValue("CurVer").ToString();
            //获取AutoCAD当前语言的注册表键:HKEY_LOCAL_MACHINE\Software\Autodesk\AutoCAD\R18.2\ACAD-a001:804
            RegistryKey keyLanguage = keyCurAutoCAD.OpenSubKey(language);
            //返回去除HKEY_LOCAL_MACHINE前缀的当前AutoCAD注册表项的键名:Software\Autodesk\AutoCAD\R18.2\ACAD-a001:804
            return keyLanguage.Name.Substring(keyCurrentUser.Name.Length + 1);
        }

        public static bool CreateDemandLoadingEntries(string appName, string appDesc, string appPath, bool currentUser, bool overwrite, int flagLOADCTRLS)
        {
            //获取AutoCAD所属的注册表键名
            var autoCADKeyName=GetAutoCADKeyName();
            //确定是HKEY_CURRENT_USER还是HKEY_LOCAL_MACHINE
            RegistryKey keyRoot =currentUser ? Registry.CurrentUser : Registry.LocalMachine;
            // 由于某些AutoCAD版本的HKEY_CURRENT_USER可能不包括Applications键值，因此要创建该键值
            // 如果已经存在该鍵，无须担心可能的覆盖操作问题，因为CreateSubKey函数会以写的方式打开它而不会执行覆盖操作
            RegistryKey keyApp =keyRoot.CreateSubKey(autoCADKeyName + "\\" + "Applications");
            //若存在同名的程序且选择不覆盖则返回
            if (!overwrite && keyApp.GetSubKeyNames().Contains(appName))
                return false;
            //创建相应的键并设置自动加载应用程序的选项
            RegistryKey keyUserApp=keyApp.CreateSubKey(appName);
            keyUserApp.SetValue("DESCRIPTION", appDesc, RegistryValueKind.String);
            keyUserApp.SetValue("LOADCTRLS", flagLOADCTRLS, RegistryValueKind.DWord);
            keyUserApp.SetValue("LOADER", appPath, RegistryValueKind.String);
            keyUserApp.SetValue("MANAGED", 1, RegistryValueKind.DWord);
            return true;//创建键成功则返回
        }

        public static bool RemoveDemandLoadingEntries(string appName, bool currentUser)
        {
            try
            {
                // 获取AutoCAD所属的注册表键名
                string cadName = GetAutoCADKeyName();
                // 确定是HKEY_CURRENT_USER还是HKEY_LOCAL_MACHINE
                RegistryKey keyRoot =currentUser ? Registry.CurrentUser : Registry.LocalMachine;
                // 以写的方式打开Applications注册表键
                RegistryKey keyApp=keyRoot.OpenSubKey(cadName + "\\" + "Applications", true);
                //删除指定名称的注册表键
                keyApp.DeleteSubKeyTree(appName);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static RegistryKey GetRegistryKey(string keyPath)
        {
            RegistryKey localMachineRegistry
                = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,
                                          Environment.Is64BitOperatingSystem
                                              ? RegistryView.Registry64
                                              : RegistryView.Registry32);

            return string.IsNullOrEmpty(keyPath)
                ? localMachineRegistry
                : localMachineRegistry.OpenSubKey(keyPath);
        }

        public static RegistryKey GetRegistryKey2(string keyPath)
        {
            RegistryKey localMachineRegistry
                = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                                          Environment.Is64BitOperatingSystem
                                              ? RegistryView.Registry64
                                              : RegistryView.Registry32);

            return string.IsNullOrEmpty(keyPath)
                ? localMachineRegistry
                : localMachineRegistry.OpenSubKey(keyPath);
        }
    }
}
