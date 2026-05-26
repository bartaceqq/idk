using System; using System.Collections.Generic; using UnityEngine; using UnityEngine.UI; using UnityEngine.EventSystems;

public class CraftableSlot : MonoBehaviour, IPointerClickHandler {
    public LevelingManager levelingManager;
    public CraftingManager craftingManager;
    public CraftingProcessHandler craftingProcessHandler;
    public Image imageslot;
    public Image background;
    public string name;
    public bool occupied = false;
    public bool locked = false;
    public List<String> neededResources;
    public CraftableItem craftableItemReference;
    public int slotnumber;
    [SerializeField] private Color32 selectedBackgroundColor = new Color32(0xA1, 0x31, 0x36, 0xFF);
    private Button slotButton;
    private Color defaultBackgroundColor;
    private bool hasCachedDefaultBackgroundColor;
    void Start() {
        ResolveReferences();
        BindButtonIfPresent();
        CacheDefaultBackgroundColor();

        if (craftingManager != null && !craftingManager.slots.Contains(this)) { craftingManager.slots.Add(this); }

        SetSelectedVisual(false);
        SetVisualVisible(occupied); }
    public void AddCraftableItem(CraftableItem craftableItem) { if (craftableItem == null) { return; }

        ResolveReferences();

        if (imageslot == null) {
            Debug.LogWarning("CraftableSlot: Image slot is not assigned.");
            return; }

        imageslot.sprite = craftableItem.sprite;
        craftableItemReference = craftableItem;
        name = craftableItem.name;
        occupied = true;
        neededResources = craftableItem.neededResources;

        if (levelingManager == null) {
            locked = false; } else if (craftableItem.minlvl <= levelingManager.level) {
            locked = false;

        } else { locked = true; }

        SetSelectedVisual(false);
        SetVisualVisible(true); }
    public void ResetRuntimeState() {
        occupied = false;
        locked = false;
        neededResources = null;
        craftableItemReference = null;
        name = string.Empty;

        if (imageslot != null) { imageslot.sprite = null; }

        SetSelectedVisual(false); }
    public void SetVisualVisible(bool visible) { if (imageslot != null) { imageslot.enabled = visible; }

        if (background != null) { background.enabled = visible; } }
    public void SetSelectedVisual(bool selected) { if (background == null) { return; }

        CacheDefaultBackgroundColor();
        bool shouldHighlight = selected && occupied && craftableItemReference != null;
        background.color = shouldHighlight ? selectedBackgroundColor : defaultBackgroundColor; }
    public void OnPointerClick(PointerEventData eventData) { if (eventData != null && eventData.button != PointerEventData.InputButton.Left) { return; }

        SelectCraftableItemFromSlot(); }
    public void SelectCraftableItemFromSlot() { if (locked || craftableItemReference == null) { return; }

        ResolveReferences();
        if (craftingProcessHandler == null) {
            Debug.LogWarning("CraftableSlot: CraftingProcessHandler was not found for slot click.", this);
            return; }

        craftingProcessHandler.SelectCraftableItem(craftableItemReference); }
    private void ResolveReferences() { if (craftingManager == null) { craftingManager = GetComponentInParent<CraftingManager>(); }

        if (craftingProcessHandler == null && craftingManager != null) { craftingProcessHandler = craftingManager.GetComponent<CraftingProcessHandler>(); }

        if (craftingProcessHandler == null) { craftingProcessHandler = GetComponentInParent<CraftingProcessHandler>(); }

        if (craftingProcessHandler == null) {
            craftingProcessHandler = UnitySceneSearch.FindFirst<CraftingProcessHandler>();
        } }
    private void BindButtonIfPresent() {
        slotButton = GetComponent<Button>();
        if (slotButton == null) { return; }

        slotButton.onClick.RemoveListener(SelectCraftableItemFromSlot);
        slotButton.onClick.AddListener(SelectCraftableItemFromSlot); }
    private void CacheDefaultBackgroundColor() { if (hasCachedDefaultBackgroundColor || background == null) { return; }

        defaultBackgroundColor = background.color;
        hasCachedDefaultBackgroundColor = true; } }
