﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using eZcad;
using eZcad.Utility;
using RESD.Cmds;
using RESD.DataExport;
using RESD.Entities;
using RESD.Options;
using Application = Microsoft.Office.Interop.Excel.Application;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Windows = eZstd.API.Windows;

namespace RESD.Utility
{
    /// <summary> 与边坡防护相关的一些通用性的操作 </summary>
    public static class SQUtils
    {
        /// <summary> 路基工程量计算系统 环境配置 </summary>
        /// <param name="docMdf"></param>
        public static void SubgradeEnvironmentConfiguration(DocumentModifier docMdf)
        {
            SymbolTableUtils.GetOrCreateAppName(docMdf.acDataBase, docMdf.acTransaction, SectionInfo.AppNameGeneral);
            SymbolTableUtils.GetOrCreateAppName(docMdf.acDataBase, docMdf.acTransaction, SectionInfo.AppNameLeft);
            SymbolTableUtils.GetOrCreateAppName(docMdf.acDataBase, docMdf.acTransaction, SectionInfo.AppNameRight);
            // var app = Utils.GetOrCreateAppName(docMdf.acDataBase, docMdf.acTransaction, SlopeDataBackup.AppName);
        }

        #region --- 从界面中提取路基横断面对象

        /// <summary> 从界面中提取选定的某些路基对象 </summary>
        /// <returns></returns>
        public static List<Line> SelecteSectionLines(Editor ed)
        {
            // Create our options object
            var pso = new PromptSelectionOptions();

            // Set our prompts to include our keywords
            string kws = pso.Keywords.GetDisplayString(true);
            pso.MessageForAdding = "\n选择要提取的横断面轴线 " + kws; // 当用户在命令行中输入A（或Add）时，命令行出现的提示字符。
            pso.MessageForRemoval = "\n选择要提取的横断面轴线 " + kws;
            // 当用户在命令行中输入Re（或Remove）时，命令行出现的提示字符。
            // pso.SingleOnly = true;

            var psr = ed.GetSelection(pso, SubgradeSection.Filter);

            if (psr.Status == PromptStatus.OK)
            {
                return psr.Value.GetObjectIds().Select(id => id.GetObject(OpenMode.ForRead) as Line).ToList();
            }
            return null;
        }

        /// <summary> 从界面中提取所有的路基对象 </summary>
        /// <returns></returns>
        public static List<Line> GetAllSectionLines(Editor ed)
        {
            var psr = ed.SelectAll(SubgradeSection.Filter);

            if (psr.Status == PromptStatus.OK)
            {
                return psr.Value.GetObjectIds().Select(id => id.GetObject(OpenMode.ForRead) as Line).ToList();
            }
            return null;
        }

        /// <summary> 从整个项目中获取全部的横断面所对应的桩号（不要求界面全部显示）。集合中的元素未按桩号值进行排序 </summary>
        /// <param name="sort">是否要对所有横断面按桩号从小到大排序</param>
        public static SubgradeSection[] GetAllSections(DocumentModifier docMdf, bool sort)
        {
            var stations = new List<SubgradeSection>();

            var res = docMdf.acEditor.SelectAll(SubgradeSection.Filter);
            if (res.Status == PromptStatus.OK)
            {
                var ids = res.Value.GetObjectIds();
                foreach (var id in ids)
                {
                    SubgradeSection centerLine = null;
                    var l = id.GetObject(OpenMode.ForRead) as Line;
                    if (l != null)
                    {
                        var si = SectionInfo.FromCenterLine(l);
                        if (si != null && si.FullyCalculated)
                        {
                            centerLine = new SubgradeSection(docMdf, l, si);
                            stations.Add(centerLine);
                        }
                    }
                    if (centerLine == null)
                    {
                        MessageBox.Show($"某些道路中心线对象所对应的横断面未进行构造，" +
                                        $"\r\n请先调用“{SectionsConstructor.CommandName}”命令，以构造整个项目的横断面系统。",
                            "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
            }
            if (sort)
            {
                stations.Sort(CompareStation);
            }
            return stations.ToArray();
        }

        #endregion

        #region --- 从界面中提取边坡等对象

        /// <summary> 从 AutoCAD 界面中选择横断面轴线 </summary>
        public static SubgradeSection GetSection(DocumentModifier docMdf)
        {
            SubgradeSection sec = null;
            var op = new PromptEntityOptions("\n选择要提取的横断面轴线");
            op.AddAllowedClass(typeof(Line), true);

            var res = docMdf.acEditor.GetEntity(op);
            if (res.Status == PromptStatus.OK)
            {
                var line = res.ObjectId.GetObject(OpenMode.ForRead) as Line;
                if (line != null && line.Layer == Options_LayerNames.LayerName_CenterAxis)
                {
                    var si = SectionInfo.FromCenterLine(line);
                    if (si != null && si.FullyCalculated)
                    {
                        sec = new SubgradeSection(docMdf, line, si);
                    }
                    else
                    {
                        MessageBox.Show($"选择的道路中心线对象所对应的横断面未进行构造，" +
                                        $"\r\n请先调用“{SectionsConstructor.CommandName}”命令，以构造整个项目的横断面系统。",
                            "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return sec;
        }

        /// <summary> 从界面中搜索边坡线 </summary>
        /// <param name="ed"></param>
        /// <param name="left">true 表示只选择左边的边坡，false 表示只选择右边的边坡，null 表示选择左右两侧的边坡</param>
        /// <returns></returns>
        public static List<Polyline> SelecteSlopeLines(Editor ed, bool? left)
        {
            // Create our options object
            var pso = new PromptSelectionOptions();

            // Set our prompts to include our keywords
            string kws = pso.Keywords.GetDisplayString(true);
            pso.MessageForAdding = "\n选择多条边坡线 " + kws; // 当用户在命令行中输入A（或Add）时，命令行出现的提示字符。
            pso.MessageForRemoval = "\n选择多条边坡线 " + kws;
            // 当用户在命令行中输入Re（或Remove）时，命令行出现的提示字符。
            // pso.SingleOnly = true;

            SelectionFilter filter = null;
            if (!left.HasValue)
            {
                filter = SlopeLine.Filter;
            }
            else if (left.Value)
            {
                filter = SlopeLine.FilterLeft;
            }
            else
            {
                filter = SlopeLine.FilterRight;
            }

            var psr = ed.GetSelection(pso, filter);

            if (psr.Status == PromptStatus.OK)
            {
                return psr.Value.GetObjectIds().Select(id => id.GetObject(OpenMode.ForRead) as Polyline).ToList();
            }
            return new List<Polyline>();
        }

        /// <summary> 从界面中选择已经构造好的边坡对象。对于已经计算过的边坡，不再进行重新计算，而保留其原有的数据 </summary>
        /// <param name="docMdf"></param>
        /// <param name="left">true 表示只选择左边的边坡，false 表示只选择右边的边坡，null 表示选择左右两侧的边坡</param>
        /// <returns></returns>
        public static List<SlopeLine> SelecteExistingSlopeLines(DocumentModifier docMdf, bool? left, bool sort)
        {
            // var allSections = ProtectionUtils.GetAllSections(docMdf);
            var slopeLines = SelecteSlopeLines(docMdf.acEditor, left: left);
            return ConstructSlopeLinesFromPlines(docMdf, slopeLines, sort: sort);
        }

        /// <summary> 从界面中选择已经构造好的边坡对象。对于已经计算过的边坡，不再进行重新计算，而保留其原有的数据 </summary>
        /// <param name="docMdf"></param>
        /// <returns></returns>
        public static List<SlopeLine> GetAllExistingSlopeLines(DocumentModifier docMdf, bool sort)
        {
            var psr = docMdf.acEditor.SelectAll(SlopeLine.Filter);
            var slpLines = new List<Polyline>();
            if (psr.Status == PromptStatus.OK)
            {
                slpLines = psr.Value.GetObjectIds().Select(id => id.GetObject(OpenMode.ForRead) as Polyline).ToList();
            }
            if (slpLines.Count > 0)
            {
                return ConstructSlopeLinesFromPlines(docMdf, slpLines, sort: sort);
            }
            else
            {
                return new List<SlopeLine>();
            }
        }

        private static List<SlopeLine> ConstructSlopeLinesFromPlines(DocumentModifier docMdf, List<Polyline> plines,
            bool sort)
        {
            var slpLines = new List<SlopeLine>();
            string errMsg;
            foreach (var sl in plines)
            {
                var slpLine = SlopeLine.Create(docMdf, sl, out errMsg);
                if (slpLine != null)
                {
                    // 将存储的数据导入边坡对象
                    slpLine.ImportSlopeData(slpLine.XData);
                    //
                    if (!slpLine.XData.FullyCalculated)
                    {
                        slpLine.CalculateXData();
                    }
                    slpLines.Add(slpLine);
                }
                else
                {
                    docMdf.WriteNow(errMsg);
                }
            }
            if (sort)
            {
                slpLines.Sort(CompareStation);
            }
            return slpLines;
        }

        #endregion
        
        #region --- 桩号处理

        private static readonly Regex StationReg = new Regex(@"K(\d+)\+(\d*.*)"); // K51+223.392

        /// <summary> 将表示里程的字符数据转换为对应的数值 </summary>
        /// <param name="station"></param>
        /// <returns>如果无法正常解析，则返回 null</returns>
        public static double? GetStationFromString(string station)
        {
            var mt = StationReg.Match(station);
            if (mt.Success)
            {
                int k;
                double m;
                if (int.TryParse(mt.Groups[1].Value, out k) && double.TryParse(mt.Groups[2].Value, out m))
                {
                    return k * 1000 + m;
                }
            }
            return null;
        }

        /// <summary> 将桩号数值表示为 K23+456.789 ~ K23+456.789 的形式 </summary>
        /// <param name="startStation">要进行转换的起始桩号的数值 </param>
        /// <param name="endStation"> 要进行转换的结尾桩号的数值 </param>
        /// <param name="maxDigits">最大的小数位数</param>
        public static string GetStationString(double startStation, double endStation, int maxDigits)
        {
            return GetStationString(startStation, maxDigits) + @"~" + GetStationString(endStation, maxDigits);
        }

        /// <summary> 将桩号数值表示为 K23+456.789 的形式 </summary>
        /// <param name="station">要进行转换的桩号的数值</param>
        /// <param name="maxDigits">最大的小数位数</param>
        public static string GetStationString(double station, int maxDigits)
        {
            string res = null;
            var k = (int)Math.Floor(station / 1000);
            var meters = station % 1000;
            var miniMeters = meters % 1;
            if (miniMeters != 0)
            {
                var digits = new string('0', maxDigits);
                res += $"K{k}+{meters.ToString("000." + digits)}";
            }
            else
            {
                // 整米数桩号
                res = $"K{k}+{meters.ToString("000")}";
            }
            return res;
        }

        #endregion

        #region --- 各种比较排序

        /// <summary> 桩号小的在前面 </summary>
        public static int CompareStation<T>(StationInfo<T> x, StationInfo<T> y)
        {
            return x.Station.CompareTo(y.Station);
        }

        /// <summary> 桩号小的在前面 </summary>
        public static int CompareStation(SubgradeSection slopeData1, SubgradeSection slopeData2)
        {
            return slopeData1.XData.Station.CompareTo(slopeData2.XData.Station);
        }

        /// <summary> 桩号小的在前面 </summary>
        public static int CompareStation(SlopeLine slopeline1, SlopeLine slopeline2)
        {
            return slopeline1.Station.CompareTo(slopeline2.Station);
        }

        #endregion

        #region ---   字典 Dictionary 与 符号表 SymbolTable 操作

        /// <summary> 索引水位线图层 </summary>
        /// <returns></returns>
        public static LayerTableRecord GetOrCreateLayer_WaterLine(DocumentModifier docMdf)
        {
            var l = eZcad.Utility.SymbolTableUtils.GetOrCreateLayer(docMdf.acTransaction, docMdf.acDataBase, SQConstants.LayerName_WaterLine);
            l.UpgradeOpen();
            l.Color = Color.FromColor(System.Drawing.Color.Aqua);
            l.LineWeight = LineWeight.LineWeight070;
            l.DowngradeOpen();
            return l;
        }

        #endregion
        
    }
}
