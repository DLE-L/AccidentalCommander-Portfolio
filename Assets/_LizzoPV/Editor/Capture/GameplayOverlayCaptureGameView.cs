using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Capture
{
    public static partial class GameplayOverlayCaptureCommand
    {
        static bool TryRestore(GameViewRestorePlan plan, EditorWindow gameView, out string error)
        {
            error = null;
            if (plan == null)
                return true;

            try
            {
                plan.Restore(
                    index => SetSelectedSizeIndex(gameView, index),
                    index => RemoveTemporarySize(index, plan.GroupName));
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        static GameViewCaptureSetup PrepareGameView(EditorWindow gameView, int width, int height)
        {
            Type gameViewSizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
            Type gameViewSizeGroupType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
            Type gameViewSizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
            Type gameViewSizeTypeEnum = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");
            if (gameViewSizesType == null || gameViewSizeGroupType == null || gameViewSizeType == null || gameViewSizeTypeEnum == null)
                throw new InvalidOperationException("Unity GameView size APIs are unavailable.");

            PropertyInfo selectedIndexProperty = gameView.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (selectedIndexProperty == null)
                throw new InvalidOperationException("Unity GameView selected-size API is unavailable.");

            int previousIndex = (int)selectedIndexProperty.GetValue(gameView);
            object sizes = gameViewSizesType.BaseType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            PropertyInfo currentGroupProperty = gameViewSizesType.GetProperty("currentGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo currentGroupTypeProperty = gameViewSizesType.GetProperty("currentGroupType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object group = currentGroupProperty?.GetValue(sizes);
            string groupName = currentGroupTypeProperty?.GetValue(sizes)?.ToString();
            MethodInfo getTotalCount = group.GetType().GetMethod("GetTotalCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo getGameViewSize = group.GetType().GetMethod("GetGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo widthProperty = gameViewSizeType.GetProperty("width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo heightProperty = gameViewSizeType.GetProperty("height", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (group == null || getTotalCount == null || getGameViewSize == null || widthProperty == null || heightProperty == null)
                throw new InvalidOperationException("Unity GameView size group APIs are unavailable.");

            int totalCount = (int)getTotalCount.Invoke(group, null);
            int selectedIndex = -1;
            for (int index = 0; index < totalCount; index++)
            {
                object size = getGameViewSize.Invoke(group, new object[] { index });
                if ((int)widthProperty.GetValue(size) == width && (int)heightProperty.GetValue(size) == height)
                {
                    selectedIndex = index;
                    break;
                }
            }

            bool createdTemporary = false;
            int temporaryIndex = -1;
            if (selectedIndex < 0)
            {
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                object size = Activator.CreateInstance(
                    gameViewSizeType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { fixedResolution, (object)width, (object)height, (object)"Temporary Gameplay Overlay Capture" },
                    null);
                MethodInfo addCustomSize = group.GetType().GetMethod("AddCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                addCustomSize.Invoke(group, new[] { size });
                selectedIndex = (int)getTotalCount.Invoke(group, null) - 1;
                temporaryIndex = selectedIndex;
                createdTemporary = true;
            }

            selectedIndexProperty.SetValue(gameView, selectedIndex);
            gameView.Repaint();
            return new GameViewCaptureSetup(new GameViewRestorePlan(previousIndex, temporaryIndex, createdTemporary, groupName));
        }

        static void SetSelectedSizeIndex(EditorWindow gameView, int index)
        {
            if (gameView == null || index < 0)
                return;
            PropertyInfo property = gameView.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                throw new InvalidOperationException("Unity GameView selected-size API is unavailable during restoration.");
            property.SetValue(gameView, index);
            gameView.Repaint();
        }

        static void RemoveTemporarySize(int index, string groupName)
        {
            Type sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
            Type groupType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
            object sizes = sizesType.BaseType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            object group = sizesType.GetProperty("currentGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(sizes);
            if (group == null || string.IsNullOrWhiteSpace(groupName) == false && sizesType.GetProperty("currentGroupType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(sizes).ToString() != groupName)
            {
                object requestedGroup = Enum.Parse(groupType, groupName);
                group = sizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(sizes, new[] { requestedGroup });
            }
            MethodInfo remove = group.GetType().GetMethod("RemoveCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            remove.Invoke(group, new object[] { index });
        }

        static Vector2 GetTargetRenderSize(EditorWindow gameView)
        {
            PropertyInfo property = gameView.GetType().GetProperty("targetRenderSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                throw new InvalidOperationException("Unity GameView target-render-size API is unavailable.");
            return (Vector2)property.GetValue(gameView);
        }

        static EditorWindow FindGameView()
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            EditorWindow fallback = null;
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i].GetType().FullName == "UnityEditor.GameView")
                {
                    fallback ??= windows[i];
                    if (windows[i].rootVisualElement != null && windows[i].rootVisualElement.panel != null)
                        return windows[i];
                }
            }

            return fallback != null && fallback.rootVisualElement != null && fallback.rootVisualElement.panel != null
                ? fallback
                : null;
        }

    }
}
