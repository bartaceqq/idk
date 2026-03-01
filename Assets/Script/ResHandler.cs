using UnityEngine;

// Controls Force Full HD behavior.
public class ForceFullHD : MonoBehaviour
{
    void Awake()
    {
        Screen.SetResolution(1920, 1080, FullScreenMode.ExclusiveFullScreen, 60);
    }
}
