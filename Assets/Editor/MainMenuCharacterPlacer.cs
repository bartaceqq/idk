using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuCharacterPlacer
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string CharacterModelPath = "Assets/Animations/PrepReWork/Character.fbx";
    private const string CharacterAnimatorControllerPath = "Assets/Animations/PrepReWork/AnimsOnly/RemadeController.controller";
    private const string MenuCharacterName = "Menu Player Character";

    [MenuItem("Tools/One More Night/Place Sample Character In Main Menu")]
    public static void PlaceSampleCharacterInMainMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot place the menu character while Unity is entering or running Play Mode.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        GameObject character = PlaceCharacterInOpenScene();

        if (character == null)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Placed {MenuCharacterName} in {MenuScenePath}.");
    }

    public static GameObject PlaceCharacterInOpenScene()
    {
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath);
        if (modelPrefab == null)
        {
            Debug.LogError($"Could not find the SampleScene character model at {CharacterModelPath}.");
            return null;
        }

        RemoveExistingMenuCharacter();

        GameObject character = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
        if (character == null)
        {
            character = Object.Instantiate(modelPrefab);
        }

        character.name = MenuCharacterName;

        Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        Vector3 position = ResolveViewportGroundPosition(camera, new Vector3(0.78f, 0.43f, 0f));
        character.transform.SetPositionAndRotation(position, ResolveFacingRotation(position, camera));
        character.transform.localScale = Vector3.one * 1.9f;

        ConfigureAnimator(character);
        DisableRuntimePhysics(character);
        ConfigureRenderers(character);

        EditorUtility.SetDirty(character);
        return character;
    }

    private static void RemoveExistingMenuCharacter()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject sceneObject in allObjects)
        {
            if (sceneObject == null || sceneObject.scene != activeScene || sceneObject.name != MenuCharacterName)
            {
                continue;
            }

            Object.DestroyImmediate(sceneObject);
        }
    }

    private static Vector3 ResolveViewportGroundPosition(Camera camera, Vector3 viewportPoint)
    {
        Vector3 fallback = AlignToGround(new Vector3(1.35f, 0f, 1.15f));
        if (camera == null)
        {
            return fallback;
        }

        Ray ray = camera.ViewportPointToRay(viewportPoint);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float distance))
        {
            return fallback;
        }

        Vector3 position = ray.GetPoint(distance);
        if (Vector3.Distance(camera.transform.position, position) < 1f || Vector3.Distance(camera.transform.position, position) > 25f)
        {
            return fallback;
        }

        return AlignToGround(position);
    }

    private static Vector3 AlignToGround(Vector3 position)
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain != null)
        {
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
        }
        else
        {
            position.y = 0f;
        }

        return position + (Vector3.up * 0.03f);
    }

    private static Quaternion ResolveFacingRotation(Vector3 position, Camera camera)
    {
        if (camera == null)
        {
            return Quaternion.Euler(0f, -145f, 0f);
        }

        Vector3 direction = camera.transform.position - position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return Quaternion.Euler(0f, -145f, 0f);
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static void ConfigureAnimator(GameObject character)
    {
        Animator animator = character.GetComponent<Animator>();
        if (animator == null)
        {
            animator = character.AddComponent<Animator>();
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CharacterAnimatorControllerPath);
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
    }

    private static void DisableRuntimePhysics(GameObject character)
    {
        foreach (Collider collider in character.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Rigidbody rigidbody in character.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(rigidbody);
        }

        foreach (CharacterController controller in character.GetComponentsInChildren<CharacterController>(true))
        {
            Object.DestroyImmediate(controller);
        }
    }

    private static void ConfigureRenderers(GameObject character)
    {
        foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }
}
