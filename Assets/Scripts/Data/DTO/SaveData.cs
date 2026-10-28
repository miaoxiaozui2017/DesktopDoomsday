namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 存档 DTO。version 不匹配走迁移或重置（见 SaveSystem）。
    /// 直接嵌套 GameState；依赖 Newtonsoft.Json 序列化（含 HashSet）。
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        public int version;                // 当前 = 1
        public GameState state;
        public long savedAtUnixMs;
    }
}
