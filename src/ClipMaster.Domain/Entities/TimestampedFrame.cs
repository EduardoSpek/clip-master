using System.Collections.Concurrent;
using ClipMaster.Domain.Interfaces;

namespace ClipMaster.Domain.Entities;

public class SynchronizedCircularBuffer<T> : ICircularBuffer<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly int _maxCapacity;
    private long _totalFramesAdded;

    public int Count => _queue.Count;
    public long TotalFramesAdded => Interlocked.Read(ref _totalFramesAdded);

    public SynchronizedCircularBuffer(int maxCapacity)
    {
        _maxCapacity = maxCapacity;
    }

    public void Add(T item)
    {
        _queue.Enqueue(item);
        Interlocked.Increment(ref _totalFramesAdded);

        while (_queue.Count > _maxCapacity)
        {
            _queue.TryDequeue(out _);
        }
    }

    public T[] GetLastNItems(int count)
    {
        var items = _queue.ToArray();
        var startIndex = Math.Max(0, items.Length - count);
        var result = new T[items.Length - startIndex];
        Array.Copy(items, startIndex, result, 0, result.Length);
        return result;
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}

public class TimestampedFrame
{
    public long TimestampMs { get; set; }
    public byte[] VideoData { get; set; } = Array.Empty<byte>();
    public byte[] AudioData { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public int AudioSampleCount { get; set; }
}
