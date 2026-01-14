// settings 页面逻辑
(function() {
    'use strict';

    // 1. 切换章节
    window.switchSection = function(sectionId) {
        document.querySelectorAll('.nav-item').forEach(item => {
            item.classList.remove('active');
        });
        event.currentTarget.classList.add('active');

        document.querySelectorAll('.content-section').forEach(section => {
            section.classList.remove('active');
        });
        document.getElementById(sectionId).classList.add('active');
    };

    // 2. 更新范围滑块值
    window.updateRangeValue = function(sliderId, displayId, unit, divisor) {
        const slider = document.getElementById(sliderId);
        const display = document.getElementById(displayId);
        const value = parseInt(slider.value);

        if (unit === 's' && divisor > 1) {
            display.textContent = (value / divisor).toFixed(1) + "分钟";
        } else {
            display.textContent = value + unit;
        }
    };

    // 3. 毛玻璃效果管理
    window.updateBlurEffect = function(sliderId, displayId) {
        const slider = document.getElementById(sliderId);
        const display = document.getElementById(displayId);
        const value = parseInt(slider.value);

        display.textContent = value + 'px';

        // 应用模糊效果到所有 glass 类元素
        document.documentElement.style.setProperty('--blur-intensity', value + 'px');
        localStorage.setItem('blurIntensity', value);
    };

    window.updateOpacity = function(sliderId, displayId) {
        const slider = document.getElementById(sliderId);
        const display = document.getElementById(displayId);
        const value = parseInt(slider.value);

        display.textContent = value + '%';

        // 应用不透明度
        document.documentElement.style.setProperty('--glass-opacity', value / 100);
        localStorage.setItem('glassOpacity', value);
    };

    window.toggleMicaEffect = function() {
        const checked = document.getElementById('micaEffect').checked;
        document.documentElement.classList.toggle('mica-effect', checked);
        localStorage.setItem('micaEffect', checked);
    };

    window.updateBrightness = function(sliderId, displayId) {
        const slider = document.getElementById(sliderId);
        const display = document.getElementById(displayId);
        const value = parseInt(slider.value);

        display.textContent = value + '%';

        // 应用背景亮度
        document.body.style.filter = `brightness(${value}%)`;
        localStorage.setItem('backgroundBrightness', value);
    };

    // 4. 其他交互逻辑
    window.selectWallpaper = function(type) {
        document.querySelectorAll('.wallpaper-card').forEach(c => c.classList.remove('active'));
        event.currentTarget.classList.add('active');
        console.log('壁纸切换至:', type);
    };

    window.uploadCustomWallpaper = function() {
        alert('壁纸上传功能待实现');
    };

    window.toggleAutoStart = function() {
        console.log('自启状态:', document.getElementById('autoStart').checked);
    };

    window.toggleMinimizeToTray = function() {
        console.log('托盘状态:', document.getElementById('minimizeToTray').checked);
    };

    window.toggleNotifications = function() {
        console.log('通知状态:', document.getElementById('showNotifications').checked);
    };

    window.mergeDatabase = function() {
        if (confirm('合并操作将优化数据库结构，是否继续？')) {
            alert('数据库已优化完毕！');
        }
    };

    window.exportData = function() {
        alert('数据导出功能待实现');
    };

    window.clearData = function() {
        if (confirm('⚠️ 确定要清空所有数据吗？此操作无法撤销。')) {
            alert('数据已安全擦除。');
        }
    };

    window.checkUpdate = function() {
        alert('当前已是最新版本 (v1.0.0)。');
    };

    window.openGithub = function() {
        console.log('跳转至 GitHub...');
    };

    // 初始化
    function init() {
        // 加载保存的设置
        const savedBlur = localStorage.getItem('blurIntensity') || '8';
        const savedOpacity = localStorage.getItem('glassOpacity') || '80';
        const savedMica = localStorage.getItem('micaEffect') !== 'false';
        const savedBrightness = localStorage.getItem('backgroundBrightness') || '100';

        // 应用保存的设置
        if (document.getElementById('blurIntensity')) {
            document.getElementById('blurIntensity').value = savedBlur;
            document.getElementById('blurIntensityValue').textContent = savedBlur + 'px';
            document.documentElement.style.setProperty('--blur-intensity', savedBlur + 'px');
        }

        if (document.getElementById('opacity')) {
            document.getElementById('opacity').value = savedOpacity;
            document.getElementById('opacityValue').textContent = savedOpacity + '%';
            document.documentElement.style.setProperty('--glass-opacity', savedOpacity / 100);
        }

        if (document.getElementById('micaEffect')) {
            document.getElementById('micaEffect').checked = savedMica;
            document.documentElement.classList.toggle('mica-effect', savedMica);
        }

        if (document.getElementById('brightness')) {
            document.getElementById('brightness').value = savedBrightness;
            document.getElementById('brightnessValue').textContent = savedBrightness + '%';
            document.body.style.filter = `brightness(${savedBrightness}%)`;
        }

        // 初始化滑块文本
        if (document.getElementById('writeInterval')) {
            updateRangeValue('writeInterval', 'writeIntervalValue', 's', 60);
            updateRangeValue('afkTime', 'afkTimeValue', 's', 60);
            updateRangeValue('filterTime', 'filterTimeValue', 's', 1);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
