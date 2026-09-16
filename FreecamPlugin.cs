using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Terkoiz.Freecam
{
    [BepInPlugin(PluginGuid, "Terkoiz.Freecam", "1.4.6-IL2CPP")]
    public class FreecamPlugin : BasePlugin
    {
        public const string PluginGuid = "com.terkoiz.freecam";

        public static ManualLogSource Logger { get; private set; }
        public static FreecamController FreecamControllerInstance { get; set; }

        // Keybinds
        public static ConfigEntry<KeyCode> ToggleFreecamMode;
        public static ConfigEntry<KeyCode> ToggleFreecamControls;
        public static ConfigEntry<KeyCode> TeleportToCamera;
        public static ConfigEntry<KeyCode> ToggleUi;

        // Camera settings
        public static ConfigEntry<float> CameraMoveSpeed;
        public static ConfigEntry<float> CameraFastMoveSpeed;
        public static ConfigEntry<float> CameraLookSensitivity;
        public static ConfigEntry<float> CameraZoomSpeed;
        public static ConfigEntry<float> CameraFastZoomSpeed;

        // Toggles
        public static ConfigEntry<bool> CameraHeightMovement;
        public static ConfigEntry<bool> CameraMousewheelZoom;

        private Harmony _harmony;

        public override void Load()
        {
            Logger = base.Log;
            Logger.LogInfo("[Freecam] ===== LOAD START =====");

            // === Config ===
            ToggleFreecamMode = Config.Bind("Keybinds", "Toggle Freecam",
                KeyCode.KeypadPlus, "Toggles Freecam");

            ToggleFreecamControls = Config.Bind("Keybinds", "Toggle Freecam Controls",
                KeyCode.KeypadPeriod, "Toggles Freecam Controls");

            TeleportToCamera = Config.Bind("Keybinds", "Teleport To Camera",
                KeyCode.KeypadMultiply, "Teleports the player to camera position");

            ToggleUi = Config.Bind("Keybinds", "Toggle UI",
                KeyCode.KeypadMinus, "Toggles the game UI");

            CameraMoveSpeed = Config.Bind("Camera Settings", "Camera Speed", 10f);
            CameraFastMoveSpeed = Config.Bind("Camera Settings", "Camera Sprint Speed", 100f);
            CameraLookSensitivity = Config.Bind("Camera Settings", "Camera Mouse Sensitivity", 3f);
            CameraZoomSpeed = Config.Bind("Camera Settings", "Camera Zoom Speed", 10f);
            CameraFastZoomSpeed = Config.Bind("Camera Settings", "Camera Zoom Sprint Speed", 50f);

            CameraHeightMovement = Config.Bind("Toggles", "Camera Height Movement Keys", true);
            CameraMousewheelZoom = Config.Bind("Toggles", "Camera Mousewheel Zoom", true);

            // === Register IL2CPP component types ===
            Logger.LogInfo("[Freecam] Registering types in IL2CPP...");
            ClassInjector.RegisterTypeInIl2Cpp<FreecamController>();
            ClassInjector.RegisterTypeInIl2Cpp<Freecam>();
            Logger.LogInfo("[Freecam] Types registered.");

            // === Apply Harmony patches ===
            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll();
                Logger.LogInfo("[Freecam] Harmony PatchAll completed.");

                CheckPatchStatus(typeof(EFT.CameraControl.CameraManager), "ForceSetPosition",
                    new[] { typeof(Vector3) });
                CheckPatchStatus(typeof(EFT.HealthSystem.ActiveHealthController), "HandleFall", null);
                CheckPatchStatus(typeof(EFT.GameWorld), "OnGameStarted", null);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Freecam] PatchAll failed: {ex}");
            }

            // === 独立 JSON 免摔伤配置 ===
            NoFallDamagePatch.LoadConfig(typeof(FreecamPlugin).Assembly.Location);

            // === 独立 JSON Freecam 通用配置 ===
            FreecamConfig.Load(typeof(FreecamPlugin).Assembly.Location);

            Logger.LogInfo("[Freecam] ===== LOAD COMPLETE =====");
        }

        private static void CheckPatchStatus(Type type, string methodName, Type[] args)
        {
            try
            {
                var m = args == null
                    ? AccessTools.Method(type, methodName)
                    : AccessTools.Method(type, methodName, args);

                if (m == null)
                {
                    Logger.LogWarning($"[Freecam] Patch target {type.Name}.{methodName} = NULL");
                    return;
                }
                var info = Harmony.GetPatchInfo(m);
                if (info == null)
                {
                    Logger.LogWarning($"[Freecam] {type.Name}.{methodName} has NO PatchInfo");
                    return;
                }
                Logger.LogInfo(
                    $"[Freecam] {type.Name}.{methodName} patches: prefixes={info.Prefixes.Count}, postfixes={info.Postfixes.Count}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Freecam] CheckPatchStatus({methodName}) failed: {ex.Message}");
            }
        }
    }
}