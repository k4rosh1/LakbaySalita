using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

public class VoiceDialogueManager : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI textComponent;
    public VoiceDialogueLine[] lines;

    [Header("Karaoke Text Color Settings")]
    public Color32 unspokenColor = new Color32(128, 128, 128, 255);
    public Color32 highlightColor = new Color32(255, 215, 0, 255);

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
        if (textComponent != null)
        {
            textComponent.text = string.Empty;
            StartDialogue();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!string.IsNullOrEmpty(simulatedSpokenWord))
            {
                ProcessVoiceInput(simulatedSpokenWord);
            }
        }
    }

    public void StartListening()
    {
        onStartListening?.Invoke();
    }

    public void StopListening()
    {
        onStopListening?.Invoke();
    }

    void StartDialogue()
    {
        index = 0;
        if (lines.Length > 0)
        {
            SetupLineSpeech();
        }
    }

    void SetupLineSpeech()
    {
        textComponent.text = lines[index].GetLine();

        textComponent.ForceMeshUpdate();

        int wordCount = textComponent.textInfo.wordCount;
        currentWords = new string[wordCount];

        for (int i = 0; i < wordCount; i++)
        {
            TMP_WordInfo wInfo = textComponent.textInfo.wordInfo[i];
            string wStr = textComponent.text.Substring(wInfo.firstCharacterIndex, wInfo.characterCount);
            currentWords[i] = CleanWordForMatching(wStr);
        }

        currentWordIndex = 0;
        SetEntireTextColor(unspokenColor);
        StartListening();
    }

    private string CleanWordForMatching(string input)
    {
        char[] punctuation = new char[] { '.', ',', '!', '?', ';', ':', '"', '-', '(', ')' };
        return input.ToLower().Trim(punctuation);
    }

    private string ApplyPhoneticFallbacks(string input)
    {
        input = input.Replace("mayo miya", "mayumiya");
        input = input.Replace("mayo miyak", "mayumiya");
        return input;
    }

    public void ProcessVoiceInput(string spokenSentence)
    {
        string cleanSentence = spokenSentence.Trim().ToLower();
        cleanSentence = ApplyPhoneticFallbacks(cleanSentence);

        char[] punctuation = new char[] { ' ', '.', ',', '!', '?', ';', ':', '"', '-' };
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
                StartListening();
            }

            if (currentWordIndex >= currentWords.Length)
            {
                StopListening();
                if (lines[index].enemyToSpawn != null) TriggerCombat();
                else NextLine();
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

        if (spoken == target) return true;

        if (target.Length <= 4 || spoken.Length <= 4)
        {
            if (Math.Abs(spoken.Length - target.Length) <= 2)
            {
                return GetLevenshteinDistance(spoken, target) <= 1;
            }
            if (target.Length <= 3 && spoken.Contains(target))
            {
                return true;
            }
            return false;
        }

        if (Math.Abs(spoken.Length - target.Length) > 1) return false;

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
        StartListening();
    }

    void EndCombat()
    {
        isInCombat = false;
        StopListening();
        NextLine();
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
            StopListening();
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

[System.Serializable]
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
            if (index < characterLine.Length) return characterLine[index];
        }
        return line;
    }
}
