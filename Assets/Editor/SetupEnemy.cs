using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Animation;
using System.Linq;

public class SetupEnemy
{
    [InitializeOnLoadMethod]
    public static void AutoSetup()
    {
        EditorApplication.delayCall += Setup;
    }

    [MenuItem("Tools/Setup Manananggal Enemy")]
    public static void Setup()
    {
        string prefabPath = "Assets/Prefab/Enemy.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            // Remove old Sprite Library / Resolver
            var library = prefab.GetComponent<SpriteLibrary>();
            if (library != null) Object.DestroyImmediate(library, true);
            var resolver = prefab.GetComponent<SpriteResolver>();
            if (resolver != null) Object.DestroyImmediate(resolver, true);
            
            // Add Animator
            var anim = prefab.GetComponent<Animator>();
            if (anim == null)
            {
                anim = prefab.AddComponent<Animator>();
            }

            // Assign Controller
            string controllerPath = "Assets/Animations/MANANANGGAL/MANANANGGALController.controller";
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller != null)
            {
                anim.runtimeAnimatorController = controller;
            }

            // Assign proper Manananggal Sprite to SpriteRenderer
            var sr = prefab.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                string spritePath = "Assets/Sprites/Character Sprites/MANANANGGAL.aseprite";
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
                Sprite firstFrame = null;
                foreach (var asset in assets)
                {
                    if (asset is Sprite sprite && sprite.name.Contains("Frame_0"))
                    {
                        firstFrame = sprite;
                        break;
                    }
                }
                
                if (firstFrame != null)
                {
                    sr.sprite = firstFrame;
                }
            }

            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log("Enemy Prefab automatically configured as Manananggal successfully!");
        }
        else
        {
            Debug.LogError("Could not find Enemy.prefab!");
        }
    }
}
