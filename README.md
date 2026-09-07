# CAD 转 Shapefile GIS 系统

基于 **C# WinForms** 与 **ArcObjects 10.8** 开发的桌面 GIS 应用，用于将 CAD 图纸转换为 Shapefile 数据，并提供地图浏览、图层管理、属性查询、缓冲区分析与坐标系处理等功能。

## 项目功能

- 支持加载 DWG、DXF 等 CAD 数据
- 将 CAD 图层转换为 Shapefile 格式
- 自动生成 `.shp`、`.shx`、`.dbf`、`.prj` 等配套文件
- 地图浏览、缩放、平移与图层显示控制
- 图层属性查看与要素查询
- 缓冲区分析
- 坐标系与投影信息处理
- 操作日志输出，方便检查数据转换过程

## 技术栈

- 开发语言：C#
- 桌面框架：Windows Forms
- GIS 平台：ArcObjects 10.8
- 开发环境：Visual Studio 2017
- 目标框架：.NET Framework 4.7.2
- 编译平台：x86

## 运行环境

使用本项目需要准备以下环境：

1. Windows 操作系统
2. Visual Studio 2017 或兼容 .NET Framework 4.7.2 的开发环境
3. ArcGIS Desktop 10.8 或 ArcGIS Engine Runtime
4. ArcObjects SDK for .NET 10.8
5. 项目需以 `x86` 平台编译运行

> ArcObjects 依赖 ArcGIS 授权环境。请确认本机已正确安装并授权 ArcGIS 后再运行。

## 快速开始

1. 克隆或下载本仓库。
2. 使用 Visual Studio 打开解决方案文件。
3. 确认 ArcObjects 10.8 的引用路径正确。
4. 将项目生成平台设置为 `x86`。
5. 编译并运行程序。
6. 在程序中加载 CAD 图纸，选择转换参数与输出目录。
7. 完成转换后，可继续浏览图层、查看属性或进行缓冲区分析。

## 输出数据说明

转换完成后，输出目录通常包含：

- `.shp`：空间几何数据
- `.shx`：空间索引文件
- `.dbf`：属性数据表
- `.prj`：坐标系与投影信息

这些文件需放在同一目录下使用。

## 项目结构

```text
group7/
├── Core/        核心转换与 GIS 业务逻辑
├── Models/      数据模型与参数对象
├── UI/          WinForms 界面
├── docs/        项目文档与说明材料
├── README.md    项目说明
└── *.sln        Visual Studio 解决方案文件
```

## 注意事项

- CAD 图层命名、单位和坐标信息会影响转换结果。
- 导出前应确认目标坐标系与源数据一致。
- 大型 CAD 文件转换可能需要较长时间。
- 本项目主要用于 GIS 课程设计、CAD 数据整理与基础空间分析学习。

## 项目定位

本项目是一个面向 GIS 数据处理实践的课程工程，重点展示 CAD 数据向 GIS 矢量数据转换的完整流程，以及 ArcObjects 在桌面 GIS 应用开发中的使用方式。

