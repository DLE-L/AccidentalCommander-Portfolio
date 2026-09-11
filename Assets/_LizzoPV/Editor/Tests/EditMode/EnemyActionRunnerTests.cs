using Lizzo.PV.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class EnemyActionRunnerTests
    {
        [Test]
        public void ValidatedRequest_UsesRangeCooldownAndManualIdleClocks()
        {
            var runner = new EnemyActionRunner(new[] {
                new EnemyAttackDefinition(EnemyAttackKind.Charge, .2f, .3f, .2f, 2f, .5f, 1f, 3f, 2f)
            }, 1f);
            var inside = new EnemyActionInput(Vector2.zero, Vector2.right * 2, true, false, false);
            Assert.That(runner.TryStartAttack(EnemyAttackKind.Charge, inside, out _), Is.False);
            Assert.That(runner.Advance(.5f, inside, false).Velocity, Is.EqualTo(Vector2.zero));
            var outside = new EnemyActionInput(Vector2.zero, Vector2.right * 3.01f, true, false, false);
            Assert.That(runner.TryStartAttack(EnemyAttackKind.Charge, outside, out _), Is.False);
            var boundary = new EnemyActionInput(Vector2.zero, Vector2.right * 3, true, false, false);
            Assert.That(runner.TryStartAttack(EnemyAttackKind.Charge, boundary, out _), Is.True);
            // Range is checked on selection, not after committing a direction.
            Assert.That(runner.Advance(.21f, outside).Phase, Is.EqualTo(EnemyActionPhase.Executing));
            runner.CancelActive();
            Assert.That(runner.TryStartAttack(EnemyAttackKind.Charge, inside, out _), Is.False);
            runner.Advance(2f, inside, false);
            Assert.That(runner.TryStartAttack(EnemyAttackKind.Charge, inside, out _), Is.True);
        }

        [Test]
        public void ChargeGrace_RetainsFinalHitWindowBeforeRecovery()
        {
            var runner = new EnemyActionRunner(new[] {
                new EnemyAttackDefinition(EnemyAttackKind.Charge, .1f, .1f, .2f, 5f, 0f, 0f, 10f, 4f, .2f)
            }, 1f);
            var input = new EnemyActionInput(Vector2.zero, Vector2.right * 3, true, false, false);
            runner.StartAttack(EnemyAttackKind.Charge, input);
            runner.Advance(.1f, input);
            var frame = runner.Advance(.1f, input);
            Assert.That(frame.Phase, Is.EqualTo(EnemyActionPhase.ImpactGrace));
            Assert.That(frame.Velocity.x, Is.EqualTo(4f));
            frame = runner.Advance(.1f, input);
            Assert.That(frame.Phase, Is.EqualTo(EnemyActionPhase.ImpactGrace));
            Assert.That(frame.Velocity, Is.EqualTo(Vector2.zero));
            Assert.That(frame.Signals.HasFlag(EnemyActionSignals.ContactHit), Is.True);
            frame = runner.Advance(.1f, input);
            Assert.That(frame.Phase, Is.EqualTo(EnemyActionPhase.Recovery));
        }

        [Test]
        public void EarlyChargeHit_SkipsGraceAndCancellationPreservesCooldown()
        {
            var runner = new EnemyActionRunner(new[] {
                new EnemyAttackDefinition(EnemyAttackKind.Charge, .1f, .5f, .2f, 5f, 0f, 0f, 10f, 4f, .2f)
            }, 1f);
            var input = new EnemyActionInput(Vector2.zero, Vector2.right * 3, true, false, false);
            runner.StartAttack(EnemyAttackKind.Charge, input);
            runner.Advance(.1f, input);
            Assert.That(runner.CompleteActive().Phase, Is.EqualTo(EnemyActionPhase.Recovery));
            runner.Advance(.2f, input);
            Assert.That(runner.Advance(.1f, input).Phase, Is.EqualTo(EnemyActionPhase.Idle));
            runner.StartAttack(EnemyAttackKind.Charge, input);
            Assert.That(runner.CancelActive().Signals.HasFlag(EnemyActionSignals.Cancelled), Is.True);
            Assert.That(runner.Advance(.1f, input).Phase, Is.EqualTo(EnemyActionPhase.Idle));
        }

        private static EnemyActionRunner Create() => new(new[] {
            new EnemyAttackDefinition(EnemyAttackKind.Area, 1.25f, 0f, .2f, 7f, 3f, 0f, 6f),
            new EnemyAttackDefinition(EnemyAttackKind.Charge, 1f, 1.2f, .2f, 7f, 0f, 1.5f, float.MaxValue, 1.9f),
            new EnemyAttackDefinition(EnemyAttackKind.Contact, .28f, 0f, .2f, 0f, 0f, 0f, float.MaxValue),
        }, 1.2f);

        [Test]
        public void Charge_CommitsDirectionAndHasWarningExecutionRecovery()
        {
            var runner = Create();
            var input = new EnemyActionInput(Vector2.zero, Vector2.right * 10f, true, false, false);
            var frame = runner.Advance(.02f, input);
            Assert.That(frame.Signals.HasFlag(EnemyActionSignals.Started), Is.True);
            Assert.That(frame.HoldsPosition, Is.True);
            input = new EnemyActionInput(Vector2.zero, Vector2.zero, true, false, false);
            for (int i=0;i<60 && runner.Phase == EnemyActionPhase.Warning;i++) frame = runner.Advance(.02f, input);
            Assert.That(runner.Phase, Is.EqualTo(EnemyActionPhase.Executing));
            frame = runner.Advance(.02f, input);
            Assert.That(frame.Velocity.x, Is.EqualTo(1.9f).Within(.001f));
            for (int i=0;i<60;i++) frame = runner.Advance(.02f, input);
            Assert.That(runner.Phase, Is.EqualTo(EnemyActionPhase.Recovery));
            Assert.That(frame.HoldsPosition, Is.True);
            for (int i=0;i<11;i++) frame = runner.Advance(.02f, input);
            Assert.That(runner.Phase, Is.EqualTo(EnemyActionPhase.Idle));
        }

        [Test]
        public void Area_ResolvesOnceAtCommittedCenterEvenWhenTargetMoves()
        {
            var runner=Create();
            var input=new EnemyActionInput(Vector2.zero, Vector2.right * 4f, true, false, false);
            runner.StartAttack(EnemyAttackKind.Area, input);
            input=new EnemyActionInput(Vector2.zero, Vector2.left * 4f, true, false, false);
            int hits=0;
            for(int i=0;i<100;i++) {
                var frame=runner.Advance(.02f,input);
                if(frame.Signals.HasFlag(EnemyActionSignals.AreaHit)) { hits++; Assert.That(frame.Center,Is.EqualTo(Vector2.right*4f)); }
            }
            Assert.That(hits,Is.EqualTo(1));
        }

        [Test]
        public void Contact_SeparatingDuringPreparationCancelsWithoutRecoveryOrHit()
        {
            var runner=Create();
            var input=new EnemyActionInput(Vector2.zero, Vector2.right*.4f,true,true,true);
            Assert.That(runner.Advance(.02f,input).Kind,Is.EqualTo(EnemyAttackKind.Contact));
            var frame=runner.Advance(.02f,new EnemyActionInput(Vector2.zero,Vector2.right*10,true,false,false));
            Assert.That(frame.Signals.HasFlag(EnemyActionSignals.Cancelled),Is.True);
            Assert.That(frame.HoldsPosition,Is.False);
            Assert.That(frame.Signals.HasFlag(EnemyActionSignals.ContactHit),Is.False);
        }

        [Test]
        public void Contact_DamageWaitsForPreparationAndRecoveryBlocksNextAction()
        {
            var runner=Create();
            var input=new EnemyActionInput(Vector2.zero,Vector2.right*.4f,true,true,true);
            runner.Advance(.02f,input);
            for(int i=0;i<10;i++) Assert.That(runner.Advance(.02f,input).Signals.HasFlag(EnemyActionSignals.ContactHit),Is.False);
            int hits=0;
            for(int i=0;i<6;i++) if(runner.Advance(.02f,input).Signals.HasFlag(EnemyActionSignals.ContactHit)) hits++;
            Assert.That(hits,Is.EqualTo(1));
            Assert.That(runner.Phase,Is.EqualTo(EnemyActionPhase.Recovery));
        }

        [Test]
        public void ResetAndMissingTargetReleaseTheActiveAction()
        {
            var runner=Create();
            var input=new EnemyActionInput(Vector2.zero,Vector2.right*10,true,false,false);
            runner.Advance(.02f,input);
            Assert.That(runner.Advance(.02f,default).Signals.HasFlag(EnemyActionSignals.Cancelled),Is.True);
            runner.StartAttack(EnemyAttackKind.Charge,input);
            runner.Reset();
            Assert.That(runner.Phase,Is.EqualTo(EnemyActionPhase.Idle));
        }
    }
}
