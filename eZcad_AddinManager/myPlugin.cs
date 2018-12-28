﻿using System.Diagnostics;
using Autodesk.AutoCAD.Runtime;
using eZcad.AddinManager;
using eZcad.Addins;
using eZcad_AddinManager;
using Exception = System.Exception;
// This line is not mandatory, but improves loading performances
[assembly: ExtensionApplication(typeof(MyPlugin))]

namespace eZcad_AddinManager
{
    /// <summary>
    /// AddinManager 插件程序集 加载与卸载时的操作
    /// </summary>
    /// <remarks>
    /// This class is instantiated by AutoCAD once and kept alive for the 
    /// duration of the session. If you don't do any one time initialization 
    /// then you should remove this class.
    /// </remarks>
    internal class MyPlugin : IExtensionApplication
    {
        #region ---   加载与卸载

        /// <summary>
        /// 加载 AddinManager 插件时自动执行
        /// </summary>
        void IExtensionApplication.Initialize()
        {
            try
            {
                 var ime = new AutoSwitchIME();
            }
            catch (Exception ex)
            {
                Debug.Print("AddinManager 插件加载时出错： \n\r" + ex.Message + "\n\r" + ex.StackTrace);
            }
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
    }
}