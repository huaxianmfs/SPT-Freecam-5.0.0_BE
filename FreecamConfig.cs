using System;
using System.IO;

namespace Terkoiz.Freecam
{
    public class FreecamConfigData
    {
        public bool rememberLastPosition = false;
    }

    /// <summary>
    /// 通用 Freecam 配置。
    /// JSON 路径：&lt;插件目录&gt;\config\FreecamConfig.json
    ///   { "rememberLastPosition": false }  → 默认不记住相机位置
    /// </summary>
    public static class FreecamConfig
    {
        public static FreecamConfigData Data { get; private set; } = new FreecamConfigData();
        public static string ConfigFilePath { get; private set; }

        public static void Load(string pluginAssemblyLocation)
        {
            try
            {
                string pluginDir = !string.IsNullOrEmpty(pluginAssemblyLocation)
                    ? Path.GetDirectoryName(pluginAssemblyLocation)
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                   "BepInEx", "plugins", "Terkoiz.Freecam");

                string configDir = Path.Combine(pluginDir, "config");
                Directory.CreateDirectory(configDir);
                ConfigFilePath = Path.Combine(configDir, "FreecamConfig.json");

                Data = new FreecamConfigData();

                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    Data.rememberLastPosition = ParseBool(json, "rememberLastPosition", false);
                    FreecamPlugin.Logger.LogInfo(
                        $"[Freecam] Config loaded. rememberLastPosition={Data.rememberLastPosition}");
                }
                else
                {
                    WriteDefault();
                    FreecamPlugin.Logger.LogInfo($"[Freecam] Created default config at {ConfigFilePath}");
                }
            }
            catch (Exception ex)
            {
                FreecamPlugin.Logger.LogError($"[Freecam] Config load failed: {ex}");
                Data = new FreecamConfigData();
            }
        }

        private static bool ParseBool(string json, string key, bool defaultValue)
        {
            if (string.IsNullOrEmpty(json)) return defaultValue;
            try
            {
                int keyIdx = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
                if (keyIdx < 0) return defaultValue;
                int colonIdx = json.IndexOf(':', keyIdx);
                if (colonIdx < 0) return defaultValue;

                int i = colonIdx + 1;
                while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
                if (i >= json.Length) return defaultValue;

                if (json.Length - i >= 4 &&
                    json.Substring(i, 4).Equals("true", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (json.Length - i >= 5 &&
                    json.Substring(i, 5).Equals("false", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            catch { }
            return defaultValue;
        }

        private static void WriteDefault()
        {
            string json =
                "{\n" +
                "    \"rememberLastPosition\": false\n" +
                "}\n";
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}