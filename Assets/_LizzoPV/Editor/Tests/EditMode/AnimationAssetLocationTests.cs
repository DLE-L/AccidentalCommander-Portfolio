using NUnit.Framework;
using UnityEditor;

namespace Lizzo.PV.EditorTests
{
    public sealed class AnimationAssetLocationTests
    {
        const string EnemyRuntimeRoot = "Assets/_LizzoPV/Gameplay/Enemies/Animations/Runtime";
        const string CompanionSharedRoot = "Assets/_LizzoPV/Gameplay/Legion/Animations/Shared";

        [Test]
        public void RuntimeAnimationAssets_UseCurrentOwnedLocations()
        {
            Assert.That(AssetDatabase.IsValidFolder(EnemyRuntimeRoot), Is.True);
            Assert.That(AssetDatabase.IsValidFolder(CompanionSharedRoot), Is.True);
            Assert.That(AssetDatabase.IsValidFolder("Assets/_LizzoPV/Gameplay/Enemies/Animations/Compatibility"), Is.False);
            Assert.That(AssetDatabase.IsValidFolder("Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility"), Is.False);

            Assert.That(AssetDatabase.FindAssets("t:AnimatorController", new[] { EnemyRuntimeRoot }), Has.Length.EqualTo(5));
            Assert.That(AssetDatabase.FindAssets("t:AnimationClip", new[] { EnemyRuntimeRoot }), Has.Length.EqualTo(20));
            Assert.That(AssetDatabase.FindAssets("t:AnimatorController", new[] { CompanionSharedRoot }), Has.Length.EqualTo(1));
            Assert.That(AssetDatabase.FindAssets("t:AnimationClip", new[] { CompanionSharedRoot }), Has.Length.EqualTo(4));
        }
    }
}
