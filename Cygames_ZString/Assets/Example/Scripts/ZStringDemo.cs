using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Text; // ZString

/// <summary>
/// ZString 零分配字符串构建示例
/// 演示：Concat / Format / CreateStringBuilder / 与普通 string 对比
/// 快捷键：
///   C - Concat 示例
///   F - Format 示例
///   B - StringBuilder 示例
///   P - 性能对比（多次拼接）
/// </summary>
public class ZStringDemo : MonoBehaviour
{
    [Header("UI（可选）")]
    [SerializeField] private Text resultText;
    [SerializeField] private Text infoText;
    [SerializeField] private Button concatButton;
    [SerializeField] private Button formatButton;
    [SerializeField] private Button builderButton;
    [SerializeField] private Button perfButton;

    private int counter = 0;
    private float lastHp = 100f;
    private Vector3 lastPos = Vector3.zero;

    private void Start()
    {
        if (concatButton) concatButton.onClick.AddListener(DoConcat);
        if (formatButton) formatButton.onClick.AddListener(DoFormat);
        if (builderButton) builderButton.onClick.AddListener(DoBuilder);
        if (perfButton) perfButton.onClick.AddListener(DoPerfCompare);

        RefreshInfo("准备就绪。按 C/F/B/P 或点击按钮测试 ZString。");
        Debug.Log("[ZStringDemo] 已启动。ZString 可显著降低字符串拼接时的 GC 分配。");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) DoConcat();
        if (Input.GetKeyDown(KeyCode.F)) DoFormat();
        if (Input.GetKeyDown(KeyCode.B)) DoBuilder();
        if (Input.GetKeyDown(KeyCode.P)) DoPerfCompare();
    }

    /// <summary>
    /// ZString.Concat：零中间分配的拼接
    /// </summary>
    private void DoConcat()
    {
        counter++;
        lastHp = UnityEngine.Random.Range(0f, 100f);
        lastPos = new Vector3(
            UnityEngine.Random.Range(-10f, 10f),
            0,
            UnityEngine.Random.Range(-10f, 10f));

        // 传统方式会有多次分配 + boxing
        // string s = "Player#" + counter + " HP:" + lastHp + " Pos:" + lastPos;

        // ZString.Concat：泛型，直接写 buffer，只分配最终 string
        string result = ZString.Concat(
            "Player#", counter,
            " HP:", lastHp.ToString("F1"),
            " Pos:", lastPos);

        ShowResult("[Concat]\n" + result);
        Debug.Log($"[ZString.Concat] {result}");
    }

    /// <summary>
    /// ZString.Format：替代 string.Format，避免 params object[] 装箱
    /// </summary>
    private void DoFormat()
    {
        counter++;
        int level = UnityEngine.Random.Range(1, 99);
        float gold = UnityEngine.Random.Range(100f, 9999f);

        // 传统：string.Format("Lv.{0} Gold:{1:F0}", level, gold); 会有装箱
        string result = ZString.Format(
            "Lv.{0}  Gold:{1:F0}  Time:{2:HH:mm:ss}",
            level,
            gold,
            DateTime.Now);

        ShowResult("[Format]\n" + result);
        Debug.Log($"[ZString.Format] {result}");
    }

    /// <summary>
    /// Utf16ValueStringBuilder：类似 StringBuilder，但 struct + 池化 buffer
    /// 必须 using 包裹，Dispose 时归还 buffer
    /// </summary>
    private void DoBuilder()
    {
        counter++;

        using (var sb = ZString.CreateStringBuilder())
        {
            sb.Append("=== Player Status ===\n");
            sb.Append("ID: ");
            sb.Append(counter);
            sb.AppendLine();

            sb.Append("HP: ");
            sb.AppendFormat("{0:F1}/{1}", lastHp, 100f);
            sb.AppendLine();

            sb.Append("Pos: ");
            sb.Append(lastPos);
            sb.AppendLine();

            // 还可以直接 AppendJoin
            sb.Append("Tags: ");
            sb.AppendJoin(", ", "Warrior", "Elite", "Boss");

            string result = sb.ToString(); // 只有这里真正分配 string
            ShowResult("[Builder]\n" + result);
            Debug.Log($"[ZString.CreateStringBuilder]\n{result}");
        }
        // Dispose 后 buffer 归还，可复用
    }

    /// <summary>
    /// 简单性能对比：大量拼接时 ZString 的优势
    /// （实际项目中用 Profiler 看 GC Alloc 更准确）
    /// </summary>
    private void DoPerfCompare()
    {
        const int N = 2000;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // ---- 传统 string + ----
        sw.Restart();
        string traditional = "";
        for (int i = 0; i < N; i++)
        {
            traditional = "x:" + i + " y:" + (i * 2) + " z:" + (i * 3);
        }
        long traditionalMs = sw.ElapsedMilliseconds;

        // ---- StringBuilder ----
        sw.Restart();
        var normalSb = new StringBuilder(64);
        string normalResult = null;
        for (int i = 0; i < N; i++)
        {
            normalSb.Clear();
            normalSb.Append("x:").Append(i).Append(" y:").Append(i * 2).Append(" z:").Append(i * 3);
            normalResult = normalSb.ToString();
        }
        long normalSbMs = sw.ElapsedMilliseconds;

        // ---- ZString.Concat ----
        sw.Restart();
        string zConcat = null;
        for (int i = 0; i < N; i++)
        {
            zConcat = ZString.Concat("x:", i, " y:", i * 2, " z:", i * 3);
        }
        long zConcatMs = sw.ElapsedMilliseconds;

        // ---- ZString Builder ----
        sw.Restart();
        string zBuilder = null;
        for (int i = 0; i < N; i++)
        {
            using (var sb = ZString.CreateStringBuilder())
            {
                sb.Append("x:");
                sb.Append(i);
                sb.Append(" y:");
                sb.Append(i * 2);
                sb.Append(" z:");
                sb.Append(i * 3);
                zBuilder = sb.ToString();
            }
        }
        long zBuilderMs = sw.ElapsedMilliseconds;

        string summary =
            $"[Perf x{N}]\n" +
            $"string +     : {traditionalMs} ms\n" +
            $"StringBuilder: {normalSbMs} ms\n" +
            $"ZString.Concat: {zConcatMs} ms\n" +
            $"ZString Builder: {zBuilderMs} ms\n" +
            $"(最后结果示例: {zConcat})";

        ShowResult(summary);
        Debug.Log(summary);
        // 注意：Editor 下结果仅供参考，真机 + Profiler 看 GC Alloc 更有意义
    }

    private void ShowResult(string msg)
    {
        if (resultText != null)
            resultText.text = msg;
        RefreshInfo("已执行。查看上方结果或 Console。");
    }

    private void RefreshInfo(string tip)
    {
        if (infoText != null)
            infoText.text = tip + "\n快捷键: C=Concat  F=Format  B=Builder  P=Perf";
    }
}
