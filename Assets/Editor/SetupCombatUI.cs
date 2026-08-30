using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEditor.SceneManagement;

public class SetupCombatUI : EditorWindow
{
    [MenuItem("Tools/Setup Combat UI")]
    public static void SetupUI()
    {
        // Force open Level 1 so we can find the Dialogue script!
        string scenePath = "Assets/Scenes/Level 1.unity";
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Dialogue dialogueScript = FindObjectOfType<Dialogue>(true);
        if (dialogueScript == null)
        {
            Debug.LogError("Could not find Dialogue script in Level 1!");
            return;
        }

        GameObject dialogueBox = dialogueScript.gameObject;
        
        // Remove existing CombatUI if it exists to avoid duplicates
        Transform oldPanel = dialogueBox.transform.Find("CombatChoicePanel");
        if (oldPanel != null) DestroyImmediate(oldPanel.gameObject);
        
        Transform oldPlayerHealth = dialogueBox.transform.parent.Find("PlayerHealthBar");
        if (oldPlayerHealth != null) DestroyImmediate(oldPlayerHealth.gameObject);

        Transform oldEnemyHealth = dialogueBox.transform.parent.Find("EnemyHealthBar");
        if (oldEnemyHealth != null) DestroyImmediate(oldEnemyHealth.gameObject);

        // 1. Create CombatChoicePanel inside DialogueBox
        GameObject panelObj = new GameObject("CombatChoicePanel", typeof(RectTransform));
        panelObj.transform.SetParent(dialogueBox.transform, false);
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(1, 0.4f); // Bottom 40% of the dialogue box
        panelRT.offsetMin = new Vector2(20, 20);
        panelRT.offsetMax = new Vector2(-20, -10);

        GridLayoutGroup grid = panelObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(300, 40);
        grid.spacing = new Vector2(20, 10);
        grid.childAlignment = TextAnchor.MiddleCenter;

        dialogueScript.choicePanel = panelObj;
        dialogueScript.choiceButtons = new Button[4];
        dialogueScript.choiceTexts = new TextMeshProUGUI[4];

        string[] letters = { "A)", "B)", "C)", "D)" };

        for (int i = 0; i < 4; i++)
        {
            GameObject btnObj = new GameObject("ChoiceButton_" + i, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(panelObj.transform, false);
            
            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.9f, 0.8f, 0.6f); // Sandy color like the dialogue box

            Button btn = btnObj.GetComponent<Button>();
            
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRT = txtObj.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = letters[i] + " Answer";
            tmp.color = Color.black;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;

            dialogueScript.choiceButtons[i] = btn;
            dialogueScript.choiceTexts[i] = tmp;

            int index = i; 
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(btn.onClick, 
                new UnityEngine.Events.UnityAction<int>(dialogueScript.OnAnswerClicked), index);
        }

        // 2. Create Player and Enemy Health Bars
        Canvas parentCanvas = dialogueBox.GetComponentInParent<Canvas>();
        
        dialogueScript.playerHealthBar = CreateHealthBar(parentCanvas.transform, "PlayerHealthBar", new Vector2(0.1f, 0.9f), Color.green);
        dialogueScript.enemyHealthBar = CreateHealthBar(parentCanvas.transform, "EnemyHealthBar", new Vector2(0.9f, 0.9f), Color.red);
        
        EditorUtility.SetDirty(dialogueScript);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Combat UI created and linked successfully in Level 1!");
    }

    private static Slider CreateHealthBar(Transform parent, string name, Vector2 anchor, Color color)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        
        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200, 30);
        
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
        bgObj.GetComponent<Image>().color = Color.black;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one; faRT.sizeDelta = new Vector2(-10, -10);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = color;

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.minValue = 0;
        slider.maxValue = 3;
        slider.value = 3;

        return slider;
    }
}

