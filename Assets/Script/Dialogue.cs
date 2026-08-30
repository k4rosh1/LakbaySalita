using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public DialogueEvent[] lines;
    public float textSpeed = 0.2f;

    [Header("Spawn Settings")]
    public Transform enemySpawnPoint;
    public float enemySpawnDistance = 13f;

    [Header("Combat & UI Settings")]
    public int questionsPerEncounter = 3;
    public GameObject choicePanel;
    public TextMeshProUGUI[] choiceTexts;
    public Button[] choiceButtons;
    public Slider playerHealthBar;
    public Slider enemyHealthBar;

    [Header("Game Over Settings")]
    public GameObject pauseMenuPanel;
    public GameObject resumeButton;
    public GameObject homeButton;
    public GameObject restartButton;
    public TextMeshProUGUI gameOverText;

    [Header("Keystone & Level Complete")]
    public GameObject keystonePrefab;
    public GameObject levelConqueredPanel;
    public TextMeshProUGUI levelConqueredText;
    public Button proceedButton;

    [Header("Points Display")]
    public TextMeshProUGUI pointsText;

    [Header("Audio Settings")]
    public AudioSource voiceAudioSource;

    public int maxPlayerHealth = 3;

    private List<DialogueEvent> narrativeLines = new List<DialogueEvent>();
    private List<DialogueEvent> questionPool = new List<DialogueEvent>();
    private HashSet<DialogueEvent> answeredQuestions = new HashSet<DialogueEvent>();

    private DialogueEvent currentEvent;
    private bool inQuizMode = false;
    private int narrativeIndex = 0;

    private int playerSkillLevel = 1;
    private int currentPlayerHealth;
    private int currentEnemyHealth;
    private GameObject currentSpawnedEnemy;
    private Vector3 lastEnemyPosition;
    private bool wasLastEnemyFinal = false;

    private bool isWaitingForAnswer = false;
    private bool isResolvingCombat = false;
    private bool isLevelComplete = false;
    private bool isTyping = false;

    void Start()
    {
        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();
            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        voiceAudioSource.spatialBlend = 0f;
        voiceAudioSource.playOnAwake = false;

        Debug.Log("Dialogue Start() was triggered!");

        foreach (var line in lines)
        {
            if (line.isQuestion) questionPool.Add(line);
            else narrativeLines.Add(line);
        }

        Debug.Log($"Loaded {narrativeLines.Count} narrative lines and {questionPool.Count} question items.");

        currentPlayerHealth = maxPlayerHealth;

        if (playerHealthBar != null) playerHealthBar.gameObject.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (enemyHealthBar != null) enemyHealthBar.gameObject.SetActive(false);
        if (levelConqueredPanel != null) levelConqueredPanel.SetActive(false);

        if (proceedButton != null)
        {
            proceedButton.onClick.AddListener(OnProceedClicked);
        }

        if (resumeButton != null && resumeButton.TryGetComponent<Button>(out Button resBtn))
        {
            resBtn.onClick.AddListener(ResumeGame);
        }

        textComponent.text = string.Empty;

        UpdatePointsDisplay();
        StartDialogue();
    }

    void UpdatePointsDisplay()
    {
        if (pointsText != null)
        {
            int selectedCharacter = PlayerPrefs.GetInt("selectedOption", 0);
            int totalPoints = PlayerPrefs.GetInt("TotalPoints_" + selectedCharacter, 0);
            pointsText.text = "Puntos: " + totalPoints;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f || isWaitingForAnswer || isResolvingCombat || isLevelComplete) return;

        bool isPressed = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isPressed = true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) isPressed = true;

        if (isPressed)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                textComponent.text = currentEvent.GetLine();
                isTyping = false;
                CheckForQuestion();
            }
            else
            {
                if (voiceAudioSource != null && voiceAudioSource.isPlaying)
                {
                    bool skipEnabled = PlayerPrefs.GetInt("SkipDialogueEnabled", 0) == 1;
                    if (skipEnabled)
                    {
                        voiceAudioSource.Stop();
                    }
                    else
                    {
                        Debug.Log("Narration is still playing. Blocking input until audio finishes.");
                        return;
                    }
                }

                NextLine();
            }
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Pause();
            Debug.Log("Narration audio paused.");
        }
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (voiceAudioSource != null)
        {
            voiceAudioSource.UnPause();
            Debug.Log("Narration audio resumed.");
        }
    }

    void StartDialogue()
    {
        if (narrativeLines.Count > 0)
        {
            narrativeIndex = 0;
            currentEvent = narrativeLines[narrativeIndex];
            CheckAndSpawnEnemy();
            StartCoroutine(TypeLine());
        }
        else
        {
            Debug.LogWarning("No narrative lines found! Make sure your lines in the Inspector don't all have 'Is Question' checked.");
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        PlayVoiceClip();
        textComponent.text = string.Empty;

        string[] words = currentEvent.GetLine().Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            textComponent.text += words[i] + " ";
            yield return new WaitForSeconds(textSpeed);
        }

        textComponent.text = currentEvent.GetLine();
        isTyping = false;
        CheckForQuestion();
    }

    void PlayVoiceClip()
    {
        if (voiceAudioSource == null)
        {
            Debug.LogWarning("Voice Audio Source is missing! Assign an AudioSource to the Dialogue script in the Inspector.");
            return;
        }

        voiceAudioSource.Stop();

        AudioClip clipToPlay = (currentEvent != null) ? currentEvent.GetVoiceClip() : null;

        if (clipToPlay != null)
        {
            voiceAudioSource.clip = clipToPlay;
            voiceAudioSource.Play();
            Debug.Log($"Playing narration clip: {clipToPlay.name}");
        }
        else
        {
            Debug.LogWarning($"No Voice Clip assigned for line: '{currentEvent?.GetLine()}'");
        }
    }

    void CheckForQuestion()
    {
        if (currentEvent.isQuestion && choicePanel != null && !isWaitingForAnswer && !isResolvingCombat)
        {
            isWaitingForAnswer = true;
            choicePanel.SetActive(true);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < currentEvent.choices.Length && !string.IsNullOrEmpty(currentEvent.choices[i]))
                {
                    choiceTexts[i].text = currentEvent.choices[i];
                    choiceButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void OnAnswerClicked(int choiceIndex)
    {
        if (!isWaitingForAnswer) return;

        isWaitingForAnswer = false;
        if (choicePanel != null) choicePanel.SetActive(false);

        answeredQuestions.Add(currentEvent);

        StartCoroutine(ResolveCombat(choiceIndex));
    }

    IEnumerator ResolveCombat(int choiceIndex)
    {
        isResolvingCombat = true;
        SelectedCharacter player = FindFirstObjectByType<SelectedCharacter>();
        Animator enemyAnim = currentSpawnedEnemy != null ? currentSpawnedEnemy.GetComponent<Animator>() : null;

        if (choiceIndex == currentEvent.correctAnswerIndex)
        {
            playerSkillLevel = Mathf.Min(3, playerSkillLevel + 1);

            int selectedCharacter = PlayerPrefs.GetInt("selectedOption", 0);
            string pointsKey = "TotalPoints_" + selectedCharacter;
            int pointsEarned = currentEvent.difficultyLevel * 10;
            int totalPoints = PlayerPrefs.GetInt(pointsKey, 0) + pointsEarned;
            PlayerPrefs.SetInt(pointsKey, totalPoints);
            PlayerPrefs.Save();
            Debug.Log("Earned " + pointsEarned + " points! Total: " + totalPoints);
            UpdatePointsDisplay();

            if (player != null) player.PlayAttack();
            yield return new WaitForSeconds(0.5f);

            if (enemyAnim != null) enemyAnim.SetTrigger("takeDamage");

            currentEnemyHealth--;
            if (enemyHealthBar != null) enemyHealthBar.value = currentEnemyHealth;

            yield return new WaitForSeconds(1.0f);

            if (currentEnemyHealth <= 0 && currentSpawnedEnemy != null)
            {
                lastEnemyPosition = currentSpawnedEnemy.transform.position;

                Destroy(currentSpawnedEnemy);
                currentSpawnedEnemy = null;
                if (enemyHealthBar != null) enemyHealthBar.gameObject.SetActive(false);
                if (playerHealthBar != null) playerHealthBar.gameObject.SetActive(false);

                if (wasLastEnemyFinal)
                {
                    DropKeystone(lastEnemyPosition);
                    isResolvingCombat = false;
                    isLevelComplete = true;

                    string unlockKey = "UnlockedLevel_" + selectedCharacter;
                    string sceneName = SceneManager.GetActiveScene().name;
                    string[] parts = sceneName.Split(' ');
                    int currentLevel = 1;
                    if (parts.Length >= 2) int.TryParse(parts[parts.Length - 1], out currentLevel);
                    int currentUnlocked = PlayerPrefs.GetInt(unlockKey, 1);
                    if (currentLevel + 1 > currentUnlocked)
                    {
                        PlayerPrefs.SetInt(unlockKey, currentLevel + 1);
                        PlayerPrefs.Save();
                    }

                    yield return new WaitForSeconds(2f);
                    ShowLevelConquered();
                    yield break;
                }
                else
                {
                    if (player != null) player.ResumeWalking();
                }
            }
        }
        else
        {
            playerSkillLevel = Mathf.Max(1, playerSkillLevel - 1);

            if (enemyAnim != null) enemyAnim.SetTrigger("attack");
            yield return new WaitForSeconds(0.5f);

            if (player != null) player.PlayHurt();

            currentPlayerHealth--;
            if (playerHealthBar != null) playerHealthBar.value = currentPlayerHealth;

            yield return new WaitForSeconds(1.0f);

            if (currentPlayerHealth <= 0)
            {
                yield return new WaitForSeconds(1.5f);
                TriggerGameOver("Naubusan ka ng lakas! Subukan muli.");
                yield break;
            }
        }

        isResolvingCombat = false;
        NextLine();
    }

    void DropKeystone(Vector3 position)
    {
        if (keystonePrefab != null)
        {
            SelectedCharacter player = FindFirstObjectByType<SelectedCharacter>();
            float groundY = player != null ? player.transform.position.y : position.y;

            Vector3 dropPos = new Vector3(position.x - 2f, groundY + 1f, position.z);
            GameObject keystone = Instantiate(keystonePrefab, dropPos, Quaternion.identity);

            Debug.Log("Keystone dropped at " + dropPos);
        }
    }

    public void ShowLevelConquered()
    {
        gameObject.SetActive(true);

        if (textComponent != null) textComponent.text = "";

        if (levelConqueredPanel != null)
        {
            levelConqueredPanel.SetActive(true);
        }
        if (levelConqueredText != null)
        {
            levelConqueredText.text = "Napagtagumpayan mo ang Level!\nNakuha mo ang Keystone!";
        }
        Time.timeScale = 0f;
    }

    void OnProceedClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Character Selection");
    }

    void TriggerGameOver(string message)
    {
        if (gameOverText != null) { gameOverText.text = message; gameOverText.gameObject.SetActive(true); }
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (resumeButton != null) resumeButton.SetActive(false);

        if (homeButton != null)
        {
            RectTransform homeRT = homeButton.GetComponent<RectTransform>();
            if (homeRT != null) homeRT.anchoredPosition = new Vector2(-70f, homeRT.anchoredPosition.y);
        }
        if (restartButton != null)
        {
            RectTransform restartRT = restartButton.GetComponent<RectTransform>();
            if (restartRT != null) restartRT.anchoredPosition = new Vector2(70f, restartRT.anchoredPosition.y);
        }

        Time.timeScale = 0f;
    }

    void NextLine()
    {
        if (inQuizMode)
        {
            if (currentEnemyHealth > 0 && currentSpawnedEnemy != null)
            {
                currentEvent = GetNextAdaptiveQuestion();
                textComponent.text = string.Empty;
                StartCoroutine(TypeLine());
            }
            else
            {
                inQuizMode = false;
                AdvanceNarrative();
            }
        }
        else
        {
            AdvanceNarrative();
        }
    }

    void AdvanceNarrative()
    {
        narrativeIndex++;
        if (narrativeIndex < narrativeLines.Count)
        {
            currentEvent = narrativeLines[narrativeIndex];
            textComponent.text = string.Empty;
            CheckAndSpawnEnemy();
            StartCoroutine(TypeLine());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void CheckAndSpawnEnemy()
    {
        if (currentEvent.enemyToSpawn != null && currentSpawnedEnemy == null)
        {
            SelectedCharacter player = FindFirstObjectByType<SelectedCharacter>();
            Vector3 spawnPosition = Vector3.zero;

            if (player != null)
            {
                player.StopWalking();
                spawnPosition = player.transform.position + new Vector3(enemySpawnDistance, 0f, 0f);
            }
            else if (enemySpawnPoint != null)
            {
                spawnPosition = enemySpawnPoint.position;
            }

            currentSpawnedEnemy = Instantiate(currentEvent.enemyToSpawn, spawnPosition, Quaternion.identity);

            wasLastEnemyFinal = currentEvent.isFinalEnemy;

            inQuizMode = true;

            currentEnemyHealth = questionsPerEncounter;
            if (enemyHealthBar != null)
            {
                enemyHealthBar.maxValue = questionsPerEncounter;
                enemyHealthBar.value = currentEnemyHealth;
                enemyHealthBar.gameObject.SetActive(true);
            }
            if (playerHealthBar != null)
            {
                playerHealthBar.gameObject.SetActive(true);
            }

            SpriteRenderer enemySprite = currentSpawnedEnemy.GetComponentInChildren<SpriteRenderer>();
            if (enemySprite != null)
            {
                enemySprite.flipX = false;
                enemySprite.sortingOrder = 5;
            }
        }
    }

    DialogueEvent GetNextAdaptiveQuestion()
    {
        List<DialogueEvent> availableQuestions = questionPool
            .Where(q => q.difficultyLevel == playerSkillLevel && !answeredQuestions.Contains(q))
            .ToList();

        if (availableQuestions.Count == 0)
        {
            availableQuestions = questionPool.Where(q => !answeredQuestions.Contains(q)).ToList();
        }

        if (availableQuestions.Count == 0)
        {
            answeredQuestions.Clear();
            availableQuestions = questionPool;
        }

        int randomIndex = UnityEngine.Random.Range(0, availableQuestions.Count);
        return availableQuestions[randomIndex];
    }
}

[Serializable]
public class DialogueEvent
{
    [TextArea] public string line;
    public GameObject enemyToSpawn;
    public bool specificCharacter;
    public string[] characterLine;

    [Header("Multiple Choice Combat")]
    public bool isQuestion;
    public string[] choices = new string[4];
    public int correctAnswerIndex;

    [Header("Dynamic Difficulty Tier")]
    [Tooltip("1 = Easy, 2 = Medium, 3 = Hard")]
    public int difficultyLevel = 1;

    [Header("Boss Settings")]
    [Tooltip("Check this if this enemy is the FINAL boss of the level")]
    public bool isFinalEnemy = false;

    [Header("Audio Settings")]
    public AudioClip voiceClip;
    public AudioClip[] characterVoiceClips;

    public string GetLine()
    {
        if (specificCharacter && characterLine != null && characterLine.Length > 0)
        {
            int index = PlayerPrefs.GetInt("selectedOption", 0);
            if (index < characterLine.Length)
                return characterLine[index];
        }
        return line;
    }

    public AudioClip GetVoiceClip()
    {
        if (specificCharacter && characterVoiceClips != null && characterVoiceClips.Length > 0)
        {
            int index = PlayerPrefs.GetInt("selectedOption", 0);
            if (index < characterVoiceClips.Length)
                return characterVoiceClips[index];
        }
        return voiceClip;
    }
}