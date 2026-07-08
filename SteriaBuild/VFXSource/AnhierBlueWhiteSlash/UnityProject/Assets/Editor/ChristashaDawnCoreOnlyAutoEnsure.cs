using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ChristashaDawnCoreOnlyAutoEnsure
{
    private const string SessionKey = "Steria.ChristashaDawn.CoreOnlyAutoEnsure.Done.v11";

    static ChristashaDawnCoreOnlyAutoEnsure()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += EnsureCoreOnlyPrefab;
    }

    private static void EnsureCoreOnlyPrefab()
    {
        try
        {
            ChristashaDawnCombatBundleBuilder.EnsureVerticalCoreOnlyPrefab();
            Debug.Log("Christasha dawn core-only prefab ensured.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to ensure Christasha dawn core-only prefab: " + ex);
        }
    }
}
