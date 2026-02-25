using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MineStone : MonoBehaviour
{
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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialCounter = counter;
        CacheMainStoneColliderDefaults();
        InitializeStoneState();
    }

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

    private void ApplyOreSelectionAndVisuals()
    {
        if (getRandomOreType == null)
        {
            Debug.LogError($"{name}: Missing GetRandomOreType reference.", this);
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

    // Update is called once per frame
    void Update()
    {

    }
    public void Mine()
    {
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

    private IEnumerator HandleStoneBreakAndRebuild()
    {
        isRebuilding = true;
        yield return new WaitForSeconds(destroyDelaySeconds);
        SetStoneVisible(false);
        yield return new WaitForSeconds(rebuildDelaySeconds);
        RebuildStone();
        isRebuilding = false;
    }

    private void RebuildStone()
    {
        counter = initialCounter;
        chosen.Clear();
        SetStoneVisible(true);
        RestoreMainStoneColliderDefaults();
        InitializeStoneState();
    }

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

    private string GetInventoryItemName(Ore ore)
    {
        if (ore == null || ore.oreName == "noore")
        {
            return "stone";
        }

        return ore.oreName;
    }

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
