using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void ClicktoMenu()
    {
        SceneManager.LoadSceneAsync(0);
    }

    public void ClicktoCharacterSelect()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void OpenTrophies()
    {
        SceneManager.LoadScene("Trophy Scene");
    }
}
