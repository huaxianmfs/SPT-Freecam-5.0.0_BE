using EFT.CameraControl;
using HarmonyLib;
using UnityEngine;

namespace Terkoiz.Freecam.Patches
{
    /// <summary>
    /// BSG 在 SPT 5.0 里给相机加了一堆距离检查。
    /// CameraManager.ForceSetPosition 实际上没有任何调用者（CallerCount=0），
    /// 所以之前拦截它是白费功夫。
    ///
    /// 真正每帧被游戏调用的，是这几个 Distance 方法。
    /// 当 Freecam 激活时，让它们返回 0 —— 游戏会认为"相机就在玩家身上"，
    /// 距离检查永远不会触发，相机自然不会被拉回去。
    /// </summary>
    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.Distance))]
    public static class CameraManagerDistancePatch
    {
        private static bool _lastLogged;

        [HarmonyPrefix]
        public static bool Prefix(ref float __result)
        {
            var inst = FreecamPlugin.FreecamControllerInstance;
            bool active = inst != null && inst.IsFreecamActive;

            if (active != _lastLogged)
            {
                _lastLogged = active;
                FreecamPlugin.Logger.LogInfo($"[Freecam] CameraManager.Distance blocking={active}");
            }

            if (active)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.SqrDistance))]
    public static class CameraManagerSqrDistancePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result)
        {
            var inst = FreecamPlugin.FreecamControllerInstance;
            if (inst != null && inst.IsFreecamActive)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.SqrDistanceWithZero))]
    public static class CameraManagerSqrDistanceWithZeroPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result)
        {
            var inst = FreecamPlugin.FreecamControllerInstance;
            if (inst != null && inst.IsFreecamActive)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }
}