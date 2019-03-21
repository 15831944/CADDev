﻿using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcad.Utility;
using eZcad_AddinManager;
using RESD.Cmds;
using RESD.Entities;
using RESD.SlopeProtection;
using RESD.Utility;
using RESD.AppSetup;

[assembly: CommandClass(typeof(SlopeConstructor))]

namespace RESD.Cmds
{
    /// <summary>
    /// 创建边坡并设置每一个边坡的数据
    /// </summary>
    [EcDescription(CommandDescription)]
    public class SlopeConstructor : ICADExCommand
    {
        private DocumentModifier _docMdf;

        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = "ConstructSlopes";
        private const string CommandText = @"创建边坡";
        private const string CommandDescription = @"创建边坡并设置每一个边坡的数据";

        /// <summary> 创建边坡并设置每一个边坡的数据 </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CommandName, CommandFlags.UsePickSet)
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, AddinOptions.ImageDirectory + "ConstructSlopes_32.png")]
        public void ConstructSlopes()
        {
            DocumentModifier.ExecuteCommand(ConstructSlopes);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage, ref IList<ObjectId> elementSet)
        {
            var s = new SlopeConstructor();
            return AddinManagerDebuger.DebugInAddinManager(s.ConstructSlopes,
                impliedSelection, ref errorMessage, ref elementSet);
        }
        #endregion

        /// <summary> 创建边坡并设置每一个边坡的数据 </summary>
        public ExternalCmdResult ConstructSlopes(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            _docMdf = docMdf;
            SQUtils.SubgradeEnvironmentConfiguration(docMdf);
            var justModifyCalculatedSlopes = ModifyOrAdd(docMdf.acEditor);
            var slopeLines = SQUtils.SelecteSlopeLines(docMdf.acEditor, left: null);
            if (slopeLines != null && slopeLines.Count > 0)
            {
                ConfigerSlopes(slopeLines, justModifyCalculatedSlopes);
            }
            return ExternalCmdResult.Commit;
        }

        /// <summary> 是要添加边坡线 还是 对已有边坡线进行修改 </summary>
        private static bool ModifyOrAdd(Editor ed)
        {
            var op = new PromptKeywordOptions(
                messageAndKeywords: "\n构造边坡数据<添加>[修改(M) / 添加(A)]:",
                globalKeywords: "修改 添加");
            // 
            op.AllowNone = true;
            op.AllowArbitraryInput = false;
            //
            var res = ed.GetKeywords(op);
            if (res.Status == PromptStatus.OK)
            {
                if (res.StringResult == "修改")
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary> 根据边坡多段线进行道路边坡的筛选、信息提取、防护方式的设置等操作 </summary>
        public void ConfigerSlopes(IList<Polyline> slopeLines, bool justModifyCalculated)
        {
            if (slopeLines == null || slopeLines.Count == 0) return;
            //
            var app = SymbolTableUtils.GetOrCreateAppName(_docMdf.acDataBase, _docMdf.acTransaction, SlopeData.AppName);

            var selectedSlpLines = new List<SlopeLine>();
            string errMsg;
            foreach (var sl in slopeLines)
            {
                var slpLine = SlopeLine.Create(_docMdf, sl, out errMsg);
                if (slpLine != null)
                {
                    // 将存储的数据导入边坡对象
                    slpLine.ImportSlopeData(slpLine.XData);
                    //
                    if (!slpLine.XData.FullyCalculated || !justModifyCalculated)
                    {
                        slpLine.CalculateXData();
                    }
                    selectedSlpLines.Add(slpLine);
                }
                else
                {
                    _docMdf.WriteNow(errMsg);
                }
            }
            if (selectedSlpLines.Count == 0) return;

            var slopesWithSlopesegs = new List<SlopeLine>();
            var slopesWithoutSlopesegs = new List<SlopeLine>();
            foreach (var slp in selectedSlpLines)
            {
                if (slp.XData.Slopes.Count + slp.XData.Platforms.Count > 0)
                {
                    slopesWithSlopesegs.Add(slp);
                }
                else
                {
                    slopesWithoutSlopesegs.Add(slp);
                }
            }
            //
            var es = EditStateIdentifier.GetCurrentEditState(_docMdf);
            es.CurrentBTR.UpgradeOpen();
            //var layer_Slope = Utils.GetOrCreateLayer(_docMdf, ProtectionConstants.LayerName_ProtectionMethod_Slope);
            //var layer_Platform = Utils.GetOrCreateLayer(_docMdf, ProtectionConstants.LayerName_ProtectionMethod_Platform);
            // _docMdf.acDataBase.Clayer = layer_Slope.ObjectId;
            //
            // 1. 对有子边坡的边坡对象进行操作：显示界面，以进行填挖方与防护设置
            var listerForm = new SlopeSegLister(slopesWithSlopesegs);
            listerForm.ShowDialog(null);
            if (listerForm.ValueChanged)
            {
                var protLayers = ProtectionTags.MapProtectionLayers(_docMdf, slopesWithSlopesegs);
                foreach (var slp in slopesWithSlopesegs)
                {
                    slp.Pline.UpgradeOpen();
                    SetSlopeUI(slp);
                    slp.PrintProtectionMethod(es.CurrentBTR, protLayers);
                    //
                    slp.FlushXData();
                    slp.Pline.DowngradeOpen();

                    if (slp.XDataToBeCleared)
                    {
                        slp.ClearXData();
                    }
                }
            }
            // 2. 对没有子边坡的边坡对象进行操作
            foreach (var slp in slopesWithoutSlopesegs)
            {
                slp.Pline.UpgradeOpen();
                SetSlopeUI(slp);
                slp.FlushXData();
                slp.Pline.DowngradeOpen();
            }

            es.CurrentBTR.DowngradeOpen();
        }

        /// <summary> 对边坡线的显示样式进行设置 </summary>
        /// <param name="slp"></param>
        public static void SetSlopeUI(SlopeLine slp)
        {
            return;
            if (slp.XData.FillCut)
            {
                // 填方边坡
                // slp.CenterLine.DrawWaterLevel(WaterLevel, waterLevelLayer.Id); // 绘制水位线
                slp.Pline.ColorIndex = 2; // 黄色
                slp.Pline.LineWeight = LineWeight.LineWeight070;
            }
            else
            {
                // 挖方
                slp.Pline.ColorIndex = 3; // 绿色
                slp.Pline.LineWeight = LineWeight.LineWeight070;
            }
        }
    }
}