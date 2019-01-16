using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcadDebuger.Addins;
using eZcadDebuger.AppSetup;
using eZcad_AddinManager;

[assembly: CommandClass(typeof(CurveAreaSumup))]
namespace eZcadDebuger.Addins
{
    /// <summary> 计算选择的所有曲线的面积与长度之和 </summary>
    [EcDescription(CommandDescription)]
    public class CurveAreaSumup : ICADExCommand
    {

        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = @"SumupCurveArea";
        private const string CommandText = @"面积求和";
        private const string CommandDescription = @"计算选择的所有曲线的面积与长度之和";

        /// <summary> 计算选择的所有曲线的面积与长度之和 </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CommandName,
            CommandFlags.Interruptible | CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, AddinOptions.CmdImageDirectory + "HighFill_32.png")]
        public void SumupCurveArea()
        {
            DocumentModifier.ExecuteCommand(SumupCurveArea);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new CurveAreaSumup();
            return AppSetup.AddinManagerDebuger.DebugInAddinManager(s.SumupCurveArea,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        private DocumentModifier _docMdf;
        /// <summary> 计算选择的所有曲线的面积与长度之和 </summary>
        public ExternalCmdResult SumupCurveArea(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            _docMdf = docMdf;
            // var pl = AddinManagerDebuger.PickObject<Curve>(docMdf.acEditor);
            var curves = SelectCurves();
            var area = 0.0;
            var length = 0.0;
            foreach (var c in curves)
            {
                if (c.Area > 0)
                {
                    area += c.Area;
                }
                length += c.GetDistanceAtParameter(c.EndParam);
            }
            docMdf.WriteLineIntoDebuger($"选中曲线数量：{curves.Count}");
            docMdf.WriteLineIntoDebuger($"曲线总长度：{length}");
            docMdf.WriteLineIntoDebuger($"曲线总面积：{area}");
            return ExternalCmdResult.Commit;
        }

        private List<Curve> SelectCurves()
        {
            var op = new PromptSelectionOptions();
            op.MessageForAdding = "\n选择要计算面积与长度的曲线"; // 当用户在命令行中输入A（或Add）时，命令行出现的提示字符。
            op.MessageForRemoval = op.MessageForAdding;

            var filterType = new[]
           {
                new TypedValue((int) DxfCode.Operator, "<OR"),
                new TypedValue((int) DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int) DxfCode.Start, "ARC"),
                new TypedValue((int) DxfCode.Start, "LINE"),
                new TypedValue((int) DxfCode.Start, "SPLINE"),
                new TypedValue((int) DxfCode.Start, "CIRCLE"),
                new TypedValue((int) DxfCode.Start, "ELLIPSE"),
                new TypedValue((int) DxfCode.Operator, "OR>")
            };

            var res = _docMdf.acEditor.GetSelection(op, new SelectionFilter(filterType));
            var curves = new List<Curve>();
            if (res.Status == PromptStatus.OK)
            {
                foreach (var id in res.Value.GetObjectIds())
                {
                    var c = id.GetObject(OpenMode.ForRead) as Curve;
                    if (c != null)
                    {
                        curves.Add(c);
                    }
                }
            }

            return curves;
        }
    }
}