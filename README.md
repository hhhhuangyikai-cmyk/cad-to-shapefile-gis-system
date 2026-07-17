# 基于 ArcObjects 的 CAD 转 Shapefile 与 GIS 浏览分析系统

本项目为《组件式 GIS 开发》课程设计工程，使用 C# WinForms + ArcObjects SDK 10.8 实现 CAD 数据加载、CAD 转 Shapefile、地图浏览、属性查询、缓冲区分析、坐标系处理和日志记录。

## 开发环境

- Visual Studio 2017
- .NET Framework 4.7.2
- ArcGIS Desktop 10.8 或 ArcGIS Engine Runtime 10.8
- ArcObjects SDK for the Microsoft .NET Framework 10.8
- 平台目标：x86

## 运行步骤

1. 使用 Visual Studio 2017 打开 `CadToShpGISSystem.sln`。
2. 在“配置管理器”中确认平台为 `x86`。
3. 确认本机已安装 ArcGIS Desktop/Engine 10.8，并且授权可用。
4. 如果 ESRI 引用丢失，在“引用”中重新添加 ArcGIS 10.8 对应程序集。
5. 编译并运行。
6. 点击“打开CAD”加载 `.dwg` 或 `.dxf` 文件。
7. 点击“开始转换”，设置输出目录、导出类型和坐标系后生成 Shapefile。
8. 使用“属性查询”和“缓冲区”进行展示分析。

## 坐标系说明

投影和坐标系处理已经集成在“开始转换”窗口中，不再单独提供“投影转换”按钮。窗口包含两个关键选项：

- `CAD原始坐标系`：CAD 当前坐标值原本属于哪个坐标系。
- `输出坐标系`：导出的 Shapefile 最终使用哪个坐标系。

课程演示中 CAD 常见为平面米制工程坐标，因此默认选择 `CGCS2000 高斯克吕格示例 EPSG:4547`，输出默认沿用源坐标系。这样导出的 Shapefile 会带 `.prj`，缓冲区分析可以直接选择“米”。

如果 CAD 的真实坐标系不是该示例，应根据数据实际情况手动选择。只有同时知道源坐标系和目标坐标系时，`IGeometry.Project` 才能进行真正的投影转换。

## 主要功能

- ArcGIS License 初始化。
- CAD 点、线、面、注记要素类加载与预览。
- CAD 按几何类型导出 Shapefile。
- 输出 Shapefile 写入 `.prj`。
- 自动移除 CAD 几何中的 Z/M 值，避免二维 Shapefile 写入失败。
- 图层列表、可见性控制、缩放、平移、全图、清空。
- 点击属性查询和属性表显示。
- 缓冲区分析，支持米、千米、度、数据坐标单位。
- 日志记录。

## 项目结构

```text
CadToShpGISSystem
├─ Program.cs
├─ MainForm.cs
├─ MainForm.Designer.cs
├─ Core
├─ Models
├─ UI
├─ Properties
├─ docs
└─ scripts
```

## 建库/提交建议

提交代码仓库时建议包含：

- `.sln`
- `.csproj`
- `App.config`
- `Program.cs`
- `MainForm.cs`
- `MainForm.Designer.cs`
- `Core/`
- `Models/`
- `UI/`
- `Properties/`
- `docs/docx/`
- `scripts/`
- `README.md`
- `.gitignore`

不要提交 `.vs/`、`bin/`、`obj/`、日志文件和测试输出 Shapefile。
