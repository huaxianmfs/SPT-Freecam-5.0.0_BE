using System;
using System.IO;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;

namespace Terkoiz.Freecam
{
    public class NoFallDamageConfig
    {
        public bool enabled = true;
    }

    /// <summary>
    /// 独立的免摔落伤害模块。
    /// JSON 配置：&lt;插件目录&gt;\config\NoFallDamage.json
    ///   { "enabled": true }  → 本地玩家摔落伤害归零
    ///   { "enabled": false } → 完全走原版逻辑
    /// 只影响本地玩家，AI / 队友照常受伤。
    /// </summary>
    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.HandleFall))]
    public static class NoFallDamagePatch
    {
        public static NoFallDamageConfig Config { get; private set; }
        public static string ConfigFilePath { get; private set; }

        public static void LoadConfig(string pluginAssemblyLocation)
        {
            try
            {
                string pluginDir = !string.IsNullOrEmpty(pluginAssemblyLocation)
                    ? Path.GetDirectoryName(pluginAssemblyLocation)
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                   "BepInEx", "plugins", "Terkoiz.Freecam");

                string configDir = Path.Combine(pluginDir, "config");
                Directory.CreateDirectory(configDir);
                ConfigFilePath = Path.Combine(configDir, "NoFallDamage.json");

                Config = new NoFallDamageConfig();

                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    Config.enabled = ParseEnabled(json, true);
                    FreecamPlugin.Logger.LogInfo($"[NoFallDamage] Loaded config. enabled={Config.enabled}");
                }
                else
                {
                    WriteDefaultConfig(Config.enabled);
                    FreecamPlugin.Logger.LogInfo($"[NoFallDamage] Created default config at {ConfigFilePath}");
                }
            }
            catch (Exception ex)
            {
                FreecamPlugin.Logger.LogError($"[NoFallDamage] Failed to load config: {ex}");
                Config = new NoFallDamageConfig();
            }
        }

        private static bool ParseEnabled(string json, bool defaultValue)
        {
            if (string.IsNullOrEmpty(json)) return defaultValue;
            try
            {
                int keyIdx = json.IndexOf("\"enabled\"", StringComparison.Ordinal);
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

        private static void WriteDefaultConfig(bool enabled)
        {
            string json =
                "{\n" +
                "    \"enabled\": " + (enabled ? "true" : "false") + "\n" +
                "}\n";
            File.WriteAllText(ConfigFilePath, json);
        }

        [HarmonyPrefix]
        public static bool Prefix(ActiveHealthController __instance, ref float __result)
        {
            if (Config == null || !Config.enabled)
                return true; // 关闭 → 走原方法

            try
            {
                if (!Singleton<GameWorld>.Instantiated) return true;
                var gw = Singleton<GameWorld>.Instance;
                if (gw == null) return true;
                var mainPlayer = gw.MainPlayer;
                if (mainPlayer == null) return true;
                if (__instance == null || __instance.Player == null) return true;

                // 只影响本地玩家
                if (__instance.Player != mainPlayer) return true;

                __result = 0f;
                return false;
            }
            catch (Exception ex)
            {
                FreecamPlugin.Logger.LogError($"[NoFallDamage] Prefix failed: {ex}");
                return true;
            }
        }
    }
}