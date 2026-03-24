using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHandler : MonoBehaviour
{   
    public InventoryAddHandler inventoryAddHandler;
    public InventoryItem inventoryItem;
    public int placeinlist = 0;
    public int idk;
    private static PhysicsMaterial fallbackNoRollMaterial;
    public List<Material> materials = new List<Material>();
    
    [Header("Parts")]
    public float timetowaittodestroy = 8f;
    public GameObject toppart;
    public GameObject bottompart;
    public int counttochop = 3;
    [Header("Chop")]
    [SerializeField] private int chopsToFall = 4;
    [SerializeField] private float hitCooldownSeconds = 0.12f;
    public GameObject chopImpactParticlePrefab;
    [SerializeField, Range(0f, 1f)] private float chopImpactBetweenFactor = 0.35f;
    [SerializeField] private Vector3 chopImpactOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private bool flattenImpactFacingToHorizontal = true;
    [SerializeField] private float destroyImpactParticleAfterSeconds = 3f;

   
    private int chopCount;
    private float nextChopAllowedTime;
    private bool hasFallen;

    public void Start()
    {
         GenerateMaterial();
    }
    private void OnValidate()
    {
       
    }
    public void GenerateMaterial()
    {
        MeshRenderer renderer = toppart.GetComponent<MeshRenderer>();
        Material[] rendmatlist = (Material[])renderer.materials.Clone();
        rendmatlist[placeinlist] = materials[Random.Range(0, materials.Count)];
        renderer.materials = rendmatlist;

        
    }
    public void Chop()
    {
        Chop(null);
    }

    public void Chop(Transform attacker)
    {
        if(counttochop >0){
        SpawnChopImpact(attacker);
        counttochop--;
        }
        else
        {
            inventoryAddHandler.AddItemToInventory(inventoryItem);
            TreeFall();
            StartCoroutine(destroyaftertime());
            
        }
       
    }
    public IEnumerator destroyaftertime()
    {
         yield return new WaitForSeconds(timetowaittodestroy);
         Destroy(toppart);
    }
public void TreeFall()
    {
        toppart.transform.rotation = Quaternion.Euler(-91f,0f,0f);
        toppart.AddComponent<Rigidbody>();
    }
    private void SpawnChopImpact(Transform attacker)
    {
        if (chopImpactParticlePrefab == null)
        {
            return;
        }

        Vector3 treeImpactPoint = ResolveTreeImpactPoint();
        Vector3 attackerPosition = ResolveAttackerPosition(attacker, treeImpactPoint);
        Vector3 spawnPosition = Vector3.Lerp(treeImpactPoint, attackerPosition, Mathf.Clamp01(chopImpactBetweenFactor)) + chopImpactOffset;

        Vector3 lookDirection = attackerPosition - spawnPosition;
        if (flattenImpactFacingToHorizontal)
        {
            lookDirection.y = 0f;
        }

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            lookDirection = attackerPosition - treeImpactPoint;
            if (flattenImpactFacingToHorizontal)
            {
                lookDirection.y = 0f;
            }
        }

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            lookDirection = transform.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        GameObject impactInstance = Instantiate(chopImpactParticlePrefab, spawnPosition, rotation);

        float destroyDelay = Mathf.Max(0f, destroyImpactParticleAfterSeconds);
        if (impactInstance != null && destroyDelay > 0f)
        {
            Destroy(impactInstance, destroyDelay);
        }
    }

    private Vector3 ResolveTreeImpactPoint()
    {
        if (bottompart != null && TryGetBounds(bottompart.transform, out Bounds bottomBounds))
        {
            float impactY = Mathf.Lerp(bottomBounds.min.y, bottomBounds.max.y, 0.55f);
            return new Vector3(bottomBounds.center.x, impactY, bottomBounds.center.z);
        }

        return transform.position + Vector3.up * 0.5f;
    }

    private static Vector3 ResolveAttackerPosition(Transform attacker, Vector3 fallbackTreePoint)
    {
        if (attacker != null)
        {
            return attacker.position;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return fallbackTreePoint + Vector3.forward;
    }

    private static bool TryGetBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        bool hasBounds = false;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer currentRenderer = renderers[i];
            if (currentRenderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = currentRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(currentRenderer.bounds);
            }
        }

        if (hasBounds)
        {
            return true;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = currentCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(currentCollider.bounds);
            }
        }

        return hasBounds;
    }

}
