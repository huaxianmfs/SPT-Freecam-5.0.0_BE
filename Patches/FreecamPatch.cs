using System;
using Comfort.Common;
using EFT;
using HarmonyLib;

namespace Terkoiz.Freecam.Patches
{
    /// <summary>
    /// 进图后把 FreecamController 挂到 GameWorld 上。
    /// 这是唯一在 IL2CPP 里可靠的挂载方式（在主菜单 new GameObject 会失效）。
    /// </summary>
    [HarmonyPatch(typeof(GameWorld), nameof(GameWorld.OnGameStarted))]
    public static class FreecamPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            FreecamPlugin.Logger.LogInfo("[Freecam] GameWorld.OnGameStarted postfix fired.");

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                FreecamPlugin.Logger.LogWarning("[Freecam] gameWorld is null in OnGameStarted postfix.");
                return;
            }

            try
            {
                FreecamPlugin.FreecamControllerInstance = gameWorld.gameObject.AddComponent<FreecamController>();
                FreecamPlugin.Logger.LogInfo(
                    $"[Freecam] FreecamController added to GameWorld. instance={(FreecamPlugin.FreecamControllerInstance == null ? "NULL" : "ok")}");
            }
            catch (Exception ex)
            {
                FreecamPlugin.Logger.LogError($"[Freecam] AddComponent<FreecamController> failed: {ex}");
            }
        }
    }
}