using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET;

public class OSList<T> : LinkedList<T>
{
    /// <summary>
    /// OSList.Initialize()
    /// </summary>
    public void Initialize()
    {
        LogTrace("OSList.Initialize()");

        Clear();
    }

    /// <summary>
    /// OSList.Add(T)
    /// </summary>
    /// <param name="TCurrentValue"></param>
    public void Add(T TCurrentValue)
    {
        LogTrace("OSList.Add()");

        AddLast(TCurrentValue);
    }
}