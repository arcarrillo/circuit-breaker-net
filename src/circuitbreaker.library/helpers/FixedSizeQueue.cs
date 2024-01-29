using System.Collections.Concurrent;

namespace circuitbreaker.library.helpers;

internal class FixedSizeQueue<T> : ConcurrentQueue<T>
{
    readonly object lockObject = new object();
    public int Size { get; private set; }
    public FixedSizeQueue(int size) => Size = size;
    public bool Full => Count == Size;

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        lock (lockObject)
        {
            while (base.Count > Size) base.TryDequeue(out _);
        }
    }
}
