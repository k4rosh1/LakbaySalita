using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TrophyManager : MonoBehaviour
{
    [Header("Trophy UI Images")]
    public Image trophyLevel1;
    public Image trophyLevel2;
    public Image trophyLevel3;
    public Image trophy500Points;
    public Image trophy1000Points;

    [Header("Sprites")]
    public Sprite badgeLevel1;
    public Sprite badgeLevel2;
    public Sprite badgeLevel3;
    public Sprite badge500Points;
    public Sprite badge1000Points;

    private void Start()
    {
        CheckAchievements();
    }

    public void CheckAchievements()
    {
        // Get currently selected character
        int selectedCharacter = PlayerPrefs.GetInt("selectedOption", 0);

        // Check this character's unlocked level
        string unlockKey = "UnlockedLevel_" + selectedCharacter;
        int unlockedLevel = PlayerPrefs.GetInt(unlockKey, 1);

        // Check this character's total points
        string pointsKey = "TotalPoints_" + selectedCharacter;
        int totalPoints = PlayerPrefs.GetInt(pointsKey, 0);

        // Achievement 1: Finish Level 1 (Means Level 2 is unlocked)
        SetTrophyState(trophyLevel1, badgeLevel1, unlockedLevel >= 2);

        // Achievement 2: Finish Level 2 (Means Level 3 is unlocked)
        SetTrophyState(trophyLevel2, badgeLevel2, unlockedLevel >= 3);

        // Achievement 3: Finish Level 3 (Means Level 4 is unlocked)
        SetTrophyState(trophyLevel3, badgeLevel3, unlockedLevel >= 4);

        // Achievement 4: Gain 500 Points
        SetTrophyState(trophy500Points, badge500Points, totalPoints >= 500);

        // Achievement 5: Gain 1000 Points
        SetTrophyState(trophy1000Points, badge1000Points, totalPoints >= 1000);
    }

    private void SetTrophyState(Image trophyImage, Sprite badgeSprite, bool isUnlocked)
    {
        if (trophyImage != null)
        {
            trophyImage.sprite = badgeSprite;

            if (isUnlocked)
            {
                // Full color for unlocked
                trophyImage.color = Color.white;
            }
            else
            {
                // Silhouette / darkened for locked
                trophyImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }
    }

    public void OnBackButtonClicked()
    {
        SceneManager.LoadScene("Character Selection");
    }
}
