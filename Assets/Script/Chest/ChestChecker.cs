using TMPro;
using UnityEngine;

public class ChestChecker : MonoBehaviour
{
   
    public GameObject player;
    public TMP_Text text;
    public Animator animator;
    public float range = 10;
    public KeyCode pressEkey;
    public ChestItemGenerator chestItemGenerator;
    public string type;
    public bool looted = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(pressEkey))
        {
            if(CheckDistance() < range && !looted)
            {
                animator.SetTrigger("OpenChest");
                StartCoroutine(chestItemGenerator.WaitFiveSeconds(type));
                looted = true;
            }
        }
        HandleProcess();
       
    }
    public void OpenChestAnimation()
    {
        
    }
    public float CheckDistance()
    {
        float d = Vector3.Distance(player.transform.position, this.gameObject.transform.position);
        return d;
        

    }
    public void HandleProcess()
    {
        if(CheckDistance() < range)
        {
            
            text.enabled = true;
            
        }
        else
        {
            
            text.enabled = false;
        }
    }
    
}
