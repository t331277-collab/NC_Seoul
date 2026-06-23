using UnityEngine;
using UnityEditor;
using System.IO;

public class VarcoVoiceSettingsWindow : EditorWindow
{
    private string apiKey = "";
    private string secretsFolderPath;
    private string apiKeyFilePath;

    [MenuItem("Window/Varco Voice Settings")]
    public static void ShowWindow()
    {
        GetWindow<VarcoVoiceSettingsWindow>("Varco Voice Settings");
    }

    private void OnEnable()
    {
        secretsFolderPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "LocalSecrets");
        apiKeyFilePath = Path.Combine(secretsFolderPath, "varco_voice_api_key.txt");
        LoadApiKey();
    }

    private void OnGUI()
    {
        GUILayout.Label("Varco Voice API Configuration", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Enter your OPENAPI_KEY. It will be saved locally in LocalSecrets/varco_voice_api_key.txt which is ignored by Git.", MessageType.Info);
        
        apiKey = EditorGUILayout.TextField("API Key", apiKey);

        if (GUILayout.Button("Save Configuration"))
        {
            SaveApiKey();
        }
    }

    private void LoadApiKey()
    {
        if (File.Exists(apiKeyFilePath))
        {
            apiKey = File.ReadAllText(apiKeyFilePath).Trim();
        }
    }

    private void SaveApiKey()
    {
        if (!Directory.Exists(secretsFolderPath))
        {
            Directory.CreateDirectory(secretsFolderPath);
            File.WriteAllText(Path.Combine(secretsFolderPath, ".gitignore"), "*");
        }

        File.WriteAllText(apiKeyFilePath, apiKey.Trim());
        Debug.Log($"[VarcoVoiceSettings] API Key saved to {apiKeyFilePath}");
    }
}
