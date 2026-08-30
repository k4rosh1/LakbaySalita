using UnityEngine;
using UnityEngine.UI;

public class SkipDialogueToggleHandler : MonoBehaviour
{
    private Toggle toggle;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            // Set initial state from PlayerPrefs
            toggle.isOn = PlayerPrefs.GetInt("SkipDialogueEnabled", 0) == 1;

            // Listen for changes
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    void OnToggleValueChanged(bool isEnabled)
    {
        PlayerPrefs.SetInt("SkipDialogueEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Skip Dialogue " + (isEnabled ? "Enabled" : "Disabled"));
    }

    void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
}
