using EFT.CameraControl;
using HarmonyLib;

namespace Terkoiz.Freecam.Patches
{
    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.ForceSetPosition))]
    public static class ForceSetCameraPositionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (FreecamPlugin.FreecamControllerInstance == null) return true;
            return !FreecamPlugin.FreecamControllerInstance.IsFreecamActive;
        }
    }
}