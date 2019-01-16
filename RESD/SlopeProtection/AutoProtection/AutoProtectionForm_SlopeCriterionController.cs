﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using RESD.Options;
using RESD.Utility;
using eZstd.UserControls;

namespace RESD.SlopeProtection
{
    public partial class AutoProtectionForm
    {
        /// <summary>
        ///  操作 边坡 信息的 Datagridview 控件
        /// </summary>
        private class SlopeCriterionController : eZDataGridView
        {
            /// <summary> 当用户选中某一行中的单元格时，用来修改集合中的值 </summary>
            public event Action<CriterionRangeList, DataGridViewCell> CriterionsToBeModified;

            public SlopeCriterionController()
            {

            }

            #region ---   eZDataGridView 配置

            public BindingList<SpCriterion> Slopes { get; private set; }

            private bool _uIConstructed;
            public void SetDataSource(IList<SpCriterion> slopes)
            {
                Slopes = new BindingList<SpCriterion>(slopes) { AllowNew = true };
                //
                if (!_uIConstructed)
                {
                    ConstructeZDataGridView();
                    _uIConstructed = true;
                }
                this.DataSource = Slopes;
                this.Refresh();
            }

            private void ConstructeZDataGridView()
            {
                var dgv = this;
                //
                dgv.ManipulateRows = true;
                dgv.ShowRowNumber = true;
                //
                dgv.AutoGenerateColumns = false;
                dgv.EditMode = DataGridViewEditMode.EditOnEnter;

                var dicimalStyle3 = new DataGridViewCellStyle();
                dicimalStyle3.Format = "0.###";

                // 创建数据列并绑定到数据源 ----------------------------------------------

                // -------------------------
                var combo = new DataGridViewComboBoxColumn();
                combo.DataSource = Enum.GetValues(typeof(Operator_Bool));
                combo.DataPropertyName = "Fill";
                combo.Width = 50;
                combo.Name = "填方";
                combo.ToolTipText = @"边坡为填方还是挖方";
                dgv.Columns.Add(combo);
                // 如果要设置对应单元格的值为某枚举项：combo.Item(combo.Index,行号).Value = Gender.Male;

                // -------------------------
                var column = new DataGridViewTextBoxColumn();
                column.DataPropertyName = "FirstSlopeRatio";
                column.ReadOnly = true;
                column.Name = "首级坡比";
                column.ToolTipText = @"第一级子边坡的坡比，如果子边坡数量为0，则恒不满足此条件";
                column.Width = 100;
                dgv.Columns.Add(column);
                // -------------------------

                column = new DataGridViewTextBoxColumn();
                column.DataPropertyName = "SlopeHeight";
                column.Name = "总坡高(m)";
                column.ToolTipText = @"整个边坡的总坡高";
                column.ReadOnly = true;
                column.Width = 100;
                dgv.Columns.Add(column);

                // -------------------------
                column = new DataGridViewTextBoxColumn();
                column.DataPropertyName = "SlopeLevel";
                column.Name = "总坡级";
                column.ToolTipText = @"整个边坡共有多少级子边坡";
                column.ReadOnly = true;
                column.ValueType = typeof(int);
                column.Width = 100;
                dgv.Columns.Add(column);

                // -------------------------
                var prots = Options_Collections.CommonFillProtections.ToList();
                prots.AddRange(Options_Collections.CommonCutProtections);
                var protsBinding = new BindingList<string>(prots);
                combo = new DataGridViewComboBoxColumn();
                // combo.drow = DataGridViewComboBoxDisplayStyle.DropDownButton;
                combo.DataSource = protsBinding;
                combo.DataPropertyName = "ProtectionMethod";
                combo.Name = "防护";
                combo.ToolTipText = @"当前面所有的条件都满足时，确定此边坡的具体防护形式";
                combo.MinimumWidth = 100;
                combo.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgv.Columns.Add(combo);
                // -------------------------
                column = new DataGridViewTextBoxColumn();
                column.DataPropertyName = "ProtectionMethod";
                column.Name = "防护";
                column.ToolTipText = @"当前面所有的条件都满足时，确定此边坡的具体防护形式";
                column.MinimumWidth = 100;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgv.Columns.Add(column);


                // 事件绑定 -------------------------------------------------------------
                dgv.CellClick += DgvOnCellClick;
                dgv.DataError += EZdgvOnDataError; // 响应表格中的数据类型不匹配等出错的情况
                                                   //dgv.CellContentClick += EZdgvOnCellContentClick;  // 响应表格中的按钮按下事件
                                                   //dgv.CurrentCellDirtyStateChanged += EZdgvOnCurrentCellDirtyStateChanged; // 在表格中Checkbox的值发生改变时立即作出响应
                Slopes.AddingNew += SlopesOnAddingNew;
            }
            
            private void SlopesOnAddingNew(object sender, AddingNewEventArgs e)
            {
                SpCriterion spc = null;
                if (Slopes != null && Slopes.Count > 0)
                {
                    spc = Slopes[Slopes.Count - 1].Clone() as SpCriterion;
                }
                else
                {
                    spc = new SpCriterion();
                }
                e.NewObject = spc;
            }

            #endregion

            #region ---   与 eZDataGridView 相关的事件处理

            private void DgvOnCellClick(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    var c = Rows[e.RowIndex].Cells[e.ColumnIndex];
                    var v = c.Value;
                    if (v is CriterionRangeList)
                    {
                        if (CriterionsToBeModified != null) { CriterionsToBeModified(v as CriterionRangeList, c); }
                    }
                }
            }


            /// <summary>
            /// 在表格中Checkbox的值发生改变时立即作出响应.
            /// 如果你想要在用户点击复选框单元格时立即响应，你可以处理CellContentClick 事件，但是此事件会在单元格的值更新之前触发。
            /// </summary>
            private void EZdgvOnCurrentCellDirtyStateChanged(object sender, EventArgs eventArgs)
            {
                // IsCurrentCellDirty属性：Gets a value indicating whether the current cell has uncommitted changes.
                if (this.IsCurrentCellDirty)
                {
                    this.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }

            }

            private void EZdgvOnDataError(object sender, DataGridViewDataErrorEventArgs e)
            {
                if ((e.Context & DataGridViewDataErrorContexts.Parsing) != 0)
                {
                    MessageBox.Show(@"输入的数据不能转换为指定的数据类型！", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    e.ThrowException = false;
                }
            }

            #endregion

        }

    }

}