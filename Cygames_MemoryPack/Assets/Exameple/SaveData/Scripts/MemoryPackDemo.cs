using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryPackDemo : MonoBehaviour
{
    [Header("UI（可选）")]
    [SerializeField] private Text infoText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button randomButton;

    private GameSaveData current = new();

    private void Start()
    {
        // 按钮绑定（没有按钮也能用键盘）
        if (saveButton) saveButton.onClick.AddListener(DoSave);
        if (loadButton) loadButton.onClick.AddListener(DoLoad);
        if (deleteButton) deleteButton.onClick.AddListener(DoDelete);
        if (randomButton) randomButton.onClick.AddListener(FillRandomData);

        RefreshUI();
        Debug.Log($"存档路径：{Application.persistentDataPath}/save.mpack");
    }

    private void Update()
    {
        // 快捷键方便测试
        if (Input.GetKeyDown(KeyCode.S)) DoSave();
        if (Input.GetKeyDown(KeyCode.L)) DoLoad();
        if (Input.GetKeyDown(KeyCode.D)) DoDelete();
        if (Input.GetKeyDown(KeyCode.R)) FillRandomData();
    }

    private void FillRandomData()
    {
        current = new GameSaveData
        {
            PlayerName = "Hero_" + Random.Range(100, 999),
            Level = Random.Range(1, 50),
            Gold = Random.Range(0, 9999),
            Position = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f)),
            Inventory = new List<ItemStack>
            {
                new() { ItemId = "potion", Count = Random.Range(1, 10) },
                new() { ItemId = "sword",  Count = 1 },
                new() { ItemId = "ore",    Count = Random.Range(5, 30) }
            }
        };
        RefreshUI();
        Debug.Log("[Demo] 已生成随机数据（还没存盘）");
    }

    private void DoSave()
    {
        SaveService.Save(current);
        RefreshUI();
    }

    private void DoLoad()
    {
        var loaded = SaveService.Load();
        if (loaded != null)
        {
            current = loaded;
            RefreshUI();
        }
    }

    private void DoDelete()
    {
        SaveService.Delete();
        current = new GameSaveData();
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (infoText == null) return;

        string inv = "";
        foreach (var item in current.Inventory)
            inv += $"{item.ItemId}x{item.Count}  ";

        infoText.text =
            $"Name : {current.PlayerName}\n" +
            $"Level: {current.Level}\n" +
            $"Gold : {current.Gold}\n" +
            $"Pos  : {current.Position}\n" +
            $"Items: {inv}\n" +
            $"文件存在: {SaveService.Exists}";
    }
}