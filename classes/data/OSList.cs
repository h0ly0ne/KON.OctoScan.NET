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
    /// OSList.IsEmpty()
    /// </summary>
    /// <returns>bool</returns>
    public bool IsEmpty()
    {
        LogTrace("OSList.IsEmpty()");

        return Count == 0;
    }

    /// <summary>
    /// OSList.Add(T)
    /// </summary>
    /// <param name="TCurrentValue"></param>
    public void Add(T TCurrentValue)
    {
        LogTrace("OSList.Add()");

        AddFirst(TCurrentValue);
    }

    /// <summary>
    /// OSList.Nodes()
    /// </summary>
    /// <returns>IEnumerable(LinkedListNode(T))</returns>
    public IEnumerable<LinkedListNode<T>> Nodes()
    {
        LogTrace("OSList.Nodes()");

        for (var llnCurrentNode = First; llnCurrentNode != null; llnCurrentNode = llnCurrentNode.Next)
            yield return llnCurrentNode;
    }
}