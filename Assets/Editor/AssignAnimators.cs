using UnityEngine;
using UnityEditor;

public class AssignAnimators
{
    [MenuItem("Tools/Assign Animators")]
    public static void Assign()
    {
        string[] guids = AssetDatabase.FindAssets("t:CharacterDatabase");
        if (guids.Length == 0) 
        {
            Debug.LogError("No CharacterDatabase found!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        CharacterDatabase db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(path);

        if (db == null) return;

        int updated = 0;
        foreach (var c in db.character)
        {
            if (string.IsNullOrEmpty(c.characterName)) continue;
            
            string[] cGuids = AssetDatabase.FindAssets(c.characterName + "Controller t:AnimatorController");
            if (cGuids.Length > 0) {
                c.characterAnimator = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(cGuids[0]));
                updated++;
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log("Updated " + updated + " characters in " + db.name);
    }
}
