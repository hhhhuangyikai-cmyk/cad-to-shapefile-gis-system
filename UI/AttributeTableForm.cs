using System.Data;
using System.Windows.Forms;

namespace CadToShpGISSystem.UI
{
    /// <summary>
    /// 属性表显示窗体。
    /// </summary>
    public class AttributeTableForm : Form
    {
        private readonly DataGridView _gridView;

        public AttributeTableForm(string title, DataTable table)
        {
            Text = string.IsNullOrEmpty(title) ? "属性表" : "属性表 - " + title;
            Width = 900;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;

            _gridView = new DataGridView();
            _gridView.Dock = DockStyle.Fill;
            _gridView.ReadOnly = true;
            _gridView.AllowUserToAddRows = false;
            _gridView.AllowUserToDeleteRows = false;
            _gridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            _gridView.DataSource = table;

            Controls.Add(_gridView);
        }
    }
}
