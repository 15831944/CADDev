using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcad_AddinManager;
using eZcad_AddinManager.AppSetup;
using eZcadDebuger.AppSetup;

namespace eZcadDebuger.OnCode
{
    [EcDescription(CommandDescription)]
    public class TextStyles : ICADExCommand
    {
        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = @"ModifyTextStyles";

        private const string CommandText = @"文字样式统一";
        private const string CommandDescription = @"统一修改文字样式";

        /// <summary> 将多个单行文字的对齐方式进行修改，并整体进行竖向中心对齐 </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CommandName,
            CommandFlags.Interruptible | CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, AddinOptions.CmdImageDirectory + "HighFill_32.png")]
        public void ModifyTextStyles()
        {
            DocumentModifier.ExecuteCommand(ModifyTextStyles);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new TextStyles();
            return AppSetup.AddinManagerDebuger.DebugInAddinManager(s.ModifyTextStyles,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        private DocumentModifier _docMdf;

        /// <summary> 将所有无效的字体样式修改为 HZTXT 字体 </summary>
        private ExternalCmdResult ModifyTextStyles(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            _docMdf = docMdf;
            var textStyles = docMdf.acTransaction.GetObject
                (docMdf.acDataBase.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            docMdf.WriteLineIntoDebuger("字体集合的修改情况");
            foreach (var textStyleId in textStyles)
            {
                bool modified = false;
                var textStyle = docMdf.acTransaction.GetObject(textStyleId, OpenMode.ForRead) as TextStyleTableRecord;
                if (IsValidTextStyle(textStyle) && KeepUnchanged(textStyle))
                {
                    modified = false;
                    // 不进行更改
                }
                else
                {
                    textStyle.UpgradeOpen();
                    textStyle.FileName = "romans.shx";
                    textStyle.BigFontFileName = "hztxt.shx";
                    textStyle.XScale = 0.7; // 宽度因子
                    textStyle.DowngradeOpen();
                    // textStyle.ObliquingAngle = 0;  // f(倾斜角度) = ObliquingAngle


                    //
                    modified = true;
                }
                docMdf.WriteLineIntoDebuger($"{textStyle.Name} 被修改： {modified}");
            }
            return ExternalCmdResult.Commit;
        }

        #region --- 字体属性判断

        /// <summary> 当前操作系统的字体集合 </summary>
        private readonly FontFamily[] FontFamilys = (new InstalledFontCollection()).Families;

        /// <summary> 某字体在当前系统中是有效定义 </summary>
        /// <param name="ts"></param>
        private bool IsValidTextStyle(TextStyleTableRecord ts)
        {
            if (ts.IsShapeFile)
            {
                // IsShapeFile 为 true， 表示字体样式中没有勾选“使用大字体”，此字体样式是否支持中文由选择的形字体决定。
                // IsShapeFile 为 false， 表示字体样式中勾选了“使用大字体”，此字体样式是否支持中文由其 BigFontFileName 决定。
                return true;
            }
            else
            {
                if (FontFamilys.Any(r => r.Name == ts.FileName))
                {
                    // 说明字体有效
                    return true;
                }
                else
                {
                    // 由于无法解析字体名称对应的文件后缀名，所以这里也不能说明字体无定义，
                    // 先都返回 true 吧。
                    return true;
                }
            }
        }


        /// <summary> 手动指定不进行修改的字体样式名称（全部写成大写） </summary>
        /// <remarks> 字体样式名中带有"|" ，表示此字体样式为参照文件中的字体样式，默认不对其进行修改 </remarks>
        private static readonly string[] KeepUnchangedTextStyle = { "|", "STS", };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        private bool KeepUnchanged(TextStyleTableRecord ts)
        {
            string tsName = ts.Name;
            // 如果在不改变的列表中，则直接返回 true
            foreach (var unChangedTs in KeepUnchangedTextStyle)
            {
                if (tsName.ToUpper().Contains(unChangedTs))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}