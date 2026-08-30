using UnityEngine;
using Vosk;
using System;
using System.IO;
using System.Collections.Generic;

public class VoskVoiceManager : MonoBehaviour
{
    [Header("References")]
    public VoiceDialogueManager dialogueManager;

    [Header("Vosk Settings")]
    [Tooltip("Type the exact name of your Tagalog model folder inside StreamingAssets")]
    public string modelFolderName = "your_model_folder_name_here";

    [Header("Extra Custom Vocabulary")]
    [Tooltip("Add any additional words here that might not be in the dialogue lines directly.")]
    public List<string> extraAcceptedWords = new List<string>();

    private Model voskModel;
    private VoskRecognizer recognizer;
    private AudioClip microphoneClip;
    private int samplingRate = 16000;
    private bool isListening = false;
    private int lastAudioPosition = 0;

    void Start()
    {
        // Auto-assign DialogueManager if forgotten in Inspector
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<VoiceDialogueManager>();
        }

        // Hide standard Vosk debug logs
        Vosk.Vosk.SetLogLevel(-1);

        string modelPath = Path.Combine(Application.streamingAssetsPath, modelFolderName);

        if (!Directory.Exists(modelPath))
        {
            Debug.LogError($"[VoskVoiceManager] Model folder not found at: {modelPath}. Check 'Model Folder Name' in the Inspector.");
            return;
        }

        try
        {
            voskModel = new Model(modelPath);

            // Extract all dialogue words dynamically
            List<string> fullVocabulary = ExtractAllDialogueWords();

            if (fullVocabulary.Count > 0)
            {
                string jsonGrammar = "[\"" + string.Join("\",\"", fullVocabulary) + "\"]";
                recognizer = new VoskRecognizer(voskModel, samplingRate, jsonGrammar);
                Debug.Log($"[VoskVoiceManager] Vosk initialized with {fullVocabulary.Count} unique words from Dialogue Manager.");
            }
            else
            {
                recognizer = new VoskRecognizer(voskModel, samplingRate);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoskVoiceManager] Failed to load Vosk model: {e.Message}");
        }
    }

    private List<string> ExtractAllDialogueWords()
    {
        HashSet<string> uniqueWords = new HashSet<string>();
        char[] punctuation = new char[] { ' ', '.', ',', '!', '?', ';', ':', '"', '—', '-', '\n', '\r' };

        // 1. Collect from VoiceDialogueManager lines
        // Note: Make sure the properties 'lines', 'line', 'characterLine', and 'combatSpells' 
        // exactly match the variable names inside your actual VoiceDialogueManager script.
        if (dialogueManager != null && dialogueManager.lines != null)
        {
            foreach (var lineData in dialogueManager.lines)
            {
                if (lineData == null) continue;

                // Standard dialogue line
                if (!string.IsNullOrEmpty(lineData.line))
                {
                    string[] words = lineData.line.ToLower().Split(punctuation, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string w in words) uniqueWords.Add(w.Trim());
                }

                // Character-specific dialogue lines
                if (lineData.characterLine != null)
                {
                    foreach (string cLine in lineData.characterLine)
                    {
                        if (string.IsNullOrEmpty(cLine)) continue;
                        string[] words = cLine.ToLower().Split(punctuation, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string w in words) uniqueWords.Add(w.Trim());
                    }
                }
            }

            // Collect spell words if available
            if (dialogueManager.combatSpells != null)
            {
                foreach (string spell in dialogueManager.combatSpells)
                {
                    if (!string.IsNullOrWhiteSpace(spell))
                        uniqueWords.Add(spell.ToLower().Trim());
                }
            }
        }

        // 2. Add extra inspector-defined vocabulary words
        if (extraAcceptedWords != null)
        {
            foreach (string word in extraAcceptedWords)
            {
                if (!string.IsNullOrWhiteSpace(word))
                    uniqueWords.Add(word.ToLower().Trim());
            }
        }

        return new List<string>(uniqueWords);
    }

    public void StartListening()
    {
        if (isListening || recognizer == null) return;

        microphoneClip = Microphone.Start(null, true, 10, samplingRate);
        isListening = true;
        lastAudioPosition = 0;
    }

    public void StopListening()
    {
        if (!isListening) return;
        Microphone.End(null);
        isListening = false;
    }

    void Update()
    {
        if (!isListening || microphoneClip == null || recognizer == null) return;

        int currentPosition = Microphone.GetPosition(null);
        if (currentPosition > lastAudioPosition)
        {
            int sampleCount = currentPosition - lastAudioPosition;

            float[] frameBuffer = new float[sampleCount];
            microphoneClip.GetData(frameBuffer, lastAudioPosition);

            short[] shortBuffer = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                shortBuffer[i] = (short)(frameBuffer[i] * 32767f);
            }

            byte[] byteBuffer = new byte[sampleCount * 2];
            Buffer.BlockCopy(shortBuffer, 0, byteBuffer, 0, byteBuffer.Length);

            if (recognizer.AcceptWaveform(byteBuffer, byteBuffer.Length))
            {
                ParseAndSendResult(recognizer.Result(), false);
            }
            else
            {
                ParseAndSendResult(recognizer.PartialResult(), true);
            }

            lastAudioPosition = currentPosition;
        }
        else if (currentPosition < lastAudioPosition)
        {
            lastAudioPosition = 0;
        }
    }

    private void ParseAndSendResult(string json, bool isPartial)
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<VoiceDialogueManager>();
            if (dialogueManager == null) return;
        }

        VoskResult result = JsonUtility.FromJson<VoskResult>(json);
        string spokenText = isPartial ? result.partial : result.text;

        if (!string.IsNullOrWhiteSpace(spokenText))
        {
            dialogueManager.ProcessVoiceInput(spokenText);
        }
    }

    void OnDestroy()
    {
        StopListening();
        recognizer?.Dispose();
        voskModel?.Dispose();
    }
}

// --- MISSING CLASS ADDED HERE ---
[System.Serializable]
public class VoskResult
{
    public string text;
    public string partial;
}