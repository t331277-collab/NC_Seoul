using System.Collections.Generic;

public static class TutorialFlagStore
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    public static void SetFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag))
            flags.Add(flag);
    }

    public static bool HasFlag(string flag)
    {
        return flags.Contains(flag);
    }

    public static void ClearAll()
    {
        flags.Clear();
    }
}
