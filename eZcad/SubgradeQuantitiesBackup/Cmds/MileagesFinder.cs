﻿using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using eZcad.SubgradeQuantityBackup.Cmds;
using eZcad.SubgradeQuantityBackup.Utility;
using eZcad.Utility;
using Application = System.Windows.Application;
using Utils = eZstd.Miscellaneous.Utils;

[assembly: CommandClass(typeof(StationsFinder))]

namespace eZcad.SubgradeQuantityBackup.Cmds
{
    /// <summary> 提取所有的横断面块参照的信息 </summary>
    public class StationsFinder
    {
        private DocumentModifier _docMdf;
        #region --- 命令设计

        /// <summary> 提取所有的横断面块参照的信息 </summary>
        [CommandMethod(eZConstants.eZGroupCommnad, "FindAllSections", CommandFlags.UsePickSet),
         DisplayName(@"横断面信息"), Description("导出所有横断面信息")]
        public void EcFindAllSections()
        {
            DocumentModifier.ExecuteCommand(FindAllSections);
        }

        /// <summary> 提取所有的横断面块参照的信息 </summary>
        public static void FindAllSections(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            var infoBlocks = GetAllInfoBlocks(docMdf);
            if (infoBlocks != null && infoBlocks.Length > 0)
            {
                docMdf.WriteNow($"\n找到{infoBlocks.Length}个横断面对象！");
                var infoPath = Utils.ChooseSaveFile("数据输出的文本", "文本(*.txt) | *.txt");
                if (infoPath == null) return;

                using (var sw = new StreamWriter(infoPath))
                {
                    foreach (var id in infoBlocks)
                    {
                        var blr = id.GetObject(OpenMode.ForRead) as BlockReference;
                        if (blr != null)
                        {
                            foreach (ObjectId attId in blr.AttributeCollection)
                            {
                                var att = attId.GetObject(OpenMode.ForRead) as AttributeReference;
                                if (att.Tag == ProtectionOptions.StationFieldDef)
                                {
                                    sw.WriteLine(att.TextString);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="docMdf"></param>
        /// <returns>如果未成功搜索到有效的里程信息，则返回 null</returns>
        public static double[] GetAllStations(DocumentModifier docMdf)
        {
            var infoBlocks = GetAllInfoBlocks(docMdf);
            if (infoBlocks != null && infoBlocks.Length > 0)
            {
                var stations = new List<double>();
                bool promp = true;
                foreach (var id in infoBlocks)
                {
                    double? mile = null;
                    var blr = id.GetObject(OpenMode.ForRead) as BlockReference;
                    if (blr != null)
                    {
                        foreach (ObjectId attId in blr.AttributeCollection)
                        {
                            var att = attId.GetObject(OpenMode.ForRead) as AttributeReference;
                            if (att.Tag == ProtectionOptions.StationFieldDef)
                            {
                                var m = ProtectionUtils.GetStationFromString(att.TextString);
                                if (m.HasValue)
                                {
                                    mile = m.Value;
                                }
                                else
                                {
                                    // 提示
                                }
                            }
                        }
                    }
                    if (mile.HasValue)
                    {
                        stations.Add(mile.Value);
                    }
                    else
                    {
                        if (promp)
                        {
                            var res = MessageBox.Show($"未找到记录横断面信息的块定义“{ProtectionOptions.BlockName_SectionInfo}”，" +
                                                      $"或者在其中未找到记录里程信息的属性定义“{ProtectionOptions.StationFieldDef}”。" +
                                                      $"\r\n是否继续提示", "提示", MessageBoxButton.YesNoCancel,
                                MessageBoxImage.Information);
                            switch (res)
                            {
                                case MessageBoxResult.Yes:
                                    promp = true;
                                    break;
                                case MessageBoxResult.No:
                                    promp = false;
                                    break;
                                case MessageBoxResult.Cancel:
                                    return null;
                            }
                        }
                    }
                }
                return stations.ToArray();
            }
            return null;
        }

        /// <summary> 提取图纸中所有的横断面信息所对应的块参照对象 </summary>
        /// <param name="docMdf"></param>
        /// <returns></returns>
        public static ObjectId[] GetAllInfoBlocks(DocumentModifier docMdf)
        {
            // 获取当前文档编辑器

            // 创建TypedValue数组，定义过滤条件
            var acTypValAr = new TypedValue[]
            {
                new TypedValue((int) DxfCode.Start, "INSERT"),
                new TypedValue((int) DxfCode.LayerName, ProtectionOptions.LayerName_SectionInfo),
                new TypedValue((int) DxfCode.BlockName, ProtectionOptions.BlockName_SectionInfo),
            };

            // 将过滤条件赋给SelectionFilter对象
            var acSelFtr = new SelectionFilter(acTypValAr);

            // 请求在图形区域选择对象
            PromptSelectionResult acSSPrompt;
            acSSPrompt = docMdf.acEditor.SelectAll(acSelFtr);

            // 如果提示状态OK，说明已选对象
            if (acSSPrompt.Status == PromptStatus.OK)
            {
                return acSSPrompt.Value.GetObjectIds();
            }
            return null;
        }       
    }
}