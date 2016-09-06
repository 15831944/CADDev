﻿// (C) Copyright 2016 by XN 
//

using System.Diagnostics;
using System.Windows;
using AutoCADDev.AddinManager;
using AutoCADDev.AssemblyInfo;
using Autodesk.AutoCAD.Runtime;
using Exception = System.Exception;

// This line is not mandatory, but improves loading performances

[assembly: ExtensionApplication(typeof(MyPlugin))]

namespace AutoCADDev.AddinManager
{

    /// <summary>
    /// AddinManager 插件程序集 加载与卸载时的操作
    /// </summary>
    /// 
    /// <remarks>
    /// This class is instantiated by AutoCAD once and kept alive for the 
    /// duration of the session. If you don't do any one time initialization 
    /// then you should remove this class.
    /// </remarks>
    internal class MyPlugin : IExtensionApplication
    {
        #region ---   加载与卸载

        void IExtensionApplication.Initialize()
        {
            try
            {

            }
            catch (Exception ex)
            {
                Debug.Print("AddinManager 插件加载时出错： \n\r" + ex.Message + "\n\r" + ex.StackTrace);
            }
        }

        void IExtensionApplication.Terminate()
        {
            try
            {
                form_AddinManager frm = form_AddinManager.GetUniqueForm();
                var nodesInfo = frm.NodesInfo;
                //
                AssemblyInfoDllManager.SaveAssemblyInfosToFile(nodesInfo);
            }
            catch (Exception ex)
            {
                Debug.Print("AddinManager 插件关闭时出错： \n\r" + ex.Message + "\n\r" + ex.StackTrace);
            }
        }

        #endregion
    }
}