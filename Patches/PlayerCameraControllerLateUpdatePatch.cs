using EFT.CameraControl;
using HarmonyLib;

namespace Terkoiz.Freecam.Patches
{
    /// <summary>
    /// BSG 在 PlayerCameraController.LateUpdate 里加了一个 hack：
    /// 当相机离玩家身体太远时强制把相机位置拉回玩家身上（本来是为了修 BTR 的相机失同步）。
    /// 这个逻辑会破坏自由相机，所以在 freecam 激活时整段跳过 LateUpdate。
    /// </summary>
    [HarmonyPatch(typeof(PlayerCameraController), "LateUpdate")]
    public static class PlayerCameraControllerLateUpdatePatch
    {
        private static bool _lastLogged;

        [HarmonyPrefix]
        public static bool Prefix()
        {
            var inst = FreecamPlugin.FreecamControllerInstance;
            bool active = inst != null && inst.IsFreecamActive;

            if (active != _lastLogged)
            {
                _lastLogged = active;
                FreecamPlugin.Logger.LogInfo($"[Freecam] PlayerCameraController.LateUpdate blocking={active}");
            }

            return !active;
        }
    }
}