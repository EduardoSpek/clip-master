namespace ClipMaster.Domain.Interfaces;

public interface ICircularBuffer<T>
{
    int Count { get; }
    long TotalFramesAdded { get; }
    void Add(T item);
    T[] GetLastNItems(int count);
    void Clear();
}
