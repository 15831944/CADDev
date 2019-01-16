using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using eZcad;
using eZcad.Utility;
using eZcad_AddinManager;
using RESD.Options;

namespace RESD.AppSetup
{
    internal class AddinManagerDebuger
    {
        internal static ExternalCommandResult DebugInAddinManager(ExternalCommand cmd,
            SelectionSet impliedSelection, ref string errorMessage, ref IList<ObjectId> elementSet)
        {
            var dat = new DllActivator_RESD();
            dat.ActivateReferences();

            using (var docMdf = new DocumentModifier(true))
            {
                try
                {
                    // 先换个行，显示效果更清爽
                    docMdf.WriteNow("\n");
                    // 刷新所有的全局选项到内存中
                    DbXdata.LoadAllOptionsFromDbToMemory(docMdf);
                    // 运行具体的命令
                    var canCommit = cmd(docMdf, impliedSelection);
                    //
                    switch (canCommit)
                    {
                        case ExternalCmdResult.Commit:
                            docMdf.acTransaction.Commit();
                            return ExternalCommandResult.Succeeded;
                            break;
                        case ExternalCmdResult.Cancel:
                            docMdf.acTransaction.Abort();
                            return ExternalCommandResult.Cancelled;
                            break;
                        default:
                            docMdf.acTransaction.Abort();
                            return ExternalCommandResult.Cancelled;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    docMdf.acTransaction.Abort(); // Abort the transaction and rollback to the previous state
                    errorMessage = ex.AppendMessage();
                    return ExternalCommandResult.Failed;
                }
            }
        }
    }
}