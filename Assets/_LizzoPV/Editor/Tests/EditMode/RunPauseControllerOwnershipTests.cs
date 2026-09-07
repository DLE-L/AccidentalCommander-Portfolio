using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunPauseControllerOwnershipTests
    {
        private GameObject _root;
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            _root = new GameObject("RunPauseControllerOwnershipTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
            Time.timeScale = _originalTimeScale;
        }

        [Test]
        public void RunStateOutcomeLocksGameplayAndRestoresNormalSelectedSpeed()
        {
            using RunState runState = new RunState();
            runState.Reset(1);
            runState.MarkLoaded();
            RunPauseController pause = _root.AddComponent<RunPauseController>();
            pause.Initialize(runState);
            Assert.That(pause.ToggleGameplaySpeed(), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(2.0f));

            Assert.That(runState.TryEnd(RunOutcome.Failure, 25), Is.True);

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(RunPauseController.IsResultGameplayLocked, Is.True);
            Assert.That(pause.SelectedGameplaySpeed, Is.EqualTo(1.0f));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [Test]
        public void OutcomeTransitionStopsTimeWithoutClaimingAResult()
        {
            using RunState runState = new RunState();
            runState.Reset(1);
            runState.MarkLoaded();
            RunPauseController pause = _root.AddComponent<RunPauseController>();
            pause.Initialize(runState);

            Invoke(pause, "BeginOutcomeTransition");

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(RunPauseController.IsResultGameplayLocked, Is.False);
            Assert.That(Time.timeScale, Is.Zero);

            Assert.That(runState.TryEnd(RunOutcome.Clear, 0), Is.True);
            Assert.That(RunPauseController.IsResultGameplayLocked, Is.True);
        }

        [Test]
        public void HitStopIsAppliedAndReleasedByRunPauseController()
        {
            RunPauseController pause = _root.AddComponent<RunPauseController>();
            pause.Initialize();

            HitStop.Request(0.1f, "ownership_test");

            Assert.That(HitStop.IsActive, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            SetField(pause, "_hitStopRestoreAtRealtime", Time.realtimeSinceStartup - 1.0f);
            Invoke(pause, "Update");
            Assert.That(HitStop.IsActive, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1.0f));
        }

        [Test]
        public void ProductionTimeScaleWritesAreOwnedByRunPauseController()
        {
            string sourceRoot = Path.Combine(Application.dataPath, "_LizzoPV");
            var writePattern = new Regex(@"Time\s*\.\s*timeScale\s*=", RegexOptions.CultureInvariant);
            var offenders = new List<string>();
            foreach (string path in Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');
                if (normalized.Contains("/Editor/"))
                    continue;
                if (writePattern.IsMatch(File.ReadAllText(path))
                    && !normalized.EndsWith("/Gameplay/Run/Runtime/RunPauseController.cs"))
                {
                    offenders.Add(normalized);
                }
            }

            Assert.That(offenders, Is.Empty);
        }

        private static void Invoke(RunPauseController pause, string methodName)
        {
            MethodInfo method = typeof(RunPauseController).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing {methodName} test seam.");
            method.Invoke(pause, null);
        }

        private static void SetField(RunPauseController pause, string fieldName, float value)
        {
            FieldInfo field = typeof(RunPauseController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing {fieldName} test seam.");
            field.SetValue(pause, value);
        }
    }
}
