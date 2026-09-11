using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    // Owns ally chain delivery; squad timing and shared status ownership stay with the caller.
    internal sealed class CompanionChainAttack
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly CompanionCombatEvents _events;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly Action<EnemyActor, string, int, CombatEffectData, CompanionPassiveCombatModifiers> _applyStatus;
        private readonly List<ChainTargetCandidate> _chainCandidates = new List<ChainTargetCandidate>(16);
        private readonly List<ChainTargetCandidate> _chainTargets = new List<ChainTargetCandidate>(8);

        internal CompanionChainAttack(RuntimeObjectRegistry registry, ICombatImmediateHitModule hits,
            Action<EnemyActor, string, int, CombatEffectData, CompanionPassiveCombatModifiers> applyStatus,
            CompanionCombatEvents presentation = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _events = presentation;
            _immediateHits = hits ?? throw new ArgumentNullException(nameof(hits));
            _applyStatus = applyStatus ?? throw new ArgumentNullException(nameof(applyStatus));
        }

        internal void Reset() { _chainCandidates.Clear(); _chainTargets.Clear(); }

        internal EffectResolution Resolve(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            int damage,
            int ownerId,
            CountableKillAttribution attribution,
            CompanionPassiveCombatModifiers modifiers)
        {
            _chainCandidates.Clear();
            foreach (EnemyActor enemy in _registry.Enemies)
            {
                if (!CompanionRuntimeTargetSelector.IsValid(enemy))
                    continue;

                _chainCandidates.Add(new ChainTargetCandidate(
                    enemy,
                    enemy.transform.position,
                    enemy.GetInstanceID()));
            }

            ChainTargetSelector.Collect(
                _chainCandidates,
                source,
                Mathf.Max(0.01f, effect.Range * modifiers.RangeMultiplier),
                Mathf.Max(0.01f, effect.ChainDistance),
                Mathf.Max(1, effect.MaxTargets + modifiers.ChainTargetBonus),
                _chainTargets);

            int affected = 0;
            Vector3 previousTarget = source;
            Vector3 firstTarget = source;
            float retention = modifiers.ResolveChainDamageRetention(effect.DamageRetentionPerTarget);
            for (int index = 0; index < _chainTargets.Count; index += 1)
            {
                ChainTargetCandidate selected = _chainTargets[index];
                EnemyActor enemy = selected.Target;
                if (!CompanionRuntimeTargetSelector.IsValid(enemy))
                    continue;

                int hitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Pow(retention, index)));
                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    intent.SourceCompanionId,
                    enemy,
                    source,
                    selected.Point,
                    hitDamage,
                    AttackVisualKind.SingleHit,
                    false,
                    attribution,
                    effect.Id)))
                {
                    if (ShouldApplyStatus(effect, affected))
                        _applyStatus(enemy, intent.SourceCompanionId, ownerId, effect, modifiers);
                    if (affected == 0) firstTarget = selected.Point;
                    _events?.PublishChainLink(effect.Id, previousTarget, selected.Point, affected);
                    previousTarget = selected.Point;
                    affected += 1;
                }
            }

            _events?.PublishEffect(effect.Id, intent.PresentationCueId, source, firstTarget,
                firstTarget - source, effect.Range, effect.ChainDistance, intent.MemberOrder);
            return new EffectResolution(
                affected > 0,
                effect.Id,
                affected > 0 ? damage : 0.0f,
                affected);
        }



        private static bool ShouldApplyStatus(CombatEffectData effect, int appliedTargetIndex)
        {
            return effect.StatusTargetLimit <= 0 || appliedTargetIndex < effect.StatusTargetLimit;
        }

    }
}
