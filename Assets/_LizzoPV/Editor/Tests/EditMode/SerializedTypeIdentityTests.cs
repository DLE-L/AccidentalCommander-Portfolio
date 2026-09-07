using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class SerializedTypeIdentityTests
    {
        const string AssetRoot = "Assets/_LizzoPV";
        const string LegacyIdentifierPrefix = "m_EditorClassIdentifier: Assembly-CSharp::Lizzo.PV.P0.";

        [Test]
        public void MaintainedSerializedAssets_HaveNoLegacyP0ClassIdentifiers()
        {
            List<string> offenders = new List<string>();
            foreach (string path in Directory.EnumerateFiles(AssetRoot, "*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(path);
                if (string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase) == false &&
                    string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase) == false &&
                    string.Equals(extension, ".unity", StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                if (File.ReadAllText(path).Contains(LegacyIdentifierPrefix, StringComparison.Ordinal))
                    offenders.Add(path.Replace('\\', '/'));
            }

            Assert.That(offenders, Is.Empty,
                "Serialized assets still use legacy P0 class identifiers:\n" + string.Join("\n", offenders));
        }
    }
}
