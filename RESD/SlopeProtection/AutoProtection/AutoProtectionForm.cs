﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using eZcad.Utility;
using RESD.SlopeProtection;
using eZstd.Data;
using Utils = eZstd.Miscellaneous.Utils;

namespace RESD.SlopeProtection
{
    /// <summary> 边坡自动防护规则的设置与选择界面 </summary>
    public partial class AutoProtectionForm : FormOk
    {
        #region ---   Fields

        private static List<AutoProtectionCriterions> LoadedSpCriterions;

        private AutoProtectionCriterions _activeSpCriterion;
        /// <summary> 边坡自动防护规则 </summary>
        public AutoProtectionCriterions ActiveSpCriterion
        {
            get { return _activeSpCriterion; }
            private set
            {
                if (value != null)
                {
                    _activeSpCriterion = value;
                    //
                    RefreshDgvDataSource(value);
                }
            }
        }

        #endregion

        #region ---   构造函数与窗口的打开、关闭

        private static AutoProtectionForm _uniqueInstance;

        public static AutoProtectionForm GetUniqueInstance()
        {
            _uniqueInstance = _uniqueInstance ?? new AutoProtectionForm();
            return _uniqueInstance;
        }

        private AutoProtectionForm()
        {
            InitializeComponent();

            // 构造初始的自动防护数据
            if (LoadedSpCriterions == null)
            {
                LoadedSpCriterions = new List<AutoProtectionCriterions>();
            }
            if (LoadedSpCriterions.Count == 0)
            {
                var defaultSPC = new AutoProtectionCriterions();
                LoadedSpCriterions.Add(defaultSPC);
            }
            ActiveSpCriterion = LoadedSpCriterions[0];
            //
            ConstructUI();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Close();
            DialogResult = DialogResult.OK;
        }

        #endregion

        #region ---   界面的刷新

        private void ConstructUI()
        {
            cmb_Operator.DataSource = Enum.GetValues(typeof(Operator_Num));
            // 事件绑定
            dgv_Slope.CriterionsToBeModified += DgvSlopeOnCriterionsToBeModified;
            // dgv_Platform.CriterionsToBeModified += DgvSlopeOnCriterionsToBeModified;
            //
            _autoSchemes = new BindingList<AutoProtectionCriterions>();
            _autoSchemes.AddingNew += AutoSchemesOnAddingNew;
            _autoSchemes.AddNew();
            //
            cbb_AutoSchemes.DataSource = _autoSchemes;
            //
        }

        private BindingList<AutoProtectionCriterions> _autoSchemes;

        private void RefreshDgvDataSource(AutoProtectionCriterions autoProtectionCriterions)
        {

            // 边坡的自动防护
            dgv_Slope.SetDataSource(autoProtectionCriterions.SlopeCriterions);

            // 平台的自动防护
            // dgv_Platform.SetDataSource(autoProtectionCriterions.PlatformCriterions);
        }

        #endregion

        #region ---   listBox_Criterions 控件的操作

        private DataGridViewCell _activeCell;

        /// <summary> 切换为要修改的单元格 </summary>
        private void DgvSlopeOnCriterionsToBeModified(CriterionRangeList slopeRangeList, DataGridViewCell cell)
        {
            listBox_Criterions.DataSource = new BindingList<CriterionRange>(slopeRangeList.AndRange);
            _activeCell = cell;
        }

        /// <summary> 添加元素 </summary>
        private void btn_append_Click(object sender, EventArgs e)
        {
            var sRanges = listBox_Criterions.DataSource as BindingList<CriterionRange>;
            if (sRanges != null)
            {
                var v = (Operator_Num)cmb_Operator.SelectedItem;
                if (v == Operator_Num.闭区间)
                {
                    var sr = new CriterionRange()
                    {
                        Operator = Operator_Num.大于等于,
                        Value = textBoxNum_Start.ValueNumber,
                    };
                    sRanges.Add(sr);
                    sr = new CriterionRange()
                    {
                        Operator = Operator_Num.小于等于,
                        Value = textBoxNum_End.ValueNumber,
                    };
                    sRanges.Add(sr);
                }
                else
                {
                    var sr = new CriterionRange()
                    {
                        Operator = v,
                        Value = textBoxNum_Start.ValueNumber,
                    };
                    //
                    sRanges.Add(sr);
                }
                // 刷新界面
                if (_activeCell != null)
                {
                    _activeCell.DataGridView.Refresh();
                }
            }
        }

        /// <summary> 删除元素 </summary>
        private void btn_delete_Click(object sender, EventArgs e)
        {
            var sRanges = listBox_Criterions.DataSource as BindingList<CriterionRange>;
            if (sRanges != null)
            {
                var toBeRemoved = listBox_Criterions.SelectedIndices.Cast<int>().ToList();
                toBeRemoved.Sort();
                toBeRemoved.Reverse();
                foreach (var ind in toBeRemoved)
                {
                    sRanges.RemoveAt(ind);
                }
                // 刷新界面
                if (_activeCell != null)
                {
                    _activeCell.DataGridView.Refresh();
                }
            }
        }

        #endregion

        #region ---   数据文件的导入与导出

        private void btn_ExportData_Click(object sender, EventArgs e)
        {
            if (ActiveSpCriterion != null)
            {
                var fpath = Utils.ChooseSaveFile("导出数据到到 xml 文件", AutoProtectionCriterions.FileExtension);
                if (fpath != null)
                {
                    var sb = new StringBuilder();
                    var succ = XmlSerializer.ExportToXmlFile(fpath, ActiveSpCriterion, ref sb);
                    if (succ)
                    {
                        MessageBox.Show("数据导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
            }
        }

        private void btn_ImportData_Click(object sender, EventArgs e)
        {
            var newdata = ImportAutoProtFromXml();
            if (newdata != null)
            {
                _autoSchemes.Add(newdata);
                cbb_AutoSchemes.SelectedItem = newdata;
            }
        }

        public static AutoProtectionCriterions ImportAutoProtFromXml()
        {
            var fpath = Utils.ChooseOpenFile("从 xml 文件中导入数据", AutoProtectionCriterions.FileExtension, false);
            if (fpath != null)
            {
                var sb = new StringBuilder();
                var succ = false;

                var newData = XmlSerializer.ImportFromXml(fpath[0], typeof(AutoProtectionCriterions), out succ, ref sb);
                if (succ)
                {
                    return newData as AutoProtectionCriterions;
                }
            }
            return null;
        }


        #endregion

        #region ---   自动防护方案的管理

        /// <summary> 添加一种全新的自动防护方案 </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_AddScheme_Click(object sender, EventArgs e)
        {
            _autoSchemes.AddNew();
            cbb_AutoSchemes.SelectedItem = _autoSchemes[_autoSchemes.Count - 1];
        }

        private void AutoSchemesOnAddingNew(object sender, AddingNewEventArgs addingNewEventArgs)
        {
            var apc = new AutoProtectionCriterions();
            addingNewEventArgs.NewObject = apc;
            apc.Name += _autoSchemes.Count == 0 ? "" : _autoSchemes.Count.ToString();
        }

        private void CbbAutoSchemesOnSelectedValueChanged(object sender, EventArgs eventArgs)
        {
            var src = cbb_AutoSchemes.SelectedItem as AutoProtectionCriterions;
            ActiveSpCriterion = src;
        }

        private void button_RenameScheme_Click(object sender, EventArgs e)
        {
            if (ActiveSpCriterion != null)
            {
                var f = new SchemeRenameForm(ActiveSpCriterion.Name);
                var res = f.ShowDialog(null);
                if (res == DialogResult.OK)
                {
                    ActiveSpCriterion.Name = f.NewName;

                    // 为了保证组合列表框中元素更新后界面显示也能更新，必须先将其DataSource值设置为null，然后再设置为真实值。
                    cbb_AutoSchemes.DataSource = null;
                    cbb_AutoSchemes.DataSource = _autoSchemes;
                }
            }
        }

        #endregion

    }
}