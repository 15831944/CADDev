using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcad.Utility;
using eZcad_AddinManager.Addins;
using eZcadTools;
using eZcadTools.Addins;
using eZcadTools.AppSetup;
using eZcadTools.Utility;
using eZcad_AddinManager;
using AddinOptions = eZcadTools.AppSetup.AddinOptions;

[assembly: CommandClass(typeof(PolylineVerterxesExtracter))]

namespace eZcadTools.Addins
{
    /// <summary> 提取三维多段线中的顶点，并生成对应的点元素 </summary>
    [EcDescription(CommandDescription)]
    public class PolylineVerterxesExtracter : ICADExCommand
    {

        private DocumentModifier _docMdf;

        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = @"Polyline2Point";
        private const string CommandText = @"提取顶点";
        private const string CommandDescription = @" 先通过3DPoly命令绘制三维多段线，再提取三维多段线中的顶点，并生成对应的点元素 ";

        /// <summary> 计算选择的所有曲线的面积与长度之和 </summary>
        /// <summary> 沿着道路纵向绘制边坡线 </summary>
        [CommandMethod(eZcadTools.AppSetup.AddinOptions.GroupCommnad, CommandName, CommandFlags.UsePickSet)
        , DisplayName(CommandText), Description(CommandDescription)
            , RibbonItem(CommandText, CommandDescription, AddinOptions.CmdImageDirectory + "ClosedAreaSumup_32.png")]
        public void Polyline2Point()
        {
            DocumentModifier.ExecuteCommand(Polyline2Point);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new PolylineVerterxesExtracter();
            return AddinManagerDebuger.DebugInAddinManager(s.Polyline2Point,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        /// <summary> 计算选择的所有曲线的面积与长度之和 </summary>
        public ExternalCmdResult Polyline2Point(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            _docMdf = docMdf;
            //
            var pl = SelectUtils.PickEntity<Polyline3d>(docMdf.acEditor, message: "选择一个三维多段线对象");
            if (pl == null) return ExternalCmdResult.Cancel;

            //
            var cs = EditStateIdentifier.GetCurrentEditState(docMdf);
            cs.CurrentBTR.UpgradeOpen();

            foreach (ObjectId id in pl)
            {
                var vert = id.GetObject(OpenMode.ForRead) as PolylineVertex3d;
                if (vert != null)
                {
                    var pt = vert.Position;
                    DBPoint p = new DBPoint(pt);
                    cs.CurrentBTR.AppendEntity(p);
                    docMdf.acTransaction.AddNewlyCreatedDBObject(p, true);
                }
            }
            cs.CurrentBTR.DowngradeOpen();

            return ExternalCmdResult.Commit;
        }
    }
}