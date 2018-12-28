﻿using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using eZcad.AddinManager;
using eZcad.RESD;
using eZcad.RESD.Cmds;

namespace eZcad.SQcmds
{

    #region ---   横断面系统


    [EcDescription("在 AutoCAD 界面中快速导航到指定的桩号")]
    public class Ec_NavigateStation : ICADExCommand
    {
        public ExternalCommandResult Execute(SelectionSet impliedSelection, ref string errorMessage,
            ref IList<ObjectId> elementSet)
        {
            var s = new StationNavigator();
            return SQAddinManagerDebuger.DebugInAddinManager(s.NavigateStation,
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
            return SQAddinManagerDebuger.DebugInAddinManager(s.ConstructSlopes,
                impliedSelection, ref errorMessage, ref elementSet);
        }
    }

    
    #endregion

}