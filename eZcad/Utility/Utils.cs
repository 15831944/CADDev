﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Excel = Microsoft.Office.Interop.Excel;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using System;
using System.Text.RegularExpressions;
using Windows = eZstd.API.Windows;

namespace eZcad.Utility
{
    /// <summary>
    /// 提供一些基础性的操作工具
    /// </summary>
    /// <remarks></remarks>
    public static class Utils
    {
        /// <summary>
        /// 返回Nullable所对应的泛型。如果不是Nullable泛型，则返回null。
        /// </summary>
        /// <param name="typeIn"></param>
        /// <returns></returns>
        public static Type GetNullableGenericArgurment(Type typeIn)
        {
            // We need to check whether the property is NULLABLE
            if (typeIn.IsGenericType && typeIn.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // If it is NULLABLE, then get the underlying type. eg if "Nullable<int>" then this will return just "int"
                return typeIn.GetGenericArguments()[0];
            }
            else
            {
                return null;
            }
        }
        
        /// <summary> 指定的字符串中是否包含有非英文字符 </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool StringHasNonEnglish(string str)
        {
            // 1、用ASCII码判断：在 ASCII码表中，英文的范围是0 - 127，而汉字则是大于127。
            return str.Any(t => (int)t > 127);
        }

        #region ---   文件的打开与保存
        /// <summary> 选择一个或多个要打开的文件。成功选择，则返回对应文件的绝对路径，否则返回 null </summary>
        /// <param name="title">对话框的标题</param>
        /// <param name="filter"> 文件过滤规则，比如 
        /// “材料库(*.txt)| *.txt”、
        /// “Excel文件(*.xls; *.xlsx; *.xlsb)| *.xls; *.xlsx; *.xlsb”、
        /// “Excel工作簿(*.xlsx)|*.xlsx| Excel二进制工作簿(*.xlsb) |*.xlsb| Excel 97-2003 工作簿(*.xls)|*.xls” </param>
        /// <param name="multiselect"> 是否支持多选 </param>
        /// <returns> 成功选择，则返回对应文件的绝对路径，如果没有选择任何文件，则返回 null </returns>
        public static string[] ChooseOpenFile(string title, string filter, bool multiselect)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = title,
                CheckFileExists = true,
                AddExtension = true,
                Filter = filter,
                FilterIndex = 0,
                Multiselect = multiselect,
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (ofd.FileNames.Length > 0)
                {
                    return ofd.FileNames;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary> 选择要将数据保存到哪个文件。成功选择，则返回对应文件的绝对路径，否则返回 null </summary>
        /// <param name="title">对话框的标题</param>
        /// <param name="filter"> 文件过滤规则，比如 
        /// “材料库(*.txt)| *.txt”、
        /// “Excel文件(*.xls; *.xlsx; *.xlsb)| *.xls; *.xlsx; *.xlsb”、
        /// “Excel工作簿(*.xlsx)|*.xlsx| Excel二进制工作簿(*.xlsb) |*.xlsb| Excel 97-2003 工作簿(*.xls)|*.xls” </param>
        /// <returns> 成功选择，则返回对应文件的绝对路径，否则返回 null </returns>
        public static string ChooseSaveFile(string title, string filter)
        {
            var ofd = new SaveFileDialog()
            {
                Title = title,
                // CheckFileExists = true, // 文件不存在则不能作为有效路径
                //  CheckPathExists = true,
                AddExtension = true,
                Filter = filter,
                FilterIndex = 0,
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                return ofd.FileName.Length > 0 ? ofd.FileName : null;
            }
            return null;
        }
        #endregion

        /// <summary> 从字符中解析出坐标点，比如“1.2, 2.3, 5” </summary>
        public static Point3d? GetPointFromString(string coord)
        {
            var s = coord.Split(',');
            var xyz = new List<double>();
            double c = 0;
            foreach (var v in s)
            {
                if (double.TryParse(v, out c))
                {
                    xyz.Add(c);
                }
            }
            //
            switch (xyz.Count)
            {
                case 2: return new Point3d(xyz[0], xyz[1], 0);
                case 3: return new Point3d(xyz[0], xyz[1], xyz[2]);
                default:
                    return null;
            }

        }

        // 块定义，插入模型空间  
        public static ObjectId BlkInDb(BlockTableRecord block, Point3d pt, Database db)
        {
            BlockReference blkRef = null;

            ObjectId id = new ObjectId();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 打开模型空间  
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modeSpce = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                //  

                // 新建一个块引用  
                blkRef = new BlockReference(pt, block.ObjectId);
                id = modeSpce.AppendEntity(blkRef);
                tr.AddNewlyCreatedDBObject(blkRef, true);

                // 遍历块记录中的所有实体，加入块引用中  
                foreach (ObjectId idTemp in block)
                {
                    // 判断该实体是否是块定义 ，也可以以块定义方式打开，但是会产生事物嵌套  
                    if (idTemp.ObjectClass.Equals(Autodesk.AutoCAD.Runtime.RXObject.GetClass(typeof(AttributeDefinition))))
                    {
                        AttributeDefinition adDef = tr.GetObject(idTemp, OpenMode.ForRead) as AttributeDefinition;
                        if (adDef != null)
                        {
                            AttributeReference ar = new AttributeReference(adDef.Position, adDef.TextString, adDef.Tag, new ObjectId());
                            blkRef.AttributeCollection.AppendAttribute(ar);
                        }
                    }
                }

                tr.Commit();
            }

            return id;
        }

        /// <summary> 将插件程序注册到注册表中 </summary>
        /// <returns></returns>
        public static bool WriteRegistryKey()
        {
            // AutoCad 2014
            /* 
Windows Registry Editor Version 5.00
[HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\AutoCAD\R19.1\ACAD-D001:804\Applications\AddinManager]
"DESCRIPTION"="AddinManager"
"LOADCTRLS"=dword:00000002
"LOADER"="E:\\zengfy data\\GithubProjects\\CADDev\\eZcad\\bin\\Debug\\eZcad.dll"
"MANAGED"=dword:00000001
             */
            try
            {
                RegistryKey localMachine = Registry.LocalMachine;
                RegistryKey SOFTWARE = localMachine.OpenSubKey("SOFTWARE", true);
                RegistryKey Autodesk = SOFTWARE.OpenSubKey("Autodesk", true);
                RegistryKey AutoCAD = Autodesk.OpenSubKey("AutoCAD", true);
                RegistryKey R16_2 = AutoCAD.OpenSubKey("R16.2", true);
                RegistryKey ACAD = R16_2.OpenSubKey("ACAD-4001:804", true);
                RegistryKey Applications = ACAD.OpenSubKey("Applications", true);

                RegistryKey MXCAD = Applications.CreateSubKey("MXCAD");
                MXCAD.SetValue("LOADCTRLS", 0x02);
                MXCAD.SetValue("LOADER", "总目录" + @"bin\Debug\MXCAD.dll");
                MXCAD.SetValue("MANAGED", 0x01);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region ---   ObjectId 集合的排序

        /// <summary>
        ///     Sorts an array of ObjectIds based on a string property and order.
        /// </summary>
        /// <param name="ids">The array of IDs to sort.</param>
        /// <param name="propertySelector">A function selecting the string property.</param>
        /// <param name="orderSelector">A function to specify the selection order.</param>
        /// <returns>An ordered enumerable of key-value pairs.</returns>
        /// <remarks>
        /// 举例1：Sort ObjectIds based on the layer name
        ///     var sorted = psr.Value.GetObjectIds().Sort(propertySelector: id => id.Layer, orderSelector: kv => kv.Value);
        /// </remarks>
        public static List<KeyValuePair<ObjectId, string>> Sort(this ObjectId[] ids,
            Func<dynamic, string> propertySelector, Func<KeyValuePair<ObjectId, string>, string> orderSelector)
        {
            return Sort<string>(ids, propertySelector, orderSelector);
        }

        /// <summary>
        ///     Sorts an array of ObjectIds based on a string property and order.
        /// </summary>
        /// <param name="ids">The array of IDs to sort.</param>
        /// <param name="propertySelector">A function selecting the string property.</param>
        /// <param name="orderSelector">A function to specify the selection order.</param>
        /// <returns>An ordered enumerable of key-value pairs.</returns>
        /// <remarks>举例：Sort ObjectIds based on the layer color
        /// var sorted = SortObjectsExtensions.Sort<int>(psr.Value.GetObjectIds(), id => id.LayerId.Color.ColorIndex, kv => -kv.Value);
        /// </remarks>
        public static List<KeyValuePair<ObjectId, T>> Sort<T>(ObjectId[] ids,
            Func<dynamic, T> propertySelector, Func<KeyValuePair<ObjectId, T>, T> orderSelector)
        {
            var map = new Dictionary<ObjectId, T>();

            foreach (dynamic id in ids)
            {
                map.Add(id, propertySelector(id));
            }
            return map.OrderBy(orderSelector).ToList();
        }
        #endregion

        /// <summary> 将表示句柄值的字符转换为句柄 </summary>
        /// <param name="handle">表示句柄的字符，即16进制的数值，比如“409E”。最小的句柄值为1。</param>
        public static Handle ConvertToHandle(string handle)
        {
            return new Handle(Convert.ToInt64(handle, 16));
        }

        #region ---   多段线变稀

        /// <summary> 将给定的线性多段线的段数变稀 </summary>
        /// <param name="cs">用来变稀的那条比较密的多段线几何，集合中的曲线必须首尾相连</param>
        /// <param name="segPoints">每隔多少个点取用一个，比如2表示只取源多段线中的第1、3、5、7 ... 个点</param>
        /// <param name="includeAllNonlinear"> true 表示保留所有的曲线段，只将直线段的顶点变疏；false 表示不管是曲线段还是直线段，最终都按顶点坐标转换为直线段 </param>
        /// <returns></returns>
        public static CompositeCurve3d GetThinedPolyline(Curve3d[] cs, int segPoints, bool includeAllNonlinear)
        {
            Point3d startPt = cs[0].StartPoint;
            Point3d endPt;
            var curves = new List<Curve3d>();
            Curve3d c = null;
            var n = cs.Length;
            var id = 1;
            if (includeAllNonlinear)
            {
                // 保留所有的曲线段，只将直线段的顶点变疏
                for (var i = 0; i < n; i++)
                {
                    c = cs[i];
                    if (c is LineSegment3d)
                    {
                        if (id % segPoints == 0)
                        {
                            // 说明到了关键点
                            endPt = c.EndPoint;
                            curves.Add(new LineSegment3d(startPt, endPt));
                            startPt = endPt;
                        }
                        id += 1;
                    }
                    else
                    {

                        if (id > 1)
                        {
                            // 说明中间有直线段
                            endPt = c.StartPoint;
                            curves.Add(new LineSegment3d(startPt, endPt));
                        }
                        else
                        {
                            // 说明前一段也是曲线
                        }
                        // 强制性添加上这一段曲线
                        curves.Add(c);
                        startPt = c.EndPoint;
                        id = 1;
                    }
                }
            }
            else
            {
                // 不管是曲线段还是直线段，最终都按顶点坐标转换为直线段
                for (var i = 0; i < n; i++)
                {
                    c = cs[i];
                    if (id % segPoints == 0)
                    {
                        // 说明到了关键点
                        endPt = c.EndPoint;
                        curves.Add(new LineSegment3d(startPt, endPt));
                        startPt = endPt;
                    }
                    id += 1;
                }
            }

            // 强制补上最后一个可能漏掉的直线段
            if (c != null && startPt != c.EndPoint)
            {
                curves.Add(new LineSegment3d(startPt, c.EndPoint));
            }

            return new CompositeCurve3d(curves.ToArray());
        }

        /// <summary>
        /// 通过限定分段长度来对多段线变稀或者变密（保留首尾两个点）
        /// </summary>
        /// <param name="cs"></param>
        /// <param name="segLength">每一分段的长度</param>
        /// <returns></returns>
        public static CompositeCurve3d GetThinedPolyline(Curve3d cs, double segLength)
        {
            var startPara = cs.GetParameterOf(cs.StartPoint);
            var endPara = cs.GetParameterOf(cs.EndPoint);
            var startPt = cs.StartPoint;
            var endPt = startPt;
            var para = startPara;
            //
            var segCount = (int)Math.Ceiling((endPara - startPara) / segLength);
            var lines = new Curve3d[segCount];

            // 最后一段的间距不由 segLength 控制
            for (int i = 0; i < segCount - 1; i++)
            {
                para += segLength;
                endPt = cs.EvaluatePoint(para);
                //
                lines[i] = new LineSegment3d(startPt, endPt);
                //
                startPt = endPt;
            }
            // 处理最后一段曲线
            lines[segCount - 1] = new LineSegment3d(startPt, cs.EndPoint);
            //
            return new CompositeCurve3d(lines);
        }

        #endregion

        #region ---   ViewTableRecord

        /// <summary> 在AutoCAD界面中显示出指定的二维矩形范围 </summary>
        public static void ShowExtentsInView(Editor ed, Extents3d ext)
        {
            // 获取当前视图
            using (ViewTableRecord view = ed.GetCurrentView())
            {
                // 不用再手动执行 view.UpgradeOpen();
                view.Height = ext.MaxPoint.Y - ext.MinPoint.Y; // 界面中显示的测量高度（不是屏幕分辨率的像素高度）
                view.Width = ext.MaxPoint.X - ext.MinPoint.X;
                view.CenterPoint = new Point2d((ext.MinPoint.X + ext.MaxPoint.X) / 2, (ext.MinPoint.Y + ext.MaxPoint.Y) / 2);
                view.Target = new Point3d(0, 0, 0);
                //
                // 最后AutoCAD会对指定的矩形框进行缩放，以确保指定的矩形框完全正中地显示在屏幕中。
                ed.SetCurrentView(view);
            }
        }
        #endregion

        #region ---   XData XRecord 相关操作

        /// <summary> 将一个布尔值转换为对应的 ExtendedData </summary>
        public static TypedValue SetExtendedDataBool(bool value)
        {
            return new TypedValue((int)DxfCode.ExtendedDataInteger16, value);
        }

        /// <summary> 从 ExtendedData 值中提取出对应的 布尔值  </summary>
        public static bool GetExtendedDataBool(TypedValue buff)
        {
            return (Int16)buff.Value == 1;
        }

        /// <summary> 将一个布尔值转换为对应的 ExtendedData </summary>
        public static TypedValue SetExtendedDataBool3(bool? value)
        {
            if (value == null)
            {
                return new TypedValue((int)DxfCode.ExtendedDataInteger16, -1);
            }
            else if (value.Value)
            {
                return new TypedValue((int)DxfCode.ExtendedDataInteger16, 1);
            }
            else
            {
                return new TypedValue((int)DxfCode.ExtendedDataInteger16, 0);
            }
        }

        /// <summary> 从 ExtendedData 值中提取出对应的 布尔值  </summary>
        public static bool? GetExtendedDataBool3(TypedValue buff)
        {
            var v = (Int16)buff.Value;
            if (v == -1)
            {
                return null;
            }
            else if (v == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary> 将一个枚举值转换为对应的 ExtendedData </summary>
        public static TypedValue SetExtendedDataEnum(Enum enumValue)
        {
            return new TypedValue((int)DxfCode.ExtendedDataInteger16, Convert.ToInt16(enumValue));
        }

        /// <summary> 从 ExtendedData 值中提取出对应的 枚举值  </summary>
        public static T GetExtendedDataEnum<T>(TypedValue buff)
        {
            return (T)Enum.ToObject(typeof(T), (short)buff.Value);
        }

        #endregion

        /// <summary> 将焦点从操作的<seealso cref="System.Windows.Forms.Form"/>转移到 AutoCAD 主界面窗口。此操作在对 无模态窗口 操作时非常有用。 </summary>
        public static void FocusOnMainUIWindow() { Application.MainWindow.Focus(); }

        #region --- Excel 程序

        private static Excel.Application _workingApp;

        /// <summary> 获取全局的 Excel 程序 </summary>
        /// <param name="visible"></param>
        /// <returns>获取失败则返回 null</returns>
        public static Excel.Application GetExcelApp(bool visible = false)
        {
            if (_workingApp != null)
            {
                var processId = 0;
                var threadId = eZstd.API.Windows.GetWindowThreadProcessId(_workingApp.Hwnd, ref processId);
                var pr = Process.GetProcessById(processId);
                if (pr == null || pr.HasExited)
                {
                    _workingApp = null;
                }
                else
                {
                    _workingApp.Visible = visible;
                    return _workingApp;
                }
            }
            if (_workingApp == null)
            {
                _workingApp = new Excel.Application { Visible = visible };
            }
            if (_workingApp == null)
            {
                throw new NullReferenceException($"无法打开 Excel 程序!");
            }
            return _workingApp;
        }

        /// <returns>成功则返回 true</returns>
        public static bool KillActiveExcelApp(Excel.Application appToKill)
        {
            if (appToKill != null)
            {
                try
                {
                    // excelApp.Quit();
                    var processId = 0;
                    var threadId = eZstd.API.Windows.GetWindowThreadProcessId(appToKill.Hwnd, ref processId);
                    var pr = Process.GetProcessById(processId);
                    pr.Kill();
                    //
                    if (appToKill.Equals(_workingApp))
                    {
                        _workingApp = null;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary> 通过匹配 Excel 工作表的名称来获取对应的表对象，如果想要的表不存在，则添加一个新的表 </summary>
        /// <param name="wkbk"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public static Worksheet GetOrCreateWorkSheet(Workbook wkbk, string sheetName)
        {
            Worksheet matchedSheet = null;
            foreach (var obj in wkbk.Worksheets)
            {
                var sht = obj as Worksheet;
                if (sht != null && sht.Name.Equals(sheetName, StringComparison.CurrentCultureIgnoreCase))
                {
                    matchedSheet = sht;
                    sht.UsedRange.Clear();
                    break;
                }
            }
            if (matchedSheet == null)
            {
                matchedSheet = wkbk.Worksheets.Add();
                matchedSheet.Name = sheetName;
            }
            // 将表格中的单元格解锁
            matchedSheet.Cells.Locked = false;
            matchedSheet.Cells.FormulaHidden = false;
            //
            return matchedSheet;
        }

        #endregion

        /// <summary>
        /// 同步发送命令
        /// </summary>
        /// <param name="command"></param>
        public static void SendCommandSync(string command)
        {
            /*
             使用SendStringToExecute()方法执行命令是异步的（同步是阻塞的，就是你做了这件事情，没做完就做不了别的事情了，异步就是做了做一件事情没做完，就可以去做别的事情了），直到.NET命令结束，所调用的AutoCAD命令才被执行。如果需要立即执行命令（同步），我们应该：
             Commands executed with SendStringToExecute are asynchronous and are not invoked until the .NET command has ended. If you need to execute a command immediately (synchronously), you should:
             	使用COM Automation库提供的acadDocument.SendCommand()方法。放心，.NET COM互操作程序集可以访问COM Automation库；
             	对于AutoCAD本地命令，以及由ObjectARX或.NET API定义的命令，调用（P/Invoke）非托管的acedCommand()方法或acedCmd()方法；
             	对于由AutoLISP定义的命令，调用（P/Invoke）非托管的acedInvoke()方法；             
             */
            AcadApplication acadApp =
                Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication as AcadApplication;
            acadApp.ActiveDocument.SendCommand(command);
        }

        #region --- 桩号处理

        private static readonly Regex StationReg = new Regex(@"K(\d+)\+(\d*.*)"); // K51+223.392

        /// <summary> 将表示里程的字符数据转换为对应的数值 </summary>
        /// <param name="station"></param>
        /// <returns>如果无法正常解析，则返回 null</returns>
        public static double? GetStationFromString(string station)
        {
            var mt = StationReg.Match(station);
            if (mt.Success)
            {
                int k;
                double m;
                if (int.TryParse(mt.Groups[1].Value, out k) && double.TryParse(mt.Groups[2].Value, out m))
                {
                    return k * 1000 + m;
                }
            }
            return null;
        }

        /// <summary> 将桩号数值表示为 K23+456.789 ~ K23+456.789 的形式 </summary>
        /// <param name="startStation">要进行转换的起始桩号的数值 </param>
        /// <param name="endStation"> 要进行转换的结尾桩号的数值 </param>
        /// <param name="maxDigits">最大的小数位数</param>
        public static string GetStationString(double startStation, double endStation, int maxDigits)
        {
            return GetStationString(startStation, maxDigits) + @"~" + GetStationString(endStation, maxDigits);
        }

        /// <summary> 将桩号数值表示为 K23+456.789 的形式 </summary>
        /// <param name="station">要进行转换的桩号的数值</param>
        /// <param name="maxDigits">最大的小数位数</param>
        public static string GetStationString(double station, int maxDigits)
        {
            string res = null;
            var k = (int)Math.Floor(station / 1000);
            var meters = station % 1000;
            var miniMeters = meters % 1;
            if (miniMeters != 0)
            {
                var digits = new string('0', maxDigits);
                res += $"K{k}+{meters.ToString("000." + digits)}";
            }
            else
            {
                // 整米数桩号
                res = $"K{k}+{meters.ToString("000")}";
            }
            return res;
        }

        #endregion
    }
}