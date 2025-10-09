# MyQuantify REST API 文档

## 概述
本文档描述了MyQuantify应用中WebView2与C#后端之间的REST API接口规范。这些API用于替代原有的`common-js-bridge.js`通信机制。

## 基础URL
```
http://localhost:5000/api/
```

## API 端点列表

### 1. 图表数据 API

#### 1.1 获取折线图数据
**请求**: `GET /charts/line?dateRange={range}`

**参数**: 
- `dateRange`: 可选，日期范围，支持 'today', '7day', '30day'

**响应**: 
```json
{
  "success": true,
  "data": [
    {
      "date": "2023-05-01",
      "work": 5.2,
      "game": 2.3,
      "afk": 1.5,
      "total": 9.0
    },
    // 更多日期数据
  ]
}
```

#### 1.2 获取饼图数据
**请求**: `GET /charts/pie?dateRange={range}&type={type}`

**参数**: 
- `dateRange`: 可选，日期范围，支持 'today', '7day', '30day'
- `type`: 必需，图表类型，支持 'activity', 'app'

**响应**: 
```json
{
  "success": true,
  "data": [
    { "name": "工作", "value": 5.2 },
    { "name": "游戏", "value": 2.3 },
    { "name": "AFK", "value": 1.5 },
    { "name": "其他/学习", "value": 0.0 }
  ]
}
```

### 2. 设置 API

#### 2.1 获取设置
**请求**: `GET /settings`

**响应**: 
```json
{
  "success": true,
  "data": {
    "language": "zh-CN",
    "afkDetectionTime": 30,
    "windowIgnoreTime": 5,
    "startOnBoot": false,
    "clipboardMonitor": true,
    "keyboardMonitor": true,
    "windowMonitor": true,
    "backgroundImage": "",
    "themeColor": "#3498db"
  }
}
```

#### 2.2 保存设置
**请求**: `POST /settings`

**请求体**: 
```json
{
  "language": "zh-CN",
  "afkDetectionTime": 30,
  "windowIgnoreTime": 5,
  "startOnBoot": false,
  "clipboardMonitor": true,
  "keyboardMonitor": true,
  "windowMonitor": true,
  "backgroundImage": "",
  "themeColor": "#3498db"
}
```

**响应**: 
```json
{
  "success": true,
  "message": "设置已成功保存"
}
```

### 3. 数据管理 API

#### 3.1 清空数据
**请求**: `DELETE /data?type={type}&timeRange={range}`

**参数**: 
- `type`: 必需，数据类型，支持 'clipboard', 'keyboard', 'window', 'all'
- `timeRange`: 必需，时间范围，支持 'all', 'today', 'week', 'month', 'custom'

**响应**: 
```json
{
  "success": true,
  "message": "数据已清空"
}
```

### 4. 剪贴板 API

#### 4.1 查询剪贴板历史
**请求**: `GET /clipboard?startDate={start}&endDate={end}`

**参数**: 
- `startDate`: 必需，开始日期，格式 YYYY-MM-DD
- `endDate`: 必需，结束日期，格式 YYYY-MM-DD

**响应**: 
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "date": "2023-05-01 10:30:45",
      "content": "示例文本",
      "length": 4
    },
    // 更多剪贴板条目
  ]
}
```

#### 4.2 获取剪贴板全文内容
**请求**: `GET /clipboard/{id}/full`

**参数**: 
- `id`: 必需，剪贴板条目ID

**响应**: 
```json
{
  "success": true,
  "data": "完整的剪贴板文本内容..."
}
```

### 5. 进程管理 API

#### 5.1 获取标签和进程数据
**请求**: `GET /windows/tags`

**响应**: 
```json
{
  "success": true,
  "data": {
    "tags": [
      {
        "name": "开发",
        "processes": [
          {
            "name": "devenv.exe",
            "windows": ["Visual Studio - MyProject.sln"]
          }
        ]
      }
    ],
    "selectedTag": "开发"
  }
}
```

#### 5.2 添加标签
**请求**: `POST /windows/tags`

**请求体**: 
```json
{
  "newTagName": "新标签"
}
```

**响应**: 
```json
{
  "success": true,
  "message": "标签添加成功"
}
```

#### 5.3 删除标签
**请求**: `DELETE /windows/tags/{name}`

**参数**: 
- `name`: 必需，标签名称

**响应**: 
```json
{
  "success": true,
  "message": "标签删除成功"
}
```

#### 5.4 选择标签
**请求**: `GET /windows/tags/{name}`

**参数**: 
- `name`: 必需，标签名称

**响应**: 
```json
{
  "success": true,
  "message": "标签选择成功"
}
```

#### 5.5 修改进程标签
**请求**: `PUT /windows/tags/change`

**请求体**: 
```json
{
  "processName": "devenv.exe",
  "oldTagName": "旧标签",
  "newTagName": "新标签"
}
```

**响应**: 
```json
{
  "success": true,
  "message": "进程标签已更新"
}
```

### 6. 每日数据 API

#### 6.1 获取每日仪表板数据
**请求**: `GET /dashboard/daily`

**响应**: 
```json
{
  "success": true,
  "data": {
    "totalActiveTime": 8.5,
    "productivityScore": 75,
    "topApplications": [
      { "name": "VSCode", "time": 3.2 },
      { "name": "Chrome", "time": 2.5 }
    ],
    "activityDistribution": {
      "work": 5.2,
      "game": 1.3,
      "afk": 2.0
    }
  }
}
```

### 7. 专注模式 API

#### 7.1 开始专注计时器
**请求**: `POST /focus/start`

**请求体**: 
```json
{
  "duration": 25,
  "taskName": "编码任务"
}
```

**响应**: 
```json
{
  "success": true,
  "message": "专注计时器已开始"
}
```

#### 7.2 暂停专注计时器
**请求**: `POST /focus/pause`

**响应**: 
```json
{
  "success": true,
  "message": "专注计时器已暂停"
}
```

#### 7.3 重置专注计时器
**请求**: `POST /focus/reset`

**响应**: 
```json
{
  "success": true,
  "message": "专注计时器已重置"
}
```

#### 7.4 更新专注设置
**请求**: `PUT /focus/settings`

**请求体**: 
```json
{
  "focusDuration": 25,
  "shortBreakDuration": 5,
  "longBreakDuration": 15,
  "autoStartBreak": true
}
```

**响应**: 
```json
{
  "success": true,
  "message": "专注设置已更新"
}
```

#### 7.5 设置专注任务
**请求**: `POST /focus/task`

**请求体**: 
```json
{
  "taskName": "学习新技能"
}
```

**响应**: 
```json
{
  "success": true,
  "message": "专注任务已设置"
}
```

### 8. 键盘热力图 API

#### 8.1 获取键盘数据
**请求**: `GET /keyboard/data?startDate={start}&endDate={end}`

**参数**: 
- `startDate`: 必需，开始日期，格式 YYYY-MM-DD
- `endDate`: 必需，结束日期，格式 YYYY-MM-DD

**响应**: 
```json
{
  "success": true,
  "data": {
    "keyCounts": {
      "A": 123,
      "B": 45,
      "C": 67,
      // 更多按键数据
    },
    "totalKeystrokes": 1500
  }
}
```

## 错误处理
所有API端点在出错时返回统一的错误格式：

```json
{
  "success": false,
  "error": "错误信息描述",
  "code": 400 // HTTP状态码
}
```

## 注意事项
1. 所有日期参数格式应为 `YYYY-MM-DD`
2. 所有时间值单位为小时，保留一位小数
3. 所有请求和响应使用UTF-8编码
4. 实际实现中，C#后端需要配置跨域策略以允许WebView2访问
5. 建议为所有API添加适当的错误处理和日志记录