using System;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class SnowflakeIdWorker
{
    // 基准时间：2020-01-01 00:00:00 UTC (1577836800000 毫秒)
    private const long Twepoch = 1577836800000L;

    // 各部分占用的位数
    private const int WorkerIdBits = 5;
    private const int DatacenterIdBits = 5;
    private const int SequenceBits = 12;

    // 各部分最大值
    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
    private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);

    // 位移偏移量
    private const int WorkerIdShift = SequenceBits;
    private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
    private const long SequenceMask = -1L ^ (-1L << SequenceBits);

    private long _workerId;
    private long _datacenterId;
    private long _sequence = 0L;
    private long _lastTimestamp = -1L;

    private readonly object _lock = new object();

    public SnowflakeIdWorker(long workerId, long datacenterId)
    {
        if (workerId > MaxWorkerId || workerId < 0)
            throw new ArgumentException($"worker Id can't be greater than {MaxWorkerId} or less than 0");
        if (datacenterId > MaxDatacenterId || datacenterId < 0)
            throw new ArgumentException($"datacenter Id can't be greater than {MaxDatacenterId} or less than 0");

        _workerId = workerId;
        _datacenterId = datacenterId;
    }

    public long NextId()
    {
        lock (_lock)
        {
            long timestamp = TimeGen();

            // 如果当前时间小于上一次 ID 生成的时间点，说明系统时钟回退过，抛出异常
            if (timestamp < _lastTimestamp)
                throw new Exception("Clock moved backwards. Refusing to generate id");

            // 如果是同一时间生成的，则进行毫秒内序列
            if (_lastTimestamp == timestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                // 毫秒内序列溢出
                if (_sequence == 0)
                {
                    // 阻塞到下一个毫秒,获得新的时间戳
                    timestamp = TilNextMillis(_lastTimestamp);
                }
            }
            else
            {
                // 时间戳改变，毫秒内序列重置
                _sequence = 0L;
            }

            _lastTimestamp = timestamp;

            // 移位并通过或运算拼到一起组成 64 位的 ID
            return ((timestamp - Twepoch) << TimestampLeftShift)
                   | (_datacenterId << DatacenterIdShift)
                   | (_workerId << WorkerIdShift)
                   | _sequence;
        }
    }

    protected long TilNextMillis(long lastTimestamp)
    {
        long timestamp = TimeGen();
        while (timestamp <= lastTimestamp)
        {
            timestamp = TimeGen();
        }

        return timestamp;
    }

    protected long TimeGen()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}