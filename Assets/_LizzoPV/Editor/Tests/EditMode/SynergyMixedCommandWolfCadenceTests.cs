using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyMixedCommandWolfCadenceTests
    {
        static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
        PresentationCatalogProvider _previous;
        GameObject _providerRoot;
        PresentationCatalog _catalog;
        GameObject _enemy;

        [SetUp] public void SetUp() { _previous=ActiveProvider.GetValue(null) as PresentationCatalogProvider; UnitPresentationSet units=AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset"); OwnedSupportPresentationSet supports=AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>("Assets/_LizzoPV/Data/Presentation/OwnedSupportPresentationSet.asset"); _catalog=ScriptableObject.CreateInstance<PresentationCatalog>(); _catalog.SetPresentationSetsForEditor(null,null,null,null,units,supports); _providerRoot=new GameObject("WolfCadenceCatalog"); _providerRoot.SetActive(false); PresentationCatalogProvider provider=_providerRoot.AddComponent<PresentationCatalogProvider>(); SerializedObject so=new SerializedObject(provider); so.FindProperty("_catalog").objectReferenceValue=_catalog; so.ApplyModifiedPropertiesWithoutUndo(); ActiveProvider.SetValue(null,provider); }
        [TearDown]
        public void TearDown()
        {
            if (_enemy != null) UnityEngine.Object.DestroyImmediate(_enemy);
            if (_providerRoot != null) UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_catalog != null) UnityEngine.Object.DestroyImmediate(_catalog);
            ActiveProvider.SetValue(null, _previous);
        }

        [Test]
        public void MixedCommand_WolfSuccessfulCycleUsesDividedPeriod()
        {
            using CanonicalTargetAreaRuntimeLifecycleTests.Fixture fixture=new CanonicalTargetAreaRuntimeLifecycleTests.Fixture();
            ConfigureFamilies(fixture); Assert.IsTrue(fixture.Run.Party.RecruitCanonical("wolf_tamer"));
            CompanionRuntime runtime=fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>(); AllyCombat combat=runtime.GetComponent<AllyCombat>();
            combat.SetCanonicalWolfOwnedProxyInfo(new CompanionWolfOwnedProxyCombatSetup("wolf_tamer",1,2.30f,4.0f,0.10f,1,1,0.20f,1,1.0f));
            MixedCommandRunModule module=new MixedCommandRunModule(fixture.Data,fixture.Run.SynergyTriggers,fixture.Run.Party); Bind(fixture.Run.Party,module); fixture.Run.Synergies.Refresh(Slots()); Assert.IsTrue(module.TryResolvePending(0));
            const float start=10f; combat.TryAdvanceCanonicalCastForTests(start); Assert.IsFalse(combat.HasWolfOwnedProxy, "Wolf must remain inactive without a target."); combat.TryAdvanceCanonicalCastForTests(10.199f); Assert.IsFalse(combat.HasWolfOwnedProxy, "Wolf must preserve the 0.20 second no-target retry."); _enemy=AddEnemy(fixture,combat.transform.position+Vector3.right*.8f); combat.TryAdvanceCanonicalCastForTests(10.2f); Assert.IsTrue(combat.HasWolfOwnedProxy, "Wolf must begin on the exact retry due time."); combat.TryAdvanceCanonicalCastForTests(10.3f); Assert.IsFalse(combat.HasWolfOwnedProxy, "Wolf must complete at the configured 0.10 second duration."); combat.TryAdvanceCanonicalCastForTests(12.299f); Assert.IsFalse(combat.HasWolfOwnedProxy, "Wolf must remain inactive immediately before the divided cadence due time."); combat.TryAdvanceCanonicalCastForTests(12.3f); Assert.IsTrue(combat.HasWolfOwnedProxy, "Wolf must begin at the Mixed Command divided 2.000 second cadence."); Unbind(fixture.Run.Party,module); module.Dispose();
        }
        static void ConfigureFamilies(CanonicalTargetAreaRuntimeLifecycleTests.Fixture f){f.Data.GetCompanionRoster("shield_guard").FamilyTags="shield_family,defense_family";f.Data.GetCompanionRoster("sword_soldier").FamilyTags="sword_family,melee_family";f.Data.GetCompanionRoster("cleric").FamilyTags="cleric_family,healing_family";f.Data.GetCompanionRoster("falcon_archer").FamilyTags="ranged_family,beast_family";f.Data.GetCompanionRoster("fire_mage").FamilyTags="magic_family,explosive_family";}
        static IReadOnlyList<SquadSlotState> Slots(){var a=new SquadSlotState[7];string[] ids={"shield_guard","sword_soldier","cleric","falcon_archer","fire_mage"};for(int i=0;i<7;i++)a[i]=new SquadSlotState($"squad_{i:00}",i<5?ids[i]:"",string.Empty,i<5?1:0,3,false,i<5?ids[i]:"");return Array.AsReadOnly(a);}
        static GameObject AddEnemy(CanonicalTargetAreaRuntimeLifecycleTests.Fixture f,Vector3 p){GameObject g=new GameObject("enemy");g.transform.position=p;MonsterController m=g.AddComponent<MonsterController>();EnemyHealthBar h=g.AddComponent<EnemyHealthBar>();HitFlash x=g.AddComponent<HitFlash>();typeof(MonsterController).GetField("_healthBar",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(m,h);typeof(MonsterController).GetField("_hitFlash",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(m,x);m.MaxHp=100;m.Hp=100;f.Run.Registry.RegisterEnemy(m);return g;}
        static void Bind(PartyService p,MixedCommandRunModule m)=>typeof(PartyService).GetMethod("BindMixedCommandRunModule",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{m});
        static void Unbind(PartyService p,MixedCommandRunModule m)=>typeof(PartyService).GetMethod("UnbindMixedCommandRunModule",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{m});
    }
}
