// bridge.js
// 封装成全局对象，方便所有页面调用
window.AppBridge = {
    // 获取原始 bridge 对象
    get raw() {
        return window.chrome.webview.hostObjects.bridge;
    },

    // 封装通用方法
    minimize: async () => {
        await window.AppBridge.raw.Minimize();
    },

    close: async () => {
        await window.AppBridge.raw.Close();
    },

    // 也可以写一个通用的执行器
    execute: async (cmd, data = {}) => {
        await window.AppBridge.raw.Execute(cmd, JSON.stringify(data));
    }
};