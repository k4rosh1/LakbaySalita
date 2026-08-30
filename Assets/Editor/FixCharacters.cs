using UnityEngine;
using UnityEditor;

public class FixCharacters
{
    [MenuItem("Tools/Fix Characters")]
    public static void Fix()
    {
        // 1. Fix Pivots (Set them to Bottom Center so they stop sinking into the ground)
        string[] aseGuids = AssetDatabase.FindAssets("t:DefaultAsset", new[] { "Assets/Sprites/Character Sprites" });
        foreach (var guid in aseGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".aseprite"))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null)
                {
                    var so = new SerializedObject(importer);
                    var alignmentProp = so.FindProperty("m_AsepriteImporterSettings.spriteAlignment");
                    if (alignmentProp != null)
                    {
                        // 7 is BottomCenter. This lifts the character out of the ground!
                        alignmentProp.intValue = 7;
                    }
                    so.ApplyModifiedProperties();
                    importer.SaveAndReimport();
                }
            }
        }

        // 2. Fix Database Animators (Using exact paths because FindAssets failed on names with parentheses)
        string[] dbGuids = AssetDatabase.FindAssets("t:CharacterDatabase");
        if (dbGuids.Length > 0)
        {
            var db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(AssetDatabase.GUIDToAssetPath(dbGuids[0]));
            if (db != null)
            {
                int count = 0;
                foreach (var c in db.character)
                {
                    if (string.IsNullOrEmpty(c.characterName)) continue;
                    
                    string ctrlPath = "Assets/Animations/" + c.characterName + "/" + c.characterName + "Controller.controller";
                    var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
                    if (ctrl != null) 
                    {
                        c.characterAnimator = ctrl;
                        count++;
                    }
                }
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                Debug.Log("Successfully assigned " + count + " animators to the database!");
            }
        }
        
        Debug.Log("Character Pivots and Animators Fixed! You can now test the game.");
    }
}
