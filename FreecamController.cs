using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace Terkoiz.Freecam
{
    // This class is registered as an IL2CPP component by FreecamPlugin.Load().
    public class FreecamController : MonoBehaviour
    {
        private GameObject _mainCamera;
        private PlayerCameraController _cameraController;
        private EftBattleUIScreen _playerUi;
        private UnityEngine.Object _gamePlayerOwner;

        private Vector3? _lastPosition;
        private Quaternion? _lastRotation;

        private bool _uiHidden;
        private bool _freecamActive;
        private bool _controlsToggled;

        private float _nextInitTime;
        private const float INIT_INTERVAL = 0.5f;

        private static PropertyInfo _enabledProperty;

        public bool IsFreecamActive => _freecamActive || _controlsToggled;
        public PlayerCameraController CameraController => _cameraController;

        public FreecamController(IntPtr ptr) : base(ptr)
        {
        }

        private void Start()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            var localPlayer = GetLocalPlayerFromWorld();
            if (localPlayer == null)
            {
                FreecamPlugin.Logger.LogWarning("FreecamController started before MainPlayer was available.");
                return;
            }

            if (_cameraController == null)
            {
                _cameraController = localPlayer.GetComponent<PlayerCameraController>();
                if (_cameraController == null)
                {
                    FreecamPlugin.Logger.LogError("Failed to locate PlayerCameraController.");
                    return;
                }
            }

            if (_mainCamera == null)
            {
                var cam = _cameraController.Camera;
                if (cam != null)
                {
                    _mainCamera = cam.gameObject;
                }

                if (_mainCamera == null)
                {
                    FreecamPlugin.Logger.LogError("Failed to locate main camera.");
                    return;
                }
            }

            if (_playerUi == null)
            {
                _playerUi = UnityEngine.Object.FindObjectOfType<EftBattleUIScreen>();
                if (_playerUi == null)
                    FreecamPlugin.Logger.LogWarning("Failed to locate raid UI. UI toggle will be unavailable.");
            }

            if (_gamePlayerOwner == null)
            {
                var owner = localPlayer.GetComponentInChildren<GamePlayerOwner>();
                if (owner != null)
                {
                    _gamePlayerOwner = owner;
                }
                else
                {
                    _gamePlayerOwner = UnityEngine.Object.FindObjectOfType<GamePlayerOwner>();
                }

                if (_gamePlayerOwner == null)
                    FreecamPlugin.Logger.LogWarning("Failed to locate GamePlayerOwner.");
            }

            FreecamPlugin.Logger.LogInfo("Freecam controller initialized.");
        }

        private void Update()
        {
            if (_mainCamera == null || _cameraController == null)
            {
                if (Time.time >= _nextInitTime)
                {
                    _nextInitTime = Time.time + INIT_INTERVAL;
                    TryInitialize();
                }
                return;
            }

            if (Input.GetKeyDown(FreecamPlugin.ToggleUi.Value))
                ToggleUi();

            if (Input.GetKeyDown(FreecamPlugin.ToggleFreecamMode.Value))
                ToggleCamera();

            if (Input.GetKeyDown(FreecamPlugin.ToggleFreecamControls.Value))
                ToggleCameraControls();

            if (Input.GetKeyDown(FreecamPlugin.TeleportToCamera.Value))
                MovePlayerToCamera();

            if (_freecamActive)
                UpdateFreecamMovement();
        }

        private void UpdateFreecamMovement()
        {
            var transform = _mainCamera.transform;
            var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var movementSpeed = fastMode
                ? FreecamPlugin.CameraFastMoveSpeed.Value
                : FreecamPlugin.CameraMoveSpeed.Value;

            var delta = movementSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                transform.position += -transform.right * delta;

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                transform.position += transform.right * delta;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                transform.position += transform.forward * delta;

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                transform.position += -transform.forward * delta;

            if (FreecamPlugin.CameraHeightMovement.Value)
            {
                if (Input.GetKey(KeyCode.Q))
                    transform.position += transform.up * delta;

                if (Input.GetKey(KeyCode.E))
                    transform.position += -transform.up * delta;

                if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
                    transform.position += Vector3.up * delta;

                if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
                    transform.position += -Vector3.up * delta;
            }

            float newRotationX =
                transform.localEulerAngles.y +
                Input.GetAxis("Mouse X") * FreecamPlugin.CameraLookSensitivity.Value;

            float newRotationY =
                transform.localEulerAngles.x -
                Input.GetAxis("Mouse Y") * FreecamPlugin.CameraLookSensitivity.Value;

            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);

            if (FreecamPlugin.CameraMousewheelZoom.Value)
            {
                float axis = Input.GetAxis("Mouse ScrollWheel");
                if (axis != 0f)
                {
                    var zoomSensitivity = fastMode
                        ? FreecamPlugin.CameraFastZoomSpeed.Value
                        : FreecamPlugin.CameraZoomSpeed.Value;

                    transform.position += transform.forward * (axis * zoomSensitivity);
                }
            }
        }

        private void ToggleCamera()
        {
            var localPlayer = GetLocalPlayerFromWorld();
            if (localPlayer == null)
                return;

            if (!_freecamActive)
                SetPlayerToFreecamMode(localPlayer);
            else
                SetPlayerToFirstPersonMode(localPlayer);
        }

        private void MovePlayerToCamera()
        {
            var localPlayer = GetLocalPlayerFromWorld();
            if (localPlayer == null || !_freecamActive)
                return;

            var position = new Vector3(
                _mainCamera.transform.position.x,
                _mainCamera.transform.position.y - 1.8f,
                _mainCamera.transform.position.z);

            localPlayer.gameObject.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, _mainCamera.transform.eulerAngles.y, 0f));

            SetPlayerToFirstPersonMode(localPlayer);
        }

        private void ToggleUi()
        {
            if (GetLocalPlayerFromWorld() == null || _playerUi == null)
                return;

            _playerUi.gameObject.SetActive(_uiHidden);
            _uiHidden = !_uiHidden;
        }

        private void SetPlayerToFreecamMode(Player localPlayer)
        {
            // *** 关键 ***
            // 告诉游戏相机控制器：相机已被外部代码接管，别再覆盖位置了
            if (_cameraController != null)
            {
                _cameraController.ExternalControl = true;
            }

            SetGamePlayerOwnerEnabled(false);

            if (FreecamPlugin.CameraRememberLastPosition.Value &&
                _lastPosition.HasValue &&
                _lastRotation.HasValue)
            {
                _mainCamera.transform.position = _lastPosition.Value;
                _mainCamera.transform.rotation = _lastRotation.Value;
            }

            _freecamActive = true;
        }

        private void SetPlayerToFirstPersonMode(Player localPlayer)
        {
            _freecamActive = false;

            if (FreecamPlugin.CameraRememberLastPosition.Value)
            {
                _lastPosition = _mainCamera.transform.position;
                _lastRotation = _mainCamera.transform.rotation;
            }

            // 交还给游戏
            if (_cameraController != null)
            {
                _cameraController.ExternalControl = false;
            }

            _controlsToggled = false;
            SetGamePlayerOwnerEnabled(true);
        }

        private void ToggleCameraControls()
        {
            if (_freecamActive)
            {
                _controlsToggled = true;
                _freecamActive = false;
                if (_cameraController != null) _cameraController.ExternalControl = false;
                SetGamePlayerOwnerEnabled(true);
            }
            else
            {
                _controlsToggled = false;
                _freecamActive = true;
                if (_cameraController != null) _cameraController.ExternalControl = true;
                SetGamePlayerOwnerEnabled(false);
            }
        }

        private static Player GetLocalPlayerFromWorld()
        {
            try
            {
                if (Singleton<GameWorld>.Instantiated)
                {
                    var gameWorld = Singleton<GameWorld>.Instance;
                    if (gameWorld != null)
                    {
                        return gameWorld.MainPlayer;
                    }
                }
            }
            catch (Exception ex)
            {
                FreecamPlugin.Logger.LogError($"GetLocalPlayerFromWorld failed: {ex}");
            }
            return null;
        }

        private void OnDestroy()
        {
            if (_cameraController != null)
            {
                _cameraController.ExternalControl = false;
            }

            SetGamePlayerOwnerEnabled(true);
            _freecamActive = false;
            _controlsToggled = false;
        }

        private void SetGamePlayerOwnerEnabled(bool enabled)
        {
            if (_gamePlayerOwner == null)
                return;

            try
            {
                Type runtimeType = _gamePlayerOwner.GetType();
                bool isBehaviour = _gamePlayerOwner is Behaviour;

                if (isBehaviour)
                {
                    ((Behaviour)_gamePlayerOwner).enabled = enabled;
                    return;
                }

                if (_enabledProperty == null)
                {
                    _enabledProperty = runtimeType.GetProperty(
                        "enabled",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                if (_enabledProperty != null)
                {
                    _enabledProperty.SetValue(_gamePlayerOwner, enabled);
                }
            }
            catch (Exception ex)
            {
                FreecamPlugin.Logger.LogError($"SetGamePlayerOwnerEnabled failed: {ex}");
            }
        }
    }
}