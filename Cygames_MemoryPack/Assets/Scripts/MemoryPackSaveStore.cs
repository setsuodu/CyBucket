using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MemoryPack;
using UnityEngine;

namespace Roguelite.SaveSystem
{
    /// <summary>
    /// 将 DTO 保存为 MemoryPack 二进制文件。
    /// Unity 对象的访问必须留在调用方的主线程；本类只接收纯 DTO。
    /// </summary>
    public sealed class MemoryPackSaveStore
    {
        private const string SaveFileName = "checkpoint.mpack";
        private const string TempFileName = "checkpoint.mpack.tmp";
        private const string ExpectedMagic = "ROGUELITE_SAVE";

        private readonly string savePath;
        private readonly string temporaryPath;
        private readonly SemaphoreSlim gate = new(1, 1);

        public MemoryPackSaveStore(string directory = null)
        {
            var saveDirectory = directory ?? Application.persistentDataPath;
            savePath = Path.Combine(saveDirectory, SaveFileName);
            temporaryPath = Path.Combine(saveDirectory, TempFileName);
        }

        public async Task SaveAsync(GameSaveData data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            // 注意：此例中快照构建应发生在主线程，然后才将纯数据交给此方法。
            var payload = MemoryPackSerializer.Serialize(data);

            await gate.WaitAsync(cancellationToken);
            try
            {
                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));

                    // 先写临时文件，避免直接覆盖正式存档。
                    File.WriteAllBytes(temporaryPath, payload);
                    cancellationToken.ThrowIfCancellationRequested();

                    // Unity 的跨平台文件系统差异较大；先删除旧文件再移动临时文件是较保守的实现。
                    // 若产品平台确认支持原子替换 API，可在这里替换为平台测试过的实现。
                    if (File.Exists(savePath)) File.Delete(savePath);
                    File.Move(temporaryPath, savePath);
                }, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<GameSaveData> LoadAsync(CancellationToken cancellationToken = default)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!File.Exists(savePath)) return null;

                    var payload = File.ReadAllBytes(savePath);
                    var data = MemoryPackSerializer.Deserialize<GameSaveData>(payload);

                    if (data == null || data.Magic != ExpectedMagic)
                    {
                        Debug.LogWarning($"存档格式不匹配：{savePath}");
                        return null;
                    }

                    return data;
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // 二进制存档可能因中断写入、版本不匹配或外部篡改而无法反序列化。
                Debug.LogWarning($"读取存档失败，将忽略该文件。原因：{exception.Message}");
                return null;
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (File.Exists(savePath)) File.Delete(savePath);
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
