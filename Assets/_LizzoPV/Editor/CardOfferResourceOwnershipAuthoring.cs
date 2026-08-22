using System;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Maintenance
{
    public static class CardOfferResourceOwnershipAuthoring
    {
        private const string SourceRoot = "Assets/_LizzoPV/Resources/Generated";
        private const string DestinationResources = "Assets/_LizzoPV/Gameplay/CardOffer/Resources";
        private const string DestinationRoot = DestinationResources + "/Generated";
        private const string HelperPath =
            "Assets/_LizzoPV/Editor/CardOfferResourceOwnershipAuthoring.cs";

        private static readonly AssetMove[] Moves =
        {
            new AssetMove("card_icons_sheet_v2.png"),
            new AssetMove("card_icons_sheet_v3.png"),
        };

        public static void Apply()
        {
            ValidateBeforeMove();

            EnsureFolder(DestinationResources);
            EnsureFolder(DestinationRoot);

            for (int index = 0; index < Moves.Length; index += 1)
            {
                string error = AssetDatabase.MoveAsset(Moves[index].SourcePath, Moves[index].DestinationPath);
                if (string.IsNullOrEmpty(error) == false)
                {
                    throw new InvalidOperationException(
                        $"[CardOffer Resource Ownership] Move failed for {Moves[index].SourcePath}: {error}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateAfterMove();

            if (AssetDatabase.DeleteAsset(HelperPath) == false)
            {
                throw new InvalidOperationException(
                    "[CardOffer Resource Ownership] Could not remove the one-shot helper.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[CardOffer Resource Ownership] PASS: moved two card icon sheets into CardOffer ownership; GUIDs and Resources paths are preserved.");
        }

        private static void ValidateBeforeMove()
        {
            if (AssetDatabase.IsValidFolder(DestinationResources)
                || AssetDatabase.IsValidFolder(DestinationRoot))
            {
                throw new InvalidOperationException(
                    "[CardOffer Resource Ownership] Destination folders already exist.");
            }

            for (int index = 0; index < Moves.Length; index += 1)
            {
                AssetMove move = Moves[index];
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(move.SourcePath) == null)
                {
                    throw new InvalidOperationException(
                        $"[CardOffer Resource Ownership] Missing source texture: {move.SourcePath}");
                }

                if (AssetDatabase.LoadMainAssetAtPath(move.DestinationPath) != null)
                {
                    throw new InvalidOperationException(
                        $"[CardOffer Resource Ownership] Destination already exists: {move.DestinationPath}");
                }

                move.Guid = AssetDatabase.AssetPathToGUID(move.SourcePath);
                if (string.IsNullOrWhiteSpace(move.Guid))
                {
                    throw new InvalidOperationException(
                        $"[CardOffer Resource Ownership] Missing source GUID: {move.SourcePath}");
                }
            }
        }

        private static void ValidateAfterMove()
        {
            for (int index = 0; index < Moves.Length; index += 1)
            {
                AssetMove move = Moves[index];
                if (AssetDatabase.LoadMainAssetAtPath(move.SourcePath) != null)
                {
                    throw new InvalidOperationException(
                        $"[CardOffer Resource Ownership] Source still exists: {move.SourcePath}");
                }

                Texture2D movedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(move.DestinationPath);
                if (movedTexture == null)
                {
                    throw new InvalidOperationException(
                        $"[CardOffer Resource Ownership] Destination texture is unavailable: {move.DestinationPath}");
                }

                string movedGuid = AssetDatabase.AssetPathToGUID(move.DestinationPath);
                if (string.Equals(move.Guid, movedGuid, StringComparison.Ordinal) == false)
                {
                    throw new InvalidOperationException(
                        $"[CardOffer Resource Ownership] GUID changed for {move.FileName}: {move.Guid} -> {movedGuid}");
                }

                string resourcePath = "Generated/" + move.FileName.Substring(0, move.FileName.Length - 4);
                Texture2D resourceTexture = Resources.Load<Texture2D>(resourcePath);
                if (resourceTexture == null
                    || string.Equals(
                        AssetDatabase.GetAssetPath(resourceTexture),
                        move.DestinationPath,
                        StringComparison.Ordinal) == false)
                {
                    throw new InvalidOperationException(
                        $"[CardOffer Resource Ownership] Resources.Load failed for {resourcePath}.");
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            if (AssetDatabase.IsValidFolder(parent) == false)
                EnsureFolder(parent);

            string guid = AssetDatabase.CreateFolder(parent, name);
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException($"[CardOffer Resource Ownership] Could not create folder: {path}");
        }

        private sealed class AssetMove
        {
            public AssetMove(string fileName)
            {
                FileName = fileName;
            }

            public string FileName { get; }
            public string SourcePath => SourceRoot + "/" + FileName;
            public string DestinationPath => DestinationRoot + "/" + FileName;
            public string Guid { get; set; }
        }
    }
}
