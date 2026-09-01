#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using MemoryPack;
using NUnit.Framework;
using UnityEngine;

namespace Roguelite.SaveSystem.Tests
{
    public sealed partial class MemoryPackSaveRoundTripTests
    {
        [Test]
        public void RoundTrip_PreservesExpectedSaveState()
        {
            var original = new GameSaveData
            {
                ApplicationSaveVersion = 2,
                SceneId = "Dungeon_01",
                Player = new PlayerSnapshot
                {
                    Position = new Vector3(12.5f, 0f, -8.25f),
                    Rotation = Quaternion.Euler(0f, 135f, 0f),
                    Health = 73,
                    Gold = 420,
                    EquippedWeaponId = "bow_rare"
                },
                Inventory = new List<InventoryStack>
                {
                    new() { ItemId = "potion_small", Quantity = 3 },
                    new() { ItemId = "ore_iron", Quantity = 12 }
                },
                DefeatedEnemyIds = new List<string> { "enemy-room-1-a" },
                SavedAtUnixMilliseconds = 1_740_000_000_000,
                TutorialCompleted = true
            };

            var bytes = MemoryPackSerializer.Serialize(original);
            var restored = MemoryPackSerializer.Deserialize<GameSaveData>(bytes);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Magic, Is.EqualTo("ROGUELITE_SAVE"));
            Assert.That(restored.SceneId, Is.EqualTo("Dungeon_01"));
            Assert.That(restored.Player.Position, Is.EqualTo(new Vector3(12.5f, 0f, -8.25f)));
            Assert.That(restored.Player.Health, Is.EqualTo(73));
            Assert.That(restored.Inventory, Has.Count.EqualTo(2));
            Assert.That(restored.Inventory[1].ItemId, Is.EqualTo("ore_iron"));
            Assert.That(restored.TutorialCompleted, Is.True);
        }

        [Test]
        public void VersionTolerant_NewBooleanUsesDefaultWhenMissingFromOldPayload()
        {
            // 该夹具代表 V1 的布局：只有编号 0 到 6，没有 V2 新增的编号 7。
            var v1 = new GameSaveDataV1Fixture
            {
                SceneId = "Dungeon_01",
                Player = new PlayerSnapshot { Health = 100 },
                Inventory = new List<InventoryStack>(),
                DefeatedEnemyIds = new List<string>()
            };

            var v1Bytes = MemoryPackSerializer.Serialize(v1);
            var restoredByV2 = MemoryPackSerializer.Deserialize<GameSaveData>(v1Bytes);

            Assert.That(restoredByV2, Is.Not.Null);
            Assert.That(restoredByV2.SceneId, Is.EqualTo("Dungeon_01"));
            Assert.That(restoredByV2.TutorialCompleted, Is.False);
        }

        // 请将其作为历史协议夹具保留，后续产品代码不应修改已有编号。
        [MemoryPackable(GenerateType.VersionTolerant)]
        private partial class GameSaveDataV1Fixture
        {
            [MemoryPackOrder(0)] public string Magic { get; set; } = "ROGUELITE_SAVE";
            [MemoryPackOrder(1)] public int ApplicationSaveVersion { get; set; } = 1;
            [MemoryPackOrder(2)] public string SceneId { get; set; } = string.Empty;
            [MemoryPackOrder(3)] public PlayerSnapshot Player { get; set; } = new();
            [MemoryPackOrder(4)] public List<InventoryStack> Inventory { get; set; } = new();
            [MemoryPackOrder(5)] public List<string> DefeatedEnemyIds { get; set; } = new();
            [MemoryPackOrder(6)] public long SavedAtUnixMilliseconds { get; set; }
        }
    }
}
#endif
