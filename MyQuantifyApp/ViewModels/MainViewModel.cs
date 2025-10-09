using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MyQuantifyApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Page _currentPage;

        public Page CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        // 命令
        public RelayCommand<string> ShowPageCommand { get; }

        public MainViewModel()
        {
            // 初始化默认页面
            CurrentPage = new Views.DailyReportView();

            // 初始化命令
            ShowPageCommand = new RelayCommand<string>(pageName =>
            {
                switch (pageName)
                {
                    case "FocusView":
                        CurrentPage = new Views.FocusView();
                        break;
                    case "DailyReportView":
                        CurrentPage = new Views.DailyReportView();
                        break;
                    case "PieChartView":
                        CurrentPage = new Views.PieChartView();
                        break;
                    case "LineChartView":
                        CurrentPage = new Views.LineChartView();
                        break;
                    case "SettingsView":
                        CurrentPage = new Views.SettingsView();
                        break;
                    case "CopyboardView":
                        CurrentPage = new Views.CopyboardView();
                        break;
                    case "KeyboradView":
                        CurrentPage = new Views.KeyboradView();
                        break;
                    case "WindowsView":
                        CurrentPage = new Views.WindowsView();
                        break;
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
