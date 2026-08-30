using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterManager : MonoBehaviour 
{

    public CharacterDatabase characterDB;
    
    public TextMeshProUGUI nameText;
    public Image artwork;

    private int selectedOption = 0;

    void Start()
    { 
        if(!PlayerPrefs.HasKey("selectedOption"))
        {
           selectedOption = 0;
        }

        else
        {
           Load();
        }
            UpdateCharacter(selectedOption);
    }
    public void NextOption()
    {
        selectedOption++;

        if(selectedOption >= characterDB.CharacterCount)
        { 
            selectedOption=0; 
        }

        UpdateCharacter(selectedOption);
        Save();
    }

    public void BackOption()
    {
        selectedOption--;

        if(selectedOption < 0)
        {
            selectedOption = characterDB.CharacterCount - 1;
        }
        UpdateCharacter(selectedOption);
        Save();
    }
    public void UpdateCharacter(int selectedOption)

    {
        Character character = characterDB.GetCharacter(selectedOption);
        artwork.sprite = character.characterSprite;
        nameText.text = character.characterName;
    }

    public void Load()
    {
        selectedOption = PlayerPrefs.GetInt("selectedOption");
    }

    public void Save()
    {
        PlayerPrefs.SetInt("selectedOption", selectedOption);
    }

    [Header("Reset Confirmation")]
    public GameObject confirmResetPanel;

    public void ShowResetConfirmation()
    {
        if (confirmResetPanel != null)
            confirmResetPanel.SetActive(true);
    }

    public void ConfirmReset()
    {
        // Reset this character's unlocked levels back to 1
        PlayerPrefs.SetInt("UnlockedLevel_" + selectedOption, 1);
        // Reset this character's points back to 0
        PlayerPrefs.SetInt("TotalPoints_" + selectedOption, 0);
        PlayerPrefs.Save();
        Debug.Log("Reset progress for character " + selectedOption);

        if (confirmResetPanel != null)
            confirmResetPanel.SetActive(false);
    }

    public void CancelReset()
    {
        if (confirmResetPanel != null)
            confirmResetPanel.SetActive(false);
    }

}
