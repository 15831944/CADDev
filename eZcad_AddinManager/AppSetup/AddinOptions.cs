using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace eZcad_AddinManager.AppSetup
{
    /// <summary>
    ///  CAD 插件加载的选项
    /// </summary>
    internal static class AddinOptions
    {
        /// <summary> 路基工程量统计程序的简写标志符 </summary>
        public const string eZcadTools = "AddinManager";

        /// <summary> 在<see cref="CommandMethodAttribute"/>中设置的外部命令的 GroupName。 </summary>

        /// <summary>
        /// 外部命令在 AutoCAD 界面中对应的控件的图片所在的文件夹。 
        /// 当引用某个图片文件时，直接通过“<seealso cref="CmdImageDirectory"/> + "picture.png"”即可
        /// </summary>
        /// <remarks>“.\”表示当前正在执行的程序集所在的文件夹，“..\”表示当前正在执行的程序集所在的文件夹</remarks>
        public const string CmdImageDirectory = @"..\eZcadTools\Resources\icons\";
        // @"D:\GithubProjects\CADDev\SubgradeQuantity\Resources\icons\"; // @"..\SubgradeQuantity\Resources\icons\";

        /// <summary>
        /// 在另一个命令正在执行时，此命令是否可以被唤醒。
        /// Modal 表示当另一个命令正在执行时，此命令不能被唤醒；Transparent 表示当另外的命令激活时这个命令可以被使用。
        /// </summary>
        public const CommandFlags ModelState = CommandFlags.Transparent; // 从后面这两个可选项中选择一个： CommandFlags.Modal、CommandFlags.Transparent;
       
        #region   ---   插件在界面中的显示

        //添加自定义功能区选项卡
        public const string TabId_AddinManager = "eZcadTools";
        /// <summary> 选项卡名称 </summary>
        public const string TabName_AddinManager = "eZcadTools";
        /// <summary> 选项卡标题 </summary>
        public const string TabTitle_AddinManager = "eZcadTools";

        public const string eZcadToolsGroupCommnad = eZcadTools;

        #endregion
    }
}