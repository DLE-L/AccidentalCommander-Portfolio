using Lizzo.PV.Data;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.Flow;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionRuntime : MonoBehaviour
    {
        private PartyService _party;
        private UnitData _unitData;
        private CompanionRuntimeSpec _spec;
        private AllyCombat _combat;
        private HitFlash _hitFlash;
        private CompanionPresentation _presentation;
        private CompanionSurvival _survival;

        [SerializeField] private CircleCollider2D _bodyCollider;
        [SerializeField] private CircleCollider2D _combatCollider;

        private bool _promoted;

        public string UnitId => _spec?.PresentedUnitId ?? _unitData?.Id ?? gameObject.name;
        public string BaseUnitId => _spec?.BaseUnitId ?? _unitData?.Id ?? gameObject.name;
        public string DisplayName => _spec?.DisplayName ?? _unitData?.DisplayName ?? gameObject.name;
        public string FamilyTags => _spec?.FamilyTags ?? _unitData?.FamilyTags ?? string.Empty;
        public float MoveSpeed => _spec?.MoveSpeed ?? _unitData?.MoveSpeed ?? 0.0f;
        public string SlotId { get; private set; }
        public string RosterSlotId { get; private set; }
        public int Hp { get; internal set; }
        public int MaxHp { get; private set; }
        public bool IsDown { get; internal set; }
        public bool IsPromoted => _promoted;
        public bool IsDamaged => Hp < MaxHp;
        public int LastAppliedHealAmount { get; internal set; }
        public Collider2D BodyCollider => _bodyCollider;
        public Collider2D CombatCollider => _combatCollider;

        internal PartyService Party => _party;
        internal AllyCombat Combat => _combat;
        internal HitFlash HitFlash => _hitFlash;
        internal CompanionPresentation Presentation => _presentation;
        internal bool Promoted => _promoted;
        internal float IncomingDamageMultiplier { get; set; } = 1.0f;

        public void Configure(PartyService party, UnitData unitData, string slotId, bool promoted)
        {
            _unitData = unitData;
            Configure(party, CompanionRuntimeSpec.FromLegacy(unitData, promoted), slotId, string.Empty);
        }

        public void Configure(PartyService party, CompanionRuntimeSpec spec, string slotId)
        {
            Configure(party, spec, slotId, string.Empty);
        }

        public void Configure(PartyService party, CompanionRuntimeSpec spec, string slotId, string rosterSlotId)
        {
            _party = party ?? throw new System.ArgumentNullException(nameof(party));
            _spec = spec ?? throw new System.ArgumentNullException(nameof(spec));
            SlotId = slotId;
            RosterSlotId = rosterSlotId ?? string.Empty;
            _promoted = spec.IsPromoted;
            MaxHp = Mathf.Max(1, Mathf.RoundToInt(spec.BaseHp * _party.Tuning.CompanionHpScale));
            Hp = MaxHp;

            ResolveRequiredComponents();
            ValidateAuthoredColliders();

            _presentation = new CompanionPresentation(this);
            _presentation.Initialize();
            _survival = new CompanionSurvival(this);
            _survival.Initialize();
        }

        public void SetFormationSlot(string slotId)
        {
            SlotId = slotId;
        }

        internal void ApplyGrowthScale(CompanionGrowthScale scale)
        {
            int maxHp = Mathf.Max(1, Mathf.RoundToInt((_spec?.BaseHp ?? _unitData?.Hp ?? 1) * _party.Tuning.CompanionHpScale * scale.HpMultiplier));
            MaxHp = maxHp;
            Hp = Mathf.Min(Hp, MaxHp);
        }

        public bool IsFamily(string familyTag)
        {
            return string.IsNullOrEmpty(familyTag) == false && FamilyTags.Contains(familyTag);
        }

        public bool ApplyHeal(int amount, string priorityReason)
        {
            EnsureConfigured();
            return _survival.ApplyHeal(amount, priorityReason);
        }

        public bool IsHurtboxOverlappingCircle(Vector2 circleCenter, float circleRadius)
        {
            if (_combatCollider == null || _combatCollider.enabled == false)
                return false;

            Vector2 hurtboxCenter = _combatCollider.transform.TransformPoint(_combatCollider.offset);
            float maxScale = Mathf.Max(
                Mathf.Abs(_combatCollider.transform.lossyScale.x),
                Mathf.Abs(_combatCollider.transform.lossyScale.y));
            float hurtboxRadius = _combatCollider.radius * maxScale;
            float overlapDistance = Mathf.Max(0.0f, circleRadius) + hurtboxRadius;
            return (hurtboxCenter - circleCenter).sqrMagnitude <= overlapDistance * overlapDistance;
        }

        public bool TryApplyBossPatternDamage(MonsterController monster, int damage, string patternId)
        {
            EnsureConfigured();
            return _survival.TryApplyBossPatternDamage(monster, damage, patternId);
        }

        private void Update()
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            _survival?.Tick();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            _survival?.TryTakeContactDamage(other.GetComponentInParent<MonsterController>());
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            _survival?.TryTakeContactDamage(collision.gameObject.GetComponentInParent<MonsterController>());
        }

        private void ResolveRequiredComponents()
        {
            _combat = GetComponent<AllyCombat>();
            _hitFlash = GetComponent<HitFlash>();

            if (_combat == null)
                Debug.LogError($"Companion prefab is missing required AllyCombat: {gameObject.name}", this);
            if (_hitFlash == null)
                Debug.LogError($"Companion prefab is missing required HitFlash: {gameObject.name}", this);
        }

        private void ValidateAuthoredColliders()
        {
            if (_combatCollider == null)
                Debug.LogError($"Companion prefab is missing required CombatCollider reference: {gameObject.name}", this);
            else if (_combatCollider.isTrigger == false)
                Debug.LogError($"Companion CombatCollider must be trigger: {gameObject.name}", this);

            if (_bodyCollider == null)
                Debug.LogError($"Companion prefab is missing required BodyCollider reference: {gameObject.name}", this);
            else if (_bodyCollider.isTrigger)
                Debug.LogError($"Companion BodyCollider must not be trigger: {gameObject.name}", this);

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body == null)
            {
                Debug.LogError($"Companion prefab is missing required Rigidbody2D: {gameObject.name}", this);
                return;
            }

            body.gravityScale = 0.0f;
            body.freezeRotation = true;
        }

        private void EnsureConfigured()
        {
            if (_survival == null)
                throw new System.InvalidOperationException($"[CompanionRuntime] Configure must be called before using '{name}'.");
        }
    }
}
