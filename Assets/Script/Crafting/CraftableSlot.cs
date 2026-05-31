using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CraftableSlot : MonoBehaviour, IPointerClickHandler
{
    public LevelingManager levelingManager; public CraftingManager craftingManager;
    public CraftingProcessHandler craftingProcessHandler; public Image imageslot;
    public Image background; public string name; public bool occupied = false;
    public bool locked = false; public List<String> neededResources;
    public CraftableItem craftableItemReference; public int slotnumber;
    public Sprite lockOverlaySprite;
    [SerializeField] private Color32 selectedBackgroundColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color NormalBackgroundColor = Color.white;
    private static Sprite cachedDefaultLockOverlaySprite;
    private Button slotButton;
    private Image lockOverlayImage;
    private bool hasCachedDefaultBackgroundColor; void Start()
    {
        ResolveReferences();
        BindButtonIfPresent(); CacheDefaultBackgroundColor();
        if (craftingManager != null && !craftingManager.slots.Contains(this)) { craftingManager.slots.Add(this); }
        SetSelectedVisual(false); SetVisualVisible(occupied);
    }
    public void AddCraftableItem(CraftableItem craftableItem)
    {
        if (craftableItem == null) { return; }
        ResolveReferences(); if (imageslot == null)
        {
            Debug.LogWarning("CraftableSlot: Image slot is not assigned."); return;
        }
        imageslot.sprite = craftableItem.sprite; imageslot.color = Color.white;
        craftableItemReference = craftableItem;
        name = craftableItem.name; occupied = true;
        neededResources = craftableItem.neededResources; RefreshLockState();
        SetSelectedVisual(false);
        SetVisualVisible(true);
    }
    public void RefreshLockState()
    {
        if (!occupied || craftableItemReference == null)
        {
            locked = false; ApplyLockVisual(false); return;
        }
        ResolveReferences();
        int currentLevel = ResolveCurrentLevel();
        locked = craftableItemReference.IsLockedForLevel(currentLevel);
        ApplyLockVisual(true);
    }
    public void ResetRuntimeState()
    {
        occupied = false;
        locked = false; neededResources = null; craftableItemReference = null;
        name = string.Empty; if (imageslot != null) { imageslot.sprite = null; imageslot.color = Color.white; }
        ApplyLockVisual(false);
        SetSelectedVisual(false);
    }
    public void SetVisualVisible(bool visible)
    {
        if (imageslot != null) { imageslot.enabled = visible; }
        if (background != null) { background.enabled = visible; }
        if (lockOverlayImage != null) { lockOverlayImage.enabled = visible && locked; }
    }
    public void SetSelectedVisual(bool selected)
    {
        if (background == null) { return; }
        CacheDefaultBackgroundColor();
        bool shouldHighlight = selected && occupied && craftableItemReference != null;
        background.color = shouldHighlight ? (Color)selectedBackgroundColor : NormalBackgroundColor;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left) { return; }
        SelectCraftableItemFromSlot();
    }
    public void SelectCraftableItemFromSlot()
    {
        if (locked || craftableItemReference == null) { return; }
        ResolveReferences(); if (craftingProcessHandler == null)
        {
            Debug.LogWarning("CraftableSlot: CraftingProcessHandler was not found for slot click.", this);
            return;
        }
        craftingProcessHandler.SelectCraftableItem(craftableItemReference);
    }
    private void ResolveReferences()
    {
        if (craftingManager == null) { craftingManager = GetComponentInParent<CraftingManager>(); }
        if (levelingManager == null && craftingManager != null) { levelingManager = craftingManager.levelingManager; }
        if (levelingManager == null) { levelingManager = UnitySceneSearch.FindFirst<LevelingManager>(); }
        if (craftingProcessHandler == null && craftingManager != null) { craftingProcessHandler = craftingManager.GetComponent<CraftingProcessHandler>(); }
        if (craftingProcessHandler == null) { craftingProcessHandler = GetComponentInParent<CraftingProcessHandler>(); }
        if (craftingProcessHandler == null)
        {
            craftingProcessHandler = UnitySceneSearch.FindFirst<CraftingProcessHandler>();
        }
    }
    private int ResolveCurrentLevel()
    {
        if (levelingManager != null) { return levelingManager.CurrentLevel; }
        return 1;
    }
    private void ApplyLockVisual(bool visible)
    {
        if (imageslot != null && craftableItemReference != null)
        {
            imageslot.sprite = craftableItemReference.sprite;
            imageslot.color = locked ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
        }
        EnsureLockOverlayImage();
        if (lockOverlayImage != null) { lockOverlayImage.enabled = visible && locked; }
    }
    private void EnsureLockOverlayImage()
    {
        if (lockOverlayImage != null) { return; }
        Sprite overlaySprite = ResolveLockOverlaySprite();
        if (overlaySprite == null || imageslot == null) { return; }
        GameObject overlayObject = new GameObject("CraftLockOverlay", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(imageslot.transform, false);
        RectTransform overlayRect = overlayObject.transform as RectTransform;
        overlayRect.anchorMin = new Vector2(0.1f, 0.1f);
        overlayRect.anchorMax = new Vector2(0.9f, 0.9f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;
        lockOverlayImage = overlayObject.GetComponent<Image>();
        lockOverlayImage.sprite = overlaySprite;
        lockOverlayImage.preserveAspect = true;
        lockOverlayImage.raycastTarget = false;
        lockOverlayImage.enabled = false;
    }
    private Sprite ResolveLockOverlaySprite()
    {
        if (lockOverlaySprite != null) { return lockOverlaySprite; }
        if (cachedDefaultLockOverlaySprite == null)
        {
            cachedDefaultLockOverlaySprite = Resources.Load<Sprite>("LockIconTransparent");
        }
        return cachedDefaultLockOverlaySprite;
    }
    private void BindButtonIfPresent()
    {
        slotButton = GetComponent<Button>();
        if (slotButton == null) { return; }
        slotButton.onClick.RemoveListener(SelectCraftableItemFromSlot);
        slotButton.onClick.AddListener(SelectCraftableItemFromSlot);
    }
    private void CacheDefaultBackgroundColor()
    {
        if (hasCachedDefaultBackgroundColor || background == null) { return; }
        background.color = NormalBackgroundColor;
        hasCachedDefaultBackgroundColor = true;
    }
}
