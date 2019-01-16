﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcad_AddinManager;
using eZcadTools.Addins;
using eZcadTools.AppSetup;
using AddinOptions = eZcadTools.AppSetup.AddinOptions;

// This line is not mandatory, but improves loading performances
// 测试中，如果不使用下面这条，则在AutoCAD中对应的 External Command 不能正常加载。
[assembly: CommandClass(typeof(SelSetIntersector))]

namespace eZcadTools.Addins
{
    /// <summary> 在新选择集中过滤出与当前选择集不相交的对象 </summary>
    [EcDescription(CommandDescription)]
    internal class SelSetIntersector : ICADExCommand
    {
        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = @"IntersectSelSet";

        private const string CommandText = @"选择交集";
        private const string CommandDescription = @"在新选择集中过滤出与当前选择集不相交的对象";

        /// <summary> 计算选择的所有曲线的面积与长度之和 </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CommandName,
            CommandFlags.Interruptible | CommandFlags.UsePickSet | CommandFlags.NoBlockEditor | CommandFlags.Redraw)
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, AddinOptions.CmdImageDirectory + "HighFill_32.png")]
        public void IntersectSelSet()
        {
            DocumentModifier.ExecuteCommand(IntersectSelSet);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new SelSetIntersector();
            return AddinManagerDebuger.DebugInAddinManager(s.IntersectSelSet,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        /// <summary> 在新选择集中过滤出与当前选择集不相交的对象 </summary>
        private ExternalCmdResult IntersectSelSet(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            var ed = docMdf.acEditor;
            if (impliedSelection != null && impliedSelection.Count > 0)
            {
                var oldSel = impliedSelection.GetObjectIds();
                var dxf = oldSel[0].ObjectClass.DxfName;
                // 必须先清空选择集
                ed.SetImpliedSelection(new ObjectId[0]);

                //
                SelectionSet sel = null;
                var continueSelect = false;
                do
                {
                    sel = GetSelectionWithKeywords(docMdf, ref dxf, out continueSelect);
                } while (continueSelect);

                if (sel != null)
                {
                    var newSels = sel.GetObjectIds();
                    var finnalSels = new List<ObjectId>();
                    foreach (var id in newSels)
                    {
                        if (oldSel.Contains(id))
                        {
                            finnalSels.Add(id);
                        }
                    }
                    ed.SetImpliedSelection(finnalSels.ToArray());
                }
            }
            else
            {
                docMdf.WriteNow("\n请先选择要用来取交集的源对象集合。");
                return ExternalCmdResult.Cancel;
            }
            return ExternalCmdResult.Commit;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="docMdf"></param>
        /// <param name="defaultDxfName"></param>
        /// <param name="continueSelect"></param>
        /// <returns></returns>
        private static SelectionSet GetSelectionWithKeywords(DocumentModifier docMdf, ref string defaultDxfName,
            out bool continueSelect)
        {
            var ed = docMdf.acEditor;

            // Create our options object
            var pso = new PromptSelectionOptions();


            pso.Keywords.Add("NoFilter", "无(N)", "无(N)"); //

            // Set our prompts to include our keywords
            var kws = pso.Keywords.GetDisplayString(true);
            pso.MessageForAdding = $"\n选择要取交集的对象。\n当前过滤类型：{defaultDxfName} " + kws; // 当用户在命令行中输入A（或Add）时，命令行出现的提示字符。
            pso.MessageForRemoval = pso.MessageForAdding; // 当用户在命令行中输入Re（或Remove）时，命令行出现的提示字符。

            // 响应事件
            var keywordsInput = false; // 用户在命令行中输入了关键字或者非关键字

            var defDxfName = defaultDxfName;
            pso.UnknownInput += delegate (object sender, SelectionTextInputEventArgs e)
            {
                keywordsInput = true;
                switch (e.Input)
                {
                    case "N": // 表示输入了关键字 NoFilter
                        defDxfName = null;
                        break;
                    case "n": // 表示输入了关键字 NoFilter
                        defDxfName = null;
                        break;
                    default:
                        defDxfName = e.Input;
                        break;
                }
                // !!! ((char)10) 对应按下 Enter 键，这一句会立即提交到AutoCAD 命令行中以结束 ed.GetSelection 对线程的阻塞。即是可以模拟当用户输入关键字时立即按下 Escape，此时 API 会直接结束 ed.GetSelection 并往下执行，其返回的 PromptSelectionResult.Status 属性值为 Error。
                docMdf.acActiveDocument.SendStringToExecute(((char)10).ToString(), true, false, true);
            };

            // Implement a callback for when keywords are entered
            // 当用户在命令行中输入关键字时进行对应操作。
            pso.KeywordInput +=
                delegate (object sender, SelectionTextInputEventArgs e)
                {
                    keywordsInput = true;
                    switch (e.Input)
                    {
                        case "NoFilter":
                            defDxfName = null;
                            break;
                        default:
                            break;
                    }
                    // !!! ((char)10) 对应按下 Enter 键，这一句会立即提交到AutoCAD 命令行中以结束 ed.GetSelection 对线程的阻塞。即是可以模拟当用户输入关键字时立即按下 Escape，此时 API 会直接结束 ed.GetSelection 并往下执行，其返回的 PromptSelectionResult.Status 属性值为 Error。
                    docMdf.acActiveDocument.SendStringToExecute(((char)10).ToString(), true, false, true);
                };

            // Finally run the selection and show any results

            PromptSelectionResult res = null;
            if (string.IsNullOrEmpty(defaultDxfName))
            {
                res = ed.GetSelection(pso);
            }
            else
            {
                var filterType = new TypedValue[1] { new TypedValue((int)DxfCode.Start, defaultDxfName) };
                res = ed.GetSelection(pso, new SelectionFilter(filterType));
            }

            if (res.Status == PromptStatus.OK)
            {
                continueSelect = false;
                return res.Value;
            }
            if (keywordsInput)
            {
                defaultDxfName = defDxfName;
                continueSelect = true;
                return null;
            }
            continueSelect = false;
            return null;
        }
    }
}