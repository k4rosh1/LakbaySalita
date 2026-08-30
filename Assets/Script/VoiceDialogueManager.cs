using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class VoiceDialogueManager : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public VoiceDialogueLine[] lines;

    [Header("Karaoke Text Color Settings")]
    public Color32 unspokenColor = new Color32(128, 128, 128, 255); // Grey
    public Color32 highlightColor = new Color32(255, 215, 0, 255);  // Gold

    [Header("Voice SDK Triggers")]
    public UnityEvent onStartListening;
    public UnityEvent onStopListening;

    [Header("Combat Settings")]
    public Transform enemySpawnPoint;
    public string[] combatSpells = { "APOY", "HIWA" };

    [Header("Debug Simulator")]
    public string simulatedSpokenWord;

    private int index;
    private string[] currentWords;
    private int currentWordIndex;

    private bool isInCombat = false;
    private GameObject spawnedEnemy;

    void Start()
    {
        textComponent.text = string.Empty;
        StartDialogue();
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (!string.IsNullOrEmpty(simulatedSpokenWord) && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            ProcessVoiceInput(simulatedSpokenWord);
            simulatedSpokenWord = string.Empty;
        }

        if (isInCombat) return;

        if (Input.GetMouseButtonDown(0) && currentWordIndex >= currentWords.Length)
        {
            NextLine();
        }
    }

    void StartDialogue()
    {
        index = 0;
        SetupLineSpeech();
    }

    void SetupLineSpeech()
    {
        string fullLine = lines[index].GetLine();
        textComponent.text = fullLine;

        char[] punctuation = new char[] { ' ', '.', ',', '!', '?', ';', ':', '"', '—' };
        currentWords = fullLine.ToLower().Split(punctuation, StringSplitOptions.RemoveEmptyEntries);
        currentWordIndex = 0;

        SetEntireTextColor(unspokenColor);
        onStartListening?.Invoke();
    }

    public void ProcessVoiceInput(string spokenSentence)
    {
        string cleanSentence = spokenSentence.Trim().ToLower();
        char[] punctuation = new char[] { ' ', '.', ',', '!', '?', ';', ':', '"', '—' };
        string[] spokenWords = cleanSentence.Split(punctuation, StringSplitOptions.RemoveEmptyEntries);

        if (!isInCombat)
        {
            foreach (string word in spokenWords)
            {
                if (currentWordIndex < currentWords.Length)
                {
                    if (IsWordMatch(word, currentWords[currentWordIndex]))
                    {
                        HighlightWord(currentWordIndex, highlightColor);
                        currentWordIndex++;
                    }
                }
            }

            if (currentWordIndex < currentWords.Length)
            {
                onStartListening?.Invoke();
            }

            if (currentWordIndex >= currentWords.Length)
            {
                onStopListening?.Invoke();
                if (lines[index].enemyToSpawn != null) TriggerCombat();
            }
        }
        else
        {
            foreach (string word in spokenWords)
            {
                foreach (string spell in combatSpells)
                {
                    if (IsWordMatch(word, spell.ToLower()))
                    {
                        if (spawnedEnemy != null)
                        {
                            Destroy(spawnedEnemy);
                            EndCombat();
                        }
                        return;
                    }
                }
            }
        }
    }

    private bool IsWordMatch(string spoken, string target)
    {
        if (string.IsNullOrEmpty(spoken) || string.IsNullOrEmpty(target)) return false;

        // 1. Exact match (Always prioritize exact matches)
        if (spoken == target) return true;

        // 2. Short words (ang, sa, ng, mga, si, at, ay) MUST match exactly
        if (target.Length <= 3 || spoken.Length <= 3) return false;

        // 3. Reject if length difference is greater than 1 character (prevents "ang" -> "isang")
        if (Math.Abs(spoken.Length - target.Length) > 1) return false;

        // 4. Allow 1 character typo/STT variation for long proper nouns (e.g., Mayumiya)
        return GetLevenshteinDistance(spoken, target) <= 1;
    }

    private int GetLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        int[,] d = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= target.Length; j++) d[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[source.Length, target.Length];
    }

    void TriggerCombat()
    {
        isInCombat = true;
        if (lines[index].enemyToSpawn != null)
        {
            SelectedCharacter player = FindFirstObjectByType<SelectedCharacter>();
            Vector3 spawnPosition = Vector3.zero;

            if (player != null)
            {
                player.StopWalking();
                spawnPosition = player.transform.position + new Vector3(13f, 0f, 0f);
            }
            else if (enemySpawnPoint != null)
            {
                spawnPosition = enemySpawnPoint.position;
            }

            spawnedEnemy = Instantiate(lines[index].enemyToSpawn, spawnPosition, Quaternion.identity);

            SpriteRenderer enemySprite = spawnedEnemy.GetComponentInChildren<SpriteRenderer>();
            if (enemySprite != null)
            {
                enemySprite.flipX = false;
            }
        }
        onStartListening?.Invoke();
    }

    void EndCombat()
    {
        isInCombat = false;
        onStopListening?.Invoke();
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            SetupLineSpeech();
        }
        else
        {
            onStopListening?.Invoke();
            gameObject.SetActive(false);
        }
    }

    private void HighlightWord(int wordIndex, Color32 color)
    {
        textComponent.ForceMeshUpdate();
        TMP_WordInfo info = textComponent.textInfo.wordInfo[wordIndex];

        for (int i = 0; i < info.characterCount; ++i)
        {
            int charIndex = info.firstCharacterIndex + i;
            if (!textComponent.textInfo.characterInfo[charIndex].isVisible) continue;

            int meshIndex = textComponent.textInfo.characterInfo[charIndex].materialReferenceIndex;
            int vertexIndex = textComponent.textInfo.characterInfo[charIndex].vertexIndex;
            Color32[] vertexColors = textComponent.textInfo.meshInfo[meshIndex].colors32;

            vertexColors[vertexIndex + 0] = color;
            vertexColors[vertexIndex + 1] = color;
            vertexColors[vertexIndex + 2] = color;
            vertexColors[vertexIndex + 3] = color;
        }
        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void SetEntireTextColor(Color32 color)
    {
        textComponent.ForceMeshUpdate();
        for (int i = 0; i < textComponent.textInfo.characterCount; i++)
        {
            if (!textComponent.textInfo.characterInfo[i].isVisible) continue;

            int meshIndex = textComponent.textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textComponent.textInfo.characterInfo[i].vertexIndex;
            Color32[] vertexColors = textComponent.textInfo.meshInfo[meshIndex].colors32;

            vertexColors[vertexIndex + 0] = color;
            vertexColors[vertexIndex + 1] = color;
            vertexColors[vertexIndex + 2] = color;
            vertexColors[vertexIndex + 3] = color;
        }
        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}

[Serializable]
public class VoiceDialogueLine
{
    [TextArea] public string line;
    public GameObject enemyToSpawn;
    public bool specificCharacter;
    public string[] characterLine;

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
}