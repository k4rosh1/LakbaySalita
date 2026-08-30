using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupKeystone : EditorWindow
{
    [MenuItem("Tools/Setup Keystone Prefab")]
    public static void Setup()
    {
        // 1. Create animation folder
        if (!AssetDatabase.IsValidFolder("Assets/Animations/Keystone"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Keystone");
        }

        // 2. Load the sprite sheet frames
        string spritePath = "Assets/Sprites/UI/NEW UI/KEYSTONE-Sheet.png";
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        
        Sprite[] frames = new Sprite[5];
        int frameCount = 0;
        foreach (Object obj in sprites)
        {
            if (obj is Sprite sprite && sprite.name.StartsWith("KEYSTONE-Sheet_"))
            {
                // Extract the frame index from the name
                string indexStr = sprite.name.Replace("KEYSTONE-Sheet_", "");
                if (int.TryParse(indexStr, out int idx) && idx < 5)
                {
                    frames[idx] = sprite;
                    frameCount++;
                }
            }
        }

        if (frameCount == 0)
        {
            Debug.LogError("Could not find KEYSTONE-Sheet sprite frames!");
            return;
        }

        // 3. Create idle animation clip
        AnimationClip idleClip = new AnimationClip();
        idleClip.frameRate = 8;
        
        // Set loop
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(idleClip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(idleClip, settings);

        // Build keyframes for the sprite swap
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i / idleClip.frameRate;
            keyframes[i].value = frames[i];
        }

        AnimationUtility.SetObjectReferenceCurve(idleClip, spriteBinding, keyframes);
        AssetDatabase.CreateAsset(idleClip, "Assets/Animations/Keystone/Keystone_Idle.anim");

        // 4. Create animator controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(
            "Assets/Animations/Keystone/KeystoneController.controller");
        
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;
        rootStateMachine.defaultState = idleState;

        // 5. Create Keystone Prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefab"))
            AssetDatabase.CreateFolder("Assets", "Prefab");

        GameObject keystoneObj = new GameObject("Keystone");

        // SpriteRenderer
        SpriteRenderer sr = keystoneObj.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = 10; // On top of everything

        // Animator
        Animator anim = keystoneObj.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // BoxCollider2D as trigger for pickup
        BoxCollider2D col = keystoneObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 1.5f); // Generous pickup area

        // Keystone script
        keystoneObj.AddComponent<Keystone>();

        // Save as prefab
        string prefabPath = "Assets/Prefab/Keystone.prefab";
        PrefabUtility.SaveAsPrefabAsset(keystoneObj, prefabPath);
        DestroyImmediate(keystoneObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Keystone prefab created successfully at " + prefabPath);
    }
}
