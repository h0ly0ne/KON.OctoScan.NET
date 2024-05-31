using Pastel;

using static KON.OctoScan.NET.Global;

namespace KON.OctoScan.NET
{
    public class OSList<T> : LinkedList<T>
    {
        public void Initialize()
        {
            lCurrentLogger.Trace("OSList.Initialize()".Pastel(ConsoleColor.Cyan));

            Clear();
        }

        public bool IsEmpty()
        {
            lCurrentLogger.Trace("OSList.IsEmpty()".Pastel(ConsoleColor.Cyan));

            return Count == 0;
        }

        public void Add(T value)
        {
            lCurrentLogger.Trace("OSList.Add()".Pastel(ConsoleColor.Cyan));

            AddFirst(value);
        }
    }
}