﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcad_AddinManager;
using RESD.Cmds;
using RESD.DataExport;
using RESD.Entities;
using RESD.Utility;
using RESD.AppSetup;

[assembly: CommandClass(typeof(InfosGetter_ThinFill))]

namespace RESD.Cmds
{
    /// <summary> 提取并计算边坡防护工程量 </summary>
        [EcDescription(CommandDescription)]
    public class InfosGetter_ThinFill : ICADExCommand
    {
        private DocumentModifier _docMdf;

        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = "ThinFillShallowCut";

        private const string CommandText = @"低填浅挖";
        private const string CommandDescription = @"提取低填浅挖工程数量表";

        /// <summary> 将所有的边坡信息提取出来并制成相应表格 </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CommandName, SQConstants.ModelState | CommandFlags.UsePickSet)
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, AddinOptions.ImageDirectory + "ThinFill_32.png")]
        public void ThinFillShallowCut()
        {
            DocumentModifier.ExecuteCommand(ThinFillShallowCut);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
                ref IList<ObjectId> elementSet)
        {
            var sp = new InfosGetter_ThinFill();
            return AddinManagerDebuger.DebugInAddinManager(sp.ThinFillShallowCut,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        /// <summary> 将所有的边坡信息提取出来并制成相应表格 </summary>
        public ExternalCmdResult ThinFillShallowCut(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            _docMdf = docMdf;
            SQUtils.SubgradeEnvironmentConfiguration(docMdf);

            var centerLines = SQUtils.SelecteSectionLines(docMdf.acEditor);
            if (centerLines == null || centerLines.Count == 0) return ExternalCmdResult.Cancel;

            // 
            // 所有的断面
            var allSections = SQUtils.GetAllSections(docMdf, sort: true);
            // var allStations = allSections.Select(r => r.XData.Station).ToArray();

            // 要处理的断面
            var handledSections = new List<SubgradeSection>();
            string errMsg = null;
            foreach (var sl in centerLines)
            {
                var info = SectionInfo.FromCenterLine(sl);
                // 方式一：重新构造
                // var axis = new CenterAxis(docMdf, sl, info);

                // 方式二：从总集合中索引
                var axis = allSections.FirstOrDefault(r => r.XData.Station == info.Station);

                if (axis != null)
                {
                    handledSections.Add(axis);
                }
            }
            //
            // 过滤 

            // 将边坡防护数据导出
            var exporter = new Exporter_ThinFillShallowCut(docMdf, handledSections, allSections);
            exporter.ExportThinFillShallowCut();
            return ExternalCmdResult.Commit;
        }
    }
}