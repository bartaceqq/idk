using UnityEngine;

[DisallowMultipleComponent]
public class PlayerXPState : MonoBehaviour
{
    private static bool sharedInitialized;
    private static int sharedLevel = 1;
    private static int sharedXP;
    private static int sharedTotalXP;
    [Min(1)] public int currentLevel = 1; [Min(0)] public int currentXP = 0;
    [Min(0)] public int totalXP = 0;
    public int CurrentLevel { get { SyncFromShared(); return currentLevel; } }
    public int CurrentXP { get { SyncFromShared(); return currentXP; } }
    public int TotalXP { get { SyncFromShared(); return totalXP; } }
    private void Awake() { EnsureSharedInitialized(); SyncFromShared(); }
    public int AddXP(int amount, XPLevelTableLoader xpLevelTableLoader)
    {
        if (amount <= 0) { return 0; }
        EnsureSharedInitialized();
        sharedXP += amount; sharedTotalXP += amount;
        int levelsGained = NormalizeSharedProgress(xpLevelTableLoader);
        SyncFromShared(); return levelsGained;
    }
    public int NormalizeProgress(XPLevelTableLoader xpLevelTableLoader)
    {
        EnsureSharedInitialized();
        int levelsGained = NormalizeSharedProgress(xpLevelTableLoader);
        SyncFromShared(); return levelsGained;
    }
    public void SetProgress(int level, int xp)
    {
        sharedLevel = Mathf.Max(1, level); sharedXP = Mathf.Max(0, xp);
        sharedTotalXP = Mathf.Max(sharedTotalXP, sharedXP);
        sharedInitialized = true; SyncFromShared();
    }
    private void OnValidate() { SanitizeValues(); }
    private void EnsureSharedInitialized()
    {
        if (sharedInitialized) { return; }
        SanitizeValues(); sharedLevel = currentLevel; sharedXP = currentXP;
        sharedTotalXP = totalXP; sharedInitialized = true;
    }
    private static int NormalizeSharedProgress(XPLevelTableLoader xpLevelTableLoader)
    {
        sharedLevel = Mathf.Max(1, sharedLevel);
        sharedXP = Mathf.Max(0, sharedXP);
        sharedTotalXP = Mathf.Max(0, sharedTotalXP);
        if (xpLevelTableLoader == null) { return 0; }
        int levelsGained = 0;
        int requiredXP = xpLevelTableLoader.GetRequiredXPForLevel(sharedLevel);
        while (requiredXP > 0 && sharedXP >= requiredXP)
        {
            sharedXP -= requiredXP; sharedLevel++; levelsGained++;
            requiredXP = xpLevelTableLoader.GetRequiredXPForLevel(sharedLevel);
        }
        return levelsGained;
    }
    private void SyncFromShared()
    {
        EnsureSharedInitialized();
        currentLevel = Mathf.Max(1, sharedLevel);
        currentXP = Mathf.Max(0, sharedXP);
        totalXP = Mathf.Max(0, sharedTotalXP);
    }
    private void SanitizeValues()
    {
        currentLevel = Mathf.Max(1, currentLevel); currentXP = Mathf.Max(0, currentXP);
        totalXP = Mathf.Max(0, totalXP);
    }
}
