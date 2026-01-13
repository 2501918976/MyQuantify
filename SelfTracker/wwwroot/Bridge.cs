using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace SelfTracker.wwwroot
{
    [ComVisible(true)]
    public class Bridge
    {
        public void ShowMessage(string msg)
        {
            System.Windows.MessageBox.Show(msg);
        }

        #region index页面

        public void Minimize()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                System.Windows.Application.Current.MainWindow.WindowState = WindowState.Minimized;
            });
        }

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                System.Windows.Application.Current.MainWindow.Hide();
            });
        }

        #endregion

        #region today页面

        //{
        //    "summary": {
        //        "score": 88,
        //        "keyCount": 12450,
        //        "activeHours": 5.4,
        //        "clipboardCount": 42,
        //        "idleHours": 1.2
        //    },
        //    "heatmap": {
        //        "app": [0.8, 0.5, 0.0, 1.0, ...], // 24个元素，对应透明度
        //        "typing": [0.2, 0.9, 0.1, 0.0, ...],
        //        "copy": [0.1, 0.0, 0.5, 0.3, ...]
        //    },
        //    "categories": [
        //        { "name": "办公开发", "time": "3.5h", "percent": 70, "color": "#4e73df" },
        //        { "name": "视频娱乐", "time": "1.0h", "percent": 20, "color": "#1cc88a" }
        //    ],
        //    "currentProcess": {
        //    "title": "Visual Studio 2022",
        //        "mode": "开发中",
        //        "duration": "1h 20m"
        //    }
        //}

        #endregion


        #region 历史页面


        #endregion


        #region 分类页面


        #endregion


        #region 设置页面


        #endregion
    }
}