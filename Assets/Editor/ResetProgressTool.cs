using UnityEngine;
using UnityEditor;

public class ResetProgressTool
{
    [MenuItem("Tools/Reset All Game Progress")]
    public static void ResetAllProgress()
    {
        // Ask for confirmation to prevent accidental clicks
        bool confirm = EditorUtility.DisplayDialog("Reset Progress",
            "Are you sure you want to delete ALL saved game data? This will reset all unlocked levels for all characters.",
            "Yes, Reset Everything", "Cancel");

        if (confirm)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("<b>[Lakbay Salita]</b> All game progress has been completely reset!");
        }
    }
}
