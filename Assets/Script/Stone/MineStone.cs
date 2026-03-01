using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Controls Mine Stone behavior.
public class MineStone : MonoBehaviour
{
    private static InfoHandler cachedInfoHandler;
    private static GetRandomOreType cachedRandomOreType;

    public string type;
    public Material greymat;
    public string texttoshow;
    public Sprite sprite;
    public InfoHandler infoHandler;
    public List<GameObject> parts = new List<GameObject>();
    public GameObject fullstone;
    public List<int> chosen = new List<int>();
    public Material blackmaterial;
    public int counter = 8;
    public GameObject[] mainstoneparts;
    [SerializeField] private float destroyDelaySeconds = 1f;
    [SerializeField] private float rebuildDelaySeconds = 30f;
    public InventoryItem inventoryItem;
    public List<GameObject> orelist = new List<GameObject>();
    public GetRandomOreType getRandomOreType;
    public string currentore;
    private Ore selectedOre;
    private int initialCounter;
    private bool isRebuilding;
    private readonly List<MeshCollider> cachedMainStoneColliders = new List<MeshCollider>();
    private readonly List<bool> cachedMainStoneColliderConvex = new List<bool>();

    // Initialize references before gameplay starts.
    private void Awake()
    {
        ResolveReferences();
    }

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
        }
    }

    void Start()
    {
        ResolveReferences();
        initialCounter = counter;
        CacheMainStoneColliderDefaults();
        InitializeStoneState();
    }

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (infoHandler == null)
        {
            if (cachedInfoHandler == null)
            {
                cachedInfoHandler = FindInfoHandlerInScene();
            }

            infoHandler = cachedInfoHandler;
        }
        else
        {
            cachedInfoHandler = infoHandler;
        }

        if (getRandomOreType == null)
        {
            if (cachedRandomOreType == null)
            {
                cachedRandomOreType = FindOreTypeProviderInScene();
            }

            getRandomOreType = cachedRandomOreType;
        }
        else
        {
            cachedRandomOreType = getRandomOreType;
        }

        if (inventoryItem == null)
        {
            inventoryItem = GetComponent<InventoryItem>();
        }

        if (inventoryItem != null)
        {
            inventoryItem.ResolveReferences();
        }
    }

    // Handle Find Info Handler In Scene.
    private static InfoHandler FindInfoHandlerInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<InfoHandler>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<InfoHandler>(true);
#endif
    }

    // Handle Find Ore Type Provider In Scene.
    private static GetRandomOreType FindOreTypeProviderInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<GetRandomOreType>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<GetRandomOreType>(true);
#endif
    }

    // Handle Initialize Stone State.
    private void InitializeStoneState()
    {
        foreach (GameObject part in parts)
        {
            MeshRenderer meshRenderer = part.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = greymat;
            }
        }

        ApplyOreSelectionAndVisuals();
    }

    // Handle Apply Ore Selection And Visuals.
    private void ApplyOreSelectionAndVisuals()
    {
        ResolveReferences();

        if (getRandomOreType == null)
        {
            Debug.LogWarning($"{name}: Missing GetRandomOreType reference.", this);
            selectedOre = null;
            currentore = "noore";

            if (inventoryItem != null)
            {
                inventoryItem.name = "stone";
                inventoryItem.inventorysprite = sprite;
            }
            return;
        }

        selectedOre = getRandomOreType.GetOreType();
        currentore = selectedOre != null ? selectedOre.oreName : "noore";

        if (inventoryItem != null)
        {
            inventoryItem.name = GetInventoryItemName(selectedOre);
            inventoryItem.inventorysprite = selectedOre != null && selectedOre.sprite != null ? selectedOre.sprite : sprite;
        }

        texttoshow = $"{ToDisplayName(GetInventoryItemName(selectedOre))} gained";

        foreach (GameObject ore in orelist)
        {
            MeshRenderer meshRenderer = ore.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                continue;
            }

            if (selectedOre == null || selectedOre.oreName == "noore" || selectedOre.material == null)
            {
                meshRenderer.enabled = false;
            }
            else
            {
                meshRenderer.enabled = true;
                meshRenderer.material = selectedOre.material;
            }
        }
    }
// Handle Mine.
    public void Mine()
    {
        ResolveReferences();
        Debug.Log("mine");
        if (isRebuilding)
        {
            return;
        }

        if (counter > 0)
        {
            counter--;

            if (parts == null || parts.Count == 0)
            {
                Debug.LogWarning($"{name}: Parts list is empty.", this);
                return;
            }

            if (chosen.Count < parts.Count)
            {
                int value = Random.Range(0, parts.Count);
                while (chosen.Contains(value))
                {
                    value = Random.Range(0, parts.Count);
                }

                chosen.Add(value);
                MeshRenderer renderer = parts[value].GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = blackmaterial;
                }
            }
            else
            {
                // Prevent lockup if there are fewer visual parts than hit counter.
                counter = 0;
            }

            if (counter == 0)
            {
                SetMainStoneCollidersConvex(true);
                StartCoroutine(HandleStoneBreakAndRebuild());
                if (inventoryItem != null && inventoryItem.slotManager != null)
                {
                    inventoryItem.slotManager.AddItem(inventoryItem);
                }
                else
                {
                    Debug.LogWarning($"{name}: Missing InventoryItem or SlotManager reference.", this);
                }

                if (infoHandler != null)
                {
                    Sprite infoSprite = inventoryItem != null && inventoryItem.inventorysprite != null ? inventoryItem.inventorysprite : sprite;
                    infoHandler.ShowInfoNow(texttoshow, infoSprite);
                }
                else
                {
                    Debug.LogWarning($"{name}: Missing InfoHandler reference.", this);
                }
            }
        }
    }

    // Handle Handle Stone Break And Rebuild.
    private IEnumerator HandleStoneBreakAndRebuild()
    {
        isRebuilding = true;
        yield return new WaitForSeconds(destroyDelaySeconds);
        SetStoneVisible(false);
        yield return new WaitForSeconds(rebuildDelaySeconds);
        RebuildStone();
        isRebuilding = false;
    }

    // Handle Rebuild Stone.
    private void RebuildStone()
    {
        counter = initialCounter;
        chosen.Clear();
        SetStoneVisible(true);
        RestoreMainStoneColliderDefaults();
        InitializeStoneState();
    }

    // Handle Set Stone Visible.
    private void SetStoneVisible(bool visible)
    {
        if (fullstone == null)
        {
            return;
        }

        if (fullstone != gameObject)
        {
            fullstone.SetActive(visible);
            return;
        }

        Renderer[] renderers = fullstone.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }

        Collider[] colliders = fullstone.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = visible;
            }
        }
    }

    // Handle Cache Main Stone Collider Defaults.
    private void CacheMainStoneColliderDefaults()
    {
        cachedMainStoneColliders.Clear();
        cachedMainStoneColliderConvex.Clear();

        if (mainstoneparts == null)
        {
            return;
        }

        for (int i = 0; i < mainstoneparts.Length; i++)
        {
            GameObject stonePart = mainstoneparts[i];
            if (stonePart == null)
            {
                continue;
            }

            MeshCollider meshCollider = stonePart.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                continue;
            }

            cachedMainStoneColliders.Add(meshCollider);
            cachedMainStoneColliderConvex.Add(meshCollider.convex);
        }
    }

    // Handle Restore Main Stone Collider Defaults.
    private void RestoreMainStoneColliderDefaults()
    {
        for (int i = 0; i < cachedMainStoneColliders.Count; i++)
        {
            if (cachedMainStoneColliders[i] != null)
            {
                cachedMainStoneColliders[i].convex = cachedMainStoneColliderConvex[i];
            }
        }
    }

    // Handle Set Main Stone Colliders Convex.
    private void SetMainStoneCollidersConvex(bool convex)
    {
        for (int i = 0; i < cachedMainStoneColliders.Count; i++)
        {
            if (cachedMainStoneColliders[i] != null)
            {
                cachedMainStoneColliders[i].convex = convex;
            }
        }
    }

    // Handle Get Inventory Item Name.
    private string GetInventoryItemName(Ore ore)
    {
        if (ore == null || ore.oreName == "noore")
        {
            return "stone";
        }

        return ore.oreName;
    }

    // Handle To Display Name.
    private string ToDisplayName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return "Stone";
        }

        string normalized = itemName.Replace('_', ' ');
        string[] parts = normalized.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
            {
                continue;
            }

            string lower = parts[i].ToLowerInvariant();
            parts[i] = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        return string.Join(" ", parts);
    }
}

