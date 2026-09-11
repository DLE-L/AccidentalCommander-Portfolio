using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CommanderStatusVisualBinding : MonoBehaviour
    {
        [SerializeField] private CommanderActor _commander;
        [SerializeField] private UnitVisualDriver _body;
        [SerializeField] private UnitStatusVisualController _visuals;
        private CommanderStatusState _state;

        public void Bind(IPrefabFactory factory, CommanderStatusState state)
        {
            Unbind();
            _state = state;
            _visuals.Bind(factory, _body.SpriteRenderer, IsActive);
            _state.Changed += OnChanged;
            for (int bit = 1; bit <= 4; bit <<= 1)
                if (IsActive(bit)) _visuals.Show(bit);
        }
        private bool IsActive(int id) => _state != null && _commander != null
            && _commander.isActiveAndEnabled && _commander.Hp > 0 && _state.IsActive((CommanderStatusKind)id);
        private void OnChanged(CommanderStatusKind kind, bool active)
        {
            if (active) _visuals.Show((int)kind); else _visuals.Hide((int)kind);
        }
        private void Unbind()
        {
            if (_state != null) _state.Changed -= OnChanged;
            _state = null;
            if (_visuals != null) _visuals.Clear();
        }
        private void OnDisable() => Unbind();
    }
}
