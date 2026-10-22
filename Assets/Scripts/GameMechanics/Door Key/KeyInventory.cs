using System.Collections.Generic;

public static class KeyInventory
{
    private static HashSet<string> collectedKeys = new HashSet<string>();

    public static void AddKey(string keyID)
    {
        if (!collectedKeys.Contains(keyID))
        {
            collectedKeys.Add(keyID);
        }
    }

    public static bool HasKey(string keyID)
    {
        return collectedKeys.Contains(keyID);
    }

    public static void ResetInventory()
    {
        collectedKeys.Clear();
    }
}