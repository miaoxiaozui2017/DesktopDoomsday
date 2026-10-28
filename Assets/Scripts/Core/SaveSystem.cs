using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using DesktopDoomsday.Core;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 存档/读档/离线收益时间计算。
    /// - 原子写：先写 .tmp 再 File.Replace。
    /// - 离线上限 8 小时，时间回拨不产生收益。
    /// - GameState 含 HashSet，序列化依赖 Newtonsoft.Json（com.unity.nuget.newtonsoft-json）。
    /// </summary>
    public class SaveSystem
    {
        public const int SaveVersion = 1;
        private const string FileName = "save.json";
        private const float OfflineCapSeconds = 8f * 3600f;

        private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public void Save(GameState state)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            state.lastSaveUnixMs = now;
            var data = new SaveData { version = SaveVersion, state = state, savedAtUnixMs = now };
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(SavePath))
                File.Replace(tmp, SavePath, SavePath + ".bak"); // 原子替换 + 旧档备份
            else
                File.Move(tmp, SavePath);
        }

        public SaveData Load()
        {
            if (!File.Exists(SavePath)) return null;
            try
            {
                var data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(SavePath));
                if (data == null) return null;
                if (data.version != SaveVersion)
                    return Migrate(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 计算离线真实秒数。now < lastSave（系统时间回拨）返回 0。
        /// </summary>
        public float ComputeOfflineSeconds(long lastSaveMs)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now < lastSaveMs) return 0f;
            float sec = (now - lastSaveMs) / 1000f;
            return Mathf.Min(sec, OfflineCapSeconds);
        }

        // TODO(Batch 5): 接入离线结算（按 0.5 消耗速率模拟 tick，不生成宝箱/副本）。
        private SaveData Migrate(SaveData data)
        {
            // MVP：版本不匹配暂直接返回（后续实现迁移或重置提示）。
            Debug.LogWarning($"[SaveSystem] version mismatch ({data.version} != {SaveVersion}), using as-is.");
            return data;
        }
    }
}
