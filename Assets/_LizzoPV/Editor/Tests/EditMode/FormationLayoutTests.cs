using System.Collections.Generic;
using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class FormationLayoutTests
    {
        [Test]
        public void SymmetricLayoutsKeepOddBottomVertexAndExposeTwentyOneStableIds()
        {
            HashSet<string> ids = new HashSet<string>();
            for (int group = 0; group < FormationLayout.GroupCapacity; group++)
            {
                for (int member = 1; member <= FormationLayout.MemberCapacity; member++)
                    Assert.That(ids.Add(FormationLayout.CreateSlotId(group, member)), Is.True);
            }
            Assert.That(ids.Count, Is.EqualTo(FormationLayout.SlotCapacity));

            const float radius = 2.0f;
            for (int count = 1; count <= FormationLayout.GroupCapacity; count++)
            {
                float sumX = 0.0f;
                for (int index = 0; index < count; index++)
                    sumX += FormationLayout.ResolveGroupCenter(count, index, radius).X;
                Assert.That(sumX, Is.Zero.Within(0.001f), $"count={count}");

                if ((count & 1) != 0)
                {
                    RunPoint bottom = FormationLayout.ResolveGroupCenter(count, 0, radius);
                    Assert.That(bottom.X, Is.Zero.Within(0.001f), $"count={count}");
                    Assert.That(bottom.Y, Is.EqualTo(-radius).Within(0.001f), $"count={count}");
                }
            }

            RunPoint left = FormationLayout.ResolveGroupCenter(2, 0, radius);
            RunPoint right = FormationLayout.ResolveGroupCenter(2, 1, radius);
            Assert.That(left.X, Is.EqualTo(-radius).Within(0.001f));
            Assert.That(right.X, Is.EqualTo(radius).Within(0.001f));
            Assert.That(left.Y, Is.Zero.Within(0.001f));
            Assert.That(right.Y, Is.Zero.Within(0.001f));
        }
    }
}
