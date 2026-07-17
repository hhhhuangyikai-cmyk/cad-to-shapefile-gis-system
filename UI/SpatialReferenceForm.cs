using System;
using System.Windows.Forms;
using CadToShpGISSystem.Core;
using ESRI.ArcGIS.Geometry;

namespace CadToShpGISSystem.UI
{
    /// <summary>
    /// 手动选择空间参考的窗体。
    /// </summary>
    public class SpatialReferenceForm : Form
    {
        private readonly RadioButton _rbUnknown;
        private readonly RadioButton _rbWgs84;
        private readonly RadioButton _rbCgcs2000;
        private readonly RadioButton _rbCgcs2000Gk;

        public ISpatialReference SelectedSpatialReference { get; private set; }

        public SpatialReferenceForm()
        {
            Text = "选择空间参考";
            Width = 420;
            Height = 240;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _rbUnknown = new RadioButton { Left = 24, Top = 24, Width = 330, Text = "保持未知坐标系" };
            _rbWgs84 = new RadioButton { Left = 24, Top = 58, Width = 330, Text = "WGS84 地理坐标系（EPSG:4326）" };
            _rbCgcs2000 = new RadioButton { Left = 24, Top = 92, Width = 330, Text = "CGCS2000 地理坐标系（EPSG:4490）" };
            _rbCgcs2000Gk = new RadioButton { Left = 24, Top = 126, Width = 360, Text = "CGCS2000 高斯克吕格示例（EPSG:4547）" };
            _rbUnknown.Checked = true;

            Button btnOk = new Button { Text = "确定", Left = 210, Top = 165, Width = 80 };
            Button btnCancel = new Button { Text = "取消", Left = 300, Top = 165, Width = 80 };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; };

            Controls.Add(_rbUnknown);
            Controls.Add(_rbWgs84);
            Controls.Add(_rbCgcs2000);
            Controls.Add(_rbCgcs2000Gk);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_rbWgs84.Checked)
            {
                SelectedSpatialReference = SpatialReferenceHelper.CreateWgs84();
            }
            else if (_rbCgcs2000.Checked)
            {
                SelectedSpatialReference = SpatialReferenceHelper.CreateCgcs2000();
            }
            else if (_rbCgcs2000Gk.Checked)
            {
                SelectedSpatialReference = SpatialReferenceHelper.CreateCgcs2000GaussKrugerZone39();
            }
            else
            {
                SelectedSpatialReference = null;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
