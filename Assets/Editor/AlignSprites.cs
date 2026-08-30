using UnityEngine;
using UnityEditor;

public class AlignSprites
{
    [MenuItem("Tools/Align All Characters")]
    public static void Align()
    {
        // 1. Fix Albularyo (PNG)
        string albPath = "Assets/Sprites/Character PNG/Albularyo.png";
        TextureImporter texImporter = AssetImporter.GetAtPath(albPath) as TextureImporter;
        if (texImporter != null)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            texImporter.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // A pivot Y of 0.2 pushes the sprite DOWN (closer to the ground)
            settings.spritePivot = new Vector2(0.5f, 0.4f);
            texImporter.SetTextureSettings(settings);
            texImporter.SaveAndReimport();
        }

        // 2. Fix Aseprite characters (skip Arkero since it's already good)
        string[] allGuids = AssetDatabase.FindAssets("", new[] { "Assets/Sprites/Character Sprites" });
        foreach (var guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".aseprite") && !path.Contains("Arkero"))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null)
                {
                    var so = new SerializedObject(importer);
                    var alignmentProp = so.FindProperty("m_AsepriteImporterSettings.spriteAlignment");
                    var pivotProp = so.FindProperty("m_AsepriteImporterSettings.customPivotPosition");
                    
                    if (alignmentProp != null && pivotProp != null)
                    {
                        alignmentProp.intValue = 9; // Custom alignment
                        pivotProp.vector2Value = new Vector2(0.5f, 0.4f); // Push down by 40%
                    }
                    so.ApplyModifiedProperties();
                    importer.SaveAndReimport();
                }
            }
        }
        
        Debug.Log("All character pivots pushed down to match Arkero's height!");
    }
}
