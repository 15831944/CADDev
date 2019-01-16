﻿using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using eZcad_AddinManager;
using RESD.AppSetup;
using RESD.Cmds;

namespace RESD.SQcmds
{

    #region ---   横断面系统


    [EcDescription("在 AutoCAD 界面中快速导航到指定的桩号")]
    public class Ec_NavigateStation : ICADExCommand
    {
        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new StationNavigator();
            return AddinManagerDebuger.DebugInAddinManager(s.NavigateStation,
                impliedSelection, ref errorMessage, ref elementSet);
        }
    }

    #endregion

    #region ---   边坡防护的设置

    [EcDescription("创建边坡并设置每一个边坡的数据")]
    public class Ec_ConstructSlopes : ICADExCommand
    {
        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new SlopeConstructor();
            return AddinManagerDebuger.DebugInAddinManager(s.ConstructSlopes,
                impliedSelection, ref errorMessage, ref elementSet);
        }
    }

    
    #endregion

}