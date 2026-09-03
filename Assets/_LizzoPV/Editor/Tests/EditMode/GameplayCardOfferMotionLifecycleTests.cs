using System;
using System.Reflection;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayCardOfferMotionLifecycleTests
    {
        [Test]
        public void Selection_DoesNotQueueExitMotionOnInactiveCardSlots()
        {
            using Fixture fixture = new Fixture();
            fixture.CardRoots[1].SetActive(false);
            fixture.CardRoots[2].SetActive(false);

            fixture.InvokeSelection(0);

            Assert.That(fixture.IsPlaying(0), Is.True);
            Assert.That(fixture.IsPlaying(1), Is.False);
            Assert.That(fixture.IsPlaying(2), Is.False);
        }

        [Test]
        public void Reopen_RestoresCardsAfterPriorSelectionExitMotion()
        {
            using Fixture fixture = new Fixture();
            fixture.InvokeSelection(0);
            fixture.SampleDisabledEnd(1);
            Assert.That(fixture.CardGroups[1].alpha, Is.Zero.Within(0.0001f));

            fixture.Binder.NotifyOpened();

            Assert.That(fixture.CardGroups[1].alpha, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(fixture.IsPlaying(1), Is.False);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject _root;
            private readonly AnimationClip _enterClip;
            private readonly AnimationClip _acceptedClip;
            private readonly AnimationClip _disabledClip;
            private readonly AudioClip _openClip;

            public readonly GameplayCardOfferPresentationBinder Binder;
            public readonly GameObject[] CardRoots = new GameObject[3];
            public readonly CanvasGroup[] CardGroups = new CanvasGroup[3];
            public readonly UiMotionPlayer[] CardMotions = new UiMotionPlayer[3];

            public Fixture()
            {
                _root = new GameObject("CardOfferMotionFixture");
                Binder = _root.AddComponent<GameplayCardOfferPresentationBinder>();
                AudioSource source = _root.AddComponent<AudioSource>();
                UiMotionPlayer overlay = CreateMotion("Overlay", _root.transform, out _);

                for (int index = 0; index < CardRoots.Length; index++)
                {
                    CardMotions[index] = CreateMotion("Card" + index, _root.transform, out CanvasGroup group);
                    CardRoots[index] = CardMotions[index].gameObject;
                    CardGroups[index] = group;
                }

                _enterClip = CreateAlphaClip("Enter", 0f, 1f);
                _acceptedClip = CreateAlphaClip("Accepted", 1f, 1f);
                _disabledClip = CreateAlphaClip("Disabled", 1f, 0f);
                _openClip = AudioClip.Create("Open", 16, 1, 8000, false);

                SetField(Binder, "_overlayMotion", overlay);
                SetField(Binder, "_cardMotions", CardMotions);
                SetField(Binder, "_sfxSource", source);
                SetField(Binder, "_openSfx", _openClip);
                SetField(Binder, "_acceptedSfx", _openClip);
                SetField(Binder, "_enterMotion", _enterClip);
                SetField(Binder, "_acceptedMotion", _acceptedClip);
                SetField(Binder, "_disabledMotion", _disabledClip);
            }

            public void InvokeSelection(int slot)
            {
                MethodInfo method = typeof(GameplayCardOfferPresentationBinder).GetMethod(
                    "HandleSelection",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                method.Invoke(Binder, new object[] { slot, "card" });
            }

            public void SampleDisabledEnd(int slot)
            {
                _disabledClip.SampleAnimation(CardRoots[slot], _disabledClip.length);
            }

            public bool IsPlaying(int slot)
            {
                FieldInfo field = typeof(UiMotionPlayer).GetField(
                    "_isPlaying",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                return (bool)field.GetValue(CardMotions[slot]);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_root);
                UnityEngine.Object.DestroyImmediate(_enterClip);
                UnityEngine.Object.DestroyImmediate(_acceptedClip);
                UnityEngine.Object.DestroyImmediate(_disabledClip);
                UnityEngine.Object.DestroyImmediate(_openClip);
            }

            private static UiMotionPlayer CreateMotion(string name, Transform parent, out CanvasGroup group)
            {
                GameObject item = new GameObject(name);
                item.transform.SetParent(parent, false);
                group = item.AddComponent<CanvasGroup>();
                Animation animation = item.AddComponent<Animation>();
                UiMotionPlayer player = item.AddComponent<UiMotionPlayer>();
                player.SetForEditor(animation);
                return player;
            }

            private static AnimationClip CreateAlphaClip(string name, float from, float to)
            {
                AnimationClip clip = new AnimationClip { name = name, legacy = true };
                clip.SetCurve(string.Empty, typeof(CanvasGroup), "m_Alpha", AnimationCurve.Linear(0f, from, 0.2f, to));
                return clip;
            }

            private static void SetField(object target, string name, object value)
            {
                FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                    throw new MissingFieldException(target.GetType().Name, name);
                field.SetValue(target, value);
            }
        }
    }
}
