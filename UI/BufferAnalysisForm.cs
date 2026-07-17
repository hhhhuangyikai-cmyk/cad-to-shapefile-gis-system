using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using CadToShpGISSystem.Core;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;

namespace CadToShpGISSystem.UI
{
    /// <summary>
    /// 缓冲区分析参数窗体。
    /// </summary>
    public class BufferAnalysisForm : Form
    {
        private readonly AxMapControl _mapControl;
        private readonly ComboBox _cmbLayers;
        private readonly NumericUpDown _numDistance;
        private readonly ComboBox _cmbUnit;
        private readonly CheckBox _chkSelectedOnly;
        private readonly TextBox _txtOutputFolder;
        private readonly TextBox _txtOutputName;

        public string OutputPath { get; private set; }

        public BufferAnalysisForm(AxMapControl mapControl)
        {
            _mapControl = mapControl;

            Text = "缓冲区分析";
            Width = 620;
            Height = 305;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblLayer = new Label { Left = 18, Top = 24, Width = 80, Text = "输入图层：" };
            _cmbLayers = new ComboBox { Left = 100, Top = 20, Width = 470, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbLayers.SelectedIndexChanged += CmbLayers_SelectedIndexChanged;

            Label lblDistance = new Label { Left = 18, Top = 64, Width = 80, Text = "缓冲距离：" };
            _numDistance = new NumericUpDown
            {
                Left = 100,
                Top = 60,
                Width = 120,
                DecimalPlaces = 2,
                Maximum = 1000000,
                Minimum = 0,
                Value = 50
            };

            Label lblUnit = new Label { Left = 235, Top = 64, Width = 45, Text = "单位：" };
            _cmbUnit = new ComboBox { Left = 280, Top = 60, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbUnit.Items.Add(new UnitItem("米", BufferDistanceUnit.Meter));
            _cmbUnit.Items.Add(new UnitItem("千米", BufferDistanceUnit.Kilometer));
            _cmbUnit.Items.Add(new UnitItem("度", BufferDistanceUnit.Degree));
            _cmbUnit.Items.Add(new UnitItem("数据坐标单位", BufferDistanceUnit.MapUnit));
            _cmbUnit.SelectedIndex = 0;

            _chkSelectedOnly = new CheckBox { Left = 435, Top = 62, Width = 140, Text = "仅对选中要素" };

            Label lblOut = new Label { Left = 18, Top = 108, Width = 80, Text = "输出目录：" };
            _txtOutputFolder = new TextBox { Left = 100, Top = 104, Width = 390 };
            Button btnBrowse = new Button { Left = 500, Top = 102, Width = 70, Text = "浏览" };
            btnBrowse.Click += BtnBrowse_Click;

            Label lblName = new Label { Left = 18, Top = 150, Width = 80, Text = "输出文件：" };
            _txtOutputName = new TextBox { Left = 100, Top = 146, Width = 390, Text = "Buffer_Result.shp" };

            Label lblHint = new Label
            {
                Left = 100,
                Top = 178,
                Width = 470,
                Height = 34,
                Text = "提示：经纬度数据请选择“米/千米”，系统会临时投影后缓冲；选择“数据坐标单位”时 50 可能表示 50 度。"
            };

            Button btnOk = new Button { Left = 410, Top = 225, Width = 80, Text = "生成" };
            Button btnCancel = new Button { Left = 500, Top = 225, Width = 70, Text = "取消" };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; };

            Controls.Add(lblLayer);
            Controls.Add(_cmbLayers);
            Controls.Add(lblDistance);
            Controls.Add(_numDistance);
            Controls.Add(lblUnit);
            Controls.Add(_cmbUnit);
            Controls.Add(_chkSelectedOnly);
            Controls.Add(lblOut);
            Controls.Add(_txtOutputFolder);
            Controls.Add(btnBrowse);
            Controls.Add(lblName);
            Controls.Add(_txtOutputName);
            Controls.Add(lblHint);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            LoadLayers();
        }

        private void LoadLayers()
        {
            _cmbLayers.Items.Clear();
            if (_mapControl == null || _mapControl.Map == null)
            {
                return;
            }

            List<IFeatureLayer> layers = ShapefileHelper.GetFeatureLayers(_mapControl.Map);
            for (int i = 0; i < layers.Count; i++)
            {
                _cmbLayers.Items.Add(new LayerItem(layers[i]));
            }

            if (_cmbLayers.Items.Count > 0)
            {
                _cmbLayers.SelectedIndex = 0;
                UpdateDefaultOutputName();
            }

            string defaultFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BufferOutput");
            _txtOutputFolder.Text = defaultFolder;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _txtOutputFolder.Text = dialog.SelectedPath;
            }
        }

        private void CmbLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDefaultOutputName();
        }

        private void UpdateDefaultOutputName()
        {
            LayerItem item = _cmbLayers.SelectedItem as LayerItem;
            if (item == null || item.Layer == null)
            {
                return;
            }

            _txtOutputName.Text = MakeSafeFileName(item.Layer.Name) + "_Buffer.shp";
        }

        private string MakeSafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Buffer_Result";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
            {
                name = name.Replace(invalidChars[i], '_');
            }

            return name.Replace(" ", "_");
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_cmbLayers.SelectedItem == null)
            {
                MessageBox.Show(this, "请先选择一个要素图层。", "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                LayerItem item = (LayerItem)_cmbLayers.SelectedItem;
                UnitItem unit = (UnitItem)_cmbUnit.SelectedItem;
                BufferAnalysis analysis = new BufferAnalysis();
                OutputPath = analysis.CreateBuffer(
                    _mapControl,
                    item.Layer,
                    Convert.ToDouble(_numDistance.Value),
                    unit.Unit,
                    _chkSelectedOnly.Checked,
                    _txtOutputFolder.Text,
                    _txtOutputName.Text);

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "缓冲区分析失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Error("缓冲分析", "缓冲区分析失败", ex);
            }
        }

        private class LayerItem
        {
            public LayerItem(IFeatureLayer layer)
            {
                Layer = layer;
            }

            public IFeatureLayer Layer { get; private set; }

            public override string ToString()
            {
                return Layer == null ? string.Empty : Layer.Name;
            }
        }

        private class UnitItem
        {
            public UnitItem(string text, BufferDistanceUnit unit)
            {
                Text = text;
                Unit = unit;
            }

            public string Text { get; private set; }

            public BufferDistanceUnit Unit { get; private set; }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
