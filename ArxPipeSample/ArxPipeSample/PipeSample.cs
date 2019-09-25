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

[assembly: ExtensionApplication(typeof(ArxPipeSample.PipeSample))]
[assembly: CommandClass(typeof(ArxPipeSample.PipeSample))]

namespace ArxPipeSample
{
    /// <summary>
    /// 插件主体类。
    /// </summary>
    public class PipeSample : IExtensionApplication
    {
        /// <summary>
        /// 扩展数据注册应用程序名。
        /// </summary>
        public const string kAppName = "pipesample";
        /// <summary>
        /// 创建管道类型1命令名。
        /// </summary>
        public const string kCmdNameMakePipeType1 = "makepipe1";
        /// <summary>
        /// 创建阀门类型1命令名。
        /// </summary>
        public const string kCmdNameMakeValveType1 = "makevalve1";

        public void Initialize()
        {
            // 初始化带式菜单。
            if (ComponentManager.Ribbon == null)
            {
                ComponentManager.ItemInitialized += Ribbon.initRibbon;
            }
            else
            {
                Ribbon.initRibbon(null, null);
            }

            // 初始化事件。
            Application.DocumentManager.DocumentCreated += (o, e) =>
            {
                e.Document.Database.ObjectErased += Valve.ValveRemoved;
            };

            foreach (Document doc in Application.DocumentManager)
            {
                doc.Database.ObjectErased += Valve.ValveRemoved;
            }
        }

        public void Terminate()
        {
        }

        /// <summary>
        /// 创建管道类型1命令。
        /// </summary>
        [CommandMethod(kCmdNameMakePipeType1)]
        public void MakePipeType1()
        {
            Pipe.MakePipeType1();
        }

        /// <summary>
        /// 创建阀门类型1命令。
        /// </summary>
        [CommandMethod(kCmdNameMakeValveType1)]
        public void MakeValveType1()
        {
            Valve.MakeValveType1();
        }
    }
}
