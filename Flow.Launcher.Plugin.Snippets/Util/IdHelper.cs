namespace Flow.Launcher.Plugin.Snippets.Util;

public class IdHelper
{
    // 静态单例，确保线程安全且 WorkerId 唯一
    private static readonly SnowflakeIdWorker _worker = new SnowflakeIdWorker(1, 1);

    /// <summary>
    /// 获取一个新的分布式唯一 ID
    /// </summary>
    public static long NewId()
    {
        return _worker.NextId();
    }
}