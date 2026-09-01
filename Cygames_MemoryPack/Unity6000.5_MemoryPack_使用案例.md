# Unity 6000.5 × Cysharp MemoryPack：Roguelite 检查点存档案例

**作者：Manus AI**  
**适用范围：Unity 6000.5、IL2CPP 或 Mono 构建、单机本地检查点存档**

## 案例目标

本文实现一个可落地的 **Roguelite 单人游戏检查点存档**。玩家在地牢内抵达存档点、手动点击保存或应用挂起时，系统将场景标识、角色位置与朝向、生命值、金币、装备、背包和已击败敌人等状态写入 `Application.persistentDataPath`。重新进入相同场景后，系统读取二进制文件并将快照恢复到当前已初始化的游戏对象。

该场景适合 MemoryPack，原因在于数据以数值、短字符串、列表和 `Vector3`、`Quaternion` 等值类型为主；它不需要 JSON 的可读性，却希望获得紧凑数据及较低的序列化开销。MemoryPack 是 Cysharp 提供的面向 C# 与 Unity 的二进制序列化器，使用源生成器生成序列化代码；官方 README 将 Unity 2022.3.12f1 列为最低支持版本，因此 Unity 6000.5 在版本范围内。[1]

> **边界定义：** 本方案保存的是“可重建的游戏状态”，不是完整的 Unity 场景快照。`GameObject`、`MonoBehaviour`、`Transform`、`ScriptableObject`、纹理、Prefab 和其他 `UnityEngine.Object` 均不直接序列化。

## 为什么使用 MemoryPack

MemoryPack 要求待序列化类型使用 `[MemoryPackable]` 标记并声明为 `partial`；其生成器会产生实现，运行时通过 `MemoryPackSerializer.Serialize` 与 `MemoryPackSerializer.Deserialize` 完成二进制往返。[1] 这意味着项目不需要依赖运行时反射来发现字段，对 IL2CPP 的部署路径更明确。

| 维度 | 本案例中的选择 | 理由 |
| --- | --- | --- |
| 存档格式 | MemoryPack 二进制 | 适合高频、结构化的本地游戏状态，不以人工阅读为目标。 |
| DTO 模式 | `GenerateType.VersionTolerant` | 旧存档需由后续版本读取，接受少量速度与体积开销以获得双向字段兼容能力。 |
| Unity 类型 | `Vector3`、`Quaternion` | 它们是非托管值类型；MemoryPack.Unity 提供 Unity 内建类型支持。[1] |
| I/O 策略 | 临时文件后提交 | 避免直接写正式文件；降低保存中断造成正式存档损坏的风险。 |
| 线程边界 | 主线程抓取/应用，后台写文件 | 不在后台访问 Unity API，只将纯 DTO 交给文件服务。 |
| 兼容策略 | 永久固定 `[MemoryPackOrder]` | 在版本容忍模式下，字段 ID 不能被重排或复用。[1] |

## 安装与工程结构

请在 Unity 菜单中打开 **Window → Package Manager**，点击左上角 **+**，选择 **Add package from git URL…**，填入以下官方 UPM 地址。若需要可复现构建，应在地址末尾追加已验证的发布标签。当前示例固定到 `1.21.4`；如果你的项目已经验证其他版本，只需同步修改 URL 末尾的标签。官方包的 `package.json` 包名是 `com.cysharp.memorypack`，Unity 最低声明为 `2022.3` / `2022.3.12f1`。[1]

```text
https://github.com/Cysharp/MemoryPack.git?path=src/MemoryPack.Unity/Assets/MemoryPack.Unity#1.21.4
```

更适合团队协作的做法是直接编辑项目根目录下的 `Packages/manifest.json`，将下面这一项合并到现有的 `dependencies` 对象中。**不要把这段 JSON 当成完整 manifest 覆盖已有文件**，因为现有项目通常还包含 Unity 内置包依赖。

```json
{
  "dependencies": {
    "com.cysharp.memorypack": "https://github.com/Cysharp/MemoryPack.git?path=src/MemoryPack.Unity/Assets/MemoryPack.Unity#1.21.4"
  }
}
```

如果原文件已有其他包，正确结果类似下面这样；逗号必须根据相邻条目调整，最后一个条目不能多写尾逗号：

```json
{
  "dependencies": {
    "com.unity.collab-proxy": "2.4.3",
    "com.unity.textmeshpro": "3.0.6",
    "com.cysharp.memorypack": "https://github.com/Cysharp/MemoryPack.git?path=src/MemoryPack.Unity/Assets/MemoryPack.Unity#1.21.4"
  }
}
```

Unity 官方规定，Git 依赖应声明在项目的 `Packages/manifest.json`，而不是包自身的 `package.json`；URL 可以使用仓库子目录的 `?path=` 扩展语法，并用 `#tag`、分支或 commit hash 固定 revision。[2] 因此，添加后应把 `Packages/manifest.json` 和 Unity 自动生成或更新的 `Packages/packages-lock.json` 一起提交到版本库。若不锁定 revision，团队成员可能在不同时间解析到默认分支的不同提交。

MemoryPack 的 Unity 集成依赖编译期代码生成。Unity 官方说明，源生成器作为脚本编译过程的额外步骤运行；对自建生成器而言，需要 Roslyn 4.3 与严格的 `RoslynAnalyzer` 标签配置。[3] 对本案例而言，应优先直接使用上述官方包，而不是自己复制或手动配置 `MemoryPack.Generator.dll`。

建议将附件中的四个脚本放入如下目录。若项目用了 Assembly Definition（`.asmdef`），请让该程序集能引用 MemoryPack 相关程序集；若 Package Manager 导入后出现生成器或程序集错误，先检查 Console 的首个错误和程序集引用边界，不要通过关闭错误来掩盖问题。

```text
Assets/
└── Scripts/
    └── Roguelite/
        └── SaveSystem/
            ├── GameSaveModels.cs
            ├── MemoryPackSaveStore.cs
            ├── SaveCoordinator.cs
            └── EditorTests/
                └── MemoryPackSaveRoundTripTests.cs
```

## 数据契约与版本规则

本案例用 `GameSaveData` 作为顶层契约。它只包含可持久化 DTO，并将玩家、背包等复杂状态拆分为独立的 `partial` 类。`GameSaveModels.cs` 中所有模型都采用 `GenerateType.VersionTolerant`，每一个持久化成员均有显式 `[MemoryPackOrder(n)]`。

MemoryPack 默认对象模式按照成员**声明顺序**写入值而不写入成员名；默认的有限演进仅允许追加成员，不能删除、调序或改类型。[1] `GenerateType.VersionTolerant` 可支持保留编号的字段删除和新增，但代价是序列化速度略慢、负载略大。[1] 对游戏存档来说，这通常是合理的工程权衡。

| 演进操作 | 是否允许 | 本案例的处理方式 |
| --- | --- | --- |
| 新增 `TutorialCompleted` | 允许 | 使用新编号 `7`；读取 V1 存档时得到 `false`。 |
| 删除旧字段 | 允许，但不能复用编号 | 删除成员后永久保留对应编号空洞。 |
| 重命名字段 | 允许 | 不改变 `MemoryPackOrder` 与类型。 |
| 调整声明顺序 | 不允许 | 不移动既有字段，并保持显式编号。 |
| `int` 改为 `long` | 不允许 | 新增新编号字段，加载后做数据迁移。 |
| 改变字段业务含义 | 不建议 | 将其视作新字段，保留旧字段用于迁移。 |

例如，版本 2 增加教程完成状态时，不能把旧字段改号，也不能复用被删除的字段编号：

```csharp
// V1 已占用 0~6。
[MemoryPackOrder(7)] public bool TutorialCompleted { get; set; }
```

## 场景接入步骤

首先，在待保存场景中创建一个名为 `SaveSystem` 的空对象，并添加 `SaveCoordinator` 组件。再将角色对象的 `PlayerSaveTarget` 与保存系统对象或背包对象的 `InventorySaveTarget` 拖入 `SaveCoordinator` 对应 Inspector 字段。示例 `PlayerSaveTarget` 仅作为可运行的演示接口；真实项目应将 `Capture` 与 `Apply` 的逻辑适配至已有角色属性、背包领域模型和敌人生命周期系统。

在 UI 中创建两个按钮，分别在 **On Click()** 里绑定 `SaveCoordinator.SaveCheckpoint` 和 `SaveCoordinator.LoadCheckpointInCurrentScene`。运行时，`SaveCheckpoint` 在主线程从 `Transform`、背包组件和已击败敌人集合构建新的 `GameSaveData`，随后把该纯数据交给 `MemoryPackSaveStore`。存储服务会序列化为 `byte[]`，先写 `checkpoint.mpack.tmp`，再替换正式的 `checkpoint.mpack`。

加载按钮在**目标场景已加载、角色和背包已完成初始化后**调用。它会读取和反序列化文件，检查存档魔数 `ROGUELITE_SAVE` 以及当前场景名，然后才在主线程调用 `player.Apply` 与 `inventory.Apply`。若产品要支持跨场景“继续游戏”，推荐另设启动加载流程：先仅读出 `SceneId`，由场景管理器加载目标场景，待场景中的依赖对象就绪后再应用 `GameSaveData`。不要在后台线程读取或写入 `Transform`、`SceneManager`、UI 和其他 Unity API。

| 文件 | 角色 | 关键内容 |
| --- | --- | --- |
| `GameSaveModels.cs` | 稳定的二进制契约 | DTO、显式字段编号、`Vector3`/`Quaternion` 状态。 |
| `MemoryPackSaveStore.cs` | 存储适配器 | 序列化、反序列化、串行访问、临时文件、异常降级。 |
| `SaveCoordinator.cs` | Unity 主线程协调器 | 运行时状态捕获、快照恢复、UI 可绑定方法。 |
| `MemoryPackSaveRoundTripTests.cs` | 回归测试 | 验证二进制往返及 V1 到 V2 的字段缺失默认值。 |

## 验证方案

通过 Unity Test Framework 在 EditMode 运行 `MemoryPackSaveRoundTripTests`。首个测试对 `Vector3`、背包列表与布尔字段进行二进制往返验证；第二个测试用没有字段 `7` 的 V1 夹具序列化，并由 V2 类型读取，确认新增字段仍使用 `false` 默认值。实际产品应将每个已发布版本的二进制夹具保存到测试资源目录，并在 CI 中持续读取它们。这样，成员调序、字段改类型或错误复用编号会尽早暴露。

| 验证级别 | 操作 | 通过标准 |
| --- | --- | --- |
| 编辑器单元测试 | 运行附件中的两个测试 | DTO 往返后关键字段一致，V1 夹具可由 V2 读取。 |
| Editor 手工测试 | 修改角色位置、背包数量和敌人状态后保存、退出 Play Mode 再加载 | Console 显示完成日志；状态恢复符合预期。 |
| 真机测试 | Android/iOS/目标主机进行保存、强制退出、重启、加载 | 文件写入路径可用，损坏文件不会导致启动崩溃。 |
| IL2CPP 测试 | 以目标平台和 Release 配置构建 | 无代码生成、裁剪或程序集错误；存档可读。 |
| 发布回归 | 用历史二进制夹具升级到当前客户端 | 不丢失仍受支持的数据，迁移逻辑符合产品规则。 |

此交付未在本沙箱中启动 Unity 编辑器或执行 IL2CPP 构建；因此，最终合并前仍需在项目目标 Unity 6000.5 版本与目标平台上运行上述测试。

## 限制、安全性与产品化建议

当前 MemoryPack Unity 版本有值得提前规避的边界。官方 README 指出 Unity 版本目前不支持 `CustomFormatter`；若共享 .NET 7+ 与 Unity 的二进制数据，对于显式 `StructLayout(LayoutKind.Auto)` 的值类型不能保证互通，`DateTimeOffset` 与 `ValueTuple` 是常见受影响类型。[1] 因此，本地存档示例仅把时间持久化为 `long SavedAtUnixMilliseconds`，而不是直接保存 `DateTimeOffset`；如需客户端与 .NET 服务端共用协议，也应避免将这些易受影响类型放进 wire schema。

MemoryPack 是高效的二进制序列化格式，**不是加密、认证或反作弊方案**。单机存档如果需要防止简单篡改，可在写入后附加 HMAC 完整性校验，并使用平台安全存储保护密钥；若对抗的是拥有设备控制权的恶意用户，则客户端密钥无法提供绝对保护，关键经济状态应由可信服务端裁决。对于云存档，应以玩家 ID、存档槽、应用存档版本和服务端迁移规则构成独立协议，不要把本地文件格式不加验证地直接当作网络 API。

最后，MemoryPack 的性能收益应使用本项目的真实存档数据、目标 CPU、目标平台和构建后 IL2CPP 包进行测量。官方性能图表来自特定测试环境，不能替代你的包体大小、GC 分配和加载卡顿基准。[1]

## 参考资料

[1] [Cysharp，*MemoryPack* 官方仓库与 README](https://github.com/Cysharp/MemoryPack)；[MemoryPack.Unity/package.json](https://raw.githubusercontent.com/Cysharp/MemoryPack/main/src/MemoryPack.Unity/Assets/MemoryPack.Unity/package.json)  
[2] [Unity Technologies，*Introduction to Git dependencies*，Unity 6.0 Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html)  
[3] [Unity Technologies，*Create and use a source generator*，Unity 6.1 Manual](https://docs.unity3d.com/6000.1/Documentation/Manual/create-source-generator.html)

