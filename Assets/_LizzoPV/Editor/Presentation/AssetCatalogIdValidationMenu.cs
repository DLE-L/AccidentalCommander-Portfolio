using System.Text;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Editor.Presentation
{
    public static class AssetCatalogIdValidationMenu
    {
        [MenuItem("Lizzo/Presentation/Validate Asset Catalog IDs", false, 350)]
        public static void ValidateProjectCatalogs()
        {
            AssetCatalogIdReport report = AssetCatalogIdIndex.BuildFromProject();
            if (report.IsValid)
            {
                Debug.Log($"[Asset Catalog] PASS records={report.Records.Count} nextAssetId={report.NextAvailableId}");
                return;
            }

            var output = new StringBuilder();
            output.Append("[Asset Catalog] FAIL issues=");
            output.Append(report.Issues.Count);
            output.Append(" records=");
            output.Append(report.Records.Count);

            for (int issueIndex = 0; issueIndex < report.Issues.Count; issueIndex++)
            {
                AssetCatalogIdIssue issue = report.Issues[issueIndex];
                output.AppendLine();
                output.Append(issue.Kind);
                output.Append(": ");
                output.Append(issue.Message);
                for (int recordIndex = 0; recordIndex < issue.Records.Count; recordIndex++)
                {
                    output.AppendLine();
                    output.Append("  - ");
                    output.Append(issue.Records[recordIndex]);
                }
            }

            Debug.LogError(output.ToString());
        }
    }
}
