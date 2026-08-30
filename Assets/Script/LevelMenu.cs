using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelMenu : MonoBehaviour
{
    public Button[] buttons;

    private void Awake()
    {
        // Per-character unlock: each character has their own progress
        int selectedCharacter = PlayerPrefs.GetInt("selectedOption", 0);
        string unlockKey = "UnlockedLevel_" + selectedCharacter;
        int unlockedLevel = PlayerPrefs.GetInt(unlockKey, 1);

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = (i < unlockedLevel);
        }
    }

    public void OpenLevel(int levelId)
    {
        string levelName = "Level " + levelId;
        SceneManager.LoadScene(levelName);
    }

    // Apply a button to reset progress 
    public void ResetProgress()
    {
        // Reset progress for ALL characters
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.DeleteKey("UnlockedLevel_" + i);
        }
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}