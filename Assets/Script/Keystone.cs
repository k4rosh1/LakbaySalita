using UnityEngine;
using UnityEngine.SceneManagement;

public class Keystone : MonoBehaviour
{
    private bool collected = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        // Check if the player touched the keystone
        SelectedCharacter player = other.GetComponent<SelectedCharacter>();
        if (player == null) player = other.GetComponentInParent<SelectedCharacter>();

        if (player != null)
        {
            collected = true;

            // Unlock the next level for THIS character only
            int currentLevelNumber = GetCurrentLevelNumber();
            int selectedCharacter = PlayerPrefs.GetInt("selectedOption", 0);
            string unlockKey = "UnlockedLevel_" + selectedCharacter;
            int currentUnlocked = PlayerPrefs.GetInt(unlockKey, 1);

            if (currentLevelNumber + 1 > currentUnlocked)
            {
                PlayerPrefs.SetInt(unlockKey, currentLevelNumber + 1);
                PlayerPrefs.Save();
            }

            // Tell Dialogue to show the Level Conquered screen
            Dialogue dialogue = FindObjectOfType<Dialogue>(true);
            if (dialogue != null)
            {
                dialogue.ShowLevelConquered();
            }

            // Destroy the keystone
            Destroy(gameObject);
        }
    }

    int GetCurrentLevelNumber()
    {
        // Extract the level number from the scene name (e.g. "Level 1" -> 1)
        string sceneName = SceneManager.GetActiveScene().name;
        string[] parts = sceneName.Split(' ');
        if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int levelNum))
        {
            return levelNum;
        }
        return 1; // Default to level 1
    }
}
