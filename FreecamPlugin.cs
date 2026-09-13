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
    [BepInPlugin(PluginGuid, "Terkoiz.Freecam", "1.0.2-DEBUG3")]
    public class FreecamPlugin : BasePlugin
    {
        public const string PluginGuid = "terkoiz.freecam";

        // 兼容原项目和新增 Patch 的静态引用
        public static ManualLogSource Logger { get; private set; }
        public static FreecamController FreecamControllerInstance { get; set; }

        public static ConfigEntry<KeyCode> ToggleFreecamMode;
        public static ConfigEntry<KeyCode> ToggleFreecamControls;
        public static ConfigEntry<KeyCode> ToggleUi;
        public static ConfigEntry<KeyCode> TeleportToCamera;
        public static ConfigEntry<float> CameraMoveSpeed;
        public static ConfigEntry<float> CameraFastMoveSpeed;
        public static ConfigEntry<float> CameraLookSensitivity;
        public static ConfigEntry<bool> CameraHeightMovement;
        public static ConfigEntry<bool> CameraMousewheelZoom;
        public static ConfigEntry<float> CameraZoomSpeed;
        public static ConfigEntry<float> CameraFastZoomSpeed;
        public static ConfigEntry<bool> CameraRememberLastPosition;

        private Harmony _harmony;

        public override void Load()
        {
            Logger = base.Log;
            Logger.LogInfo("[FCDBG3] ===== DEBUG3 LOAD START =====");

            // 绑定 Config 项
            ToggleFreecamMode = Config.Bind("Controls", "Toggle Freecam", KeyCode.KeypadPlus);
            ToggleFreecamControls = Config.Bind("Controls", "Toggle Controls", KeyCode.KeypadPeriod);
            ToggleUi = Config.Bind("Controls", "Toggle UI", KeyCode.KeypadMinus);
            TeleportToCamera = Config.Bind("Controls", "Teleport Player to Camera", KeyCode.KeypadMultiply);

            CameraMoveSpeed = Config.Bind("Settings", "Movement Speed", 5f);
            CameraFastMoveSpeed = Config.Bind("Settings", "Fast Movement Speed", 20f);
            CameraLookSensitivity = Config.Bind("Settings", "Look Sensitivity", 3f);
            CameraHeightMovement = Config.Bind("Settings", "Q/E/R/F Height Controls", true);
            CameraMousewheelZoom = Config.Bind("Settings", "Mousewheel Zoom", true);
            CameraZoomSpeed = Config.Bind("Settings", "Zoom Speed", 10f);
            CameraFastZoomSpeed = Config.Bind("Settings", "Fast Zoom Speed", 30f);
            CameraRememberLastPosition = Config.Bind("Settings", "Remember Last Position", true);

            // 在 IL2CPP 中注册 FreecamController 组件
            Logger.LogInfo("[FCDBG3] Registering FreecamController...");
            ClassInjector.RegisterTypeInIl2Cpp<FreecamController>();
            Logger.LogInfo("[FCDBG3] FreecamController registered.");

            // 创建挂载 GameObject
            var freecamObject = new GameObject("FreecamControllerObject");
            UnityEngine.Object.DontDestroyOnLoad(freecamObject);
            FreecamControllerInstance = freecamObject.AddComponent<FreecamController>();
            Logger.LogInfo("[FCDBG3] FreecamController instance created.");

            // 应用 Harmony Patch
            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll();
                Logger.LogInfo("[FCDBG3] Harmony PatchAll completed.");
                LogPatchStatus(_harmony);
                Logger.LogInfo("Successfully applied patches.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply patches: {ex}");
            }

            Logger.LogInfo("[FCDBG3] Freecam plugin loaded. Version=1.0.2-DEBUG3");
        }

        private static void LogPatchStatus(Harmony harmony)
        {
            try
            {
                var original = AccessTools.Method(typeof(EFT.CameraControl.CameraManager), nameof(EFT.CameraControl.CameraManager.ForceSetPosition), new[] { typeof(Vector3) });
                Logger.LogInfo("[FCDBG3] ForceSetPosition target=" + (original == null ? "NULL" : original.ToString()));
                if (original != null)
                {
                    var info = Harmony.GetPatchInfo(original);
                    if (info == null)
                    {
                        Logger.LogWarning("[FCDBG3] ForceSetPosition has NO Harmony PatchInfo!");
                    }
                    else
                    {
                        Logger.LogInfo("[FCDBG3] ForceSetPosition patches: prefixes=" + info.Prefixes.Count + ", postfixes=" + info.Postfixes.Count);
                    }

                    var update = AccessTools.Method(typeof(EFT.CameraControl.PlayerCameraController), nameof(EFT.CameraControl.PlayerCameraController.UpdatePointOfView));
                    Logger.LogInfo("[FCDBG3] UpdatePointOfView target=" + (update == null ? "NULL" : update.ToString()));
                    var updateInfo = update == null ? null : Harmony.GetPatchInfo(update);
                    Logger.LogInfo("[FCDBG3] UpdatePointOfView patches=" + (updateInfo == null ? "NONE" : ("prefixes=" + updateInfo.Prefixes.Count + ", postfixes=" + updateInfo.Postfixes.Count)));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[FCDBG3] Patch status check failed: " + ex);
            }
        }
    }
}