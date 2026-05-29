using TMPro; using UnityEngine; using UnityEngine.UI; [DisallowMultipleComponent]
[RequireComponent(typeof(PlayerXPState))]
[RequireComponent(typeof(XPLevelTableLoader))] public class XPHandler : MonoBehaviour {
public Slider xpSlider; public TMP_Text levelText; public TMP_Text xpAmountText;
private PlayerXPState playerXPState; private XPLevelTableLoader xpLevelTableLoader;
private Graphic[] cachedGraphics; private InventoryManager inventoryManager;
private InventoryController inventoryController; private bool hasVisibilityState;
private bool currentVisibilityState; void Awake() { CacheDependencies(); EnsureReady();
CacheGraphics(); RefreshUI(); } void Start() { RefreshUI();
SetVisible(ResolveInventoryVisibility()); }
void Update() { SetVisible(ResolveInventoryVisibility()); }
public void AddXP(int count) { if (count <= 0) { return; } CacheDependencies();
if (!EnsureReady()) { return; } playerXPState.AddXP(count, xpLevelTableLoader);
RefreshUI(); } public void RefreshUI() { CacheDependencies();
if (!EnsureReady()) { return; } int currentLevel = playerXPState.CurrentLevel;
int currentXP = playerXPState.CurrentXP;
int maxXP = xpLevelTableLoader.GetRequiredXPForLevel(currentLevel);
if (xpSlider != null) { xpSlider.minValue = 0f; xpSlider.maxValue = maxXP;
xpSlider.wholeNumbers = true; xpSlider.value = currentXP; }
if (levelText != null) { levelText.text = "LEVEL " + currentLevel; }
if (xpAmountText != null) { xpAmountText.text = currentXP + "/" + maxXP; } }
public int GetCurrentLevel() { CacheDependencies();
return playerXPState != null ? playerXPState.CurrentLevel : 1; }
public int GetCurrentXP() { CacheDependencies();
return playerXPState != null ? playerXPState.CurrentXP : 0; }
public int GetCurrentLevelMaxXP() { CacheDependencies(); return xpLevelTableLoader != null
? xpLevelTableLoader.GetRequiredXPForLevel(GetCurrentLevel()) : 100; }
private void CacheDependencies() { if (playerXPState == null) { playerXPState = GetComponent<PlayerXPState>(); }
if (xpLevelTableLoader == null) { xpLevelTableLoader = GetComponent<XPLevelTableLoader>(); }
if (inventoryManager == null) {
inventoryManager = UnitySceneSearch.FindFirst<InventoryManager>(); }
if (inventoryController == null) {
inventoryController = UnitySceneSearch.FindFirst<InventoryController>(); } }
private void CacheGraphics() { if (cachedGraphics == null || cachedGraphics.Length == 0) { cachedGraphics = GetComponentsInChildren<Graphic>(true); } }
private bool EnsureReady() { if (playerXPState == null || xpLevelTableLoader == null) {
Debug.LogWarning("XPHandler: Missing PlayerXPState or XPLevelTableLoader component.", this);
return false; } xpLevelTableLoader.EnsureLoaded();
playerXPState.NormalizeProgress(xpLevelTableLoader); return true; }
public void SetVisible(bool visible) { if (hasVisibilityState && currentVisibilityState == visible) { return; }
CacheGraphics(); if (cachedGraphics != null) {
for (int i = 0; i < cachedGraphics.Length; i++) { if (cachedGraphics[i] != null) { cachedGraphics[i].enabled = visible; } } }
if (xpSlider != null) { xpSlider.enabled = visible; } currentVisibilityState = visible;
hasVisibilityState = true; } private bool ResolveInventoryVisibility() {
CacheDependencies(); bool visible = false; bool foundSource = false;
if (inventoryManager != null) { visible |= inventoryManager.UIShown; foundSource = true; }
if (inventoryController != null) { visible |= inventoryController.UIshown;
foundSource = true; }
if (!foundSource) { visible = InventoryManager.IsInventoryOpen || InventoryController.IsInventoryOpen; }
return visible; } }
