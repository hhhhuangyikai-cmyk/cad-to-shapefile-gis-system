using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using CadToShpGISSystem.Core;
using CadToShpGISSystem.Models;
using CadToShpGISSystem.UI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;

namespace CadToShpGISSystem
{
    /// <summary>
    /// 主界面窗体：负责菜单、工具栏、地图显示、图层管理和模块调度。
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly LicenseInitializer _licenseInitializer;
        private readonly CadLoader _cadLoader;
        private readonly CadToShpConverter _converter;
        private readonly AttributeQuery _attributeQuery;

        private MapMouseAction _currentAction = MapMouseAction.None;
        private string _currentCadFilePath;
        private bool _updatingLayerTree;
        private const int LeftPanelDefaultWidth = 260;
        private const int LeftPanelMaxWidth = 320;
        private const int RightPanelMinWidth = 520;

        public MainForm()
            : this(null)
        {
        }

        public MainForm(LicenseInitializer licenseInitializer)
        {
            _licenseInitializer = licenseInitializer;
            _cadLoader = new CadLoader();
            _converter = new CadToShpConverter();
            _attributeQuery = new AttributeQuery();

            InitializeComponent();
            ConfigureSplitLayout();

            lblLicense.Text = "License: " + (_licenseInitializer == null ? "未知" : _licenseInitializer.CurrentLicenseName);
            LogHelper.LogAdded += LogHelper_LogAdded;
            SetStatus("系统初始化完成。");
        }

        /// <summary>
        /// 设置左侧图层管理区和右侧地图区的默认比例。
        /// SplitContainer 初次布局后才知道真实宽度，因此在 Shown 事件中再校正一次。
        /// </summary>
        private void ConfigureSplitLayout()
        {
            splitContainer.FixedPanel = FixedPanel.Panel1;
            splitContainer.SplitterMoved += SplitContainer_SplitterMoved;
            Resize += MainForm_Resize;
            Shown += MainForm_Shown;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            ResetSplitLayout();
        }

        private void ResetSplitLayout()
        {
            SetLeftPanelWidth(LeftPanelDefaultWidth);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            EnsureMapPanelHasRoom();
        }

        private void SplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            EnsureMapPanelHasRoom();
        }

        private void EnsureMapPanelHasRoom()
        {
            if (splitContainer == null || splitContainer.Width <= 0)
            {
                return;
            }

            int rightWidth = splitContainer.Width - splitContainer.SplitterDistance - splitContainer.SplitterWidth;
            int safeRightMinWidth = GetSafeRightPanelMinWidth();
            if (splitContainer.SplitterDistance > LeftPanelMaxWidth || rightWidth < safeRightMinWidth)
            {
                SetLeftPanelWidth(LeftPanelDefaultWidth);
            }
        }

        private void SetLeftPanelWidth(int width)
        {
            if (splitContainer == null || splitContainer.Width <= 0)
            {
                return;
            }

            int safeRightMinWidth = GetSafeRightPanelMinWidth();
            if (splitContainer.Panel2MinSize != safeRightMinWidth)
            {
                splitContainer.Panel2MinSize = safeRightMinWidth;
            }

            int minLeftWidth = Math.Min(220, Math.Max(100, splitContainer.Width - safeRightMinWidth - splitContainer.SplitterWidth));
            int maxLeftWidth = splitContainer.Width - safeRightMinWidth - splitContainer.SplitterWidth;
            if (maxLeftWidth < minLeftWidth)
            {
                return;
            }

            int targetWidth = Math.Max(minLeftWidth, Math.Min(width, maxLeftWidth));
            splitContainer.SplitterDistance = targetWidth;
        }

        private int GetSafeRightPanelMinWidth()
        {
            if (splitContainer == null || splitContainer.Width <= 0)
            {
                return 100;
            }

            int maxPossibleRightMin = splitContainer.Width - 100 - splitContainer.SplitterWidth;
            if (maxPossibleRightMin < 100)
            {
                return 100;
            }

            return Math.Min(RightPanelMinWidth, maxPossibleRightMin);
        }

        private void OpenCad_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "CAD 文件|*.dwg;*.dxf";
            dialog.Title = "选择 CAD 文件";

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                SetBusy(true, "正在加载 CAD 数据...");
                _currentCadFilePath = dialog.FileName;

                List<LayerInfo> layerInfos = _cadLoader.LoadCadToMap(axMapControl, dialog.FileName);
                RefreshLayerTree();
                AppendCadStatisticsToTree(layerInfos);
                MapTools.FullExtent(axMapControl);

                SetStatus("CAD 加载完成：" + Path.GetFileName(dialog.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "打开 CAD 失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Error("CAD加载", "打开 CAD 失败", ex);
            }
            finally
            {
                SetBusy(false, "就绪");
            }
        }

        private void OpenShp_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Shapefile|*.shp";
            dialog.Multiselect = true;
            dialog.Title = "选择 Shapefile";

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                SetBusy(true, "正在加载 Shapefile...");
                for (int i = 0; i < dialog.FileNames.Length; i++)
                {
                    ShapefileHelper.AddShapefileToMap(axMapControl, dialog.FileNames[i]);
                }

                RefreshLayerTree();
                MapTools.FullExtent(axMapControl);
                SetStatus("Shapefile 加载完成。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "打开 Shapefile 失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Error("Shapefile", "打开 Shapefile 失败", ex);
            }
            finally
            {
                SetBusy(false, "就绪");
            }
        }

        private void Convert_Click(object sender, EventArgs e)
        {
            ConversionSettingForm form = new ConversionSettingForm(_currentCadFilePath);
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                SetBusy(true, "正在转换 CAD...");
                progressBar.Visible = true;
                progressBar.Value = 0;
                progressBar.Maximum = 4;

                List<ConversionResult> results = _converter.ConvertCadToShapefiles(
                    form.CadFilePath,
                    form.OutputFolder,
                    form.OutputPrefix,
                    form.ExportPoint,
                    form.ExportPolyline,
                    form.ExportPolygon,
                    form.ExportAnnotation,
                    form.SourceSpatialReference,
                    form.OutputSpatialReference);

                int loadedCount = 0;
                for (int i = 0; i < results.Count; i++)
                {
                    ConversionResult result = results[i];
                    progressBar.Value = Math.Min(progressBar.Maximum, i + 1);

                    if (result.Success && result.ExportCount > 0 && File.Exists(result.OutputShapefilePath))
                    {
                        ShapefileHelper.AddShapefileToMap(axMapControl, result.OutputShapefilePath);
                        loadedCount++;
                    }
                }

                RefreshLayerTree();
                MapTools.FullExtent(axMapControl);
                ShowConversionSummary(results);
                SetStatus("转换完成，已加载 " + loadedCount + " 个结果图层。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "CAD 转换失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Error("CAD转换", "CAD 转换失败", ex);
            }
            finally
            {
                progressBar.Visible = false;
                SetBusy(false, "就绪");
            }
        }

        private void Identify_Click(object sender, EventArgs e)
        {
            _currentAction = MapMouseAction.Identify;
            SetStatus("属性查询：请在地图上点击要素。");
        }

        private void Buffer_Click(object sender, EventArgs e)
        {
            BufferAnalysisForm form = new BufferAnalysisForm(axMapControl);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                RefreshLayerTree();
                SetStatus("缓冲区分析完成：" + form.OutputPath);
            }
        }

        private void ZoomIn_Click(object sender, EventArgs e)
        {
            _currentAction = MapMouseAction.ZoomIn;
            SetStatus("放大：在地图上拖框，或单击执行中心放大。");
        }

        private void ZoomOut_Click(object sender, EventArgs e)
        {
            _currentAction = MapMouseAction.ZoomOut;
            SetStatus("缩小：在地图上单击执行中心缩小。");
        }

        private void Pan_Click(object sender, EventArgs e)
        {
            _currentAction = MapMouseAction.Pan;
            SetStatus("平移：在地图上拖动。");
        }

        private void FullExtent_Click(object sender, EventArgs e)
        {
            MapTools.FullExtent(axMapControl);
            SetStatus("已全图显示。");
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            MapTools.ClearLayers(axMapControl);
            RefreshLayerTree();
            SetStatus("地图已清空。");
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void About_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                this,
                "基于 ArcObjects 的 CAD 转 Shapefile 与 GIS 浏览分析系统\n" +
                "功能：CAD 预览、Shapefile 转换、属性查询、缓冲区分析、日志记录。\n" +
                "开发环境：VS2017 + .NET Framework 4.7.2 + ArcGIS Engine/Desktop 10.8。",
                "关于系统",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void AxMapControl_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            lblCoordinate.Text = string.Format("X: {0:F3}  Y: {1:F3}", e.mapX, e.mapY);
        }

        private void AxMapControl_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (e.button != 1)
            {
                return;
            }

            try
            {
                if (_currentAction == MapMouseAction.Identify)
                {
                    DataTable table = _attributeQuery.IdentifyAt(axMapControl, e.mapX, e.mapY);
                    if (table.Rows.Count > 0)
                    {
                        AttributeTableForm form = new AttributeTableForm("查询结果", table);
                        form.Show(this);
                        SetStatus("属性查询完成，结果数：" + table.Rows.Count);
                    }
                    else
                    {
                        SetStatus("当前位置未识别到要素。");
                    }
                }
                else if (_currentAction == MapMouseAction.ZoomIn)
                {
                    ESRI.ArcGIS.Geometry.IEnvelope envelope = axMapControl.TrackRectangle();
                    if (envelope != null && !envelope.IsEmpty)
                    {
                        axMapControl.Extent = envelope;
                        axMapControl.ActiveView.Refresh();
                    }
                    else
                    {
                        MapTools.ZoomIn(axMapControl);
                    }
                }
                else if (_currentAction == MapMouseAction.ZoomOut)
                {
                    MapTools.ZoomOut(axMapControl);
                }
                else if (_currentAction == MapMouseAction.Pan)
                {
                    axMapControl.Pan();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "地图操作失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Error("地图操作", "地图鼠标操作失败", ex);
            }
        }

        private void TreeLayers_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_updatingLayerTree || e.Node == null || e.Node.Tag == null)
            {
                return;
            }

            ILayer layer = e.Node.Tag as ILayer;
            if (layer != null)
            {
                MapTools.SetLayerVisible(axMapControl, layer, e.Node.Checked);
                SetStatus("图层可见性：" + layer.Name + " = " + e.Node.Checked);
            }
        }

        private void TreeLayers_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeLayers.SelectedNode = e.Node;
            if (e.Button == MouseButtons.Right && e.Node != null && e.Node.Tag is ILayer)
            {
                layerContextMenu.Show(treeLayers, e.Location);
            }
        }

        private void ZoomToLayer_Click(object sender, EventArgs e)
        {
            ILayer layer = GetSelectedTreeLayer();
            MapTools.ZoomToLayer(axMapControl, layer);
        }

        private void RemoveLayer_Click(object sender, EventArgs e)
        {
            ILayer layer = GetSelectedTreeLayer();
            if (layer == null)
            {
                return;
            }

            MapTools.RemoveLayer(axMapControl, layer);
            RefreshLayerTree();
        }

        private void OpenAttributeTable_Click(object sender, EventArgs e)
        {
            IFeatureLayer featureLayer = GetSelectedTreeLayer() as IFeatureLayer;
            if (featureLayer == null || featureLayer.FeatureClass == null)
            {
                return;
            }

            DataTable table = ShapefileHelper.BuildAttributeTable(featureLayer.FeatureClass, 2000);
            AttributeTableForm form = new AttributeTableForm(featureLayer.Name, table);
            form.Show(this);
        }

        private void ToggleLayerVisible_Click(object sender, EventArgs e)
        {
            if (treeLayers.SelectedNode == null || !(treeLayers.SelectedNode.Tag is ILayer))
            {
                return;
            }

            treeLayers.SelectedNode.Checked = !treeLayers.SelectedNode.Checked;
        }

        private void RefreshLayerTree()
        {
            _updatingLayerTree = true;
            try
            {
                treeLayers.Nodes.Clear();
                for (int i = 0; i < axMapControl.LayerCount; i++)
                {
                    ILayer layer = axMapControl.get_Layer(i);
                    TreeNode node = new TreeNode(layer.Name);
                    node.Name = layer.Name;
                    node.Tag = layer;
                    node.Checked = layer.Visible;
                    treeLayers.Nodes.Add(node);
                }

                treeLayers.ExpandAll();
            }
            finally
            {
                _updatingLayerTree = false;
            }
        }

        private void AppendCadStatisticsToTree(IList<LayerInfo> layerInfos)
        {
            if (layerInfos == null)
            {
                return;
            }

            _updatingLayerTree = true;
            try
            {
                for (int i = 0; i < layerInfos.Count; i++)
                {
                    LayerInfo info = layerInfos[i];
                    TreeNode[] nodes = treeLayers.Nodes.Find(info.Name, false);
                    TreeNode targetNode = null;
                    if (nodes.Length > 0)
                    {
                        targetNode = nodes[0];
                    }
                    else
                    {
                        for (int n = 0; n < treeLayers.Nodes.Count; n++)
                        {
                            if (treeLayers.Nodes[n].Text == info.Name)
                            {
                                targetNode = treeLayers.Nodes[n];
                                break;
                            }
                        }
                    }

                    if (targetNode == null)
                    {
                        continue;
                    }

                    targetNode.Nodes.Add("要素总数：" + info.FeatureCount);
                    foreach (KeyValuePair<string, int> pair in info.LayerStatistics)
                    {
                        targetNode.Nodes.Add(pair.Key + "：" + pair.Value);
                    }
                }

                treeLayers.ExpandAll();
            }
            finally
            {
                _updatingLayerTree = false;
            }
        }

        private ILayer GetSelectedTreeLayer()
        {
            if (treeLayers.SelectedNode == null)
            {
                return null;
            }

            return treeLayers.SelectedNode.Tag as ILayer;
        }

        private void ShowConversionSummary(IList<ConversionResult> results)
        {
            if (results == null || results.Count == 0)
            {
                MessageBox.Show(this, "没有执行任何转换任务。", "转换结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string message = "";
            for (int i = 0; i < results.Count; i++)
            {
                ConversionResult result = results[i];
                message += string.Format(
                    "{0}\n状态：{1}\n数量：{2}\n输出：{3}\n信息：{4}\n\n",
                    result.CadFeatureClassName,
                    result.Success ? "成功" : "失败",
                    result.ExportCount,
                    result.OutputShapefilePath,
                    result.Message);
            }

            MessageBox.Show(this, message, "转换结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetBusy(bool busy, string status)
        {
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            SetStatus(status);
        }

        private void SetStatus(string status)
        {
            lblStatus.Text = status;
            LogHelper.Info("状态", status);
        }

        private void LogHelper_LogAdded(string message)
        {
            if (txtLogs == null || txtLogs.IsDisposed)
            {
                return;
            }

            if (txtLogs.InvokeRequired)
            {
                txtLogs.BeginInvoke(new Action<string>(LogHelper_LogAdded), message);
                return;
            }

            txtLogs.AppendText(message + Environment.NewLine);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogHelper.LogAdded -= LogHelper_LogAdded;
        }
    }
}
