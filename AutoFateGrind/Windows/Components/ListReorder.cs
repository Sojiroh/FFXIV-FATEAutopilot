using System.Collections.Generic;

namespace AutoFateGrind.Windows.Components;

internal static class ListReorder
{
    public static bool Apply<T>(IList<T> list, int count, int? moveUp, int? moveDown, int? remove)
    {
        if (moveUp is { } upIndex && upIndex > 0)
        {
            (list[upIndex - 1], list[upIndex]) = (list[upIndex], list[upIndex - 1]);
            return true;
        }
        if (moveDown is { } downIndex && downIndex < count - 1)
        {
            (list[downIndex + 1], list[downIndex]) = (list[downIndex], list[downIndex + 1]);
            return true;
        }
        if (remove is { } removeIndex)
        {
            list.RemoveAt(removeIndex);
            return true;
        }
        return false;
    }

    public static bool Move<T>(IList<T> list, int from, int to)
    {
        if (from < 0 || from >= list.Count || to < 0 || to >= list.Count || from == to)
        {
            return false;
        }
        var moved = list[from];
        list.RemoveAt(from);
        list.Insert(to, moved);
        return true;
    }
}
