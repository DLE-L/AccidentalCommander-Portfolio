using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    // Resolves authored direct/area hits. Action timing and selection remain in the squad runtime;
    // Hit payloads share status ordering with projectile deliveries.
    internal sealed class CompanionImmediateAttack
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly CompanionCombatEvents _events;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly CombatAreaAttack _areaAttack;
        private readonly Lizzo.PV.Combat.Projectiles.ICombatProjectileModule _projectiles;
        private readonly CompanionRuntimeImmediateTargetCollector _immediateTargets = new CompanionRuntimeImmediateTargetCollector();

        internal CompanionImmediateAttack(RuntimeObjectRegistry registry, ICombatImmediateHitModule immediateHits,
            CompanionCombatEvents presentation = null, Lizzo.PV.Combat.Projectiles.ICombatProjectileModule projectiles = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _events = presentation;
            _projectiles = projectiles;
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _areaAttack = new CombatAreaAttack(_registry, _immediateHits);
        }

        private readonly List<PendingSlash> _pending = new List<PendingSlash>();
        internal void Reset() { _pending.Clear(); _immediateTargets.Reset(); _areaAttack.Reset(); }
        internal void Advance(float deltaSeconds)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (i >= _pending.Count) continue;
                var slash = _pending[i];
                slash.Remaining -= deltaSeconds;
                if (slash.Remaining > 0) { _pending[i] = slash; continue; }
                _pending.RemoveAt(i);
                Resolve(in slash.Intent, slash.Effect, slash.Source, slash.Target, in slash.Commander,
                    slash.Damage, slash.Attribution, false, slash.Modifiers, true);
            }
        }
        private struct PendingSlash
        {
            internal float Remaining;
            internal EffectIntent Intent;
            internal CombatEffectData Effect;
            internal Vector3 Source, Target, Commander;
            internal int Damage;
            internal CountableKillAttribution Attribution;
            internal CompanionPassiveCombatModifiers Modifiers;
        }

        internal EffectResolution Resolve(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            in Vector3 commanderPosition,
            int damage,
            CountableKillAttribution attribution,
            bool area,
            CompanionPassiveCombatModifiers modifiers, bool isAfterimage = false)
        {
            IReadOnlyList<ICombatImmediateHitTarget> targets = area
                ? _areaAttack.SelectTargets(CombatImmediateHitFaction.Ally, target,
                    Mathf.Max(.01f, effect.Radius) * Mathf.Max(.01f, modifiers.AreaRadiusMultiplier))
                : _immediateTargets.Collect(_registry.Enemies, effect, source, target, false,
                    modifiers.RangeMultiplier * (effect.DeliveryKind == CombatDeliveryKind.Cone ? modifiers.AreaRadiusMultiplier : 1f));
            int affected = 0;
            int ownerId = CompanionRuntimeEffectAttribution.Resolve(in intent).OwnerId;
            Vector3 forward = target - source;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
            }

            int maxTargets = area || effect.DeliveryKind == CombatDeliveryKind.Cone || effect.AffectsAllTargetsInShape
                ? targets.Count
                : Mathf.Max(1, effect.MaxTargets + modifiers.ChainTargetBonus);
            for (int index = 0; index < targets.Count && affected < maxTargets; index += 1)
            {
                EnemyActor enemy = (EnemyActor)targets[index];
                int hitDamage = damage;
                if (effect.CloseDamageRadius > 0.0f
                    && (enemy.transform.position - commanderPosition).sqrMagnitude
                        <= effect.CloseDamageRadius * effect.CloseDamageRadius)
                {
                    hitDamage = Mathf.Max(1, Mathf.RoundToInt(damage * modifiers.CloseDamageMultiplier));
                }
                var hitRequest = CombatImmediateHitRequest.CreateAllyDirectTarget(
                    intent.SourceCompanionId,
                    enemy,
                    source,
                    enemy.transform.position,
                    Mathf.Max(1, Mathf.RoundToInt(hitDamage * (area && effect.CenterDamageRadiusRatio > 0f
                        && (CombatTargeting.ResolveTargetPoint(enemy, target) - target).sqrMagnitude
                            <= Mathf.Pow(effect.Radius * modifiers.AreaRadiusMultiplier * effect.CenterDamageRadiusRatio, 2f)
                        ? modifiers.CenterDamageMultiplier : 1.0f))),
                    area ? AttackVisualKind.AreaHit : AttackVisualKind.ForwardSlash,
                    false,
                    attribution,
                    effect.Id,
                    statusPayload: area || effect.DeliveryKind == CombatDeliveryKind.Cone || effect.StatusTargetLimit <= 0 || affected < effect.StatusTargetLimit
                        ? CompanionRuntimeCombatWorld.CreateStatusPayload(intent.SourceCompanionId, ownerId, effect, modifiers)
                        : default);
                if (area ? _areaAttack.TryApply(index, in hitRequest) : _immediateHits.TryApply(in hitRequest))
                {
                    affected += 1;
                    if (effect.Push > 0.0f && (!effect.PushNormalEnemiesOnly || !enemy.IsElite))
                    {
                        enemy.ApplySmoothKnockback(forward, effect.Push * modifiers.ForcedMovementMultiplier);
                    }
                }
            }

            if (!isAfterimage && affected > 0 && effect.DeliveryKind == CombatDeliveryKind.Cone)
                for (int echo = 0; echo < modifiers.AfterimageCount; echo++)
                    _pending.Add(new PendingSlash { Remaining = .12f * (echo + 1), Intent = intent, Effect = effect,
                        Source = source, Target = target, Commander = commanderPosition, Damage = damage,
                        Attribution = attribution, Modifiers = modifiers });
            if (area && modifiers.FragmentCount > 0)
                CompanionFragmentBurst.Spawn(_projectiles, effect, intent.SourceCompanionId, target, damage,
                    modifiers.FragmentCount, attribution);

            _events?.PublishEffect(
                isAfterimage ? effect.Id + "_afterimage" : effect.Id,
                isAfterimage ? string.Empty : intent.PresentationCueId,
                source,
                target,
                forward,
                effect.Range * modifiers.RangeMultiplier * (effect.DeliveryKind == CombatDeliveryKind.Cone ? modifiers.AreaRadiusMultiplier : 1f),
                effect.Radius * modifiers.AreaRadiusMultiplier,
                intent.MemberOrder);
            return new EffectResolution(area || affected > 0, effect.Id, affected > 0 ? damage : 0.0f, affected);
        }

    }
}
