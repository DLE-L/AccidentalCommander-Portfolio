using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTools.UI.Catalog
{
    internal static partial class GUIProBlueCatalogGenerator
    {
        const string VendorPrefabRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Blue/Prefabs";
        const string TemplatePrefabRoot = "Assets/_LizzoPV/UI/Templates/GUIProBlue";
        const string SharedSpriteRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common";
        const string ThemeSpriteRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Blue/Sprites";
        const string OutputPath = "Artifacts/GeneratedCatalogs/GUIProBlue_Catalog.md";
        static readonly Regex GuidRegex = new Regex(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);

        [MenuItem("Lizzo/UI/GUI Pro Blue/Rebuild Full Catalog", false, 350)]
        static void RebuildFullCatalog()
        {
            try
            {
                CatalogSnapshot snapshot = CatalogBuilder.Build();
                string markdown = CatalogRenderer.Render(snapshot);
                string fullPath = Path.Combine(ProjectRoot, OutputPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, markdown, new UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log(snapshot.BuildLog("Rebuild Full Catalog", markdown.Length));
            }
            catch (Exception exception)
            {
                Debug.LogError("[GUI Pro Blue Catalog] Rebuild Full Catalog BLOCKED\n" + exception);
            }
        }

        [MenuItem("Lizzo/UI/GUI Pro Blue/Validate Full Catalog", false, 351)]
        static void ValidateFullCatalog()
        {
            try
            {
                CatalogValidation validation = CatalogValidator.Validate();
                if (validation.Errors.Count == 0) Debug.Log(validation.ToLog());
                else Debug.LogError(validation.ToLog());
            }
            catch (Exception exception)
            {
                Debug.LogError("[GUI Pro Blue Catalog] Validate Full Catalog BLOCKED\n" + exception);
            }
        }

        static string ProjectRoot
        {
            get
            {
                string assetsPath = Application.dataPath.Replace('\\', '/');
                return Directory.GetParent(assetsPath).FullName.Replace('\\', '/');
            }
        }

        static string ToFullPath(string assetPath)
        {
            return Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

    }
}
