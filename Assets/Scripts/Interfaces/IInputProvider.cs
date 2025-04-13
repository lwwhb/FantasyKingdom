using UnityEngine;
namespace Unity.FantasyKingdom
{
    public interface IInputProvider
    {
        public bool HasTouch { get; }
        public bool HasMouse { get; }
        
        public Vector2 MovementInput();
        public Vector2 LookInput { get; }
        public Vector2 MouseInput { get;  }
        public float ZoomInput { get; }
        public bool RotationButtonInput();
        public bool CanAlwaysRotate { get; }

        public bool DPadNorth();

        public bool CycleQualityUpButton();
        public bool CycleQualityDownButton();

        public bool StatPanelButton { get; }

        public bool StatPanelGesture { get; }

        public bool DragButton { get; }

        public Vector2 DragDelta { get; }
        public Vector2 MousePosition { get;  }
    }
}