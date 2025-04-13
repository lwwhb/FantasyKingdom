using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEditor;

namespace Unity.FantasyKingdom
{
    public class RTSCameraController : MonoBehaviour
    {

        #region Events

        public event EventHandler<OnZoomHandledEventArgs> OnZoomHandled;
        public event EventHandler<OnZoomDoneEventArgs> OnZoomDone;
        public EventHandler<OnZoomMidPointEventArgs> OnZoomMidPoint;
        public class OnZoomHandledEventArgs : EventArgs
        {
            public float zoomDelta;
            public int zoomLevel;
            public float prevShadowDist, shadowDist;
            public Material prevHeight, height;
            public Material prevCube, cube;
            public ZoomLevelData data, prevData;
        }

        public class OnZoomDoneEventArgs : EventArgs
        {
            public int zoomLevel;
            public float shadowDist;
            public ZoomLevelData data;
        }

        public class OnZoomMidPointEventArgs: EventArgs
        {
            public int zoomLevel;
        }

        public event EventHandler OnRotateStarted;
        public event EventHandler OnRotateStopped;
        public event EventHandler<OnRotateHandledEventArgs> OnRotateHandled;
        public class OnRotateHandledEventArgs : EventArgs
        {
            public Vector2 currentRotation;
            public Vector2 targetRotation;
            public bool clockwise;
        }

        public event EventHandler<OnCameraSettingsChangedEventArgs> OnCameraSettingsChanged;

        public class OnCameraSettingsChangedEventArgs : EventArgs
        {
            public int settings_index;
            public int zoomLevel;
            public float nearClip, farClip;
            public float shadowDistance;
            public Material prevHeightFog, heightFog;
            public Material prevCubeFog, CubeFog;
        }


        #endregion

        
        [Range(0,1)]
        private float forceLodPoint = 0.4f;


        [Header("Refs")]
        [SerializeField]
        [Tooltip("The Cinemachine Virtual Camera to be controlled by the controller.")]
        public CinemachineCamera VirtualCamera;

        [SerializeField]
        [Tooltip("The target for the camera to follow.")]
        public Transform CameraTarget;


        [Space]
        [Header("Settings")]
        public CameraControllerSettings[] Settings;

        [Tooltip("How far high the camera is")]
        public float CameraDistance;
        [Expandable]
        public CameraControllerSettings currentSettings;

        [Space]
        [Header("Boundaries")]
        [SerializeField]
        public bool enableBoundaries = true;

        [SerializeField, Range(-10000, 0)] public float BoundaryMinX = -500f;

        [SerializeField, Range(0, 10000)] public float BoundaryMaxX = 500f;

        [SerializeField, Range(-10000, 0)] public float BoundaryMinZ = -500f;

        [SerializeField, Range(0, 10000)] public float BoundaryMaxZ = 500f;

        #region Private Fields

        private IInputProvider _inputProvider;
        private Vector2 _currentMousePosition;
        private CinemachinePositionComposer _framingTransposer;
        private GameObject _virtualCameraGameObject;
        private float _currentCameraZoom;
        private float _currentCameraRotate;
        private float _currentCameraTilt;
        private float _targetCameraRotate;
        private float _originalCameraRotate;
        private float _targetCameraTilt;
        private float _cameraRotateSmoothDampVelRef;
        private float _cameraTiltSmoothDampVelRef;
        private bool _currentRotateDir;
        private bool _isRotating;
        private int _zoomLevelIndex;
        private int _prevZoomIndex;
        private CameraType _currentCameraType;
        private bool _zoomDone;
        private float prevZoomInput;
        private float _startingZoomLevel;
        private bool _cameraState = false;
        private bool midPointFired;
        private ZoomLevelData _zoomLevelData;
        private ZoomLevelData _prevZoomLevelData;
        public enum CameraType
        {
            GameplayCamera = 0,
            FreeCamera = 1
           
        }

        #endregion

        
        void Start()
        {
            currentSettings = Settings[0];
            _virtualCameraGameObject = VirtualCamera.gameObject;
            _framingTransposer = VirtualCamera.GetComponent<CinemachinePositionComposer>();
            _currentCameraZoom = _framingTransposer.CameraDistance;
            _startingZoomLevel = _currentCameraZoom;
            _targetCameraRotate = _virtualCameraGameObject.transform.localRotation.eulerAngles.y;
            _currentCameraRotate = _targetCameraRotate;
            _originalCameraRotate = _targetCameraRotate;
            _targetCameraTilt = _virtualCameraGameObject.transform.localRotation.eulerAngles.x;
            _currentCameraTilt = _targetCameraTilt;
            _inputProvider = GetComponent<IInputProvider>();
            var settingsZoomLevelData = Settings[(int)CameraType.GameplayCamera].ZoomLevelData[_zoomLevelIndex];
            var settingsFreeCamera = Settings[(int)CameraType.FreeCamera];
            OnCameraSettingsChanged?.Invoke(this, new OnCameraSettingsChangedEventArgs
            {
                settings_index = (int)CameraType.GameplayCamera,
                zoomLevel = _zoomLevelIndex,
                prevCubeFog = settingsFreeCamera.FreeCamCubeFog,
                prevHeightFog = settingsFreeCamera.FreeCamHeightFog,
                heightFog = settingsZoomLevelData.HeightFog,
                CubeFog = settingsZoomLevelData.CubeFog,
                nearClip = Settings[(int)CameraType.GameplayCamera].ZoomLevelData[_zoomLevelIndex].CameraNearPlane,
                farClip = Settings[(int)CameraType.GameplayCamera].ZoomLevelData[_zoomLevelIndex].CameraFarPlane,
                shadowDistance = settingsZoomLevelData.MaxShadowDistance
            });

            Debug.Assert(_inputProvider != null, "No Input Provider found! Please ensure there's one attached to this gameObject", gameObject);
        }

        void Update()
        {
            _currentMousePosition = _inputProvider.MousePosition;

            if (Application.isMobilePlatform)
            {
                HandleTouchDrag();
            }
            else
            {
                if (_inputProvider.HasMouse)
                {
                    HandleScreenSideMove(_currentMousePosition);
                }
                
                HandleMove();
            }

            HandleRotation();
            HandleZoom();
            HandleBoundaries();
            
            if (_inputProvider.DPadNorth())
                SwitchCurrentSettings();
        }

        public void SwitchCurrentSettings()
        {
            _cameraState = !_cameraState;
            _zoomDone = false;
            CameraType nextSettingsIndex = _cameraState ? CameraType.FreeCamera : CameraType.GameplayCamera;
            var settingsZoomLevelData = Settings[(int)CameraType.GameplayCamera].ZoomLevelData[_zoomLevelIndex];
            var settingsFreeCamera = Settings[(int)CameraType.FreeCamera];

            if (_currentCameraType == CameraType.GameplayCamera)
            {
                OnCameraSettingsChanged?.Invoke(this, new OnCameraSettingsChangedEventArgs
                {
                    settings_index = (int)nextSettingsIndex,
                    zoomLevel = _zoomLevelIndex,
                    prevCubeFog = settingsZoomLevelData.CubeFog,
                    prevHeightFog = settingsZoomLevelData.HeightFog,
                    heightFog = settingsFreeCamera.FreeCamHeightFog,
                    CubeFog = settingsFreeCamera.FreeCamCubeFog,
                    nearClip = settingsFreeCamera.NearClip,
                    farClip = settingsFreeCamera.FarClip
                    
                });
            }
            else
            {
                OnCameraSettingsChanged?.Invoke(this, new OnCameraSettingsChangedEventArgs
                {
                    settings_index = (int)nextSettingsIndex,
                    zoomLevel = _zoomLevelIndex,
                    prevCubeFog = settingsFreeCamera.FreeCamCubeFog,
                    prevHeightFog = settingsFreeCamera.FreeCamHeightFog,
                    heightFog = settingsZoomLevelData.HeightFog,
                    CubeFog = settingsZoomLevelData.CubeFog,
                    nearClip = Settings[(int)CameraType.GameplayCamera].ZoomLevelData[_zoomLevelIndex].CameraNearPlane,
                    farClip = Settings[(int)CameraType.GameplayCamera].ZoomLevelData[_zoomLevelIndex].CameraFarPlane,
                    shadowDistance = settingsZoomLevelData.MaxShadowDistance
                });
                _currentCameraZoom = settingsZoomLevelData.ZoomAmount;
                _targetCameraTilt = settingsZoomLevelData.TiltAmount;
            }
            currentSettings = Settings[(int)nextSettingsIndex];
            _currentCameraType = nextSettingsIndex;
        }

        #region private methods


        private void HandleTouchDrag()
        {
            if (!_isRotating && _inputProvider.DragButton)
            {
                Vector3 vectorChange = new Vector3(_inputProvider.DragDelta.x, 0, _inputProvider.DragDelta.y) * -1;
                MoveTargetRelativeToCamera(vectorChange, currentSettings.CameraDragSpeed / 10);
            }
        }
        private void HandleScreenSideMove(Vector3 mousePos)
        {
            GetMouseScreenSide(mousePos, out int widthPos, out int heightPos);
            Vector3 moveVector = new Vector3(widthPos, 0, heightPos);
            if (moveVector != Vector3.zero && !_isRotating)
            {
                MoveTargetRelativeToCamera(moveVector, currentSettings.CameraScreenSideSpeed);
            }
        }

        private void HandleMove()
        {
            Vector2 moveInput = _inputProvider.MovementInput();
            if (moveInput.sqrMagnitude > 0f && (!_isRotating || _inputProvider.CanAlwaysRotate))
            {
                Vector3 moveVector = new Vector3(moveInput.x, 0, moveInput.y);
                MoveTargetRelativeToCamera(moveVector, currentSettings.CameraScreenSideSpeed);
            }
        }

        private void HandleRotation()
        {
            bool rotationMousePressed = _inputProvider.RotationButtonInput() || _inputProvider.CanAlwaysRotate;
            if (!_isRotating && rotationMousePressed)
            {
                LockMouse(true);
                _isRotating = true;
                OnRotateStarted?.Invoke(this, EventArgs.Empty);

            }

            bool mouseRelease = !_inputProvider.RotationButtonInput() && _inputProvider.LookInput == Vector2.zero;

            if (_isRotating && mouseRelease)
            {
                LockMouse(false);
                _isRotating = false;
                OnRotateStopped?.Invoke(this, EventArgs.Empty);

            }

            if (_isRotating)
            {
                Vector2 rotationInput = _inputProvider.LookInput;
                if (rotationInput.x != 0)
                {
                    _currentRotateDir = rotationInput.x > 0 ? true : false;
                    _targetCameraRotate += rotationInput.x * currentSettings.CameraRotateSpeed;
                    if (currentSettings.IsRestricted)
                    {
                        _targetCameraRotate = Mathf.Clamp(_targetCameraRotate, currentSettings.CameraRotateMin, currentSettings.CameraRotateMax);
                    }
                }

                if (rotationInput.y != 0 && !currentSettings.IsRestricted)
                {
                    _targetCameraTilt -= rotationInput.y * currentSettings.CameraRotateSpeed * 0.1f;
                    _targetCameraTilt = Mathf.Clamp(_targetCameraTilt, currentSettings.CameraTiltMin, currentSettings.CameraTiltMax);
                }

            }
            else if (currentSettings.IsRestricted)
            {
                _targetCameraRotate = _originalCameraRotate;
            }

            _currentCameraRotate = Mathf.SmoothDamp(_currentCameraRotate, _targetCameraRotate, ref _cameraRotateSmoothDampVelRef,
                currentSettings.CameraTargetRotateSmoothTime / 100, Mathf.Infinity, Time.deltaTime);

            _currentCameraTilt = Mathf.SmoothDamp(_currentCameraTilt, _targetCameraTilt, ref _cameraTiltSmoothDampVelRef,
                currentSettings.CameraTargetRotateSmoothTime / 100, Mathf.Infinity, Time.deltaTime);

            _virtualCameraGameObject.transform.eulerAngles = new Vector3(_currentCameraTilt, _currentCameraRotate, 0);
            OnRotateHandled?.Invoke(this, new OnRotateHandledEventArgs
            {
                clockwise = _currentRotateDir,
                currentRotation = new Vector2(_currentCameraRotate, _currentCameraTilt),
                targetRotation = new Vector2(_targetCameraRotate, _targetCameraTilt)
            });
        }


        private void HandleZoom()
        {
            float zoomInput = _inputProvider.ZoomInput;

            if (currentSettings.IsRestricted)
            {
                
                if (prevZoomInput == 0 && _zoomDone)
                {
                    
                    if (zoomInput < 0 && _zoomLevelIndex < currentSettings.ZoomLevelData.Count - 1)
                    {
                        _zoomDone = false;
                        _zoomLevelIndex++;
                    }
                    if (zoomInput > 0 && _zoomLevelIndex > 0)
                    {
                        _zoomDone = false;
                        _zoomLevelIndex--;
                    }
                   
                    _currentCameraZoom = currentSettings.ZoomLevelData[_zoomLevelIndex].ZoomAmount;
                    _targetCameraTilt = currentSettings.ZoomLevelData[_zoomLevelIndex].TiltAmount;
                    midPointFired = false;
                }
                _zoomLevelData = currentSettings.ZoomLevelData[_zoomLevelIndex];
                _prevZoomLevelData = currentSettings.ZoomLevelData[_prevZoomIndex];
            }
            else
            {
                if (zoomInput != 0)
                {
                    _startingZoomLevel = _framingTransposer.CameraDistance;
                    _zoomDone = false;
                }
                _currentCameraZoom -= zoomInput * currentSettings.CameraZoomSpeed;
                _currentCameraZoom = Mathf.Clamp(_currentCameraZoom, currentSettings.CameraZoomMin, currentSettings.CameraZoomMax);
            }
            
            // It's important for this value to be the real distance between the target and the camera,
            // not _framingTransposer.CameraDistance, which is the distance the camera eases towards,
            // as the real distance has to be used to lerp fog properties.
            float cameraDistance = (CameraTarget.position - VirtualCamera.transform.position).magnitude;
            
            if (!_zoomDone)
            {
                t += Time.deltaTime;
                float zoomDelta = Remap(cameraDistance, _startingZoomLevel, _currentCameraZoom, 0, 1);

               

                if (currentSettings.IsRestricted)
                {
                    if (zoomDelta > forceLodPoint && !midPointFired)
                    {
                        midPointFired = true;
                        OnZoomMidPoint?.Invoke(this, new OnZoomMidPointEventArgs
                        {
                            zoomLevel = _zoomLevelIndex
                        });
                    }
                    OnZoomHandled?.Invoke(this, new OnZoomHandledEventArgs
                    {
                        zoomDelta = zoomDelta,
                        zoomLevel = _zoomLevelIndex,
                        height = _zoomLevelData.HeightFog,
                        cube = _zoomLevelData.CubeFog,
                        prevHeight = _prevZoomLevelData.HeightFog,
                        prevCube = _prevZoomLevelData.CubeFog,
                        prevShadowDist = currentSettings.ZoomLevelData[_prevZoomIndex].MaxShadowDistance,
                        shadowDist = currentSettings.ZoomLevelData[_zoomLevelIndex].MaxShadowDistance,
                        data = currentSettings.ZoomLevelData[_zoomLevelIndex],
                        prevData = currentSettings.ZoomLevelData[_prevZoomIndex]
                    });
                }
            }
            
            _framingTransposer.CameraDistance = Mathf.Lerp(_startingZoomLevel, _currentCameraZoom, t / currentSettings.CameraZoomSmoothTime);
            
            if (Mathf.Abs(cameraDistance - _currentCameraZoom) < 5.0f)
            {
                if (!_zoomDone)
                {
                  _zoomDone = true;
                    t = 0;
                    _prevZoomIndex = _zoomLevelIndex;
                    _framingTransposer.CameraDistance = _currentCameraZoom;
                    _startingZoomLevel = _framingTransposer.CameraDistance;
                    if (currentSettings.IsRestricted)
                    {
                        OnZoomDone?.Invoke(this, new OnZoomDoneEventArgs
                        {
                            zoomLevel = _zoomLevelIndex,
                            shadowDist = currentSettings.ZoomLevelData[_zoomLevelIndex].MaxShadowDistance,
                            data = currentSettings.ZoomLevelData[_zoomLevelIndex]
                        });
                    }
                }
            }
            prevZoomInput = zoomInput;
        }

        float t = 0;
        private void GetMouseScreenSide(Vector3 mousePosition, out int width, out int height)
        {
            int heightPos = 0;
            int widthPos = 0;
            if (mousePosition.x >= 0 && mousePosition.x <= currentSettings.ScreenSidesZoneSize)
                widthPos = -1;
            else if (mousePosition.x >= Screen.width - currentSettings.ScreenSidesZoneSize && mousePosition.x <= Screen.width)
                widthPos = 1;
            if (mousePosition.y >= 0 && mousePosition.y <= currentSettings.ScreenSidesZoneSize)
                heightPos = -1;
            else if (mousePosition.y >= Screen.height - currentSettings.ScreenSidesZoneSize && mousePosition.y <= Screen.height)
                heightPos = 1;
            width = widthPos;
            height = heightPos;
        }
        private void MoveTargetRelativeToCamera(Vector3 direction, float speed)
        {
            float minZoom = 1;
            if (currentSettings.IsRestricted)
            {
                minZoom = currentSettings.ZoomLevelData[0].ZoomAmount;
            }
            else
            {
                minZoom = currentSettings.CameraZoomMin;
            }

            float relativeZoomCameraMoveSpeed = _framingTransposer.CameraDistance / minZoom;
            Vector3 camForward = _virtualCameraGameObject.transform.forward;
            Vector3 camRight = _virtualCameraGameObject.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            Vector3 relativeDir = (2 * direction.z * camForward) + (camRight * direction.x);

            CameraTarget.Translate(relativeDir * (relativeZoomCameraMoveSpeed * speed * Time.deltaTime));
        }

        private void HandleBoundaries()
        {
            if (CameraTarget.position.x > BoundaryMaxX)
                CameraTarget.position = new Vector3(BoundaryMaxX, CameraTarget.position.y, CameraTarget.position.z);
            if (CameraTarget.position.x < BoundaryMinX)
                CameraTarget.position = new Vector3(BoundaryMinX, CameraTarget.position.y, CameraTarget.position.z);
            if (CameraTarget.position.z > BoundaryMaxZ)
                CameraTarget.position = new Vector3(CameraTarget.position.x, CameraTarget.position.y, BoundaryMaxZ);
            if (CameraTarget.position.z < BoundaryMinZ)
                CameraTarget.position = new Vector3(CameraTarget.position.x, CameraTarget.position.y, BoundaryMinZ);
        }

        private void LockMouse(bool lockMouse)
        {
            Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = lockMouse ? false : true;
        }

        public float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return Mathf.Clamp((value - from1) / (to1 - from1) * (to2 - from2) + from2, from2, to2);
        }


        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (enableBoundaries)
            {
                Handles.color = Color.green;
                Handles.DrawLine(new Vector3(BoundaryMinX, 0, BoundaryMinZ), new Vector3(BoundaryMaxX, 0, BoundaryMinZ));
                Handles.DrawLine(new Vector3(BoundaryMaxX, 0, BoundaryMinZ), new Vector3(BoundaryMaxX, 0, BoundaryMaxZ));
                Handles.DrawLine(new Vector3(BoundaryMinX, 0, BoundaryMinZ), new Vector3(BoundaryMinX, 0, BoundaryMaxZ));
                Handles.DrawLine(new Vector3(BoundaryMinX, 0, BoundaryMaxZ), new Vector3(BoundaryMaxX, 0, BoundaryMaxZ));
                Handles.Label(new Vector3(BoundaryMinX, 0, 0), $"Min X: {BoundaryMinX}");
                Handles.Label(new Vector3(BoundaryMaxX, 0, 0), $"Max X: {BoundaryMaxX}");
                Handles.Label(new Vector3(0, 0, BoundaryMinZ), $"Min Z: {BoundaryMinZ}");
                Handles.Label(new Vector3(0, 0, BoundaryMaxZ), $"Max Z: {BoundaryMaxZ}");
            }
        }
      
#endif

    }
}
