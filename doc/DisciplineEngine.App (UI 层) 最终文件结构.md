
## DisciplineEngine.App (UI ��) �����ļ��ṹ

����ṹ�����ػ�����Ӧ�õ���ڡ���Ҫ����ģ��͸������ߡ�

### I. Views ��ͼ�ļ��ṹ

�����û������ļ������������ں͸�������ģ�飬��λ�ڴ�Ŀ¼��

```
DisciplineEngine.App/
������ Views/
��   ������ MainWindow.xaml               # Ӧ�ó���������ڣ��������㵼���������������л� Focus / Settings / Reports��
��   ������ MainDashboardView.xaml        # ����ҳ�桿Ӧ�õĻ�ӭҳ/�����Ǳ��̡�
��   ������ FocusView.xaml                # ��רעҳ�桿��ʱ�����ܽ��档
��   ������ SettingsView.xaml             # ������ҳ�桿���ý��档
��   ������ DailyReportView.xaml          # ���ն����ݡ��û���ձ�����ͼ��
��   ������ WeeklyReportView.xaml         # ���ܶ����ݡ��û���ܱ�����ͼ��
��   ������ MonthlyReportView.xaml        # ���¶����ݡ��û���±�����ͼ��
��
������ CustomControls/                   # �������û��Զ���ؼ�Ŀ¼
    ������ ...                           # ����Զ��尴ť����Ƭ�ȿ����ÿؼ���
```

### II. ViewModels ��ͼģ���ļ��ṹ

��ͼģ������ͼһһ��Ӧ�����������ݺ�ҵ���߼���

```
DisciplineEngine.App/
������ ViewModels/
��   ������ MainDashboardViewModel.cs     # ���Ǳ��̵����ݺ��߼���
��   ������ FocusViewModel.cs             # רעҳ���߼���
��   ������ SettingsViewModel.cs          # ����ҳ���߼���
��   ������ DailyReportViewModel.cs       # �ձ�����߼�������׼����
��   ������ WeeklyReportViewModel.cs      # �ܱ�����߼�������׼����
��   ������ MonthlyReportViewModel.cs     # �±�����߼�������׼����
��   ��
��   ������ Base/                         # ����������
��       ������ ObservableObject.cs       # MVVM ����ʵ�֣��� INotifyPropertyChanged����
��       ������ 



.cs           # ����ʵ�֣��� ICommand����
��       ������ Converters.cs             # XAML ת��������ö�١���ɫת������
```