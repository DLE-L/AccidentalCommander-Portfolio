using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Editor.Presentation
{
    public sealed class AssetCatalogIdReassignWindow : EditorWindow
    {
        private AssetCatalogKind _kind;
        private int _oldId;
        private int _newId;

        [MenuItem("Lizzo/Presentation/Reassign Asset ID", false, 351)]
        public static void Open()
        {
            GetWindow<AssetCatalogIdReassignWindow>("Reassign Asset ID");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Changes the Catalog ID and every matching typed Profile reference under Assets/_LizzoPV. " +
                "The operation stops before mutation when the new ID is already referenced.",
                MessageType.Info);

            _kind = (AssetCatalogKind)EditorGUILayout.EnumPopup("Asset Kind", _kind);
            _oldId = EditorGUILayout.IntField("Old Asset ID", _oldId);
            _newId = EditorGUILayout.IntField("New Asset ID", _newId);

            if (GUILayout.Button("Use Next Global ID"))
            {
                _newId = AssetCatalogIdIndex.BuildFromProject().NextAvailableId;
            }

            using (new EditorGUI.DisabledScope(_oldId <= 0 || _newId <= 0 || _oldId == _newId))
            {
                if (GUILayout.Button("Reassign Asset ID"))
                {
                    if (AssetCatalogIdReassignService.TryReassignProject(
                            _kind,
                            _oldId,
                            _newId,
                            out AssetIdReassignResult result,
                            out string issue))
                    {
                        Debug.Log(
                            $"[Asset Catalog] Reassigned {_kind} Asset ID {_oldId} -> {_newId}. " +
                            $"assets={result.ChangedAssetCount} properties={result.ChangedPropertyCount}");
                    }
                    else
                    {
                        Debug.LogError($"[Asset Catalog] Reassign failed: {issue}");
                    }
                }
            }
        }
    }
}
