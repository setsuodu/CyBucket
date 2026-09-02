# Cygames_MessagePipe（仅 Scripts）

MessagePipe 示例代码，只包含 Scripts 文件夹。

## 文件说明

| 文件 | 作用 |
|------|------|
| Messages.cs | 消息定义（struct） |
| MessagePipeBootstrap.cs | BuiltinContainer 初始化 + RequestHandler 注册 |
| MessagePipeDemo.cs | Pub/Sub + Request-Response 演示 |

## 使用步骤

1. 项目已通过 OpenUPM / NuGet 安装 MessagePipe
2. 场景中创建空物体，挂 `MessagePipeBootstrap`（先执行）
3. 再挂 `MessagePipeDemo`（可绑 UI Text / Button）
4. 运行后按键测试：
   - **D** 造成伤害
   - **K** 死亡
   - **S** 加分
   - **R** 异步请求状态

## 核心 API 片段

```csharp
// 发布
GlobalMessagePipe.GetPublisher<PlayerDamagedMessage>().Publish(msg);

// 订阅
var d = GlobalMessagePipe.GetSubscriber<PlayerDamagedMessage>()
    .Subscribe(OnDamaged);
// 记得 Dispose

// 请求-响应
var handler = GlobalMessagePipe.GetAsyncRequestHandler<Req, Res>();
var res = await handler.InvokeAsync(req, ct);
```

官方：https://github.com/Cysharp/MessagePipe
