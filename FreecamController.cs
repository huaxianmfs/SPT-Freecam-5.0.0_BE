using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;
using UnityEngine;

namespace Terkoiz.Freecam
{
    public class FreecamController : MonoBehaviour
    {
        private GameObject _mainCamera;
        private Freecam _freeCamScript;

        private EftBattleUIScreen _playerUi;
        private bool _uiHidden;

        private GamePlayerOwner _gamePlayerOwner;

        private Vector3? _lastPosition;
        private Quaternion? _lastRotation;

        private bool _controlsToggled;

        public bool IsFreecamActive => _freeCamScript != null && (_freeCamScript.IsActive || _controlsToggled);

        public FreecamController(IntPtr ptr) : base(ptr) { }

        public void Start()
        {
            FreecamPlugin.Logger.LogInfo("[Freecam] FreecamController.Start() called");

            // Get Main Camera
            var localPlayer = GetLocalPlayerFromWorld();
            if (localPlayer == null)
            {
                FreecamPlugin.Logger.LogError("[Freecam] localPlayer is null in Start");
                return;
            }

            var pcc = localPlayer.GetComponent<PlayerCameraController>();
            if (pcc == null || pcc.Camera == null)
            {
                FreecamPlugin.Logger.LogError("[Freecam] PlayerCameraController or Camera is null");
                return;
            }
            _mainCamera = pcc.Camera.gameObject;

            // Get Player UI
            try { _playerUi = Singleton<CommonUI>.Instance?.EftBattleUIScreen; }
            catch (Exception ex) { FreecamPlugin.Logger.LogWarning($"[Freecam] UI lookup failed: {ex.Message}"); }

            // Add Freecam script to main camera
            _freeCamScript = _mainCamera.AddComponent<Freecam>();
            if (_freeCamScript == null)
                FreecamPlugin.Logger.LogError("[Freecam] Failed to add Freecam script");
            else
                FreecamPlugin.Logger.LogInfo("[Freecam] Freecam script added to main camera");

            // Get GamePlayerOwner
            _gamePlayerOwner = localPlayer.GetComponentInChildren<GamePlayerOwner>();
            if (_gamePlayerOwner == null)
                FreecamPlugin.Logger.LogWarning("[Freecam] GamePlayerOwner not found");

            FreecamPlugin.Logger.LogInfo(
                $"[Freecam] Init complete. cam={(_mainCamera != null)}, ui={(_playerUi != null)}, owner={(_gamePlayerOwner != null)}");
        }

        public void Update()
        {
            if (_freeCamScript == null)
                return;

            // 诊断：任意键按下就打一行，方便确认 Update 是否真的在跑
            if (Input.anyKeyDown)
            {
                FreecamPlugin.Logger.LogInfo(
                    $"[Freecam] keyDown='{Input.inputString}', freecam={_freeCamScript.IsActive}, ctrl={_controlsToggled}");
            }

            if (Input.GetKeyDown(FreecamPlugin.ToggleUi.Value)) ToggleUi();
            if (Input.GetKeyDown(FreecamPlugin.ToggleFreecamMode.Value)) ToggleCamera();
            if (Input.GetKeyDown(FreecamPlugin.ToggleFreecamControls.Value)) ToggleCameraControls();
            if (Input.GetKeyDown(FreecamPlugin.TeleportToCamera.Value)) MovePlayerToCamera();
        }

        private void ToggleCamera()
        {
            var localPlayer = GetLocalPlayerFromWorld();
            if (localPlayer == null) return;

            FreecamPlugin.Logger.LogInfo($"[Freecam] ToggleCamera pressed. active={_freeCamScript.IsActive}");

            if (!_freeCamScript.IsActive)
                SetPlayerToFreecamMode(localPlayer);
            else
                SetPlayerToFirstPersonMode(localPlayer);
        }

        private void MovePlayerToCamera()
        {
            var localPlayer = GetLocalPlayerFromWorld();
            if (localPlayer == null) return;

            if (_freeCamScript.IsActive)
            {
                var position = new Vector3(
                    _mainCamera.transform.position.x,
                    _mainCamera.transform.position.y - 1.8f,
                    _mainCamera.transform.position.z);

                localPlayer.gameObject.transform.SetPositionAndRotation(
                    position,
                    Quaternion.Euler(0, _mainCamera.transform.eulerAngles.y, 0));

                SetPlayerToFirstPersonMode(localPlayer);
            }
        }

        private void ToggleUi()
        {
            if (GetLocalPlayerFromWorld() == null || _playerUi == null) return;
            _playerUi.gameObject.SetActive(_uiHidden);
            _uiHidden = !_uiHidden;
        }

        // === 原版关键逻辑：切 Freecam ===
        private void SetPlayerToFreecamMode(Player localPlayer)
        {
            FreecamPlugin.Logger.LogInfo("[Freecam] === Entering Freecam mode ===");

            // 1. 玩家 POV → ThirdPerson（让身体可见）
            localPlayer.PointOfView = EPointOfView.ThirdPerson;
            FreecamPlugin.Logger.LogInfo($"[Freecam] Player.PointOfView -> {localPlayer.PointOfView}");

            // 2. PlayerBody.PointOfView.Value = FreeCamera（反射）
            var playerBody = GetPlayerBody(localPlayer);
            FreecamPlugin.Logger.LogInfo($"[Freecam] PlayerBody = {(playerBody == null ? "NULL" : "ok")}");

            if (playerBody != null)
            {
                SetPlayerBodyPov(playerBody, EPointOfView.FreeCamera);

                // 3. 让相机控制器立刻刷新
                try
                {
                    localPlayer.GetComponent<PlayerCameraController>().UpdatePointOfView();
                    FreecamPlugin.Logger.LogInfo("[Freecam] UpdatePointOfView called.");
                }
                catch (Exception ex)
                {
                    FreecamPlugin.Logger.LogError($"[Freecam] UpdatePointOfView failed: {ex}");
                }
            }

            // 4. 关玩家输入
            if (_gamePlayerOwner != null)
                _gamePlayerOwner.enabled = false;

            // 5. 恢复上次位置
            if (FreecamConfig.Data.rememberLastPosition && _lastPosition.HasValue && _lastRotation.HasValue)
            {
                _mainCamera.transform.position = _lastPosition.Value;
                _mainCamera.transform.rotation = _lastRotation.Value;
            }

            _freeCamScript.IsActive = true;
            FreecamPlugin.Logger.LogInfo("[Freecam] === Freecam mode ON ===");
        }

        private void SetPlayerToFirstPersonMode(Player localPlayer)
        {
            _freeCamScript.IsActive = false;

            if (FreecamConfig.Data.rememberLastPosition)
            {
                _lastPosition = _mainCamera.transform.position;
                _lastRotation = _mainCamera.transform.rotation;
            }

            if (_gamePlayerOwner != null) _gamePlayerOwner.enabled = true;
            localPlayer.PointOfView = EPointOfView.FirstPerson;
            FreecamPlugin.Logger.LogInfo("[Freecam] === Freecam mode OFF ===");
        }

        private void ToggleCameraControls()
        {
            if (_freeCamScript.IsActive)
            {
                _controlsToggled = true;
                _freeCamScript.IsActive = false;
                if (_gamePlayerOwner != null) _gamePlayerOwner.enabled = true;
            }
            else
            {
                _controlsToggled = false;
                _freeCamScript.IsActive = true;
                if (_gamePlayerOwner != null) _gamePlayerOwner.enabled = false;
            }
        }

        private static Player GetLocalPlayerFromWorld()
        {
            try
            {
                var gw = Singleton<GameWorld>.Instance;
                if (gw != null) return gw.MainPlayer;
            }
            catch (Exception ex)
            {
                FreecamPlugin.Logger.LogError($"[Freecam] GetLocalPlayerFromWorld: {ex}");
            }
            return null;
        }

        private static PlayerBody GetPlayerBody(Player player)
        {
            if (player == null) return null;

            try
            {
                var prop = player.GetType().GetProperty("PlayerBody",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                {
                    var body = prop.GetValue(player) as PlayerBody;
                    if (body != null) return body;
                }
            }
            catch { }

            try
            {
                var field = player.GetType().GetField("_playerBody",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    var body = field.GetValue(player) as PlayerBody;
                    if (body != null) return body;
                }
            }
            catch { }

            try
            {
                var body = player.GetComponent<PlayerBody>();
                if (body != null) return body;
            }
            catch { }

            return null;
        }

        private static void SetPlayerBodyPov(PlayerBody playerBody, EPointOfView pov)
        {
            try
            {
                var povProp = playerBody.GetType().GetProperty("PointOfView",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (povProp == null)
                {
                    FreecamPlugin.Logger.LogWarning("[Freecam] PlayerBody.PointOfView property not found.");
                    return;
                }

                var bindable = povProp.GetValue(playerBody);
                if (bindable == null)
                {
                    FreecamPlugin.Logger.LogWarning("[Freecam] PlayerBody.PointOfView value is null.");
                    return;
                }

                var valueProp = bindable.GetType().GetProperty("Value",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (valueProp == null)
                {
                    FreecamPlugin.Logger.LogWarning("[Freecam] BindableState.Value property not found.");
                    return;
                }

                valueProp.SetValue(bindable, pov);
                FreecamPlugin.Logger.LogInfo($"[Freecam] PlayerBody.PointOfView.Value set to {pov}");
            }
            catch (Exception ex)
            {
                FreecamPlugin.Logger.LogError($"[Freecam] SetPlayerBodyPov failed: {ex}");
            }
        }

        public void OnDestroy()
        {
            if (_freeCamScript != null)
                Destroy(_freeCamScript);
        }
    }
}