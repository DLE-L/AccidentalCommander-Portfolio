using System.Runtime.Serialization;
using Lizzo.PV.Legion.Synergy;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class Build1SynergyProgressionTests
    {
        [Test]
        public void FirstOrderedFamilyTag_UsesTheFirstCommaDelimitedTokenOnly()
        {
            Assert.That(Build1SynergyProgressionRules.TryGetCountablePrimaryTag("shield_family,defense_family", out string first), Is.True);
            Assert.That(first, Is.EqualTo("shield_family"));
            Assert.That(Build1SynergyProgressionRules.TryGetCountablePrimaryTag("unknown_family,shield_family", out _), Is.False);
        }

        [Test]
        public void ResolveStage_PrioritizesCompleteAndNeverDowngrades()
        {
            Assert.That(Build1SynergyProgressionRules.ResolveStage(Build1SynergyStage.None, true, true), Is.EqualTo(Build1SynergyStage.Complete));
            Assert.That(Build1SynergyProgressionRules.ResolveStage(Build1SynergyStage.Ready, false, false), Is.EqualTo(Build1SynergyStage.Ready));
            Assert.That(Build1SynergyProgressionRules.ResolveStage(Build1SynergyStage.Complete, false, false), Is.EqualTo(Build1SynergyStage.Complete));
        }

        [Test]
        public void ReadyProgression_CombatCompatibilitySurfaceIsNeutral()
        {
            Build1SynergyProgression progression = (Build1SynergyProgression)FormatterServices.GetUninitializedObject(
                typeof(Build1SynergyProgression));
            Assert.DoesNotThrow(() => progression.Tick(10.0f, runReady: true, paused: false));
            Assert.That(progression.GetMoveSpeedMultiplier(null), Is.EqualTo(1.0f));
        }
    }
}
