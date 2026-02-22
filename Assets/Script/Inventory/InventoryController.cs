using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    public bool UIshown = false;
    public KeyCode keycode;
    public GameObject inventoryobject;

    void Start()
    {
        
        ApplyUIState();
       
    }

    void Update()
    {
        if (Input.GetKeyDown(keycode))
        {
            UIshown = !UIshown;
            ApplyUIState();
        }
    }

    private void ApplyUIState()
    {
        if (inventoryobject == null)
        {
            Debug.LogWarning("InventoryController: inventoryobject is not assigned.", this);
            return;
        }

        Image[] images = inventoryobject.GetComponentsInChildren<Image>(true);
        TMP_Text[] tmpTexts = inventoryobject.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < images.Length; i++)
        {
            images[i].enabled = UIshown;
        }

        for (int i = 0; i < tmpTexts.Length; i++)
        {
            tmpTexts[i].enabled = UIshown;
        }
    }
}
