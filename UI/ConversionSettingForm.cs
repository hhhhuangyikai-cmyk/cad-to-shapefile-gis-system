using System;
using System.IO;
using System.Windows.Forms;
using Path = System.IO.Path;
using CadToShpGISSystem.Core;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.UI
{
    /// <summary>
    /// CAD 转 Shapefile 参数设置窗体。
    /// 注意：CAD 数据经常没有坐标系，所以这里区分“CAD 原始坐标系”和“输出坐标系”。
    /// 只有二者都明确且不一致时，ArcObjects 才能执行真正的投影转换。
    /// </summary>
    public class ConversionSettingForm : Form
    {
        private readonly TextBox _txtCadFile;
        private readonly TextBox _txtOutputFolder;
        private readonly TextBox _txtPrefix;
        private readonly CheckBox _chkPoint;
        private readonly CheckBox _chkLine;
        private readonly CheckBox _chkPolygon;
        private readonly CheckBox _chkAnnotation;
        private readonly ComboBox _cmbSourceSpatialReference;
        private readonly ComboBox _cmbOutputSpatialReference;

        public string CadFilePath { get; private set; }
        public string OutputFolder { get; private set; }
        public string OutputPrefix { get; private set; }
        public bool ExportPoint { get; private set; }
        public bool ExportPolyline { get; private set; }
        public bool ExportPolygon { get; private set; }
        public bool ExportAnnotation { get; private set; }
        public ISpatialReference SourceSpatialReference { get; private set; }
        public ISpatialReference OutputSpatialReference { get; private set; }

        public ConversionSettingForm(string defaultCadFile)
        {
            Text = "CAD 转 Shapefile 设置";
            Width = 700;
            Height = 435;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblCad = new Label { Left = 18, Top = 24, Width = 90, Text = "输入 CAD：" };
            _txtCadFile = new TextBox { Left = 110, Top = 20, Width = 430 };
            Button btnCad = new Button { Left = 550, Top = 18, Width = 75, Text = "浏览" };
            btnCad.Click += BtnCad_Click;

            Label lblOut = new Label { Left = 18, Top = 64, Width = 90, Text = "输出目录：" };
            _txtOutputFolder = new TextBox { Left = 110, Top = 60, Width = 430 };
            Button btnOut = new Button { Left = 550, Top = 58, Width = 75, Text = "浏览" };
            btnOut.Click += BtnOut_Click;

            Label lblPrefix = new Label { Left = 18, Top = 104, Width = 90, Text = "文件前缀：" };
            _txtPrefix = new TextBox { Left = 110, Top = 100, Width = 220, Text = "Export" };

            GroupBox group = new GroupBox { Left = 18, Top = 138, Width = 280, Height = 80, Text = "导出类型" };
            _chkPoint = new CheckBox { Left = 18, Top = 28, Width = 60, Text = "点", Checked = true };
            _chkLine = new CheckBox { Left = 85, Top = 28, Width = 60, Text = "线", Checked = true };
            _chkPolygon = new CheckBox { Left = 152, Top = 28, Width = 60, Text = "面", Checked = true };
            _chkAnnotation = new CheckBox { Left = 218, Top = 28, Width = 60, Text = "注记", Checked = true };
            group.Controls.Add(_chkPoint);
            group.Controls.Add(_chkLine);
            group.Controls.Add(_chkPolygon);
            group.Controls.Add(_chkAnnotation);

            GroupBox srGroup = new GroupBox
            {
                Left = 315,
                Top = 130,
                Width = 350,
                Height = 166,
                Text = "坐标系处理（开始转换时完成）"
            };

            Label lblSourceSr = new Label { Left = 14, Top = 28, Width = 130, Text = "CAD原始坐标系：" };
            _cmbSourceSpatialReference = new ComboBox
            {
                Left = 14,
                Top = 50,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            FillSourceSpatialReferenceItems(_cmbSourceSpatialReference);

            Label lblOutputSr = new Label { Left = 14, Top = 84, Width = 130, Text = "输出坐标系：" };
            _cmbOutputSpatialReference = new ComboBox
            {
                Left = 14,
                Top = 106,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            FillOutputSpatialReferenceItems(_cmbOutputSpatialReference);

            srGroup.Controls.Add(lblSourceSr);
            srGroup.Controls.Add(_cmbSourceSpatialReference);
            srGroup.Controls.Add(lblOutputSr);
            srGroup.Controls.Add(_cmbOutputSpatialReference);

            Label lblHint = new Label
            {
                Left = 18,
                Top = 305,
                Width = 647,
                Height = 52,
                Text = "说明：本功能已集成到“开始转换”。若 CAD 是米制工程坐标，保持默认即可，输出 Shapefile 会带投影信息，后续缓冲区可直接用米；若要转 WGS84，必须先把 CAD 原始坐标系选成真实源坐标系，再把输出坐标系设为 WGS84。"
            };

            Button btnOk = new Button { Text = "开始转换", Left = 485, Top = 365, Width = 85 };
            Button btnCancel = new Button { Text = "取消", Left = 585, Top = 365, Width = 80 };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; };

            Controls.Add(lblCad);
            Controls.Add(_txtCadFile);
            Controls.Add(btnCad);
            Controls.Add(lblOut);
            Controls.Add(_txtOutputFolder);
            Controls.Add(btnOut);
            Controls.Add(lblPrefix);
            Controls.Add(_txtPrefix);
            Controls.Add(group);
            Controls.Add(srGroup);
            Controls.Add(lblHint);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            if (!string.IsNullOrEmpty(defaultCadFile))
            {
                _txtCadFile.Text = defaultCadFile;
                _txtPrefix.Text = Path.GetFileNameWithoutExtension(defaultCadFile);
                _txtOutputFolder.Text = Path.Combine(Path.GetDirectoryName(defaultCadFile), "ShpOutput");
            }
        }

        private void FillSourceSpatialReferenceItems(ComboBox comboBox)
        {
            comboBox.Items.Add("未知/不指定");
            comboBox.Items.Add("WGS84 地理坐标系 EPSG:4326");
            comboBox.Items.Add("CGCS2000 地理坐标系 EPSG:4490");
            comboBox.Items.Add("CGCS2000 高斯克吕格示例 EPSG:4547（米制，推荐缓冲）");
            comboBox.Items.Add("Web Mercator EPSG:3857（米制演示）");
            // 课程演示场景中，CAD 通常是平面米制坐标。默认选择米制投影，
            // 可以避免转换后的 Shapefile 仍为 Unknown，导致缓冲区无法按米计算。
            comboBox.SelectedIndex = 3;
        }

        private void FillOutputSpatialReferenceItems(ComboBox comboBox)
        {
            comboBox.Items.Add("沿用 CAD 原始坐标系");
            comboBox.Items.Add("WGS84 地理坐标系 EPSG:4326");
            comboBox.Items.Add("CGCS2000 地理坐标系 EPSG:4490");
            comboBox.Items.Add("CGCS2000 高斯克吕格示例 EPSG:4547（米制，推荐缓冲）");
            comboBox.Items.Add("Web Mercator EPSG:3857（米制演示）");
            comboBox.SelectedIndex = 0;
        }

        private void BtnCad_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "CAD 文件|*.dwg;*.dxf";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _txtCadFile.Text = dialog.FileName;
                _txtPrefix.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                _txtOutputFolder.Text = Path.Combine(Path.GetDirectoryName(dialog.FileName), "ShpOutput");
            }
        }

        private void BtnOut_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _txtOutputFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (!File.Exists(_txtCadFile.Text))
            {
                MessageBox.Show(this, "请选择有效的 CAD 文件。", "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_txtOutputFolder.Text))
            {
                MessageBox.Show(this, "请选择输出目录。", "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_chkPoint.Checked && !_chkLine.Checked && !_chkPolygon.Checked && !_chkAnnotation.Checked)
            {
                MessageBox.Show(this, "请至少选择一种导出类型。", "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cmbSourceSpatialReference.SelectedIndex == 0 && _cmbOutputSpatialReference.SelectedIndex == 0)
            {
                DialogResult confirm = MessageBox.Show(
                    this,
                    "当前 CAD 原始坐标系和输出坐标系都未指定，转换结果仍会是 Unknown。\n\n后续如果缓冲区选择“米/千米”，会因为无法判断单位而报错。建议保持默认的米制投影坐标系，或手动选择真实坐标系。\n\n是否仍然继续？",
                    "坐标系提示",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }
            }

            if (_cmbSourceSpatialReference.SelectedIndex == 0 && _cmbOutputSpatialReference.SelectedIndex > 0)
            {
                DialogResult confirm = MessageBox.Show(
                    this,
                    "当前没有指定 CAD 原始坐标系。程序只能给输出 Shapefile 写入所选坐标系，不能保证完成真正投影转换。\n\n是否继续？",
                    "坐标系提示",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }
            }

            CadFilePath = _txtCadFile.Text;
            OutputFolder = _txtOutputFolder.Text;
            OutputPrefix = string.IsNullOrEmpty(_txtPrefix.Text) ? "Export" : _txtPrefix.Text.Trim();
            ExportPoint = _chkPoint.Checked;
            ExportPolyline = _chkLine.Checked;
            ExportPolygon = _chkPolygon.Checked;
            ExportAnnotation = _chkAnnotation.Checked;
            SourceSpatialReference = GetSelectedSpatialReference(_cmbSourceSpatialReference);
            OutputSpatialReference = GetSelectedSpatialReference(_cmbOutputSpatialReference);

            DialogResult = DialogResult.OK;
        }

        private ISpatialReference GetSelectedSpatialReference(ComboBox comboBox)
        {
            if (comboBox.SelectedIndex == 1)
            {
                return SpatialReferenceHelper.CreateWgs84();
            }

            if (comboBox.SelectedIndex == 2)
            {
                return SpatialReferenceHelper.CreateCgcs2000();
            }

            if (comboBox.SelectedIndex == 3)
            {
                return SpatialReferenceHelper.CreateCgcs2000GaussKrugerZone39();
            }

            if (comboBox.SelectedIndex == 4)
            {
                return SpatialReferenceHelper.CreateWebMercator();
            }

            return null;
        }
    }
}
