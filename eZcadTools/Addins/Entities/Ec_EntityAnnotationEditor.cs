using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcad.Utility;
using eZcad_AddinManager;
using eZcadTools.Addins.Entities;
using eZcadTools.AppSetup;
using eZcadTools.Debug;
using eZcadTools.Utility;
using AddinOptions = eZcadTools.AppSetup.AddinOptions;

[assembly: CommandClass(typeof(EntityAnnotationEditor))]

namespace eZcadTools.Addins.Entities
{
    /// <summary> <seealso cref="CommandDescription"/> </summary>
    [EcDescription(CommandDescription)]
    internal class EntityAnnotationEditor : ICADExCommand
    {
        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = @"EditEntityAnnotation";

        private const string CommandText = @"图元注释";
        private const string CommandDescription = @"对图元中的自定义注释进行读写";

        /// <summary> <seealso cref="CommandDescription"/> </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CommandName, CommandFlags.Interruptible | CommandFlags.UsePickSet)
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, AddinOptions.CmdImageDirectory + "HighFill_32.png")]
        public void TextRegex()
        {
            DocumentModifier.ExecuteCommand(EditEntityAnnotation);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new EntityAnnotationEditor();
            return AddinManagerDebuger.DebugInAddinManager(s.EditEntityAnnotation,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        private DocumentModifier _docMdf;

        /// <summary> <seealso cref="CommandDescription"/> </summary>
        public ExternalCmdResult EditEntityAnnotation(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            _docMdf = docMdf;
            //
            var ent = SelectUtils.PickEntity<Entity>(_docMdf.acEditor, "查看任意一个元素的注释");
            if (ent == null) return ExternalCmdResult.Commit;
            AnnotationEntity.SetAppName(_docMdf);
            var annoEnt = new AnnotationEntity(ent);
            var annots = annoEnt.ExtractAnnotsFromXdata();
            //
            annoEnt.WriteAnnotationsOnEditor(docMdf);
            bool cont;
            Entity newEnt = null;
            do
            {
                bool clearAnnot = false;
                bool editInForm = false;
                bool setNewValue = false;
                string newAnno = null;
                cont = ReadEntityAnnot(docMdf.acEditor, out clearAnnot, out editInForm, out setNewValue, out newEnt);
                if (!cont) return ExternalCmdResult.Commit;
                //
                if (clearAnnot)
                {
                    annoEnt.ClearAnnotations();
                    _docMdf.WriteNow();
                }
                else if (editInForm)
                {
                    _docMdf.WriteNow();
                }
                else if (setNewValue)
                {
                    newAnno = annots.Count == 0 ? "" : annots[0];
                    cont = SetAnnotations(docMdf.acEditor, ref newAnno);
                    if (!cont) return ExternalCmdResult.Commit;
                    annoEnt.SetAnnotsToXdata(newAnno);
                    //
                    annoEnt.WriteAnnotationsOnEditor(docMdf);
                }
                else
                {
                    // 选择了另一个元素
                    if (newEnt != null)
                    {
                        annoEnt = new AnnotationEntity(newEnt);
                        annots = annoEnt.ExtractAnnotsFromXdata();
                        //
                        annoEnt.WriteAnnotationsOnEditor(docMdf);
                    }
                }
            } while (cont);

            //
            return ExternalCmdResult.Commit;
        }

        #region ---   界面操作

        /// <summary> 在界面中选择一个对象 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ed"></param>
        /// <returns></returns>
        private static bool ReadEntityAnnot(Editor ed, out bool clearAnnots, out bool editInForm, out bool setNewValue, out Entity ent)
        {
            clearAnnots = false;
            editInForm = false;
            setNewValue = false;
            ent = null;
            var op = new PromptEntityOptions("查看任意一个元素的注释")
            {
                AllowNone = true,
            };
            //op.SetRejectMessage($"请选择一个 {typeof(T).FullName} 对象");
            //op.AddAllowedClass(typeof(T), exactMatch: false);
            op.SetMessageAndKeywords(messageAndKeywords: "\n[清除注释(C) / 窗口编辑(F) / 设置新值(S)]:",
              globalKeywords: "K清除注释 K窗口编辑 K设置新值"); // 默认值写在前面

            var res = ed.GetEntity(op);
            if (res.Status == PromptStatus.OK)
            {
                ent = res.ObjectId.GetObject(OpenMode.ForRead) as Entity;
                // 继续读取下一个元素注释
                return true;
            }
            else if (res.Status == PromptStatus.Keyword)
            {
                var key = res.StringResult;
                if (key == "K清除注释")
                {
                    clearAnnots = true;
                }
                else if (key == "K窗口编辑")
                {
                    editInForm = true;
                }
                else if (key == "K设置新值")
                {
                    setNewValue = true;
                }
                else
                {
                    // 输入的为一般字符，可以直接作为属性值
                    return true;
                }
                return true;
            }
            else if (res.Status == PromptStatus.None)
            {
                // 直接按下了 右键 或 Enter
                return false;
            }
            return false;
        }

        /// <summary> 在命令行中获取一个字符 </summary>
        /// <param name="value">成功获得的数值</param>
        /// <returns>操作成功，则返回 true，操作失败或手动取消操作，则返回 false</returns>
        private static bool GetAnnotationsKey(Editor ed, out bool clearAnnots, out bool editInForm, out bool setNewValue,
            out string newAnnot)
        {
            clearAnnots = false;
            editInForm = false;
            setNewValue = false;
            newAnnot = "";
            var op = new PromptKeywordOptions("元素注释编辑器")
            {
                AllowArbitraryInput = false,
                AllowNone = false,
            };
            op.SetMessageAndKeywords(messageAndKeywords: "\n[清除注释(C) / 窗口编辑(F) / 设置新值(S)]:",
                globalKeywords: "K清除注释 K窗口编辑 K设置新值"); // 默认值写在前面
            //
            var res = ed.GetKeywords(op);
            if (res.Status == PromptStatus.OK)
            {
                var key = res.StringResult;
                if (key == "K清除注释")
                {
                    clearAnnots = true;
                }
                else if (key == "K窗口编辑")
                {
                    editInForm = true;
                }
                else if (key == "K设置新值")
                {
                    setNewValue = true;
                }
                else
                {
                    // 输入的为一般字符，可以直接作为属性值
                    newAnnot = key;
                }
                return true;
            }
            return false;
        }


        /// <summary> 在命令行中获取一个字符 </summary>
        /// <param name="annot">成功获得的数值</param>
        /// <returns>操作成功，则返回 true，操作失败或手动取消操作，则返回 false</returns>
        private static bool SetAnnotations(Editor ed, ref string annot)
        {
            var op = new PromptStringOptions("\n元素的描述信息")
            {
                AllowSpaces = true,
                UseDefaultValue = true,
                // DefaultValue = annot,
            };
            var res = ed.GetString(op);
            if (res.Status == PromptStatus.OK)
            {
                annot = res.StringResult;
                return true;
            }
            return false;
        }

        #endregion
    }
}