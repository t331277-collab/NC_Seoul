using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System;
using System.Text;

[Serializable]
public class VoiceSynthesisRequest
{
    public string text;
    public string language;
    public string voice;
    public VoiceProperties properties;
    public int n_fm_steps;
    public int seed;
    public bool return_metadata;
    public string media_type;
}

[Serializable]
public class VoiceProperties
{
    public float speed;
    public float pitch;
}

[Serializable]
public class VoiceSynthesisResponse
{
    public string audio;
    public string media_type;
}

[Serializable]
public class TutorialVoiceSettings
{
    public string voice;
    public string language;
    public float speed;
    public float pitch;
    public int n_fm_steps;
    public int seed;
    public string media_type;
}

public class VarcoVoiceClient : MonoBehaviour
{
    private const string ApiUrl = "https://openapi.ai.nc.com/tts/lite/v1/api/synthesize";
    private const string ApiKeyRelativePath = "LocalSecrets/varco_voice_api_key.txt";
    private const string VoiceSettingsRelativePath = "Data/TutorialVoiceSettings.json";
    private string apiKey = "";
    private TutorialVoiceSettings settings;
    private string cachePath;

    private void Awake()
    {
        string keyPath = Path.Combine(Application.streamingAssetsPath, ApiKeyRelativePath);
        if (File.Exists(keyPath))
        {
            apiKey = File.ReadAllText(keyPath).Trim();
        }
        else
        {
            Debug.LogError($"[VarcoVoiceClient] API Key not found in StreamingAssets: {keyPath}");
        }

        string settingsPath = Path.Combine(Application.streamingAssetsPath, VoiceSettingsRelativePath);
        if (File.Exists(settingsPath))
        {
            string json = File.ReadAllText(settingsPath);
            settings = JsonUtility.FromJson<TutorialVoiceSettings>(json);
        }
        else
        {
            Debug.LogError($"[VarcoVoiceClient] Voice settings not found in StreamingAssets: {settingsPath}");
        }

        cachePath = Path.Combine(Application.persistentDataPath, "VoiceCache/Tutorial");
        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }
    }

    public void Synthesize(string text, string nodeId, Action<AudioClip> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            onError?.Invoke("API Key is missing.");
            return;
        }
        
        string fileName = $"{nodeId}_{GetStableTextHash(text)}.mp3";
        string filePath = Path.Combine(cachePath, fileName);
        
        // If already cached, just load it
        if (File.Exists(filePath))
        {
            StartCoroutine(LoadAudioClipFromPath(filePath, onSuccess, onError));
            return;
        }

        StartCoroutine(SendSynthesisRequest(text, filePath, onSuccess, onError));
    }

    private IEnumerator SendSynthesisRequest(string text, string savePath, Action<AudioClip> onSuccess, Action<string> onError)
    {
        if (settings == null)
        {
            onError?.Invoke("Voice settings missing.");
            yield break;
        }

        VoiceSynthesisRequest reqObj = new VoiceSynthesisRequest
        {
            text = text,
            language = settings.language,
            voice = settings.voice,
            properties = new VoiceProperties { speed = settings.speed, pitch = settings.pitch },
            n_fm_steps = settings.n_fm_steps,
            seed = settings.seed,
            return_metadata = false,
            media_type = settings.media_type
        };

        string jsonPayload = JsonUtility.ToJson(reqObj);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("OPENAPI_KEY", apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"API Error: {request.error} - {request.downloadHandler.text}");
            }
            else
            {
                string responseJson = request.downloadHandler.text;
                VoiceSynthesisResponse response = JsonUtility.FromJson<VoiceSynthesisResponse>(responseJson);
                
                if (!string.IsNullOrEmpty(response.audio))
                {
                    byte[] audioBytes = Convert.FromBase64String(response.audio);
                    File.WriteAllBytes(savePath, audioBytes);
                    StartCoroutine(LoadAudioClipFromPath(savePath, onSuccess, onError));
                }
                else
                {
                    onError?.Invoke("API returned empty audio data.");
                }
            }
        }
    }

    private IEnumerator LoadAudioClipFromPath(string path, Action<AudioClip> onSuccess, Action<string> onError)
    {
        string uri = "file:///" + path.Replace("\\", "/");
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Failed to load AudioClip from cache: {www.error}");
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                onSuccess?.Invoke(clip);
            }
        }
    }

    private string GetStableTextHash(string text)
    {
        if (string.IsNullOrEmpty(text)) return "0";
        unchecked
        {
            uint hash = 2166136261u;
            foreach (char c in text)
            {
                hash = (hash ^ c) * 16777619u;
            }
            return hash.ToString("X8");
        }
    }
}
