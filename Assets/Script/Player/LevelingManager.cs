using UnityEngine;
using System.Collections.Generic;
public class LevelingManager : MonoBehaviour
{
    public int level;
    public XPHandler xpHandler;
    public int CurrentLevel
    {
        get
        {
            ResolveXPHandler();
            level = xpHandler != null ? xpHandler.GetCurrentLevel() : Mathf.Max(1, level);
            return level;
        }
    }
    void Start()
    {
        level = Mathf.Max(1, level);
        ResolveXPHandler();
        level = CurrentLevel;
    }
    void Update() { level = CurrentLevel; }
    private void ResolveXPHandler()
    {
        if (xpHandler == null) { xpHandler = UnitySceneSearch.FindFirst<XPHandler>(); }
    }
}
