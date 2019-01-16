using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace RESD.Utility
{
    public static class SQConstants
    {
        /// <summary>
        /// 在另一个命令正在执行时，此命令是否可以被唤醒。
        /// Modal 表示当另一个命令正在执行时，此命令不能被唤醒；Transparent 表示当另外的命令激活时这个命令可以被使用。
        /// </summary>
        public const CommandFlags ModelState = CommandFlags.Transparent; // 从后面这两个可选项中选择一个： CommandFlags.Modal、CommandFlags.Transparent;
 
        /// <summary> 在界面中进行 Editor.Select 搜索时，对于重合的区域所给出的容差 </summary>
        public const double CoincideTolerance = 1e-6;

        #region ---   图层名

        public const string LayerName_WaterLine = "SQ_剪切标高线";
        public const string LayerName_ProtectionMethod_Slope = "SQ_边坡防护形式";
        public const string LayerName_ProtectionMethod_Platform = "SQ_平台防护形式";
        public const string LayerName_LongitudinalSlopes = "SQ_边坡纵剖面示意图";

        #endregion

        /// <summary> 在用一个字符串表示某种防护方式及其对应规格时，可以通过“_”进行分隔，比如 挂网喷锚_6m  </summary>
        public const char ProtectionMethodStyleSeperator = '_';

        /// <summary> 当两个桩号区间进行合并时，判断这两个区间是否重合的最小距离。比如 [100~110] 与 [110.0004~120] 这两个区间可以认为是可以进行合并的 </summary>
        public const double RangeMergeTolerance = 0.005;

        /// <summary> 利用路基横断面图中的道路中心轴线向下搜索对应的“数据栏”块参照时，X方向的偏移值。 </summary>
        /// <remarks>正值表示向右偏移，负值表示向左偏移</remarks>
        public const double AxisBottomXOffset = 20;

        /// <summary> 利用路基横断面图中的道路中心轴线向下搜索对应的“数据栏”块参照时，Y方向的偏移值。 </summary>
        /// <remarks>正值表示向上偏移，负值表示向上偏移</remarks>
        public const double AxisBottomYOffset = 40;

        /// <summary> 边坡上平台马道的最长宽度 </summary>
        public const double MaxPlatformLength = 3.0;

        /// <summary> 边坡或者平台的最小长度，小于此长度则被忽略 </summary>
        public const double MinSlopeSegLength = 0.05;

        /// <summary> 水平向量 </summary>
        public static readonly Vector3d HorizontalVec3 = new Vector3d(1, 0, 0);

        /// <summary> 水平向量 </summary>
        public static readonly Vector2d HorizontalVec2 = new Vector2d(1, 0);
    }
}