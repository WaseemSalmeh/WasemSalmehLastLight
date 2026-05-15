using System.IO;
using LastLight.Data;
using UnityEngine;

namespace LastLight.Core
{
    public static class SaveService
    {
        private const string FileName = "lastlight-save.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static PlayerProgress LoadOrCreate()
        {
            if (!File.Exists(SavePath))
            {
                return new PlayerProgress();
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var progress = JsonUtility.FromJson<PlayerProgress>(json);
                return progress ?? new PlayerProgress();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load save file at '{SavePath}': {ex.Message}");
                return new PlayerProgress();
            }
        }

        public static void Save(PlayerProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(progress, true);
            File.WriteAllText(SavePath, json);
        }
    }
}

