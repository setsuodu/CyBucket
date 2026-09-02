# CyClient — Cysharp All-in-One 客户端底座

开箱即用骨架：**VContainer + MessagePipe + UniTask + MemoryPack + LiteNetLib + AssetBundleFramework + UI Toolkit**

## 目录

```
Assets/Scripts/
  Core/       AppLifetimeScope, AppEntryPoint, UIService, GameSession, GameConstants
  Network/    NetworkService, MinimalGameServer, NetPackets (MemoryPack)
  UI/         LoginPresenter, HomePresenter, UIDocumentBinder, MatchBootstrapInstaller
  Gameplay/   PlayerController, RemotePlayerView, MatchBootstrap
  Messages/   AppMessages
Assets/StreamingAssets/ab_config.json
Assets/packages.config   ← NugetForUnity: MemoryPack
Packages/manifest.json   ← OpenUPM 依赖
```

## OpenUPM 依赖

- com.cysharp.unitask
- com.cysharp.messagepipe
- com.cysharp.messagepipe.vcontainer
- jp.hadashikick.vcontainer
- com.revenantx.litenetlib `2.1.4`
- com.setsuodu.assetbundleframework `1.0.0`

## NugetForUnity

- MemoryPack `1.21.4`（含 Core / Generator）

## 场景搭建（最小）

1. 空物体 `App` → `AppLifetimeScope`（Parent 空）
2. 同场景 UIDocument + `UIDocumentBinder`
3. 空物体 `Match` → `MatchBootstrap` + `MatchBootstrapInstaller`
4. **联调服务器**：空物体挂 `MinimalGameServer`（可同一场景或独立 Server 场景）
5. 可选：胶囊 Prefab 拖到 MatchBootstrap.editorFallbackPrefab

## 流程

Login 输入名 → Home 点【匹配】→ 连 `127.0.0.1:9050` → JoinAck → 生成本地玩家 → WASD 移动上报 → 服务器广播 MoveSnapshot → 远端插值

## 资源

- AB 路径约定：`characters/player` 包内资源名 `Player`
- Editor 无 AB 时用 editorFallbackPrefab 或自动胶囊

## 注意

- ABManager API 以你安装的 `com.setsuodu.assetbundleframework` 为准；若命名空间不同，改 `AppEntryPoint` / `MatchBootstrap` 的引用即可
- LiteNetLib 2.x 与 1.x 事件签名略有差异，以包内源码为准微调 `OnNetworkReceive` 参数
