using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
public class MineStone : MonoBehaviour
{
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void Mine()
    {
        if (counter > 0)
        {
            counter--;
            int value = Random.Range(0, 11);
            while(chosen.Contains(value)){
                value = Random.Range(0, 11);
            }
            chosen.Add(value);
            MeshRenderer renderer = parts[value].GetComponent<MeshRenderer>();
            renderer.material = blackmaterial;
            if (counter == 0)
            {
            
             foreach(GameObject objectik in mainstoneparts)
                        {
                            MeshCollider meshCollider = objectik.GetComponent<MeshCollider>();
                        meshCollider.convex = true;
                            Rigidbody rigidbody = objectik.GetComponent<Rigidbody>();
                            rigidbody.useGravity = true;
                        }
                 StartCoroutine(DestroyAfterSeconds(fullstone, destroyDelaySeconds));
                  infoHandler.texttoshow = this.texttoshow;       
                    infoHandler.toshowimage = this.sprite;
                        StartCoroutine(infoHandler.showinfo());
            }
        }
    }

 private IEnumerator DestroyAfterSeconds(GameObject objectik, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (objectik != null)
        {
            objectik.SetActive(false);
        }
    }
}