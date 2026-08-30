using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuAchievements : MonoBehaviour
{
    public void OpenTrophyScene()
    {
        SceneManager.LoadScene("Trophy Scene");
    }
}
