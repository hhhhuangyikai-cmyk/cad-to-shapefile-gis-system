using System.ComponentModel;
using System.Windows.Forms;
using ESRI.ArcGIS.Controls;

namespace CadToShpGISSystem
{
    partial class MainForm
    {
        private IContainer components = null;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblCoordinate;
        private ToolStripStatusLabel lblLicense;
        private ToolStripProgressBar progressBar;
        private SplitContainer splitContainer;
        private TabControl leftTabControl;
        private TabPage tabLayers;
        private TabPage tabSettings;
        private TabPage tabLogs;
        private TreeView treeLayers;
        private TextBox txtLogs;
        private Label lblSettingsHint;
        private AxMapControl axMapControl;
        private ContextMenuStrip layerContextMenu;
        private ToolStripMenuItem miZoomToLayer;
        private ToolStripMenuItem miRemoveLayer;
        private ToolStripMenuItem miOpenAttributeTable;
        private ToolStripMenuItem miLayerVisible;
        private ToolStripButton btnOpenCad;
        private ToolStripButton btnOpenShp;
        private ToolStripButton btnConvert;
        private ToolStripButton btnIdentify;
        private ToolStripButton btnBuffer;
        private ToolStripButton btnZoomIn;
        private ToolStripButton btnZoomOut;
        private ToolStripButton btnPan;
        private ToolStripButton btnFullExtent;
        private ToolStripButton btnClear;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            menuStrip = new MenuStrip();
            toolStrip = new ToolStrip();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            lblCoordinate = new ToolStripStatusLabel();
            lblLicense = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            splitContainer = new SplitContainer();
            leftTabControl = new TabControl();
            tabLayers = new TabPage();
            tabSettings = new TabPage();
            tabLogs = new TabPage();
            treeLayers = new TreeView();
            txtLogs = new TextBox();
            lblSettingsHint = new Label();
            axMapControl = new AxMapControl();
            layerContextMenu = new ContextMenuStrip(components);
            miZoomToLayer = new ToolStripMenuItem();
            miRemoveLayer = new ToolStripMenuItem();
            miOpenAttributeTable = new ToolStripMenuItem();
            miLayerVisible = new ToolStripMenuItem();
            btnOpenCad = new ToolStripButton();
            btnOpenShp = new ToolStripButton();
            btnConvert = new ToolStripButton();
            btnIdentify = new ToolStripButton();
            btnBuffer = new ToolStripButton();
            btnZoomIn = new ToolStripButton();
            btnZoomOut = new ToolStripButton();
            btnPan = new ToolStripButton();
            btnFullExtent = new ToolStripButton();
            btnClear = new ToolStripButton();

            ((ISupportInitialize)(splitContainer)).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            leftTabControl.SuspendLayout();
            tabLayers.SuspendLayout();
            tabSettings.SuspendLayout();
            tabLogs.SuspendLayout();
            ((ISupportInitialize)(axMapControl)).BeginInit();
            SuspendLayout();

            ToolStripMenuItem menuFile = new ToolStripMenuItem("文件");
            ToolStripMenuItem menuOpenCad = new ToolStripMenuItem("打开 CAD");
            ToolStripMenuItem menuOpenShp = new ToolStripMenuItem("打开 Shapefile");
            ToolStripMenuItem menuExit = new ToolStripMenuItem("退出");
            menuOpenCad.Click += OpenCad_Click;
            menuOpenShp.Click += OpenShp_Click;
            menuExit.Click += Exit_Click;
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuOpenCad, menuOpenShp, new ToolStripSeparator(), menuExit });

            ToolStripMenuItem menuConvert = new ToolStripMenuItem("数据转换");
            ToolStripMenuItem menuStartConvert = new ToolStripMenuItem("CAD 转 Shapefile");
            menuStartConvert.Click += Convert_Click;
            menuConvert.DropDownItems.Add(menuStartConvert);

            ToolStripMenuItem menuAnalysis = new ToolStripMenuItem("空间分析");
            ToolStripMenuItem menuIdentify = new ToolStripMenuItem("属性查询");
            ToolStripMenuItem menuBuffer = new ToolStripMenuItem("缓冲区分析");
            menuIdentify.Click += Identify_Click;
            menuBuffer.Click += Buffer_Click;
            menuAnalysis.DropDownItems.AddRange(new ToolStripItem[] { menuIdentify, menuBuffer });

            ToolStripMenuItem menuMap = new ToolStripMenuItem("地图操作");
            ToolStripMenuItem menuZoomIn = new ToolStripMenuItem("放大");
            ToolStripMenuItem menuZoomOut = new ToolStripMenuItem("缩小");
            ToolStripMenuItem menuPan = new ToolStripMenuItem("平移");
            ToolStripMenuItem menuFull = new ToolStripMenuItem("全图显示");
            ToolStripMenuItem menuClear = new ToolStripMenuItem("清空地图");
            menuZoomIn.Click += ZoomIn_Click;
            menuZoomOut.Click += ZoomOut_Click;
            menuPan.Click += Pan_Click;
            menuFull.Click += FullExtent_Click;
            menuClear.Click += Clear_Click;
            menuMap.DropDownItems.AddRange(new ToolStripItem[] { menuZoomIn, menuZoomOut, menuPan, menuFull, menuClear });

            ToolStripMenuItem menuHelp = new ToolStripMenuItem("帮助");
            ToolStripMenuItem menuAbout = new ToolStripMenuItem("关于系统");
            menuAbout.Click += About_Click;
            menuHelp.DropDownItems.Add(menuAbout);
            menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuConvert, menuAnalysis, menuMap, menuHelp });
            menuStrip.Dock = DockStyle.Top;

            btnOpenCad.Text = "打开CAD";
            btnOpenCad.Click += OpenCad_Click;
            btnOpenShp.Text = "打开SHP";
            btnOpenShp.Click += OpenShp_Click;
            btnConvert.Text = "开始转换";
            btnConvert.Click += Convert_Click;
            btnIdentify.Text = "属性查询";
            btnIdentify.Click += Identify_Click;
            btnBuffer.Text = "缓冲区";
            btnBuffer.Click += Buffer_Click;
            btnZoomIn.Text = "放大";
            btnZoomIn.Click += ZoomIn_Click;
            btnZoomOut.Text = "缩小";
            btnZoomOut.Click += ZoomOut_Click;
            btnPan.Text = "平移";
            btnPan.Click += Pan_Click;
            btnFullExtent.Text = "全图";
            btnFullExtent.Click += FullExtent_Click;
            btnClear.Text = "清空";
            btnClear.Click += Clear_Click;
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                btnOpenCad, btnOpenShp, new ToolStripSeparator(), btnConvert,
                new ToolStripSeparator(), btnIdentify, btnBuffer,
                new ToolStripSeparator(), btnZoomIn, btnZoomOut, btnPan, btnFullExtent, btnClear
            });
            toolStrip.Dock = DockStyle.Top;

            lblStatus.Text = "就绪";
            lblStatus.Spring = true;
            lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblCoordinate.Text = "X: --  Y: --";
            lblCoordinate.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblLicense.Text = "License: --";
            lblLicense.BorderSides = ToolStripStatusLabelBorderSides.Left;
            progressBar.Width = 160;
            progressBar.Visible = false;
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, progressBar, lblCoordinate, lblLicense });
            statusStrip.Dock = DockStyle.Bottom;

            splitContainer.Dock = DockStyle.Fill;
            splitContainer.FixedPanel = FixedPanel.Panel1;
            splitContainer.Location = new System.Drawing.Point(0, 49);
            splitContainer.Name = "splitContainer";
            splitContainer.Panel1MinSize = 100;
            splitContainer.Panel2MinSize = 100;
            splitContainer.SplitterDistance = 120;
            splitContainer.SplitterWidth = 5;
            splitContainer.TabIndex = 0;

            leftTabControl.Dock = DockStyle.Fill;
            leftTabControl.Controls.Add(tabLayers);
            leftTabControl.Controls.Add(tabSettings);
            leftTabControl.Controls.Add(tabLogs);

            tabLayers.Text = "图层管理";
            treeLayers.Dock = DockStyle.Fill;
            treeLayers.CheckBoxes = true;
            treeLayers.HideSelection = false;
            treeLayers.AfterCheck += TreeLayers_AfterCheck;
            treeLayers.NodeMouseClick += TreeLayers_NodeMouseClick;
            tabLayers.Controls.Add(treeLayers);

            tabSettings.Text = "转换设置";
            lblSettingsHint.Dock = DockStyle.Fill;
            lblSettingsHint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblSettingsHint.Text = "通过“开始转换”设置 CAD 输入、输出目录、几何类型和坐标系。";
            tabSettings.Controls.Add(lblSettingsHint);

            tabLogs.Text = "日志信息";
            txtLogs.Dock = DockStyle.Fill;
            txtLogs.Multiline = true;
            txtLogs.ScrollBars = ScrollBars.Both;
            txtLogs.ReadOnly = true;
            txtLogs.WordWrap = false;
            tabLogs.Controls.Add(txtLogs);

            splitContainer.Panel1.Controls.Add(leftTabControl);
            axMapControl.Dock = DockStyle.Fill;
            axMapControl.Name = "axMapControl";
            axMapControl.TabIndex = 0;
            axMapControl.OnMouseMove += AxMapControl_OnMouseMove;
            axMapControl.OnMouseDown += AxMapControl_OnMouseDown;
            splitContainer.Panel2.Controls.Add(axMapControl);

            miZoomToLayer.Text = "缩放到图层";
            miZoomToLayer.Click += ZoomToLayer_Click;
            miRemoveLayer.Text = "移除图层";
            miRemoveLayer.Click += RemoveLayer_Click;
            miOpenAttributeTable.Text = "打开属性表";
            miOpenAttributeTable.Click += OpenAttributeTable_Click;
            miLayerVisible.Text = "切换可见性";
            miLayerVisible.Click += ToggleLayerVisible_Click;
            layerContextMenu.Items.AddRange(new ToolStripItem[]
            {
                miZoomToLayer, miOpenAttributeTable, miLayerVisible, new ToolStripSeparator(), miRemoveLayer
            });

            AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1180, 760);
            Controls.Add(splitContainer);
            Controls.Add(statusStrip);
            Controls.Add(toolStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "基于 ArcObjects 的 CAD 转 Shapefile 与 GIS 浏览分析系统";
            FormClosing += MainForm_FormClosing;

            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((ISupportInitialize)(splitContainer)).EndInit();
            splitContainer.ResumeLayout(false);
            leftTabControl.ResumeLayout(false);
            tabLayers.ResumeLayout(false);
            tabSettings.ResumeLayout(false);
            tabLogs.ResumeLayout(false);
            tabLogs.PerformLayout();
            ((ISupportInitialize)(axMapControl)).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
