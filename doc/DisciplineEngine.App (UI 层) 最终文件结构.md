
## DisciplineEngine.App (UI 层) 最终文件结构

这个结构清晰地划分了应用的入口、主要功能模块和辅助工具。

### I. Views 视图文件结构

所有用户界面文件，包括主窗口和各个功能模块，都位于此目录。

```
DisciplineEngine.App/
├── Views/
│   ├── MainWindow.xaml               # 应用程序的主窗口，包含顶层导航或侧边栏，用于切换 Focus / Settings / Reports。
│   ├── MainDashboardView.xaml        # 【主页面】应用的欢迎页/核心仪表盘。
│   ├── FocusView.xaml                # 【专注页面】计时器功能界面。
│   ├── SettingsView.xaml             # 【设置页面】设置界面。
│   ├── DailyReportView.xaml          # 【日度数据】用户活动日报表视图。
│   ├── WeeklyReportView.xaml         # 【周度数据】用户活动周报表视图。
│   └── MonthlyReportView.xaml        # 【月度数据】用户活动月报表视图。
│
└── CustomControls/                   # 独立的用户自定义控件目录
    └── ...                           # 存放自定义按钮、卡片等可重用控件。
```

### II. ViewModels 视图模型文件结构

视图模型与视图一一对应，负责处理数据和业务逻辑。

```
DisciplineEngine.App/
├── ViewModels/
│   ├── MainDashboardViewModel.cs     # 主仪表盘的数据和逻辑。
│   ├── FocusViewModel.cs             # 专注页面逻辑。
│   ├── SettingsViewModel.cs          # 设置页面逻辑。
│   ├── DailyReportViewModel.cs       # 日报表的逻辑和数据准备。
│   ├── WeeklyReportViewModel.cs      # 周报表的逻辑和数据准备。
│   ├── MonthlyReportViewModel.cs     # 月报表的逻辑和数据准备。
│   │
│   └── Base/                         # 基础工具类
│       ├── ObservableObject.cs       # MVVM 基础实现（如 INotifyPropertyChanged）。
│       ├── 



.cs           # 命令实现（如 ICommand）。
│       └── Converters.cs             # XAML 转换器（如枚举、颜色转换）。
```