﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AutoCAD;
using Autodesk.Windows;
using Autodesk.AutoCAD.Runtime;
using eZcad_AddinManager;
using eZcad_AddinManager.AppSetup;
using eZcad_AddinManager.Addins;
using eZcad_AddinManager.AppSetup;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Exception = System.Exception;
using Orientation = System.Windows.Controls.Orientation;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AddinSetup))]
// 下面关于 assembly: ExtensionApplication 在实际使用时必须添加

[assembly: ExtensionApplication(typeof(AddinSetup))]

namespace eZcad_AddinManager
{
    // This class is instantiated by AutoCAD once and kept alive for the 
    // duration of the session. If you don't do any one time initialization 
    // then you should remove this class.
    internal class AddinSetup : IExtensionApplication
    {
        #region --- 插件加载与卸载时的操作

        void IExtensionApplication.Initialize()
        {
            // Add one time initialization here
            // One common scenario is to setup a callback function here that 
            // unmanaged code can call. 
            // To do this:
            // 1. Export a function from unmanaged code that takes a function
            //    pointer and stores the passed in value in a global variable.
            // 2. Call this exported function in this function passing delegate.
            // 3. When unmanaged code needs the services of this managed module
            //    you simply call acrxLoadApp() and by the time acrxLoadApp 
            //    returns  global function pointer is initialized to point to
            //    the C# delegate.
            // For more info see: 
            // http://msdn2.microsoft.com/en-US/library/5zwkzwf4(VS.80).aspx
            // http://msdn2.microsoft.com/en-us/library/44ey4b32(VS.80).aspx
            // http://msdn2.microsoft.com/en-US/library/7esfatk4.aspx
            // as well as some of the existing AutoCAD managed apps.

            // Initialize your plug-in application here
            InitializeComponent();
            try
            {
                // 加载插件时自动执行某些命令，而不通过手动输出命令来执行
                var ime = new AutoSwitchIME();
            }
            catch (Exception ex)
            {
                Debug.Print("AddinManager 插件加载时出错： \n\r" + ex.Message + "\n\r" + ex.StackTrace);
            }
            // 
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"{AddinOptions.TabId}程序加载成功\n");
        }

        /// <summary> 在Addin插件卸载过程中，是否已经将AddinManager窗口中的插件路径保存在mySettings中。 </summary>
        /// <remarks>因为在Addin插件卸载过程中，可能会多次进入此Terminate函数</remarks>
        private bool _hasValidNodesInfoSaved = false;
        void IExtensionApplication.Terminate()
        {
            try
            {
                form_AddinManager frm = form_AddinManager.GetUniqueForm();
                var nodesInfo = frm.NodesInfo;
                var count = nodesInfo.Count;
                if (!_hasValidNodesInfoSaved)
                {
                    AssemblyInfoDllManager.SaveAssemblyInfosToSettings(nodesInfo);
                    if (count > 0)
                    {
                        _hasValidNodesInfoSaved = true;
                    }
                }
                //
            }
            catch (Exception ex)
            {
                Debug.Print("AddinManager 插件关闭时出错： \n\r" + ex.Message + "\n\r" + ex.StackTrace);
            }
        }
        #endregion

        private static string IconDir = "";
        /// <summary> 打开工具选项卡界面所输入的命令 </summary>
        private const string CmdStartRibbon = AddinOptions.GroupCommnad + @"Ribbon";

        private void InitializeComponent()
        {
            var assPath = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
            IconDir = Directory.GetCurrentDirectory();
            IconDir = new FileInfo(assPath).Directory.FullName;
            // CreateRibbon();

            // ComponentManager.ItemInitialized 事件在每一次添加对象（选项卡 RibbonTab、不包括：工具栏）时都会触发。
            // ComponentManager.ItemInitialized += ComponentManager_ItemInitialized;
        }

        /// <summary> 添加自定义功能区选项卡 </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CmdStartRibbon, AddinOptions.ModelState)]
        public void CreateRibbon()
        {
            AcadApplication acadApplication =
                Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication as AcadApplication;
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (ComponentManager.Ribbon == null)
            {
                MessageBox.Show($"请先通过 RIBBON 命令打开选项卡，然后重复 {CmdStartRibbon} 命令。",
                    @"提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;

            RibbonControl ribCntrl = ComponentManager.Ribbon;
            // ribCntrl can also be RibbonServices.RibbonPaletteSet.RibbonControl, this class can be found in AcWindows.dll;  

            // 如果已经加载，则显示出来即可
            var existingRibbonTab = ribCntrl.Tabs.FirstOrDefault(r => r.Id == AddinOptions.TabId
                                                                      && r.Title == AddinOptions.TabTitle
                                                                      && r.Name == AddinOptions.TabName);

            if (existingRibbonTab != null)
            {
                eZcad.Utility.Utils.SendCommandSync("Ribbon ");
                // doc.SendStringToExecute("Ribbon ", false, false, false); // AutoCAD 2014
                // ed.Command(new object[] { "Ribbon" }); // AutoCAD 2016
                existingRibbonTab.IsActive = true;
                return;
            }
            else
            {
                //Add the tab
                RibbonTab ribTab = new RibbonTab
                {
                    Title = AddinOptions.TabTitle,
                    Id = AddinOptions.TabId,
                    Name = AddinOptions.TabName
                };
                ribCntrl.Tabs.Add(ribTab);
                //
                try
                {
                    AddControls(ribTab);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "选项卡工具加载出错\r\n" + ex.StackTrace, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                //set as active tab
                ribTab.IsActive = true;
            }

            eZcad.Utility.Utils.SendCommandSync("Ribbon ");
            //doc.SendStringToExecute("Ribbon ", false, false, false); // AutoCAD 2014
            //ed.Command(new object[] { "Ribbon" }); // AutoCAD 2016
        }

        /// <summary> 在选项卡中添加工具 </summary>
        /// <param name="ribTab"></param>
        private static void AddControls(RibbonTab ribTab)
        {
            // ----------------------------- 项目信息 ----------------------------------------
            var pnl_Project = CreatePanel(ribTab, "工具");
            // var pnl_Settings = CreatePanel(ribTab, "设置");
        }

        private static RibbonPanel CreatePanel(RibbonTab sourceTab, string panelTitle)
        {
            //create the panel source
            RibbonPanelSource ribPanelSource = new RibbonPanelSource();
            ribPanelSource.Title = panelTitle;

            //create the panel
            RibbonPanel ribPanel = new RibbonPanel();
            ribPanel.Source = ribPanelSource;
            sourceTab.Panels.Add(ribPanel);
            return ribPanel;
        }

        #region --- 添加按钮

        private static void AddButton(RibbonPanel panel, MethodInfo method, RibbonItemSize size)
        {
            string commandName;
            string buttonText;
            string description;
            BitmapImage largeImage;
            BitmapImage smallImage;
            GetMethodElements(method, out commandName, out buttonText, out description, out largeImage, out smallImage);
            //
            var ribButton = CreateButton(commandName, buttonText, description, size, largeImage);
            panel.Source.Items.Add(ribButton);
        }

        private static void AddButton(RibbonSplitButton splitButton, MethodInfo method, RibbonItemSize size)
        {
            string commandName;
            string buttonText;
            string description;
            BitmapImage largeImage;
            BitmapImage smallImage;
            GetMethodElements(method, out commandName, out buttonText, out description, out largeImage, out smallImage);
            //
            var ribButton = CreateButton(commandName, buttonText, description, size, largeImage);
            splitButton.Items.Add(ribButton);
        }

        /// <summary> 在选项面板中添加一个按钮 </summary>
        /// <param name="commandName">按钮所对应的命令名，命令后不能加空格</param>
        /// <param name="buttonText">按钮的名称</param>
        /// <param name="description">按钮的功能描述</param>
        /// <param name="size">图片显示为大图像还是小图像 </param>
        /// <param name="largeImage"> 按钮所对应的图像，其像素大小为 32*32 </param>
        /// <param name="smallImage">按钮所对应的图像，其像素大小为 16*16 </param>
        private static RibbonButton CreateButton(string commandName, string buttonText,
            string description = null,
            RibbonItemSize size = RibbonItemSize.Large, BitmapImage largeImage = null, BitmapImage smallImage = null)
        {
            //create button1
            var ribButton = new RibbonButton
            {
                Text = buttonText,
                Description = description,
                ShowText = true,
                Orientation = Orientation.Vertical, // 竖向则文字显示在图片正文，水平则文字显示在图片右边
                //
                Image = smallImage,
                LargeImage = largeImage,
                Size = size, // 按钮图片是显示为大图标还是正常小图标。
                ShowImage = true,
                //
                AllowInStatusBar = true,
                AllowInToolBar = true,

                // HelpTopic = "帮助",
                // HelpSource = new Uri("www.baidu.com"),

                //pay attention to the SPACE(or line feed) after the command name
                CommandParameter = commandName + "\n", // "Circle ",
                CommandHandler = new AdskCommandHandler()
            };
            //
            return ribButton;
        }

        #endregion

        /// <summary> 创建可下拉的按钮列表 </summary>
        /// <param name="panel"></param>
        /// <param name="buttonText"></param>
        /// <returns></returns>
        private static RibbonSplitButton CreateSplitButton(RibbonPanel panel, string buttonText)
        {
            var sb = new RibbonSplitButton()
            {
                Text = buttonText,
                ShowText = true,
                Size = RibbonItemSize.Large,
                ListStyle = RibbonSplitButtonListStyle.List,
            };
            panel.Source.Items.Add(sb);
            return sb;
        }

        /// <summary> 从方法的Attribute中提取界面所需要的元素 </summary>
        private static void GetMethodElements(MethodInfo method, out string commandName, out string buttonText,
            out string description, out BitmapImage largeImage, out BitmapImage smallImage)
        {
            commandName = null;
            buttonText = null;
            description = null;
            largeImage = null;
            smallImage = null;
            // 命令
            var commandMethod =
                method.GetCustomAttributes(typeof(CommandMethodAttribute)).First() as CommandMethodAttribute;
            if (commandMethod == null)
            {
                return;
            }
            commandName = commandMethod.GroupName + "." + commandMethod.GlobalName;

            var ri = method.GetCustomAttributes(typeof(RibbonItemAttribute)).FirstOrDefault() as RibbonItemAttribute;
            if (ri != null)
            {
                buttonText = ri.Text;
                description = ri.Description;
                //
                if (!string.IsNullOrEmpty(ri.LargeImagePath))
                {
                    var fp = Path.Combine(IconDir, ri.LargeImagePath);
                    if (File.Exists(fp))
                    {
                        largeImage = new BitmapImage(new Uri(fp));
                    }
                }
                if (!string.IsNullOrEmpty(ri.SmallImagePath))
                {
                    var fp = Path.Combine(IconDir, ri.LargeImagePath);
                    // var fp = Path.GetFullPath(ri.SmallImagePath);
                    if (File.Exists(fp))
                    {
                        smallImage = new BitmapImage(new Uri(fp));
                    }
                }
            }
        }

        /// <summary> 一个通用的类，用来响应各种 RibbonButton 按钮的事件 </summary>
        public class AdskCommandHandler : ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                //is from Ribbon Button
                RibbonButton ribBtn = parameter as RibbonButton;
                if (ribBtn != null)
                {
                    //execute the command 
                    var doc = Application.DocumentManager.MdiActiveDocument;
                    doc.SendStringToExecute((string)ribBtn.CommandParameter,
                        true, false, true);
                }
            }
        }
    }
}