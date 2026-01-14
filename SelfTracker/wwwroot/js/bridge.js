// WebView2 桥接对象包装器
(function () {
    'use strict';

    // 1. 内部检测是否在 WebView2 环境
    const isWebView2 = window.chrome && window.chrome.webview;

    // 2. 获取原始 C# 对象的 Proxy (注意这里名字必须和 C# 中的 "AppBridge" 一致)
    const rawBridge = isWebView2 ? window.chrome.webview.hostObjects.AppBridge : null;

    window.AppBridge = {
        /**
         * 基础窗口控制
         */
        minimize: async function () {
            if (isWebView2) {
                // 注意：C# 方法名在 JS 中默认是首字母小写的，除非你在 C# 做了配置
                await rawBridge.BtnMinimize_Click();
            } else {
                console.log('[Mock] minimize()');
            }
        },

        close: async function () {
            if (isWebView2) {
                await rawBridge.BtnClose_Click();
            } else {
                console.log('[Mock] close()');
            }
        },

        /**
         * 数据获取方法封装
         */
        GetTodayStats: async function () {
            if (isWebView2) {
                return await rawBridge.GetTodayStats();
            } else {
                return JSON.stringify({
                    score: 85, totalKeystrokes: 8542, totalCopies: 156,
                    activeTime: 4.2, afkTime: 0.8, growthRate: 12, currentStatus: "深度工作"
                });
            }
        },

        Get24HActivityMap: async function () {
            if (isWebView2) {
                return await rawBridge.Get24HActivityMap();
            } else {
                return JSON.stringify({
                    application: Array(24).fill(5),
                    typing: Array(24).fill(4),
                    copying: Array(24).fill(2)
                });
            }
        },

        GetCategoryRanking: async function () {
            if (isWebView2) {
                return await rawBridge.GetCategoryRanking();
            } else {
                return JSON.stringify([
                    { categoryName: "办公开发", icon: "💼", duration: 2.5, percentage: 62, color: "#4e73df" },
                    { categoryName: "娱乐休闲", icon: "🎮", duration: 1.2, percentage: 30, color: "#1cc88a" }
                ]);
            }
        },

        GetCurrentProcess: async function () {
            if (isWebView2) {
                return await rawBridge.GetCurrentProcess();
            } else {
                return JSON.stringify({
                    processName: "Code.exe", windowTitle: "VS Code - App.js",
                    icon: "💻", duration: 45, status: "运行中"
                });
            }
        },

        /**
         * 规则引擎方法
         */
        获取所有分类: async function () {
            if (isWebView2) {
                return await rawBridge.获取所有分类();
            } else {
                return JSON.stringify([{ Id: 1, CategoryName: "办公开发", ColorCode: "#4e73df", CategoryRules: [] }]);
            }
        },

        获取未分类的活动: async function () {
            if (isWebView2) {
                return await rawBridge.获取未分类的活动();
            } else {
                return JSON.stringify([{ ProcessName: "chrome.exe", WindowTitle: "Google Chrome" }]);
            }
        },

        新增一个规则: async function (ruleDataJson) {
            if (isWebView2) await rawBridge.新增一个规则(ruleDataJson);
            else console.log('[Mock] 新增一个规则', ruleDataJson);
        },

        /**
         * 系统设置
         */
        GetSystemSettings: async function () {
            if (isWebView2) {
                return await rawBridge.GetSystemSettings();
            } else {
                return JSON.stringify({
                    writeInterval: 300, afkTime: 120, filterTime: 3,
                    autoStart: false, minimizeToTray: true, showNotifications: true
                });
            }
        },

        SaveSystemSettings: async function (settingsDataJson) {
            if (isWebView2) await rawBridge.SaveSystemSettings(settingsDataJson);
            else console.log('[Mock] SaveSystemSettings', settingsDataJson);
        }
    };

    console.log('AppBridge 已初始化，当前环境:', isWebView2 ? 'WebView2' : 'Browser Mock');
})();