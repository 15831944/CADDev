using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using eZcad;
using eZcad.Utility;
using eZcadTools.Addins;
using eZcadTools.AppSetup;
using eZcad_AddinManager;

// This line is not mandatory, but improves loading performances
// 测试中，如果不使用下面这条，则在AutoCAD中对应的 External Command 不能正常加载。
[assembly: CommandClass(typeof(PointElevationRecorder))]

namespace eZcadTools.Addins
{
    /// <summary> 录入点的标高值 </summary>
    [EcDescription(CommandDescription)]
    internal class PointElevationRecorder : ICADExCommand
    {
        #region --- 命令设计

        /// <summary> 命令行命令名称，同时亦作为命令语句所对应的C#代码中的函数的名称 </summary>
        public const string CommandName = @"RecordPointElevation";

        private const string CommandText = @"录入标高";
        private const string CommandDescription = "录入点的标高值，并生成标高块，块名“" + BlockDefName + "”，块属性名“" + BlockAttributeName + "”";

        /// <summary> 批量修改标注样式 </summary>
        [CommandMethod(AddinOptions.GroupCommnad, CommandName,
            CommandFlags.Interruptible | CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)
        , DisplayName(CommandText), Description(CommandDescription)
        , RibbonItem(CommandText, CommandDescription, AddinOptions.CmdImageDirectory + "录入标高_32.png")]
        public void RecordPointElevation()
        {
            DocumentModifier.ExecuteCommand(RecordPointElevation);
        }

        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new PointElevationRecorder();
            return AddinManagerDebuger.DebugInAddinManager(s.RecordPointElevation,
                impliedSelection, ref errorMessage, ref elementSet);
        }

        #endregion

        private DocumentModifier _docMdf;

        private const string BlockDefName = "GC";
        private const string BlockAttributeName = "标高";

        /// <summary> 批量修改标注样式 </summary>
        public ExternalCmdResult RecordPointElevation(DocumentModifier docMdf, SelectionSet impliedSelection)
        {
            var dimStyles = docMdf.acTransaction.GetObject
                (docMdf.acDataBase.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;

            Point3d[] points = GetPoints(docMdf);
            int pointsCount = points.Length;
            docMdf.WriteNow($"一共选择了{pointsCount}个点");
            if (pointsCount > 0)
            {
                double[] elevations = GetElevations(docMdf, pointsCount);
                int elevsCount = elevations.Length;
                if (elevsCount == pointsCount)
                {
                    // 创建模型
                    // 以只读方式打开块表   Open the Block table for read
                    var acBlkTbl =
                        docMdf.acTransaction.GetObject(docMdf.acDataBase.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // 以写方式打开模型空间块表记录   Open the Block table record Model space for write
                    var model =
                        docMdf.acTransaction.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as
                            BlockTableRecord;

                    // CreatePoints(docMdf, model, points, elevations);
                    BlockTableRecord blockDef = FindBlockDefinition(docMdf, BlockDefName);
                    CreateBlockRefs(docMdf, model, blockDef, points, elevations);

                    model.DowngradeOpen();
                }
                else
                {
                    docMdf.WriteNow($"输入的标高个数与选择点的个数不相等");
                    return ExternalCmdResult.Cancel;
                }
            }
            return ExternalCmdResult.Commit;
        }

        private Point3d[] GetPoints(DocumentModifier docMdf)
        {
            var points = new List<Point3d>();
            int pointsCount = 1;
            Point3d? pickedPt = GetPoint(docMdf, $"添加要设置标高的点");
            while (pickedPt != null)
            {
                points.Add(pickedPt.Value);
                docMdf.WriteNow($"{pointsCount}({pickedPt.Value.X.ToString("0.000")},   {pickedPt.Value.Y.ToString("0.000")})");
                pointsCount += 1;
                pickedPt = GetPoint(docMdf, $"添加要设置标高的点");
            }
            return points.ToArray();
        }

        /// <summary> 在界面中点选一个点 </summary>
        private static Point3d? GetPoint(DocumentModifier docMdf, string msg)
        {
            var op = new PromptPointOptions(message: $"\n{msg}")
            {
                AllowNone = true, // true 表示允许用户直接按下回车或者右键，以退出 GetPoint() 方法，此时返回的 PromptPointResult.Status 为 None。
                //AllowArbitraryInput = false
            };
            //
            var res = docMdf.acEditor.GetPoint(op);
            if (res.Status == PromptStatus.OK)
            {
                return res.Value;
            }
            return null;
        }

        private double[] GetElevations(DocumentModifier docMdf)
        {
            var elevations = new List<double>();
            int elevsCount = 1;
            double? elevation = GetDouble(docMdf, $"添加要设置标高的点{elevsCount}");
            while (elevation != null)
            {
                elevations.Add(elevation.Value);
                elevsCount += 1;
                elevation = GetDouble(docMdf, $"添加要设置标高的点{elevsCount}");
            }
            return elevations.ToArray();
        }

        private double[] GetElevations(DocumentModifier docMdf, int wantedCounts)
        {
            var elevations = new List<double>();
            int elevsCount = 1;
            double? elevation;
            for (int i = 0; i < wantedCounts; i++)
            {
                elevation = GetDouble(docMdf, $"输入标高{i + 1}/{wantedCounts}");
                if (elevation != null)
                {
                    elevations.Add(elevation.Value);
                }
                else
                {
                    break;
                }
            }
            return elevations.ToArray();
        }

        /// <summary> 在命令行中获取一个双精度浮点 </summary>
        private static double? GetDouble(DocumentModifier docMdf, string msg)
        {
            var op = new PromptDoubleOptions(message: $"\n{msg}")
            {
                //
                AllowNegative = true,
                AllowNone = true, // true 表示允许用户直接按下回车或者右键，以退出 GetPoint() 方法，此时返回的 PromptPointResult.Status 为 None。
                AllowZero = true,
                AllowArbitraryInput = false
            };

            //
            var res = docMdf.acEditor.GetDouble(op);
            if (res.Status == PromptStatus.OK)
            {
                return res.Value;
            }
            return null;
        }

        private void CreatePoints(DocumentModifier docMdf, BlockTableRecord space, Point3d[] points, double[] elevations)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Point3d pos = new Point3d(points[i].X, points[i].Y, elevations[i]);
                DBPoint dbPt = new DBPoint(pos);
                space.AppendEntity(dbPt);
                docMdf.acTransaction.AddNewlyCreatedDBObject(dbPt, true);
            }
        }

        private BlockTableRecord FindBlockDefinition(DocumentModifier docMdf, string blockDefName)
        {
            var acBlkTbl = docMdf.acTransaction.GetObject(docMdf.acDataBase.BlockTableId, OpenMode.ForRead) as BlockTable;
            if (acBlkTbl.Has(blockDefName))
            {
                return docMdf.acTransaction.GetObject(acBlkTbl[blockDefName], OpenMode.ForRead) as BlockTableRecord;
            }
            return null;
        }

        private void CreateBlockRefs(DocumentModifier docMdf, BlockTableRecord space, BlockTableRecord blockDef,
            Point3d[] points, double[] elevations)
        {
            AttributeDefinition attDef = blockDef.GetAttributeDefinition(BlockAttributeName);
            if (attDef == null)
            {
                docMdf.WriteNow($"未找到块定义中的属性“{BlockAttributeName}”");
                return;
            }
            for (int i = 0; i < points.Length; i++)
            {
                Point3d pos = new Point3d(points[i].X, points[i].Y, elevations[i]);
                BlockReference blockRef = CreateBlockRef(docMdf, space, blockDef, attDef, pos);
            }
        }

        private BlockReference CreateBlockRef(DocumentModifier docMdf, BlockTableRecord space, BlockTableRecord blockDef,
            AttributeDefinition attDef,
            Point3d position)
        {
            BlockReference bref = new BlockReference(position, blockDef.Id);
            space.AppendEntity(bref);
            docMdf.acTransaction.AddNewlyCreatedDBObject(bref, true);
            //
            AttributeReference ar = new AttributeReference(attDef.Position.Add(position.GetAsVector()),
                position.Z.ToString(), BlockAttributeName, style: attDef.TextStyleId);
            bref.AttributeCollection.AppendAttribute(ar);
            ar.DowngradeOpen();  //  AttributeReference 在 DowngradeOpen 后，才会立即显示，否则要通过Refresh等方法使其 DowngradeOpen 才能显示。
            bref.DowngradeOpen();
            return bref;
        }
    }
}