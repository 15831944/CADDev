using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcadDebuger.AppSetup;
using eZcad_AddinManager;

namespace eZcadDebuger.OnCode
{
    /// <summary> 批量修改标注样式 </summary>
    [EcDescription(CommandDescription)]
    public class DimStyles : ICADExCommand
    {
        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = @"ModifyDimStyle";

        private const string CommandText = @"修改标注";
        private const string CommandDescription = @"批量修改标注样式";

        /// <summary> 批量修改标注样式 </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CommandName,
            CommandFlags.Interruptible | CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, AddinOptions.CmdImageDirectory + "HighFill_32.png")]
        public void ModifyDimStyle()
        {
            DocumentModifier.ExecuteCommand(ModifyDimStyle);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new DimStyles();
            return AddinManagerDebuger.DebugInAddinManager(s.ModifyDimStyle,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        private DocumentModifier _docMdf;

        /// <summary> 批量修改标注样式 </summary>
        public ExternalCmdResult ModifyDimStyle(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            var dimStyles = docMdf.acTransaction.GetObject
                (docMdf.acDataBase.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
            foreach (var dimStyleId in dimStyles)
            {
                var dimStyle = docMdf.acTransaction.GetObject(dimStyleId, OpenMode.ForWrite) as DimStyleTableRecord;

                // 开始修改标注样式
                if (dimStyle.Name.StartsWith("D"))
                {
                    // 对标注样式进行修改

                    // dimStyle.Dimdec = 3; // 主单位精度
                    dimStyle.Dimgap = 0.65; // 文字从尺寸线偏移
                    dimStyle.Dimadec = 1;

                }
                else
                {
                    dimStyle.Dimdec = 0;
                }
            }

            return ExternalCmdResult.Commit;
        }
    }
}