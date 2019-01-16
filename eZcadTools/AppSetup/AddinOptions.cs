using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace eZcadTools.AppSetup
{
    /// <summary>
    ///  CAD 插件加载的选项
    /// </summary>
    internal static class AddinOptions
    {
        /// <summary> 本程序集的标志性名称 </summary>
        public const string AddinTag = "eZcadTools";

        /// <summary>
        /// 外部命令在 AutoCAD 界面中对应的控件的图片所在的文件夹。 
        /// 当引用某个图片文件时，直接通过“<seealso cref="CmdImageDirectory"/> + "picture.png"”即可
        /// </summary>
        /// <remarks>“.\”表示当前正在执行的程序集所在的文件夹，“..\”表示当前正在执行的程序集所在的文件夹</remarks>
        public const string CmdImageDirectory = @"..\eZcadTools\Resources\icons\";
        // @"D:\GithubProjects\CADDev\SubgradeQuantity\Resources\icons\"; // @"..\SubgradeQuantity\Resources\icons\";

        #region   ---   插件在界面中的显示

        /// <summary> The framework does not use or validate this id.It is left to the applications to set this id and use it. The default value is null. </summary>
        public const string TabId = null;

        /// <summary> 选项卡名称，
        /// The framework uses the Title property of the tab to display the tab title in the ribbon. 
        /// The name property is not currently used by the framework. 
        /// Applications can use this property to store a longer name for the tab if it is required in other UI customization dialogs. 
        /// The default value is null. </summary>
        public const string TabName = null;

        /// <summary> 选项卡标题，显示在选项卡的界面中 </summary>
        public const string TabTitle = AddinTag;

        /// <summary> 在<see cref="CommandMethodAttribute"/>中设置的外部命令的 GroupName。
        /// 比如一个命令名的全称为 eZcadTools.ScaleText，其中 eZcadTools 即为 GroupName </summary>
        public const string GroupCommnad = AddinTag;

        #endregion

    }
}