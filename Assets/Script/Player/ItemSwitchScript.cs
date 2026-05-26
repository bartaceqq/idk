using System.Collections; using System.Collections.Generic; using UnityEngine;
public class ItemSwitchScript : MonoBehaviour {
    public List<Item> items = new List<Item>();
    public int currentitemid;
    public string currentitemname;
    public bool requireWeaponSlotAssignment = true;

    public Item item;

    private readonly HashSet<string> equippedItemNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private ActionScript _actionScript;
    private Coroutine _pendingSwordTransition;
    private bool _isSwordTransitioning;
    private static readonly KeyCode[] WeaponSlotHotkeys = {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9 };
    private static readonly string[] WeaponSlotHotkeyIds = {
        GameSettings.Key.WeaponSlot1,
        GameSettings.Key.WeaponSlot2,
        GameSettings.Key.WeaponSlot3,
        GameSettings.Key.WeaponSlot4,
        GameSettings.Key.WeaponSlot5,
        GameSettings.Key.WeaponSlot6,
        GameSettings.Key.WeaponSlot7,
        GameSettings.Key.WeaponSlot8,
        GameSettings.Key.WeaponSlot9 };

    private void Awake() { RefreshItemPresentations(); }

    private void Update() {
        EnsureCurrentSwordTrailEffect();

        if (_isSwordTransitioning) { return; }

        if (GameplayUiState.IsGameplayInputBlocked) { return; }

        ActionScript resolvedActionScript = ResolveActionScript();
        if (resolvedActionScript != null &&
            (resolvedActionScript.IsGameplayInputLocked() || resolvedActionScript.IsSwordBlockActive())) { return; }

        EnsureCurrentSelectionIsAllowed();

        if (requireWeaponSlotAssignment) { if (TryHandleWeaponSlotHotkeys()) { return; }

            return; }

        TryHandleLegacyItemHotkeys(); }
    private bool TryHandleWeaponSlotHotkeys() {
        List<WeaponSlot> orderedSlots = WeaponSlot.GetOrderedWeaponSlots();
        int slotCount = Mathf.Min(orderedSlots.Count, WeaponSlotHotkeys.Length);
        for (int i = 0; i < slotCount; i++) {
            KeyCode hotkey = GameSettings.GetKey(WeaponSlotHotkeyIds[i], WeaponSlotHotkeys[i]);
            if (ShouldReserveSwordSpecialHotkey(hotkey)) { continue; }

            if (!Input.GetKeyDown(hotkey)) { continue; }

            WeaponSlot slot = orderedSlots[i];
            if (slot == null) { return true; }

            string slotItemName = slot.GetAssignedItemName();
            if (string.IsNullOrEmpty(slotItemName)) {
                UnequipCurrentItem();
                return true; }

            ToggleItemByName(slotItemName);
            return true; }

        return false; }
    private void TryHandleLegacyItemHotkeys() {
        for (int i = 0; i < items.Count; i++) {
            Item candidate = items[i];
            if (candidate == null || !Input.GetKeyDown(candidate.key)) { continue; }

            if (!CanUseItem(candidate)) { continue; }

            SwitchToItem(candidate);
            return; } }
    public void ToggleItemByName(string itemNameToToggle) {
        string normalized = NormalizeItemName(itemNameToToggle);
        if (string.IsNullOrEmpty(normalized)) { return; }

        if (string.Equals(NormalizeItemName(currentitemname), normalized, System.StringComparison.OrdinalIgnoreCase)) {
            UnequipCurrentItem();
            return; }

        if (!TryResolveItemByName(normalized, out Item resolvedItem) || !CanUseItem(resolvedItem)) { return; }

        SwitchToItem(resolvedItem); }
    public void ApplyEquippedItemNames(IEnumerable<string> names) {
        equippedItemNames.Clear();
        if (names != null) {
            foreach (string name in names) {
                string normalized = NormalizeItemName(name);
                if (!string.IsNullOrEmpty(normalized)) { equippedItemNames.Add(normalized); } } }

        EnsureCurrentSelectionIsAllowed();
        if (!_isSwordTransitioning) { RefreshItemPresentations(); } }
    public bool HasItemNamed(string itemNameToFind) {
        string normalized = NormalizeItemName(itemNameToFind);
        if (string.IsNullOrEmpty(normalized)) { return false; }

        for (int i = 0; i < items.Count; i++) {
            Item candidate = items[i];
            if (candidate == null) { continue; }

            if (string.Equals(NormalizeItemName(candidate.name), normalized, System.StringComparison.OrdinalIgnoreCase)) { return true; } }

        return false; }
    public bool TryGetEquippedSword(out Sword equippedSword) {
        equippedSword = null;
        if (Sword.TryResolve(item, out equippedSword)) { return true; }

        string equippedName = NormalizeItemName(currentitemname);
        if (string.IsNullOrEmpty(equippedName) ||
            !TryResolveItemByName(equippedName, out Item resolvedItem)) { return false; }

        return Sword.TryResolve(resolvedItem, out equippedSword); }
    private bool CanUseItem(Item candidate) { if (candidate == null) { return false; }

        if (!requireWeaponSlotAssignment) { return true; }

        if (equippedItemNames.Count == 0) { return false; }

        string normalized = NormalizeItemName(candidate.name);
        return !string.IsNullOrEmpty(normalized) && equippedItemNames.Contains(normalized); }
    private void EnsureCurrentSelectionIsAllowed() { if (item == null || CanUseItem(item)) { return; }

        UnequipCurrentItem(); }
    private void SwitchToItem(Item targetItem) { if (targetItem == null || _isSwordTransitioning || targetItem == item) { return; }

        Item previousItem = item;
        bool previousIsSword = IsSwordItem(previousItem);
        bool targetIsSword = IsSwordItem(targetItem);

        if (previousItem != null && previousItem != targetItem && previousIsSword) {
            StartSwordTransition(previousItem, targetItem);
            return; }

        CancelPendingSwordTransition();

        if (previousItem != null && previousItem != targetItem) {
            ActionScript resolvedActionScript = ResolveActionScript();
            resolvedActionScript?.CancelUpperBodyAction();
            resolvedActionScript?.CancelSwordBlock();
            ApplyInactiveItemPresentation(previousItem); }

        if (targetIsSword) {
            ActivateItemImmediate(targetItem, false);
            EnsureSwordTrailEffect(targetItem);
            ApplyHolsteredItemPresentation(targetItem);
            StartSwordEquipTransition(targetItem);
            return; }

        ActivateItemImmediate(targetItem);
        RefreshInactiveItemPresentations(); }
    private void UnequipCurrentItem() { if (_isSwordTransitioning) { return; }

        if (item != null && IsSwordItem(item)) {
            StartSwordTransition(item, null);
            return; }

        CancelPendingSwordTransition();

        if (item != null) {
            ActionScript resolvedActionScript = ResolveActionScript();
            resolvedActionScript?.CancelUpperBodyAction();
            resolvedActionScript?.CancelSwordBlock();
            ApplyInactiveItemPresentation(item); }

        ClearCurrentSelection();
        RefreshInactiveItemPresentations(); }
    private static void SetItemObjectVisible(Item targetItem, bool visible) { if (targetItem == null || targetItem.itemobject == null) { return; }

        Renderer[] renderers = targetItem.itemobject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++) { if (renderers[i] != null) { renderers[i].enabled = visible; } } }
    private void EnsureCurrentSwordTrailEffect() { EnsureSwordTrailEffect(item); }
    private void EnsureSwordTrailEffect(Item targetItem) { if (targetItem == null || targetItem.itemobject == null) { return; }

        if (!IsSwordItem(targetItem)) { return; }

        if (targetItem.itemobject.GetComponent<SwordTrailEffect>() == null) { targetItem.itemobject.AddComponent<SwordTrailEffect>(); } }
    private void StartSwordTransition(Item swordItem, Item nextItem) { if (swordItem == null) { return; }

        CancelPendingSwordTransition();
        _pendingSwordTransition = StartCoroutine(PlaySwordHideTransition(swordItem, nextItem)); }
    private void StartSwordEquipTransition(Item swordItem) { if (swordItem == null) { return; }

        CancelPendingSwordTransition();
        _pendingSwordTransition = StartCoroutine(PlaySwordEquipTransition(swordItem)); }
    private void CancelPendingSwordTransition() { if (_pendingSwordTransition == null) { return; }

        StopCoroutine(_pendingSwordTransition);
        _pendingSwordTransition = null;
        _isSwordTransitioning = false; }
    private IEnumerator PlaySwordHideTransition(Item swordItem, Item nextItem) {
        _isSwordTransitioning = true;

        ActionScript resolvedActionScript = ResolveActionScript();
        resolvedActionScript?.CancelUpperBodyAction();
        resolvedActionScript?.CancelSwordBlock();

        bool playedHide = resolvedActionScript != null && resolvedActionScript.TryUnequipSword();
        if (playedHide && resolvedActionScript != null) {
            yield return WaitForSwordStateToFinish(
                resolvedActionScript,
                resolvedActionScript.swordUnequipStateName); }

        ApplyInactiveItemPresentation(swordItem);
        ClearCurrentSelection();

        if (nextItem != null) {
            bool nextIsSword = IsSwordItem(nextItem);
            if (nextIsSword) {
                ActivateItemImmediate(nextItem, false);
                EnsureSwordTrailEffect(nextItem);
                ApplyHolsteredItemPresentation(nextItem);
                _isSwordTransitioning = false;
                _pendingSwordTransition = null;
                StartSwordEquipTransition(nextItem);
                yield break; }

            ActivateItemImmediate(nextItem); }

        _isSwordTransitioning = false;
        _pendingSwordTransition = null;
        RefreshInactiveItemPresentations(); }
    private IEnumerator PlaySwordEquipTransition(Item swordItem) {
        _isSwordTransitioning = true;
        ApplyHolsteredItemPresentation(swordItem);

        ActionScript resolvedActionScript = ResolveActionScript();
        bool playedEquip = resolvedActionScript != null && resolvedActionScript.TryEquipSword();
        if (playedEquip && resolvedActionScript != null) {
            yield return WaitForSwordStateToFinish(
                resolvedActionScript,
                resolvedActionScript.swordEquipStateName); }

        if (item == swordItem) { ApplyActiveItemPresentation(swordItem); } else { ApplyInactiveItemPresentation(swordItem); }

        _isSwordTransitioning = false;
        _pendingSwordTransition = null;
        RefreshInactiveItemPresentations(); }
    private static IEnumerator WaitForSwordAnimationLockToFinish(ActionScript actionScript) { if (actionScript == null) { yield break; }

        float timeoutAt = Time.time + Mathf.Max(0.2f, actionScript.GetRemainingGameplayInputLockSeconds() + 0.25f);
        while (actionScript.IsGameplayInputLocked() && Time.time < timeoutAt) { yield return null; } }
    private static IEnumerator WaitForSwordStateToFinish(ActionScript actionScript, string stateName) { if (actionScript == null || string.IsNullOrWhiteSpace(stateName)) { yield break; }

        Animator animator = ResolveCharacterAnimator(actionScript);
        if (!TryGetBaseLayerIndex(actionScript, animator, out int layerIndex)) {
            yield return WaitForSwordAnimationLockToFinish(actionScript);
            yield break; }

        float timeoutAt = Time.time + Mathf.Max(0.5f, actionScript.GetRemainingGameplayInputLockSeconds() + 1.5f);
        bool enteredTargetState = false;

        while (Time.time < timeoutAt) {
            if (TryGetAnimatorStateInfo(animator, layerIndex, actionScript.baseLayerName, stateName, out _)) {
                enteredTargetState = true;
                yield return null;
                continue; }

            if (enteredTargetState) { yield break; }

            if (!actionScript.IsGameplayInputLocked()) { break; }

            yield return null; }

        if (!enteredTargetState) { yield return WaitForSwordAnimationLockToFinish(actionScript); } }
    private static Animator ResolveCharacterAnimator(ActionScript actionScript) { if (actionScript == null) { return null; }

        if (actionScript.movementAnimationScript != null &&
            actionScript.movementAnimationScript.animator != null) { return actionScript.movementAnimationScript.animator; }

        if (actionScript.swordAnimationScript != null &&
            actionScript.swordAnimationScript.animator != null) { return actionScript.swordAnimationScript.animator; }

        if (actionScript.pickaxeAnimationScript != null &&
            actionScript.pickaxeAnimationScript.animator != null) { return actionScript.pickaxeAnimationScript.animator; }

        return null; }
    private static bool TryGetBaseLayerIndex(ActionScript actionScript, Animator animator, out int layerIndex) {
        layerIndex = -1;
        if (animator == null || animator.layerCount <= 0) { return false; }

        if (actionScript != null && !string.IsNullOrWhiteSpace(actionScript.baseLayerName)) { layerIndex = animator.GetLayerIndex(actionScript.baseLayerName); }

        if (layerIndex < 0) { layerIndex = 0; }

        return layerIndex >= 0 && layerIndex < animator.layerCount; }
    private static bool TryGetAnimatorStateInfo(
        Animator animator,
        int layerIndex,
        string baseLayerName,
        string stateName,
        out AnimatorStateInfo stateInfo) {
        stateInfo = default;
        if (animator == null || string.IsNullOrWhiteSpace(stateName)) { return false; }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (MatchesStateName(current, stateName, baseLayerName)) {
            stateInfo = current;
            return true; }

        if (!animator.IsInTransition(layerIndex)) { return false; }

        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        if (!MatchesStateName(next, stateName, baseLayerName)) { return false; }

        stateInfo = next;
        return true; }
    private static bool MatchesStateName(AnimatorStateInfo state, string stateName, string baseLayerName) { if (string.IsNullOrWhiteSpace(stateName)) { return false; }

        if (state.IsName(stateName)) { return true; }

        if (!string.IsNullOrWhiteSpace(baseLayerName) &&
            state.IsName($"{baseLayerName}.{stateName}")) { return true; }

        return false; }
    private void ActivateItemImmediate(Item targetItem, bool applyDrawnPresentation = true) { if (targetItem == null) { return; }

        currentitemid = targetItem.ID;
        currentitemname = targetItem.name;
        item = targetItem;

        if (applyDrawnPresentation) {
            ApplyActiveItemPresentation(targetItem);
            return; }

        SetItemObjectVisible(targetItem, true); }
    private void ClearCurrentSelection() {
        item = null;
        currentitemid = 0;
        currentitemname = string.Empty; }
    private void RefreshItemPresentations() { if (item != null && !_isSwordTransitioning) { ApplyActiveItemPresentation(item); }

        RefreshInactiveItemPresentations(); }
    private void RefreshInactiveItemPresentations() {
        for (int i = 0; i < items.Count; i++) {
            Item candidate = items[i];
            if (candidate == null || candidate == item) { continue; }

            ApplyInactiveItemPresentation(candidate); } }
    private void ApplyActiveItemPresentation(Item targetItem) { if (targetItem == null) { return; }

        targetItem.ApplyDrawnPresentation();
        SetItemObjectVisible(targetItem, true); }
    private void ApplyHolsteredItemPresentation(Item targetItem) { if (targetItem == null) { return; }

        targetItem.ApplyHolsteredPresentation();
        SetItemObjectVisible(targetItem, true); }
    private void ApplyInactiveItemPresentation(Item targetItem) {
        if (ShouldShowItemHolstered(targetItem)) {
            ApplyHolsteredItemPresentation(targetItem);
            return; }

        SetItemObjectVisible(targetItem, false); }
    private bool ShouldShowItemHolstered(Item targetItem) { if (targetItem == null || !targetItem.ShouldRemainVisibleWhenHolstered()) { return false; }

        if (!requireWeaponSlotAssignment) { return true; }

        string normalized = NormalizeItemName(targetItem.name);
        return !string.IsNullOrEmpty(normalized) && equippedItemNames.Contains(normalized); }
    private bool IsSwordItem(Item candidate) { if (candidate == null) { return false; }

        if (Sword.TryResolve(candidate, out _)) { return true; }

        return string.Equals(
            MapCommonWeaponName(candidate.name),
            "Sword",
            System.StringComparison.OrdinalIgnoreCase); }
    private static string NormalizeItemName(string rawName) { if (string.IsNullOrWhiteSpace(rawName)) { return string.Empty; }

        string normalized = rawName.Trim();
        if (normalized.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase)) { normalized = normalized.Substring(0, normalized.Length - "(Clone)".Length).Trim(); }

        return normalized; }
    private bool TryResolveItemByName(string itemNameToResolve, out Item resolvedItem) {
        resolvedItem = null;
        string normalized = NormalizeItemName(itemNameToResolve);
        if (string.IsNullOrEmpty(normalized)) { return false; }

        for (int i = 0; i < items.Count; i++) {
            Item candidate = items[i];
            if (candidate == null) { continue; }

            if (!string.Equals(NormalizeItemName(candidate.name), normalized, System.StringComparison.OrdinalIgnoreCase)) { continue; }

            resolvedItem = candidate;
            return true; }

        return false; }
    private ActionScript ResolveActionScript() { if (_actionScript != null) { return _actionScript; }

        _actionScript = GetComponent<ActionScript>();
        if (_actionScript != null) { return _actionScript; }

        _actionScript = GetComponentInParent<ActionScript>();
        if (_actionScript != null) { return _actionScript; }

        _actionScript = UnitySceneSearch.FindFirst<ActionScript>();

        return _actionScript; }
    private bool ShouldReserveSwordSpecialHotkey(KeyCode key) {
        if (key != KeyCode.Alpha3 &&
            key != KeyCode.Alpha4 &&
            key != KeyCode.Alpha5 &&
            key != GameSettings.GetKey(GameSettings.Key.SwordSpecial1, KeyCode.Alpha3) &&
            key != GameSettings.GetKey(GameSettings.Key.SwordSpecial2, KeyCode.Alpha4) &&
            key != GameSettings.GetKey(GameSettings.Key.SwordSpecial3, KeyCode.Alpha5)) { return false; }

        return IsSwordEquipped(); }
    public bool IsSwordEquipped() { if (TryGetEquippedSword(out _)) { return true; }

        string equippedName = NormalizeItemName(currentitemname);
        if (string.IsNullOrEmpty(equippedName) && item != null) { equippedName = NormalizeItemName(item.name); }

        if (!string.IsNullOrEmpty(equippedName)) {
            string mappedName = MapCommonWeaponName(equippedName);
            if (!string.IsNullOrEmpty(mappedName)) { return string.Equals(mappedName, "Sword", System.StringComparison.OrdinalIgnoreCase); }

            return string.Equals(equippedName, "Sword", System.StringComparison.OrdinalIgnoreCase); }

        return currentitemid == 3; }
    private static string MapCommonWeaponName(string rawName) {
        string normalized = NormalizeItemName(rawName);
        if (string.IsNullOrEmpty(normalized)) { return string.Empty; }

        string token = normalized.Replace(" ", string.Empty).ToLowerInvariant();
        if (token.Contains("sword")) { return "Sword"; }

        if (token.Contains("pickaxe") || token.Contains("pick")) { return "Pickaxe"; }

        if (token.Contains("axe")) { return "Axe"; }

        return string.Empty; } }
