using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Enemies.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyStatusVisualBinding : MonoBehaviour
    {
        [SerializeField] private EnemyActor _enemy;
        [SerializeField] private UnitStatusVisualController _visuals;
        [SerializeField] private UnitVisualDriver _body;

        public void Bind(IPrefabFactory factory) => _visuals.Bind(factory, _body.SpriteRenderer, IsActive);
        private bool IsActive(int id) => _enemy != null && _enemy.isActiveAndEnabled && _enemy.Hp > 0
            && _enemy.HasCompanionStatus((CompanionEnemyStatusKind)id, Time.time);
        private void OnEnable() { if (_enemy != null) _enemy.StatusApplied += Applied; }
        private void OnDisable() { if (_enemy != null) _enemy.StatusApplied -= Applied; }
        private void Applied(EnemyActor enemy, CompanionEnemyStatusKind kind) => _visuals.Show((int)kind);
    }
}
