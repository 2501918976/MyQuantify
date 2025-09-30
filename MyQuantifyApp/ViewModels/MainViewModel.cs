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
            CurrentPage = new Views.FocusView();

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
                    case "WeeklyReportView":
                        CurrentPage = new Views.WeeklyReportView();
                        break;
                    case "MonthlyReportView":
                        CurrentPage = new Views.MonthlyReportView();
                        break;
                    case "SettingsView":
                        CurrentPage = new Views.SettingsView();
                        break;
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
