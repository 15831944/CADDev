using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using eZcad;
using eZcad.Utility;
using RESD.Entities;
using RESD.Options;
using RESD.Utility;
using eZstd.Enumerable;

namespace RESD.DataExport
{
    /// <summary> 软基换填工程量 </summary>
    /// <remarks>对于填方路基，如果自然地表为淤泥等软弱土层，则需要对一定厚度内的基础进行换填等处理</remarks>
    public class Exporter_SoftSubSubstitution : DataExporter
    {
        #region --- Types

        /// <summary> 换填的工程量 </summary>
        private class SubstitutedSoil : HalfValue
        {
            /// <summary> 此段软弱地基处理的表面积（两侧路基范围内的自然地面宽度，加上附加换填宽度） </summary>
            public double TreatedSurfaceArea { get; set; }

            //
            public override void Merge(IMergeable connectedHalf)
            {
                var conn = connectedHalf as SubstitutedSoil;
                //var dist1 = Math.Abs(ParentStation - EdgeStation);
                //var dist2 = Math.Abs(conn.EdgeStation - conn.ParentStation);

                TreatedSurfaceArea = (conn.TreatedSurfaceArea + TreatedSurfaceArea);
            }

            /// <summary> 两个相邻区间是否可以合并到同一行 </summary>
            /// <param name="next">与本区间紧密相连的下一个区间</param>
            public override bool IsMergeable(IMergeable next)
            {
                return true;
            }

            public override void CutByBlock(double blockStation)
            {
                // 对于低填浅挖，这里啥也不用做
            }

            public string GetDescription()
            {
                return "软土路基处理";
            }
        }

        #endregion

        #region --- Fields

        private static readonly Criterion_SoftSub _softSubCriterion = Criterion_SoftSub.UniqueInstance;
        private static readonly Criterion_ThinFillShallowCut _thinFillCriterion = Criterion_ThinFillShallowCut.UniqueInstance;

        /// <summary> 整个项目中的所有横断面 </summary>
        private readonly IList<SubgradeSection> _sectionsToHandle;

        /// <summary> 整个道路中所有断面所占的几何区间， 以及对应的初始化的工程量数据 </summary>
        private readonly SortedDictionary<double, CrossSectionRange<SubstitutedSoil>> _sortedRanges;

        #endregion

        /// <summary> 软基换填 构造函数 </summary>
        /// <param name="docMdf"></param>
        /// <param name="sectionsToHandle">要进行处理的断面</param>
        /// <param name="allSections"></param>
        public Exporter_SoftSubSubstitution(DocumentModifier docMdf, List<SubgradeSection> sectionsToHandle,
            IList<SubgradeSection> allSections) : base(docMdf, allSections.Select(r => r.XData.Station).ToArray())
        {
            sectionsToHandle.Sort(SQUtils.CompareStation);
            _sectionsToHandle = sectionsToHandle;
            //
            _sortedRanges = InitializeGeometricRange<SubstitutedSoil>(AllStations);
        }

        /// <summary> 软基换填 </summary>
        public void ExportSubstitutionSoil()
        {

            var softSubSections = new List<CrossSectionRange<SubstitutedSoil>>();

            // 断面的判断与计算
            double treatedGroundLength;
            foreach (var sec in _sectionsToHandle)
            {

                var xdata = sec.XData;
                if (IsSoftSub(_docMdf.acDataBase, sec, out treatedGroundLength))
                {
                    var thsc = _sortedRanges[xdata.Station];
                    //
                    thsc.BackValue.TreatedSurfaceArea = treatedGroundLength * Math.Abs(thsc.BackValue.EdgeStation - thsc.BackValue.ParentStation);
                    thsc.FrontValue.TreatedSurfaceArea = treatedGroundLength * Math.Abs(thsc.FrontValue.EdgeStation - thsc.FrontValue.ParentStation);
                    //
                    softSubSections.Add(thsc);
                }
            }
            var countAll = softSubSections.Count;
            if (countAll == 0)
            {
                _docMdf.WriteNow($"软基换填断面的数量：{countAll}");
                return;
            }

            // 对桥梁隧道结构进行处理：截断对应的区间
            CutWithBlocks(softSubSections, Options_Collections.RangeBlocks);

            // 将位于桥梁隧道区间之内的断面移除
            softSubSections = softSubSections.Where(r => !r.IsNull).ToList();

            // 对于区间进行合并
            softSubSections = MergeLinkedSections(softSubSections);


            countAll = softSubSections.Count;
            _docMdf.WriteNow($"软基换填断面的数量：{countAll}");
            if (countAll == 0) return;

            // 将结果整理为二维数组，用来进行表格输出
            var rows = new List<object[]>();
            var header = new string[] { "起始桩号", "结束桩号", "桩号区间", "长度", "处理措施", "换填表面积", };
            rows.Add(header);

            for (int i = 0; i < softSubSections.Count; i++)
            {
                var thsc = softSubSections[i];
                thsc.UnionBackFront();
                //
                rows.Add(new object[]
                {
                    thsc.BackValue.EdgeStation,
                    thsc.FrontValue.EdgeStation,
                    SQUtils.GetStationString(thsc.BackValue.EdgeStation, thsc.FrontValue.EdgeStation,
                        maxDigits: 0),
                    thsc.FrontValue.EdgeStation - thsc.BackValue.EdgeStation,
                    thsc.BackValue.GetDescription(),
                    thsc.BackValue.TreatedSurfaceArea,
                });
            }

            var sheetArr = ArrayConstructor.FromList2D(listOfRows: rows);
            // sheetArr = sheetArr.InsertVector<object, string, object>(true, new[] { header }, new[] { -1.5f, });

            var sheet_Infos = new List<WorkSheetData>
            {
                new WorkSheetData(WorkSheetDataType.ThinFillShallowCut, "路基换填", sheetArr)
            };

            ExportWorkSheetDatas(sheet_Infos);
        }

        #region --- 判断软弱地基

        /// <summary> 软基填方 </summary>
        /// <param name="centerAxis"></param>
        /// <param name="treatedGroundLength"> 软弱地基宽度（两侧路基范围内的自然地面宽度，加上附加换填宽度）</param>
        /// <returns></returns>
        public static bool IsSoftSub(Database db, SubgradeSection centerAxis, out double treatedGroundLength)
        {
            var center = centerAxis.XData;
            bool isSoftSubs = false;
            treatedGroundLength = 0.0;

            // 1. 基本判断标准
            if (!center.IsCenterFill()) return false;
            var centerDepth = center.CenterElevation_Road - center.CenterElevation_Ground;

            // 低填加固厚度
            var thinFillTreatedDepth = _thinFillCriterion.低填处理高度 - centerDepth;
            if ((_softSubCriterion.换填厚度D - thinFillTreatedDepth < _softSubCriterion.最小换填厚度))
            {
                // 如果除去低填处理的厚度T之后，剩下的换填厚度(D-T)还大于此最小换填厚度"，则认为此断面应该计入D的换填厚度
                return false;
            }


            // 限制填方路基范围内的自然地面不能过陡，以过滤到陡坡路堤或者挖台阶时的重复处理


            // 2. 边坡的坡底点
            var leftBottom = center.LeftSlopeExists
                ? center.LeftSlopeHandle.GetDBObject<Polyline>(db).EndPoint
                : center.LeftRoadSurfaceHandle.GetDBObject<Polyline>(db).EndPoint;
            var rightBottom = center.RightSlopeExists
                ? center.RightSlopeHandle.GetDBObject<Polyline>(db).EndPoint
                : center.RightRoadSurfaceHandle.GetDBObject<Polyline>(db).EndPoint;

            // private bool IsSideSoftSub(Database db, SectionInfo section, Point3d slopeBottom, out double treatedLength)
            double leftTreatedGroundLength;
            double rightTreatedGroundLength;
            bool isLeftSideSoftSub = IsSideSoftSub(db, center, true, leftBottom, out leftTreatedGroundLength);
            bool isRightSideSoftSub = IsSideSoftSub(db, center, false, rightBottom, out rightTreatedGroundLength);
            treatedGroundLength = leftTreatedGroundLength + rightTreatedGroundLength;
            //
            return (leftTreatedGroundLength + rightTreatedGroundLength > 0.1);
        }


        /// <summary>
        /// 对某一侧路基是否需要换填进行判断与计量
        /// </summary>
        /// <param name="db"></param>
        /// <param name="section">某个路基断面</param>
        /// <param name="slopeBottom">某侧填方边坡的坡脚</param>
        /// <param name="treatedLength">需要换填处理的宽度</param>
        /// <returns></returns>
        private static bool IsSideSoftSub(Database db, SectionInfo section, bool leftSide, Point3d slopeBottom, out double treatedLength)
        {
            treatedLength = 0;

            // 道路中心线与自然地面的交点
            var groundPt = new Point3d(section.CenterX, section.GetYFromElev(section.CenterElevation_Ground), 0);

            // 3. 坡底点是否位于低填区间
            var withinLf = Exporter_ThinFillShallowCut.WithinThinFillRange(groundPt, 1 / _thinFillCriterion.低填射线坡比,
                1 / _thinFillCriterion.低填射线坡比, slopeBottom);

            if (withinLf != 0) return false;

            // 计算处理宽度
            var groundLine = leftSide ? section.LeftGroundSurfaceHandle.GetDBObject<Polyline>(db)
                : section.RightGroundSurfaceHandle.GetDBObject<Polyline>(db);
            var groundLineCurve = groundLine.Get2dLinearCurve();
            var end1Para = groundLineCurve.GetClosestPointTo(new Point2d(slopeBottom.X, slopeBottom.Y)).Parameter;
            var end2Para = groundLineCurve.GetClosestPointTo(new Point2d(groundPt.X, groundPt.Y)).Parameter;
            var smallerPara = end1Para < end2Para ? end1Para : end2Para;
            var largerPara = end1Para > end2Para ? end1Para : end2Para;
            treatedLength = groundLineCurve.GetLength(smallerPara, largerPara) + _softSubCriterion.附加宽度;
            return true;
        }

        #endregion

    }
}