using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Unity.FantasyKingdom
{
    public class InputProvider : MonoBehaviour, IInputProvider
    {
        private enum MouseButton
        {
            Left,
            Right,
            Middle
        }

        [SerializeField]
        [Tooltip("The mouse button for rotating.")]
        private MouseButton rotationMouseButton = MouseButton.Right;

        [SerializeField]
        [Tooltip("Threshold for the pinch gesture.")]
        private float pinchThreshold = 15.0f;


        [SerializeField]
        [Tooltip("Minimum width of rotation gesture")]
        private float min_rotation_width = 1.0f;

        [SerializeField]
        [Tooltip("Minimum angle threshold the gesture needs to exceed in order to register")]
        private float min_rotation_angle = 1.0f;
        
        public Vector2 MouseInput { get => _mouseInput; }
        public Vector2 MousePosition { get => _mousePosition;}

        public Vector2 MovementInput() => GetMovementInput();
        public Vector2 LookInput => GetLookInput();

        public bool RotationButtonInput() => GetRotationButtonInput() || _mouseInput.x != 0f;

        public bool CanAlwaysRotate { get; private set; }

        public bool GamePadConnected { get; private set; }

        public bool DPadNorth()
        {
            return _switchSettingsAction?.WasPressedThisFrame() ?? default;
        }

        public bool CycleQualityUpButton()
        {
            return _cycleQualityUpAction?.WasPressedThisFrame() ?? default;
        }
        public bool CycleQualityDownButton()
        {
            return _cycleQualityDownAction?.WasPressedThisFrame() ?? default;
        }

        public bool StatPanelGesture { get; private set; }

        public bool StatPanelButton { get; private set; }

        public float ZoomInput { get => _zoomInput;}

        public bool DragButton { get => _dragInput; }

        public Vector2 DragDelta { get => _dragDelta; }

        [SerializeField] InputActionAsset _inputActionAsset;
        
        private Vector2 _mousePosition;
        private float _zoomInput;
        private Vector2 _mouseInput;
        private Vector2 _dragDelta;
        private bool _dragInput;
        private bool _isRotating;
        private Vector2 _rotationStartVector = Vector2.zero;

        public bool HasTouch => Touchscreen.current != null;
        public bool HasMouse => Mouse.current != null;

        Vector2 GetMovementInput() => _moveAction?.ReadValue<Vector2>() ?? default;
        Vector2 GetLookInput()
        {
            if (Application.isMobilePlatform)
            {
                return _mouseInput;
            }

            return _lookAction?.ReadValue<Vector2>() ?? default;
        }

        bool GetRotationButtonInput() => _rotationMouseButtonControl?.isPressed ?? default;

        ButtonControl _rotationMouseButtonControl;
        InputAction _moveAction;
        InputAction _lookAction;
        InputAction _toggleStatsAction;
        InputAction _switchSettingsAction;
        InputAction _cycleQualityUpAction;
        InputAction _cycleQualityDownAction;
        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            _inputActionAsset.Enable();

            if(rotationMouseButton == MouseButton.Left) _rotationMouseButtonControl = Mouse.current?.leftButton;
            else if(rotationMouseButton == MouseButton.Middle) _rotationMouseButtonControl = Mouse.current?.middleButton;
            else if(rotationMouseButton == MouseButton.Right) _rotationMouseButtonControl = Mouse.current?.rightButton;
            
            _moveAction = _inputActionAsset.FindAction("Move");
            _lookAction = _inputActionAsset.FindAction("Look");
            _toggleStatsAction = _inputActionAsset.FindAction("ToggleStats");
            _switchSettingsAction = _inputActionAsset.FindAction("SwitchSettings");
            _cycleQualityUpAction = _inputActionAsset.FindAction("CycleQualityUp");
            _cycleQualityDownAction = _inputActionAsset.FindAction("CycleQualityDown");
            Debug.Assert(_moveAction != null);
            Debug.Assert(_lookAction != null);
            Debug.Assert(_toggleStatsAction != null);
            Debug.Assert(_switchSettingsAction != null);
            Debug.Assert(_cycleQualityUpAction != null);
            Debug.Assert(_cycleQualityDownAction != null);
            if (QualitySettings.count < 2)
            {
                _cycleQualityUpAction.Disable();
                _cycleQualityDownAction.Disable();
            }
        }
        
        void Update()
        {
            if (Application.isMobilePlatform)
            {
                var touchScreen = Touchscreen.current;
                if (touchScreen != null)
                {
                    var touches = Touch.activeTouches;
                    UpdateInput(touches);
                }
                else
                {
                    Debug.LogError($"Unexpectedly encountered no touch screen on mobile device!");
                }
            }
            else
            {
                StatPanelButton = _toggleStatsAction?.WasPressedThisFrame() ?? default;
                
                // Center mouse position (might be overriden later)
                _mousePosition = new Vector2(Screen.width / 2f, Screen.height / 2f);

                var mouse = Mouse.current;
                var keyboard = Keyboard.current;
                var gamepad = Gamepad.current;
                
                if (mouse != null && keyboard != null)
                {
                    UpdateInput(mouse, keyboard, gamepad);
                }

                GamePadConnected = gamepad != null && gamepad.enabled;
                CanAlwaysRotate = GamePadConnected;

                if (GamePadConnected)
                {
                    UpdateInput(gamepad);
                }
            }
        }

        void DetectToggleMenuGesture(ReadOnlyArray<Touch> touches)
        {
            const TouchPhase kExpectedTouchPhase = TouchPhase.Began;

            StatPanelGesture = false;
            
            if (touches.Count == 4)
            {
                foreach (var touch in touches)
                {
                    if (touch.phase == kExpectedTouchPhase && touch.tapCount == 2)
                    {
                        StatPanelGesture = true;
                        return;
                    }
                }
            }
        }
        
        void UpdateInput(ReadOnlyArray<Touch> touches)
        {
            DetectToggleMenuGesture(touches);

            if (touches.Count == 1)
            {
                _dragInput = true;
                _mousePosition = touches[0].screenPosition;
                _dragDelta = touches[0].delta;
            }
            else
            {
                _dragInput = false;
                _dragDelta = Vector2.zero;
            }
            
            if (touches.Count > 1)
            {
                var zero = touches[0];
                var one = touches[1];

                if (zero.phase == TouchPhase.Began || one.phase == TouchPhase.Began) 
                {
                    _rotationStartVector = one.screenPosition - zero.screenPosition;
                }


                if (zero.phase == TouchPhase.Moved || one.phase == TouchPhase.Moved)
                {
                    Vector2 currRotVec = one.screenPosition - zero.screenPosition;
                    float angle = Vector2.SignedAngle(_rotationStartVector, currRotVec);
                    if(Mathf.Abs(angle) > min_rotation_angle && _rotationStartVector.sqrMagnitude > min_rotation_width)
                    {
                        _mouseInput.x = angle;
                        _isRotating = true;
                    }
                    else
                    {
                        _isRotating= false;
                        _mouseInput.x = 0f;
                    }
                   
                  
                    _rotationStartVector = currRotVec;

                    if (!_isRotating)
                    {
                        Vector2 touchZeroPrevPos = zero.screenPosition - zero.delta;
                        Vector2 touchOnePrevPos = one.screenPosition - one.delta;
                        float prevDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                        float touchDeltaMag = (zero.screenPosition - one.screenPosition).magnitude;
                        float deltaDiff = prevDeltaMag - touchDeltaMag;
                        if (Mathf.Abs(deltaDiff) >= pinchThreshold)
                        {
                            _zoomInput = (deltaDiff *0.1f) * -1;

                        }
                        else
                        {
                            _mouseInput.y = zero.delta.y ;
                            _zoomInput = 0;
                        }
                    }
                }

                if(zero.phase == TouchPhase.Stationary && one.phase == TouchPhase.Stationary)
                {
                    _mouseInput = Vector2.zero;
                    _zoomInput = 0;
                }
            }
            else
            {
                _zoomInput = 0;
                _mouseInput = Vector2.zero;
            }
        }

        void UpdateInput(Mouse mouse, Keyboard keyboard, Gamepad gamepad)
        {
            // Block mouse input if gamepad is connected
            if (gamepad is not { enabled: true })
            {
                _mousePosition = mouse.position.value;
                _zoomInput = mouse.scroll.value.y;
            }
        }
        
        void UpdateInput(Gamepad gamepad)
        {
            _zoomInput = 0f;

            if (gamepad.leftShoulder.isPressed)
                _zoomInput += 1f;
            
            if (gamepad.rightShoulder.isPressed)
                _zoomInput -= 1f;
        }
    }
}
