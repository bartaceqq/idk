using UnityEngine;

public class TreeTest : MonoBehaviour {
    public GameObject treechopped;
    void Update() {
        if (Input.GetKeyDown(KeyCode.E)) {
            Instantiate(treechopped, this.gameObject.transform.position, this.gameObject.transform.rotation);
            Destroy(this); } } }
