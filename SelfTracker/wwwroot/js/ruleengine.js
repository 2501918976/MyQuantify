// ruleengine 页面逻辑
var ruleEngine = {
    dragData: null,
    categories: [],

    async call(method, param = null) {
        const bridge = window.AppBridge || window.chrome?.webview?.hostObjects?.bridge;
        if (!bridge) return console.error("Bridge not found");
        return param ? await bridge[method](JSON.stringify(param)) : await bridge[method]();
    },

    async init() {
        await this.refreshAll();
    },

    async refreshAll() {
        await this.loadCategories();
        await this.loadRules();
        await this.reloadCapture();
    },

    async loadCategories() {
        try {
            const res = await this.call("获取所有分类");
            this.categories = JSON.parse(res);
            const container = document.getElementById("catManagerList");
            container.innerHTML = this.categories.map(cat => `
                <div style="display:flex; gap:8px; margin-bottom:8px">
                    <input class="glass-bordered" style="flex:1; padding:4px" value="${cat.CategoryName}"
                           onchange="ruleEngine.updateCategory(${cat.Id}, this.value)">
                    <button class="btn-del" onclick="ruleEngine.deleteCategory(${cat.Id})">×</button>
                </div>
            `).join("");
        } catch (e) {
            console.warn("Failed to load categories:", e);
        }
    },

    async loadRules() {
        const list = document.getElementById("ruleList");
        list.innerHTML = "";
        let anyRules = false;

        this.categories.forEach(cat => {
            (cat.CategoryRules || []).forEach(rule => {
                anyRules = true;
                this.appendRow(rule);
            });
        });

        if (!anyRules) {
            list.innerHTML = '<div style="text-align:center; padding:40px; opacity:0.3">拖拽左侧进程到此处创建规则</div>';
        }
    },

    appendRow(rule) {
        const row = document.createElement("div");
        row.className = "rule-row";
        row.innerHTML = `
            <input value="${rule.ProcessName}" onchange="ruleEngine.saveRule(${rule.Id}, this)">
            <select onchange="ruleEngine.saveRule(${rule.Id}, this)">
                <option value="0" ${rule.LogicType === 0 ? 'selected' : ''}>AND</option>
                <option value="1" ${rule.LogicType === 1 ? 'selected' : ''}>OR</option>
            </select>
            <input value="${rule.TitleMatchValue || ''}" placeholder="窗口标题关键词..." onchange="ruleEngine.saveRule(${rule.Id}, this)">
            <select onchange="ruleEngine.saveRule(${rule.Id}, this)">
                <option value="1" ${rule.TitleMatchType === 1 ? 'selected' : ''}>包含</option>
                <option value="0" ${rule.TitleMatchType === 0 ? 'selected' : ''}>全匹配</option>
                <option value="2" ${rule.TitleMatchType === 2 ? 'selected' : ''}>正则</option>
            </select>
            <select onchange="ruleEngine.saveRule(${rule.Id}, this)">
                ${this.categories.map(c => `<option value="${c.Id}" ${c.Id === rule.CategoryId ? 'selected' : ''}>${c.CategoryName}</option>`).join('')}
            </select>
            <button class="btn-del" onclick="ruleEngine.deleteRule(${rule.Id})">×</button>
        `;
        document.getElementById("ruleList").appendChild(row);
    },

    async reloadCapture() {
        try {
            const res = await this.call("获取未分类的活动");
            const items = JSON.parse(res);
            const container = document.getElementById("pendingList");
            container.innerHTML = items.map(item => `
                <div class="pending-item" draggable="true"
                     ondragstart="ruleEngine.onDragStart(event, '${item.ProcessName}', '${(item.WindowTitle || "").replace(/'/g, "\\'")}')">
                    <div style="font-weight:bold; font-size:13px">${item.ProcessName}</div>
                    <div style="font-size:11px; opacity:0.6; white-space:nowrap; overflow:hidden; text-overflow:ellipsis">${item.WindowTitle || '无标题窗口'}</div>
                </div>
            `).join("");
        } catch (e) {
            console.warn("Failed to load capture:", e);
        }
    },

    // 交互逻辑
    onDragStart(e, proc, title) {
        this.dragData = { proc, title };
    },

    onDragOver(e) {
        e.preventDefault();
        document.getElementById("ruleList").classList.add("drag-over");
    },

    onDragLeave() {
        document.getElementById("ruleList").classList.remove("drag-over");
    },

    async onDrop(e) {
        e.preventDefault();
        this.onDragLeave();
        if (!this.dragData || this.categories.length === 0) return;

        await this.call("新增一个规则", {
            ProcessName: this.dragData.proc,
            TitleMatchValue: this.dragData.title,
            CategoryId: this.categories[0].Id,
            LogicType: 0,
            TitleMatchType: 1,
            IsEnabled: true
        });
        this.refreshAll();
    },

    async saveRule(id, el) {
        const row = el.closest(".rule-row");
        const inputs = row.querySelectorAll("input");
        const selects = row.querySelectorAll("select");
        await this.call("修改一个规则", {
            Id: id,
            ProcessName: inputs[0].value,
            LogicType: Number(selects[0].value),
            TitleMatchValue: inputs[1].value,
            TitleMatchType: Number(selects[1].value),
            CategoryId: Number(selects[2].value),
            IsEnabled: true
        });
    },

    async deleteRule(id) {
        if (confirm("确定删除该规则吗？")) {
            await this.call("删除一个规则", { Id: id });
            this.refreshAll();
        }
    },

    openTagModal() {
        document.getElementById("catModal").classList.add('show');
    },

    closeTagModal() {
        document.getElementById("catModal").classList.remove('show');
    },

    async addNewTag() {
        await this.call("新增一个分类", { CategoryName: "新标签", ColorCode: "#4e73df" });
        this.loadCategories();
    },

    async updateCategory(id, name) {
        await this.call("新增修改分类", { Id: id, CategoryName: name });
        this.loadCategories();
    }
};

// 执行初始化
ruleEngine.init();
