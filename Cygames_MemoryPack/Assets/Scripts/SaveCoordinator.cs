using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roguelite.SaveSystem
{
    /// <summary>
    /// 示例中的角色状态组件。真实项目可替换为既有角色控制器或领域服务。
    /// </summary>
    public sealed class PlayerSaveTarget : MonoBehaviour
    {
        [field: SerializeField] public int Health { get; private set; } = 100;
        [field: SerializeField] public int Gold { get; private set; }
        [field: SerializeField] public string EquippedWeaponId { get; private set; } = "sword_basic";

        public void Apply(PlayerSnapshot snapshot)
        {
            transform.SetPositionAndRotation(snapshot.Position, snapshot.Rotation);
            Health = snapshot.Health;
            Gold = snapshot.Gold;
            EquippedWeaponId = snapshot.EquippedWeaponId;
        }
    }

    [Serializable]
    public sealed class InventoryEntry
    {
        public string ItemId;
        public int Quantity;
    }

    /// <summary>
    /// 示例背包状态，Inspector 中可观察。实际项目通常替换为背包领域模型。
    /// </summary>
    public sealed class InventorySaveTarget : MonoBehaviour
    {
        [SerializeField] private List<InventoryEntry> entries = new();

        public List<InventoryStack> Capture()
        {
            return entries
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ItemId) && x.Quantity > 0)
                .Select(x => new InventoryStack { ItemId = x.ItemId, Quantity = x.Quantity })
                .ToList();
        }

        public void Apply(IEnumerable<InventoryStack> data)
        {
            entries = data
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ItemId) && x.Quantity > 0)
                .Select(x => new InventoryEntry { ItemId = x.ItemId, Quantity = x.Quantity })
                .ToList();
        }
    }

    /// <summary>
    /// 所有 Unity API 调用都发生在该 MonoBehaviour 中的主线程。
    /// 文件读写被委托给 MemoryPackSaveStore。
    /// </summary>
    public sealed class SaveCoordinator : MonoBehaviour
    {
        [SerializeField] private PlayerSaveTarget player;
        [SerializeField] private InventorySaveTarget inventory;
        [SerializeField] private bool tutorialCompleted;

        private readonly HashSet<string> defeatedEnemyIds = new();
        private MemoryPackSaveStore saveStore;
        private CancellationTokenSource lifetime;

        private void Awake()
        {
            saveStore = new MemoryPackSaveStore();
            lifetime = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }

        /// <summary>可绑定到 UI Button 的 OnClick。</summary>
        public async void SaveCheckpoint()
        {
            if (player == null || inventory == null)
            {
                Debug.LogError("SaveCoordinator 尚未配置 Player 与 Inventory 引用。");
                return;
            }

            try
            {
                // 快照在主线程构建，绝不在 Task.Run 中读取 Transform 或其他 Unity 对象。
                var data = new GameSaveData
                {
                    ApplicationSaveVersion = 2,
                    SceneId = SceneManager.GetActiveScene().name,
                    Player = new PlayerSnapshot
                    {
                        Position = player.transform.position,
                        Rotation = player.transform.rotation,
                        Health = player.Health,
                        Gold = player.Gold,
                        EquippedWeaponId = player.EquippedWeaponId
                    },
                    Inventory = inventory.Capture(),
                    DefeatedEnemyIds = defeatedEnemyIds.OrderBy(id => id).ToList(),
                    SavedAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    TutorialCompleted = tutorialCompleted
                };

                await saveStore.SaveAsync(data, lifetime.Token);
                Debug.Log($"存档完成：场景={data.SceneId}，物品栈={data.Inventory.Count}");
            }
            catch (OperationCanceledException)
            {
                // 场景销毁或应用退出时取消，不需要报告为失败。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// 在目标场景及其 Player/Inventory 已初始化后调用。跨场景加载应先读取 SceneId，
        /// 使用 SceneManager 加载场景后，再在该场景调用本方法的应用步骤。
        /// </summary>
        public async void LoadCheckpointInCurrentScene()
        {
            if (player == null || inventory == null)
            {
                Debug.LogError("SaveCoordinator 尚未配置 Player 与 Inventory 引用。");
                return;
            }

            try
            {
                var data = await saveStore.LoadAsync(lifetime.Token);
                if (data == null)
                {
                    Debug.Log("没有可用存档，保持默认状态。");
                    return;
                }

                if (data.SceneId != SceneManager.GetActiveScene().name)
                {
                    Debug.LogWarning($"存档属于场景 {data.SceneId}，当前场景为 {SceneManager.GetActiveScene().name}。未应用状态。");
                    return;
                }

                // 反序列化已经完成；以下 Unity 对象访问仍在 Unity 同步上下文的主线程执行。
                player.Apply(data.Player);
                inventory.Apply(data.Inventory);
                defeatedEnemyIds.Clear();
                defeatedEnemyIds.UnionWith(data.DefeatedEnemyIds ?? Enumerable.Empty<string>());
                tutorialCompleted = data.TutorialCompleted;

                Debug.Log($"存档已恢复：版本={data.ApplicationSaveVersion}，保存时间={data.SavedAtUnixMilliseconds}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public void MarkEnemyDefeated(string persistentEnemyId)
        {
            if (!string.IsNullOrWhiteSpace(persistentEnemyId))
                defeatedEnemyIds.Add(persistentEnemyId);
        }
    }
}
