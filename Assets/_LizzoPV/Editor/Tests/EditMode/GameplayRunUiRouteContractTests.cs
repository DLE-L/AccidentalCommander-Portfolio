using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.P0.Units;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayRunUiRouteContractTests
    {
        [Test]
        public void ImplementationsShareTheNormalizedRunUiContract()
        {
            Assert.That(typeof(IGameplayRunUi).IsAssignableFrom(typeof(GameplayRunUiController)), Is.True);
            Assert.That(typeof(IGameplayRunUi).IsAssignableFrom(typeof(Lizzo.PV.UI.GameplayUIController)), Is.True);

            MethodInfo runStatus = typeof(IGameplayRunUi).GetMethod(nameof(IGameplayRunUi.SetRunStatus));
            Assert.That(runStatus, Is.Not.Null);
            Assert.That(runStatus.GetParameters(), Has.Length.EqualTo(2));
        }

        [Test]
        public void LiveCallersDependOnInterfaces()
        {
            Assert.That(typeof(GameScene).GetField("_uiController", BindingFlags.Instance | BindingFlags.NonPublic).FieldType,
                Is.EqualTo(typeof(IGameplayRunUi)));
            Assert.That(typeof(EliteSpawnController).GetField("_uiController", BindingFlags.Instance | BindingFlags.NonPublic).FieldType,
                Is.EqualTo(typeof(IGameplayRunUiFeedback)));
            Assert.That(typeof(BossSpawnController).GetField("_uiController", BindingFlags.Instance | BindingFlags.NonPublic).FieldType,
                Is.EqualTo(typeof(IGameplayRunUiFeedback)));
        }

        [Test]
        public void BootstrapRetainsLegacyFieldAndAddsOneExplicitCleanRouteField()
        {
            FieldInfo legacy = typeof(RunBootstrap).GetField("gameplayUiController", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo clean = typeof(RunBootstrap).GetField("gameplayRunUiController", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(legacy, Is.Not.Null);
            Assert.That(legacy.FieldType, Is.EqualTo(typeof(Lizzo.PV.UI.GameplayUIController)));
            Assert.That(clean, Is.Not.Null);
            Assert.That(clean.FieldType, Is.EqualTo(typeof(GameplayRunUiController)));
        }

        [Test]
        public void CleanRouteUsesExplicitSixModuleReferencesAndNoForbiddenLoopOrSearchApis()
        {
            Type routeType = typeof(GameplayRunUiController);
            Type[] requiredTypes =
            {
                typeof(GameplayHudController),
                typeof(Lizzo.PV.Gameplay.CardOffer.GameplayCardOfferController),
                typeof(Lizzo.PV.Gameplay.Pause.GameplayPauseController),
                typeof(Lizzo.PV.Gameplay.Result.GameplayResultController),
                typeof(GameplayFeedbackController),
                typeof(Lizzo.PV.Gameplay.Input.GameplayInputLayerController),
            };

            FieldInfo[] serializedFields = routeType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(field => field.GetCustomAttributes(typeof(SerializeField), false).Length > 0)
                .ToArray();
            foreach (Type requiredType in requiredTypes)
                Assert.That(serializedFields.Any(field => field.FieldType == requiredType), Is.True, requiredType.Name);

            string sourcePath = Path.Combine(Application.dataPath, "_LizzoPV/Gameplay/Runtime/Route/GameplayRunUiController.cs");
            string source = File.ReadAllText(sourcePath);
            Assert.That(source, Does.Not.Contain("void Update("));
            Assert.That(source, Does.Not.Contain("Coroutine"));
            Assert.That(source, Does.Not.Contain("System.Threading.Tasks.Task"));
            Assert.That(source, Does.Not.Contain("Find("));
        }
    }
}
