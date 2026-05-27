using System.IO;
using UnityEngine;

public static class SimulationConfigStorage
{
    private static string ConfigFolder =>  Path.Combine(Application.persistentDataPath, "configs");

    public static void Save(SimulationConfig config, string fileName)
    {
        if (!Directory.Exists(ConfigFolder))
            Directory.CreateDirectory(ConfigFolder);

        string json = JsonUtility.ToJson(config, true);
        string path = Path.Combine(ConfigFolder, fileName + ".json");

        File.WriteAllText(path, json);

        Debug.Log($"Config saved to: {path}");
    }

    public static SimulationConfig Load(string fileName, bool logIfMissing = true)
    {
        string path = Path.Combine(ConfigFolder, fileName + ".json");

        if (!File.Exists(path))
        {
            if (logIfMissing)
                Debug.LogWarning($"Config file not found: {path}");

            return null;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SimulationConfig>(json);
    }

    public static SimulationConfig LoadFromResources(string resourcePath)
    {
        // ВАЖНО: путь без .json
        // Например: "configs/default_config"
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);

        if (asset == null)
        {
            Debug.LogError($"Default config not found in Resources: {resourcePath}");
            return null;
        }

        return JsonUtility.FromJson<SimulationConfig>(asset.text);
    }

    public static SimulationConfig LoadOrDefault( string userFileName, string defaultResourcePath = "configs/default_config")
    {
        SimulationConfig config = Load("1q23", false);

        if (config != null)
        {
            Debug.Log($"Loaded user config: {userFileName}");
            return config;
        }

        Debug.Log("User config not found. Loading default config from build.");
        return LoadFromResources(defaultResourcePath);
    }

    public static string GetConfigFolderPath()
    {
        return ConfigFolder;
    }
}