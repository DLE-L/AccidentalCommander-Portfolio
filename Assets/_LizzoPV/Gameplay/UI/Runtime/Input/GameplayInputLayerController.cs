using UnityEngine;

namespace Lizzo.PV.Gameplay.Input
{
    [DisallowMultipleComponent]
    public sealed class GameplayInputLayerController : MonoBehaviour
    {
        [SerializeField]
        private GameplayFloatingJoystickController _joystick;

        public GameplayFloatingJoystickController Joystick => _joystick;

        public bool Configure()
        {
            if (_joystick == null)
            {
                Debug.LogError("[GameplayInputLayerController] Authored GameplayFloatingJoystickController reference is required.", this);
                return false;
            }

            return _joystick.Configure();
        }

        public bool BindPlayer(PlayerController player)
        {
            return Configure() && _joystick.BindPlayer(player);
        }

        public bool SetInputEnabled(bool enabled)
        {
            return Configure() && _joystick.SetInputEnabled(enabled);
        }
    }
}
