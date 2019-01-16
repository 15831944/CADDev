using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace eZcad.Utility
{
    public static class acadConstants
    {
        /// <summary> 水平向量 </summary>
        public static readonly Vector3d HorizontalVec3 = new Vector3d(1, 0, 0);

        /// <summary> 水平向量 </summary>
        public static readonly Vector2d HorizontalVec2 = new Vector2d(1, 0);
    }
}