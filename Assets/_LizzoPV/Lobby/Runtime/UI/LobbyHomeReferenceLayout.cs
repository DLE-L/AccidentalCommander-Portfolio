using UnityEngine;

namespace Lizzo.PV.UI
{
    [ExecuteAlways]
    public sealed class LobbyHomeReferenceLayout : MonoBehaviour
    {
        const float ReferenceWidth = 865f;
        const float ReferenceHeight = 1819f;
        const float ProfileCompositeDesignWidth = 1080f;
        const float WideProfileLeft = 13.13f;
        const float CtaClearance = 19f;
        const float PersistentHeaderHeight = 192f;
        const float WideFrameAspectThreshold = ReferenceWidth / ReferenceHeight;
        const float Epsilon = 0.01f;

        [SerializeField] RectTransform _persistentHeaderRoot;
        [SerializeField] RectTransform _headerChassis;
        [SerializeField] RectTransform _profileSlot;
        [SerializeField] RectTransform _profileLevelBadge;
        [SerializeField] RectTransform _profileLevelBadgeExtent;
        [SerializeField] RectTransform _profileLevelText;
        [SerializeField] RectTransform _gold;
        [SerializeField] RectTransform _goldAdd;
        [SerializeField] RectTransform _gem;
        [SerializeField] RectTransform _gemAdd;
        [SerializeField] RectTransform _settings;
        [SerializeField] RectTransform _legionPass;
        [SerializeField] RectTransform _eventPanel;
        [SerializeField] RectTransform _eventIcon;
        [SerializeField] RectTransform _eventTitle;
        [SerializeField] RectTransform _eventStatus;
        [SerializeField] RectTransform _miniPanel;
        [SerializeField] RectTransform _miniIcon;
        [SerializeField] RectTransform _miniTitle;
        [SerializeField] RectTransform _miniStatus;
        [SerializeField] RectTransform _startBattle;
        [SerializeField] RectTransform _selectedBattleFrame;

        RectTransform _safeArea;
        bool _applying;
        bool _missingReferencesReported;

        void OnEnable()
        {
            _safeArea = transform as RectTransform;
            if (Application.isPlaying)
                RefreshLayout();
        }

        void OnValidate()
        {
            _safeArea = transform as RectTransform;
        }

        void OnRectTransformDimensionsChange()
        {
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (_applying)
            {
                return;
            }

            _safeArea = transform as RectTransform;
            if (!HasRequiredReferences())
            {
                if (!_missingReferencesReported)
                {
                    Debug.LogError("[LobbyHomeReferenceLayout] Required RectTransform reference is missing.", this);
                    _missingReferencesReported = true;
                }

                return;
            }

            _missingReferencesReported = false;
            _applying = true;
            try
            {
                ApplyHeaderRoot();
                ApplyContentFrameRect(_headerChassis, 151f, 49f, 703f, 116f);
                ApplyProfileSlot();
                ApplyRect(_gold, 187f, 78f, 264f, 56f);
                ApplyRect(_goldAdd, 401f, 80f, 50f, 53f);
                ApplyRect(_gem, 497f, 78f, 245f, 56f);
                ApplyRect(_gemAdd, 692f, 80f, 50f, 53f);
                ApplySquare(_settings, 799.5f, 106.5f, 65f);
                ApplyContentFrameRect(_legionPass, 114f, 209f, 646f, 162f);
                ApplyProfileBadge();
                ApplyRect(_eventPanel, 17f, 555f, 167f, 113f);
                ApplySquare(_eventIcon, 104f, 492f, 148f);
                ApplyPanelText(_eventPanel, _eventTitle, 0.32f);
                ApplyPanelText(_eventPanel, _eventStatus, 0.71f);
                ApplyRect(_miniPanel, 676f, 555f, 166f, 113f);
                ApplySquare(_miniIcon, 760f, 492f, 148f);
                ApplyPanelText(_miniPanel, _miniTitle, 0.32f);
                ApplyPanelText(_miniPanel, _miniStatus, 0.71f);
                ApplyStartBattle();
            }
            finally
            {
                _applying = false;
            }
        }

        bool HasRequiredReferences()
        {
            return _safeArea != null
                && _persistentHeaderRoot != null
                && _headerChassis != null
                && _profileSlot != null
                && _profileLevelBadge != null
                && _profileLevelBadgeExtent != null
                && _profileLevelText != null
                && _gold != null
                && _goldAdd != null
                && _gem != null
                && _gemAdd != null
                && _settings != null
                && _legionPass != null
                && _eventPanel != null
                && _eventIcon != null
                && _eventTitle != null
                && _eventStatus != null
                && _miniPanel != null
                && _miniIcon != null
                && _miniTitle != null
                && _miniStatus != null
                && _startBattle != null
                && _selectedBattleFrame != null;
        }

        void ApplyHeaderRoot()
        {
            ApplyContentFrameRect(_persistentHeaderRoot, 0f, 0f, ReferenceWidth, PersistentHeaderHeight);
        }

        void ApplyProfileBadge()
        {
            Rect frame = GetContentFrame();
            float targetBottomY = _safeArea.InverseTransformPoint(GetWorldTop(_legionPass)).y
                - frame.width * (5f / ReferenceWidth);
            float currentBottomY = _safeArea.InverseTransformPoint(GetWorldBottom(_profileLevelBadgeExtent)).y;
            Vector3 verticalDelta = _safeArea.TransformVector(new Vector3(0f, targetBottomY - currentBottomY, 0f));
            OffsetByWorldVector(_profileLevelBadge, verticalDelta);
            OffsetByWorldVector(_profileLevelText, verticalDelta);
        }

        void ApplyProfileSlot()
        {
            float compositeScale = GetContentFrame().width / ProfileCompositeDesignWidth;
            ApplyScaledContentFrameRect(_profileSlot, IsWideContentFrame() ? WideProfileLeft : 13f, 21f, 142f, 193f, compositeScale);
        }

        bool IsWideContentFrame()
        {
            Rect safe = _safeArea.rect;
            return safe.width / safe.height > WideFrameAspectThreshold;
        }

        void ApplyRect(RectTransform target, float left, float top, float width, float height)
        {
            Rect safe = GetContentFrame();
            Vector3 bottomLeftWorld = _safeArea.TransformPoint(new Vector3(
                safe.xMin + safe.width * (left / ReferenceWidth),
                safe.yMax - safe.height * ((top + height) / ReferenceHeight),
                0f));
            Vector3 topRightWorld = _safeArea.TransformPoint(new Vector3(
                safe.xMin + safe.width * ((left + width) / ReferenceWidth),
                safe.yMax - safe.height * (top / ReferenceHeight),
                0f));
            SetWorldRect(target, bottomLeftWorld, topRightWorld);
        }

        void ApplySquare(RectTransform target, float centerX, float centerY, float side)
        {
            Rect safe = GetContentFrame();
            float sidePixels = safe.height * (side / ReferenceHeight);
            Vector3 centerWorld = _safeArea.TransformPoint(new Vector3(
                safe.xMin + safe.width * (centerX / ReferenceWidth),
                safe.yMax - safe.height * (centerY / ReferenceHeight),
                0f));
            RectTransform parent = target.parent as RectTransform;
            Vector3 localCenter = parent.InverseTransformPoint(centerWorld);
            SetRect(target, localCenter, new Vector2(sidePixels, sidePixels));
        }

        void ApplyContentFrameRect(RectTransform target, float left, float top, float width, float height)
        {
            Rect frame = GetContentFrame();
            float scale = frame.width / ReferenceWidth;
            Vector3 centerWorld = _safeArea.TransformPoint(new Vector3(
                frame.xMin + (left + width * 0.5f) * scale,
                frame.yMax - (top + height * 0.5f) * scale,
                0f));
            RectTransform parent = target.parent as RectTransform;
            SetRect(target, parent.InverseTransformPoint(centerWorld), new Vector2(width * scale, height * scale));
        }

        void ApplyScaledContentFrameRect(RectTransform target, float left, float top, float width, float height, float compositeScale)
        {
            Rect frame = GetContentFrame();
            float frameScale = frame.width / ReferenceWidth;
            Vector3 centerWorld = _safeArea.TransformPoint(new Vector3(
                frame.xMin + (left + width * 0.5f) * frameScale,
                frame.yMax - (top + height * 0.5f) * frameScale,
                0f));
            Vector3 desiredScale = new Vector3(compositeScale, compositeScale, 1f);
            if ((target.localScale - desiredScale).sqrMagnitude > Epsilon * Epsilon)
            {
                target.localScale = desiredScale;
            }

            RectTransform parent = target.parent as RectTransform;
            SetRect(target, parent.InverseTransformPoint(centerWorld), new Vector2(
                width * frameScale / compositeScale,
                height * frameScale / compositeScale));
        }

        void ApplyStartBattle()
        {
            Rect frame = GetContentFrame();
            float width = frame.width * (573f / ReferenceWidth);
            float height = frame.height * (254f / ReferenceHeight);
            Vector3 selectedTopWorld = GetWorldTop(_selectedBattleFrame);
            float bottomY = _safeArea.InverseTransformPoint(selectedTopWorld).y + frame.height * (CtaClearance / ReferenceHeight);
            float centerX = frame.xMin + frame.width * ((145f + 573f * 0.5f) / ReferenceWidth);
            Vector3 bottomLeftWorld = _safeArea.TransformPoint(new Vector3(centerX - width * 0.5f, bottomY, 0f));
            Vector3 topRightWorld = _safeArea.TransformPoint(new Vector3(centerX + width * 0.5f, bottomY + height, 0f));
            SetWorldRect(_startBattle, bottomLeftWorld, topRightWorld);
        }

        Rect GetContentFrame()
        {
            Rect safe = _safeArea.rect;
            float width = safe.width;
            if (safe.width / safe.height > WideFrameAspectThreshold)
                width = safe.height * (ReferenceWidth / ReferenceHeight);
            return new Rect(safe.center.x - width * 0.5f, safe.yMin, width, safe.height);
        }

        static Vector3 GetWorldTop(RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            return (corners[1] + corners[2]) * 0.5f;
        }

        static Vector3 GetWorldBottom(RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            return (corners[0] + corners[3]) * 0.5f;
        }

        static void OffsetByWorldVector(RectTransform target, Vector3 worldVector)
        {
            RectTransform parent = target.parent as RectTransform;
            Vector3 localVector = parent.InverseTransformVector(worldVector);
            float y = target.anchoredPosition.y + localVector.y;
            if (Mathf.Abs(target.anchoredPosition.y - y) > Epsilon)
            {
                target.anchoredPosition = new Vector2(target.anchoredPosition.x, y);
            }
        }

        void ApplyPanelText(RectTransform panel, RectTransform text, float topFraction)
        {
            Vector2 desired = new Vector2(0f, panel.rect.height * (0.5f - topFraction));
            if ((text.anchoredPosition - desired).sqrMagnitude > Epsilon * Epsilon)
            {
                text.anchoredPosition = desired;
            }
        }

        static void SetWorldRect(RectTransform target, Vector3 bottomLeftWorld, Vector3 topRightWorld)
        {
            RectTransform parent = target.parent as RectTransform;
            Vector3 bottomLeft = parent.InverseTransformPoint(bottomLeftWorld);
            Vector3 topRight = parent.InverseTransformPoint(topRightWorld);
            SetRect(target, (bottomLeft + topRight) * 0.5f, new Vector2(topRight.x - bottomLeft.x, topRight.y - bottomLeft.y));
        }

        static void SetRect(RectTransform target, Vector3 localCenter, Vector2 size)
        {
            if ((target.anchorMin - Vector2.one * 0.5f).sqrMagnitude > Epsilon * Epsilon)
            {
                target.anchorMin = Vector2.one * 0.5f;
            }

            if ((target.anchorMax - Vector2.one * 0.5f).sqrMagnitude > Epsilon * Epsilon)
            {
                target.anchorMax = Vector2.one * 0.5f;
            }

            if ((target.pivot - Vector2.one * 0.5f).sqrMagnitude > Epsilon * Epsilon)
            {
                target.pivot = Vector2.one * 0.5f;
            }

            if ((target.sizeDelta - size).sqrMagnitude > Epsilon * Epsilon)
            {
                target.sizeDelta = size;
            }

            Vector2 position = new Vector2(localCenter.x, localCenter.y);
            if ((target.anchoredPosition - position).sqrMagnitude > Epsilon * Epsilon)
            {
                target.anchoredPosition = position;
            }
        }

    }
}
