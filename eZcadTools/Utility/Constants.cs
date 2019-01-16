using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace eZcadTools.Utility
{
    public static class Constants
    {
        /// <summary>
        /// 在另一个命令正在执行时，此命令是否可以被唤醒。
        /// Modal 表示当另一个命令正在执行时，此命令不能被唤醒；Transparent 表示当另外的命令激活时这个命令可以被使用。
        /// </summary>
        public const CommandFlags ModelState = CommandFlags.Transparent; // 从后面这两个可选项中选择一个： CommandFlags.Modal、CommandFlags.Transparent;

        /// <summary> 在界面中进行 Editor.Select 搜索时，对于重合的区域所给出的容差 </summary>
        public const double CoincideTolerance = 1e-6;
        
    }
}