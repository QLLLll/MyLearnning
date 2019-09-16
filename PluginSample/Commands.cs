using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.Interop;

using Newtonsoft.Json;

[assembly: ExtensionApplication(typeof(PluginSample.Commands))]
[assembly: CommandClass(typeof(PluginSample.Commands))]

namespace PluginSample
{
    /// <summary>
    /// 菜单面板视图。
    /// </summary>
    public class MenuPanelView : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MenuPanelView()
        {
            // 9个子菜单+1个空白空间。
            _expanded = Enumerable.Repeat("0", 9).Concat(new[] { "*" }).ToArray();
        }

        /// <summary>
        /// 子菜单展开状态。
        /// 由程序控制保持有且只有一个展开状态为"*"，其余为"0"。
        /// </summary>
        private string[] _expanded;
        public string[] Expanded
        {
            get { return _expanded; }
            set
            {
                _expanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Expanded"));
            }
        }
    }

    /// <summary>
    /// 边坡设计画面视图。
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class BpDesignView : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double Lujiankuan { get; set; }
        public double Lujiangaocheng { get; set; }
        public double Churukougaocheng { get; set; }

        private int _bianpojishu = 1;
        public int Bianpojishu
        {
            get { return _bianpojishu; }
            set
            {
                _bianpojishu = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HasErjibianpo"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HasSanjibianpo"));
            }
        }

        public int Yijipolvi { get; set; } = 0;
        public int Erjipolvi { get; set; } = 0;
        public int Sanjipolvi { get; set; } = 0;
        public double Yijipogao { get; set; }
        public double Erjipogao { get; set; }
        public int Huitubilii { get; set; } = 2;

        [JsonIgnore]
        public double[] kPolv { get; } = new[] { 1.5, 1.75, 2 };
        [JsonIgnore]
        public int[] kBili { get; } = new[] { 20, 50, 100, 200, 500, 1000 };
        [JsonIgnore]
        public bool HasErjibianpo { get { return Bianpojishu > 1; } }
        [JsonIgnore]
        public bool HasSanjibianpo { get { return Bianpojishu > 2; } }
        [JsonIgnore]
        public double Yijipolv { get { return kPolv[Yijipolvi]; } }
        [JsonIgnore]
        public double Erjipolv { get { return kPolv[Erjipolvi]; } }
        [JsonIgnore]
        public double Sanjipolv { get { return kPolv[Sanjipolvi]; } }
        [JsonIgnore]
        public double Huitubili { get { return kBili[Huitubilii]; } }
    }

    /// <summary>
    /// AutoCAD命令集合。
    /// </summary>
    public class Commands : IExtensionApplication
    {
        static PaletteSet MenuPanelPalette = new PaletteSet("综合设计");

        static Commands()
        {
            var menuPanel = new MenuPanel(new MenuPanelView());
            MenuPanelPalette.AddVisual("综合设计", menuPanel);
        }

        [CommandMethod("psshowmenu")]
        public static void ShowMenu()
        {
            MenuPanelPalette.Visible = true;
            MenuPanelPalette.Dock = DockSides.Left;
        }

        public void Initialize()
        {
            // 添加综合设计面板入口到AutoCAD菜单。
            var menubar = Application.MenuBar as AcadMenuBar;
            menubar.Item(2).AddMenuItem(0, "综合设计", "psshowmenu ");
        }

        public void Terminate()
        {
        }
    }
}
