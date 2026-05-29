using UnityEngine;
[DisallowMultipleComponent] public class PlayerXPState : MonoBehaviour {
[Min(1)] public int currentLevel = 1; [Min(0)] public int currentXP = 0;
[Min(0)] public int totalXP = 0; public int CurrentLevel => Mathf.Max(1, currentLevel);
public int CurrentXP => Mathf.Max(0, currentXP);
public int TotalXP => Mathf.Max(0, totalXP);
public int AddXP(int amount, XPLevelTableLoader xpLevelTableLoader) {
if (amount <= 0) { return 0; } SanitizeValues(); currentXP += amount; totalXP += amount;
return NormalizeProgress(xpLevelTableLoader); }
public int NormalizeProgress(XPLevelTableLoader xpLevelTableLoader) { SanitizeValues();
if (xpLevelTableLoader == null) { return 0; } int levelsGained = 0;
int requiredXP = xpLevelTableLoader.GetRequiredXPForLevel(currentLevel);
while (requiredXP > 0 && currentXP >= requiredXP) { currentXP -= requiredXP;
currentLevel++; levelsGained++;
requiredXP = xpLevelTableLoader.GetRequiredXPForLevel(currentLevel); }
return levelsGained; } public void SetProgress(int level, int xp) {
currentLevel = Mathf.Max(1, level); currentXP = Mathf.Max(0, xp); }
private void OnValidate() { SanitizeValues(); } private void SanitizeValues() {
currentLevel = Mathf.Max(1, currentLevel); currentXP = Mathf.Max(0, currentXP);
totalXP = Mathf.Max(0, totalXP); } }
