// history 页面逻辑
var historyView = {
    chart: null,
    currentType: 'line',

    async init() {
        // 1. 初始化日期 (默认为最近一周)
        const end = new Date();
        const start = new Date();
        start.setDate(start.getDate() - 7);
        document.getElementById('startDate').valueAsDate = start;
        document.getElementById('endDate').valueAsDate = end;

        // 2. 初始化 ECharts
        if (typeof echarts === 'undefined') {
            await this.loadEcharts();
        }
        this.chart = echarts.init(document.getElementById('historyMainChart'));

        // 3. 初始加载数据
        this.query();

        // 4. 监听窗口缩放
        window.addEventListener('resize', () => this.chart && this.chart.resize());
    },

    loadEcharts() {
        return new Promise((resolve) => {
            if (typeof echarts !== 'undefined') {
                resolve();
                return;
            }
            const script = document.createElement('script');
            script.src = "../js/echarts.min.js";
            script.onload = resolve;
            document.head.appendChild(script);
        });
    },

    async query() {
        const bridge = window.AppBridge || window.chrome?.webview?.hostObjects?.bridge;
        if (!bridge) return this.renderMockData();

        try {
            const params = {
                start: document.getElementById('startDate').value,
                end: document.getElementById('endDate').value
            };
            const json = await bridge.GetHistoryStats(JSON.stringify(params));
            this.render(JSON.parse(json));
        } catch (e) {
            console.warn("Bridge调用失败，使用演示数据", e);
            this.renderMockData();
        }
    },

    render(data) {
        const themeColor = getComputedStyle(document.documentElement).getPropertyValue('--primary-color').trim() || '#4e73df';

        const option = {
            backgroundColor: 'transparent',
            tooltip: {
                trigger: 'axis',
                backgroundColor: 'rgba(255, 255, 255, 0.9)',
                borderRadius: 12,
                borderWidth: 0,
                shadowBlur: 10,
                shadowColor: 'rgba(0,0,0,0.1)'
            },
            grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
            xAxis: {
                type: 'category',
                data: data.dates,
                axisLine: { lineStyle: { color: 'rgba(0,0,0,0.1)' } },
                axisLabel: { color: 'rgba(0,0,0,0.4)', fontSize: 11 }
            },
            yAxis: {
                type: 'value',
                splitLine: { lineStyle: { type: 'dashed', color: 'rgba(0,0,0,0.05)' } },
                axisLabel: { color: 'rgba(0,0,0,0.4)' }
            },
            series: [{
                name: '击键数',
                data: data.values,
                type: this.currentType,
                smooth: true,
                symbolSize: 8,
                itemStyle: { color: themeColor },
                areaStyle: this.currentType === 'line' ? {
                    color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                        { offset: 0, color: themeColor + '66' },
                        { offset: 1, color: themeColor + '00' }
                    ])
                } : null
            }]
        };
        this.chart.setOption(option);

        // 更新指标文字
        document.getElementById('statKeystrokes').innerText = data.totalKeystrokes.toLocaleString();
        document.getElementById('statCopies').innerText = data.totalCopies.toLocaleString();
        document.getElementById('statFocusTime').innerText = data.totalHours + 'h';
    },

    switchType(type) {
        this.currentType = type;
        document.getElementById('btnLine').classList.toggle('active', type === 'line');
        document.getElementById('btnBar').classList.toggle('active', type === 'bar');
        this.query();
    },

    setQuickDate(days) {
        const end = new Date();
        const start = new Date();
        start.setDate(start.getDate() - days);
        document.getElementById('startDate').valueAsDate = start;
        document.getElementById('endDate').valueAsDate = end;
        this.query();
    },

    renderMockData() {
        const mock = {
            dates: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            values: [4200, 5800, 7200, 6100, 8900, 3200, 2100],
            totalKeystrokes: 37500,
            totalCopies: 452,
            totalHours: 42.5
        };
        this.render(mock);
    }
};

// 启动初始化
historyView.init();
