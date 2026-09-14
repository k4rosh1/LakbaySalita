using UnityEngine;
using UnityEngine.Networking;
using Ionic.Zip;
using Vosk;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class VoskVoiceManager : MonoBehaviour
{
    [Header("References")]
    public VoiceDialogueManager dialogueManager;

    [Header("Vosk Settings")]
    [Tooltip("The zip file name inside StreamingAssets (for Android) AND the folder name (for PC). E.g., 'vosk-model-tl-ph-generic-0.6' for the folder, and 'vosk-model.zip' for Android.")]
    public string modelFolderName = "vosk-model-tl-ph-generic-0.6";
    public string modelZipName = "vosk-model.zip";

    [Header("Extra Custom Vocabulary")]
    [Tooltip("Add any additional words here that might not be in the dialogue lines directly.")]
    public List<string> extraAcceptedWords = new List<string>();

    private Model voskModel;
    private VoskRecognizer recognizer;
    private AudioClip microphoneClip;
    private int samplingRate = 16000;
    private bool isListening = false;
    private int lastAudioPosition = 0;
    private bool isReady = false;

    void Start()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<VoiceDialogueManager>();
        }

        Vosk.Vosk.SetLogLevel(-1);
        StartCoroutine(InitVosk());
    }

    private IEnumerator InitVosk()
    {
        string finalModelPath = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        // Request Microphone Permission
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            yield return new WaitForSeconds(1f); // wait for user prompt
        }

        finalModelPath = Path.Combine(Application.persistentDataPath, modelFolderName);
        if (!Directory.Exists(finalModelPath))
        {
            Debug.Log("[Vosk] Decompressing model on Android device...");
            string zipPath = Path.Combine(Application.streamingAssetsPath, modelZipName);
            string tempZipPath = Path.Combine(Application.persistentDataPath, "temp_model.zip");

            // Use DownloadHandlerFile to avoid OutOfMemoryException on large 300MB zip
            if (zipPath.Contains("://") || zipPath.Contains(":///"))
            {
                using (UnityWebRequest www = new UnityWebRequest(zipPath, UnityWebRequest.kHttpVerbGET))
                {
                    www.downloadHandler = new DownloadHandlerFile(tempZipPath);
                    yield return www.SendWebRequest();
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("[Vosk] Failed to copy zip from StreamingAssets: " + www.error);
                        yield break;
                    }
                }
            }
            else
            {
                File.Copy(zipPath, tempZipPath, true);
            }

            Debug.Log("[Vosk] Zip copied to persistentDataPath. Extracting in background thread...");
            
            bool doneExtracting = false;
            Exception extractException = null;

            // Extract in background thread to prevent App Not Responding (ANR) freeze!
            Task.Run(() =>
            {
                try
                {
                    using (Stream dataStream = File.OpenRead(tempZipPath))
                    using (var zipFile = ZipFile.Read(dataStream))
                    {
                        zipFile.ExtractAll(Application.persistentDataPath, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
                catch (Exception ex)
                {
                    extractException = ex;
                }
                finally
                {
                    doneExtracting = true;
                }
            });

            // Wait for background thread to finish
            while (!doneExtracting)
            {
                yield return null; 
            }

            if (extractException != null)
            {
                Debug.LogError("[Vosk] Extraction failed: " + extractException.Message);
                yield break;
            }

            // Cleanup the temporary zip file to save 300MB of storage
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            Debug.Log("[Vosk] Extraction complete!");
            yield return new WaitForSeconds(0.5f);
        }
#else
        // On Desktop / Editor, we can just read the folder directly from StreamingAssets!
        finalModelPath = Path.Combine(Application.streamingAssetsPath, modelFolderName);
        yield return null;
#endif

        if (!Directory.Exists(finalModelPath))
        {
             Debug.LogError($"[Vosk] Final model path not found at: {finalModelPath}");
             yield break;
        }

        try
        {
            Debug.Log("[Vosk] Initializing Native C++ Library...");
            voskModel = new Model(finalModelPath);
            List<string> fullVocabulary = ExtractAllDialogueWords();

            if (fullVocabulary.Count > 0)
            {
                // ADD [unk] so Vosk can detect unknown words instead of failing!
                if (!fullVocabulary.Contains("[unk]")) fullVocabulary.Add("[unk]");
                
                string jsonGrammar = "[\"" + string.Join("\",\"", fullVocabulary) + "\"]";
                recognizer = new VoskRecognizer(voskModel, samplingRate, jsonGrammar);
                Debug.Log($"[VoskVoiceManager] Vosk initialized with {fullVocabulary.Count} unique words.");
            }
            else
            {
                recognizer = new VoskRecognizer(voskModel, samplingRate);
            }
            isReady = true;
            Debug.Log("[Vosk] Successfully initialized and ready to listen!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoskVoiceManager] Failed to load Vosk model: {e.Message}");
        }
    }

    private List<string> ExtractAllDialogueWords()
    {
        HashSet<string> uniqueWords = new HashSet<string>();
        char[] punctuation = new char[] { ' ', '.', ',', '!', '?', ';', ':', '"', '-', '\n', '\r' };

        if (dialogueManager != null && dialogueManager.lines != null)
        {
            foreach (var lineData in dialogueManager.lines)
            {
                if (lineData == null) continue;
                if (!string.IsNullOrEmpty(lineData.line))
                {
                    string[] words = lineData.line.ToLower().Split(punctuation, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string w in words) uniqueWords.Add(w.Trim());
                }
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
            if (dialogueManager.combatSpells != null)
            {
                foreach (string spell in dialogueManager.combatSpells)
                {
                    if (!string.IsNullOrWhiteSpace(spell))
                        uniqueWords.Add(spell.ToLower().Trim());
                }
            }
        }
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
        if (isListening || recognizer == null || !isReady) 
        {
            Debug.LogWarning("[Vosk] Not ready to listen yet!");
            return;
        }

        microphoneClip = Microphone.Start(null, true, 10, samplingRate);
        isListening = true;
        lastAudioPosition = 0;
        Debug.Log("[Vosk] Microphone Started!");
    }

    public void StopListening()
    {
        if (!isListening) return;
        Microphone.End(null);
        isListening = false;
        Debug.Log("[Vosk] Microphone Stopped!");
    }

    void Update()
    {
        if (!isListening || microphoneClip == null || recognizer == null || !isReady) return;

        int currentPosition = Microphone.GetPosition(null);
        if (currentPosition > lastAudioPosition)
        {
            int sampleCount = currentPosition - lastAudioPosition;
            float[] frameBuffer = new float[sampleCount];
            microphoneClip.GetData(frameBuffer, lastAudioPosition);

            short[] shortBuffer = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++) shortBuffer[i] = (short)(frameBuffer[i] * 32767f);

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
        if (dialogueManager == null) return;
        VoskResult result = JsonUtility.FromJson<VoskResult>(json);
        string spokenText = isPartial ? result.partial : result.text;

        if (!string.IsNullOrWhiteSpace(spokenText))
        {
            Debug.Log($"<color=cyan>Vosk Heard: '{spokenText}'</color>");
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

[System.Serializable]
public class VoskResult
{
    public string text;
    public string partial;
}
