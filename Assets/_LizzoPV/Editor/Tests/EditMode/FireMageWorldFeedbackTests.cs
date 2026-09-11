using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class FireMageWorldFeedbackTests
    {
        [Test]
        public void FireMageField_HasPersistentAuthoredAreaFeedback()
        {
            const string prefabPath =
                "Assets/_LizzoPV/Gameplay/Legion/Presentation/FireMage/Prefabs/VFX/dot_fire_field_v1.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<VfxWrapperInstance>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(true), Is.Not.Null);

            string addressableSettings = System.IO.File.ReadAllText(
                "Assets/AddressableAssetsData/AssetGroups/Gameplay Presentation.asset");
            StringAssert.Contains("m_Address: vfx/dot_fire_field_v1", addressableSettings);
        }
    }
}
