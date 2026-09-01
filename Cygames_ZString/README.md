# Cygames_ZString

ZString（Cysharp）零分配字符串构建示例。

## 安装

1. 打开本工程
2. 使用 **NugetForUnity** 安装 `ZString`（推荐 2.6.0+）
3. 将 `ZStringDemo` 脚本挂到任意 GameObject，可选绑定 UI Text / Button

## 功能演示

| 快捷键 | 功能 |
|--------|------|
| C | ZString.Concat |
| F | ZString.Format |
| B | CreateStringBuilder |
| P | 简单性能对比（string+ / StringBuilder / ZString） |

## 核心用法

```csharp
// Concat
string s = ZString.Concat("x:", x, " y:", y);

// Format（无 boxing）
string s = ZString.Format("Hp:{0:F1}", hp);

// Builder（must using）
using (var sb = ZString.CreateStringBuilder())
{
    sb.Append("Hello ");
    sb.Append(name);
    string result = sb.ToString();
}
```

官方仓库：https://github.com/Cysharp/ZString
