// today 页面逻辑
(function() {
    'use strict';

    // 初始化函数
    function init() {
        loadTodayData();
        renderActivityMap();
        renderCategoryRank();
        updateActiveProcess();
    }

    // 加载今日数据
    async function loadTodayData() {
        const bridge = window.AppBridge || window.chrome?.webview?.hostObjects?.bridge;
        if (!bridge) {
            console.warn('Bridge not available, using mock data');
            return;
        }

        try {
            // 调用后端接口获取数据
            // const data = await bridge.GetTodayStats();
            // updateStats(data);
        } catch (e) {
            console.error('Failed to load today data:', e);
        }
    }

    // 渲染活动热力图
    function renderActivityMap() {
        // 这里可以根据实际数据渲染 24 小时的活动块
        // 目前保留静态 HTML
    }

    // 渲染分类排行
    function renderCategoryRank() {
        // 这里可以根据实际数据渲染排行列表
        // 目前保留静态 HTML
    }

    // 更新当前活动进程
    function updateActiveProcess() {
        // 定期更新当前正在运行的进程信息
        setInterval(() => {
            // 调用后端获取当前进程
        }, 5000);
    }

    // 页面加载时初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
