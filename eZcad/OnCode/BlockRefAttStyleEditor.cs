using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using eZcad.AddinManager;
using eZcad.Addins;
using eZcad.Debug;
using eZcad.OnCode;
using eZcad.Utility;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Exception = System.Exception;
[assembly: CommandClass(typeof(BlockRefAttStyleEditor))]

namespace eZcad.OnCode
{
    /// <summary> 对块参照实例中的属性定义的样式进行修改 </summary>
    [EcDescription(CommandDescription)]
    public class BlockRefAttStyleEditor : ICADExCommand
    {
        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = @"EditBlockRefAttStyles";
        private const string CommandText = @"修改块属性样式";
        private const string CommandDescription = @"对块参照实例中的属性定义的样式进行修改";

        /// <summary> 对块参照实例中的属性定义的样式进行修改 </summary>
        [CommandMethod(eZConstants.eZGroupCommnad, CommandName,
            CommandFlags.Interruptible | CommandFlags.UsePickSet) //  | CommandFlags.NoBlockEditor
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, eZConstants.ImageDirectory + "HighFill_32.png")]
        public void EditBlockRefAttStyles()
        {
            DocumentModifier.ExecuteCommand(EditBlockRefAttStyles);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new BlockRefAttStyleEditor();
            return eZcadAddinManagerDebuger.DebugInAddinManager(s.EditBlockRefAttStyles,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        private DocumentModifier _docMdf;

        /// <summary> 对块参照实例中的属性定义的样式进行修改 </summary>
        public ExternalCmdResult EditBlockRefAttStyles(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            var layerZero = docMdf.acDataBase.LayerZero;
            var ByBlockColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByBlock, 0);
            var ByBlockLineType = docMdf.acDataBase.ByBlockLinetype;
            var ByBlockLineWeight = Autodesk.AutoCAD.DatabaseServices.LineWeight.ByBlock;
            //
            var attDefs = SelectBlockReference();
            foreach (BlockReference blockRef in attDefs)
            {
                // 对块参照对象中的每一个属性字段进行设置。
                var atts = blockRef.GetAttributeReferences();
                foreach (var att in atts)
                {
                    att.UpgradeOpen();
                    att.LayerId = layerZero;
                    //
                    att.Color = ByBlockColor;
                    att.LineWeight = ByBlockLineWeight;
                    att.LinetypeId = ByBlockLineType;

                    // 调整文字样式
                    // att.Height = 1;

                    //
                    att.DowngradeOpen();
                }
            }
            return ExternalCmdResult.Commit;
        }

        /// <summary> 举例，选择多个属性定义对象 </summary>
        public static List<BlockReference> SelectBlockReference()
        {
            // 创建一个 TypedValue 数组，用于定义过滤条件
            var filterTypes = new TypedValue[]
            {
                new TypedValue((int) DxfCode.Start, "INSERT"),
            };

            // Create our options object
            var op = new PromptSelectionOptions();

            // Add our keywords
            //op.Keywords.Add("First");
            //op.Keywords.Add("Second");

            // Set our prompts to include our keywords
            string kws = op.Keywords.GetDisplayString(true);
            op.MessageForAdding = "\n 请选择一个或多个块参照 " + kws; // 当用户在命令行中输入A（或Add）时，命令行出现的提示字符。
            op.MessageForRemoval = "\nPlease remove objects from selection or " + kws;


            //获取当前文档编辑器
            Editor acDocEd = Application.DocumentManager.MdiActiveDocument.Editor;

            // 请求在图形区域选择对象
            var res = acDocEd.GetSelection(op, new SelectionFilter(filterTypes));

            var output = new List<BlockReference>();
            // 如果提示状态OK，表示对象已选
            if (res.Status == PromptStatus.OK)
            {
                var acSSet = res.Value.GetObjectIds();
                foreach (var id in acSSet)
                {
                    var obj = id.GetObject(OpenMode.ForRead) as BlockReference;
                    if (obj != null)
                    {
                        output.Add(obj);
                    }
                }
            }
            return output;
        }

    }
}