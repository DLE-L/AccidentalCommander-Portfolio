using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class RuntimeScriptNamingTests
    {
        private static readonly Regex MilestoneIdentifier =
            new Regex(@"(?<![A-Za-z0-9_])M\d+(?=[^0-9]|$)", RegexOptions.Compiled);

        [Test]
        public void ProjectRuntimeScripts_DoNotUseMilestoneIdentifiers()
        {
            string projectSourceDirectory = Path.Combine(Application.dataPath, "_LizzoPV");

            foreach (string scriptPath in Directory.EnumerateFiles(projectSourceDirectory, "*.cs", SearchOption.AllDirectories))
            {
                string normalizedPath = scriptPath.Replace('\\', '/');
                if (normalizedPath.Contains("/Editor/") || normalizedPath.Contains("/Tests/"))
                    continue;

                string fileName = Path.GetFileName(scriptPath);
                string source = File.ReadAllText(scriptPath);

                Assert.That(
                    MilestoneIdentifier.IsMatch(fileName),
                    Is.False,
                    $"Runtime script filename must be milestone-neutral: {fileName}");
                Assert.That(
                    MilestoneIdentifier.IsMatch(source),
                    Is.False,
                    $"Runtime script content must be milestone-neutral: {fileName}");
            }
        }
    }
}
