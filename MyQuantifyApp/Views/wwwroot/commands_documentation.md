# MyQuantify 页面指令说明文档

本文档统计了每个HTML页面与C#后端通过Bridge.js通信的指令（命令）。

## Day.html

### 一次性加载数据指令
- **指令名称**：`getDailyData`
- **功能描述**：获取当日所有活动数据，用于更新仪表盘和图表
- **参数**：无
- **返回数据**：包含各类活动统计数据的对象
- **调用方式**：通过`Bridge.send('getDailyData', null, true)`调用

## Focus.html

### 保存专注会话数据指令
- **指令名称**：`saveFocusSession`
- **功能描述**：保存专注会话的详细数据到C#后端
- **参数**：
  - `taskName`：任务名称
  - `startTime`：开始时间
  - `endTime`：结束时间
  - `focusDuration`：专注时长（分钟）
  - `breakDuration`：休息时长（分钟）
  - `isCompleted`：是否完成
- **返回数据**：无
- **调用方式**：通过`Bridge.send('saveFocusSession', sessionData)`调用

### 加载专注设置指令
- **指令名称**：`loadFocusSettings`
- **功能描述**：从C#后端加载专注设置
- **参数**：无
- **返回数据**：包含专注设置的对象
- **调用方式**：通过`Bridge.send('loadFocusSettings', null, true)`调用

### 保存专注设置指令
- **指令名称**：`saveFocusSettings`
- **功能描述**：保存专注设置到C#后端
- **参数**：包含设置的对象
- **返回数据**：无
- **调用方式**：通过`Bridge.send('saveFocusSettings', settings)`调用

## Keyboard.html

### 加载键盘数据指令
- **指令名称**：`getKeyboardData`
- **功能描述**：获取指定日期的键盘按键数据
- **参数**：
  - `date`：目标日期（格式：YYYY-MM-DD）
- **返回数据**：包含各按键按下次数的对象
- **调用方式**：通过`Bridge.send('getKeyboardData', { date: date }, true)`调用

## LineChart.html

### 一次性加载折线图数据指令
- **指令名称**：`getLineChartData`
- **功能描述**：获取近30天的活动数据，用于绘制各类趋势图
- **参数**：
  - `type`：数据类型（'activity'）
  - `timeRange`：时间范围（'month'）
- **返回数据**：包含近30天数据的数组
- **调用方式**：通过`Bridge.send('getLineChartData', { type: 'activity', timeRange: 'month' }, true)`调用

## PieChart.html

### 一次性加载饼图数据指令
- **指令名称**：`getPieChartData`
- **功能描述**：获取指定天数内的活动和应用时间占比数据
- **参数**：
  - `days`：天数（通常为30）
- **返回数据**：包含每日活动数据的数组
- **调用方式**：通过`Bridge.send('getPieChartData', { days: 30 }, true)`调用

## Windows.html

### 标签管理与进程分类指令

#### 加载标签数据指令
- **指令名称**：`getTagsData`
- **功能描述**：获取所有标签及其包含的进程和窗口数据
- **参数**：无
- **返回数据**：
  - `tags`：标签数组
  - `selectedTag`：当前选中的标签名
- **调用方式**：通过`Bridge.send('getTagsData', null, true)`调用

#### 选择标签指令
- **指令名称**：`selectTag`
- **功能描述**：选择指定标签
- **参数**：
  - `tagName`：标签名称
- **返回数据**：无
- **调用方式**：通过`Bridge.send('selectTag', { tagName })`调用

#### 添加标签指令
- **指令名称**：`addTag`
- **功能描述**：添加新的自定义标签
- **参数**：
  - `tagName`：新标签名称
- **返回数据**：更新后的标签数组
- **调用方式**：通过`Bridge.send('addTag', { tagName }, true)`调用

#### 删除标签指令
- **指令名称**：`deleteTag`
- **功能描述**：删除指定标签
- **参数**：
  - `tagName`：要删除的标签名称
- **返回数据**：更新后的标签数组
- **调用方式**：通过`Bridge.send('deleteTag', { tagName }, true)`调用

#### 更改进程标签指令
- **指令名称**：`changeTag`
- **功能描述**：将进程从一个标签移动到另一个标签
- **参数**：
  - `processName`：进程名称
  - `oldTagName`：原标签名称
  - `newTagName`：新标签名称
- **返回数据**：更新后的标签数组
- **调用方式**：通过`Bridge.send('changeTag', { processName, oldTagName, newTagName }, true)`调用

## Settings.html

根据用户要求，此页面暂不修改，指令保持不变。

## Clipboard.html

根据代码分析，此页面应该使用Bridge.js与C#交互，但详细指令在当前查看的代码中未明确显示。

## 注意事项

1. 所有页面均已移除JavaScript模拟数据，当Bridge通信失败时将显示空白数据
2. 所有页面均使用`Bridge.send(command, params, awaitResponse)`方式与C#通信
3. 第三个参数`awaitResponse`为`true`时，表示需要等待C#返回数据