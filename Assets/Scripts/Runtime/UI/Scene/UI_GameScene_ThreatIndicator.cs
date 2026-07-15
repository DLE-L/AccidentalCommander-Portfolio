using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UI_GameScene
{
    public void ShowThreatDirection(Transform target, string label, Color accentColor, float durationSeconds = THREAT_INDICATOR_DURATION)
    {
        if (target == null || ResolveThreatIndicatorReferences() == false)
            return;

        _threatIndicatorTarget = target;
        _threatIndicatorUntil = durationSeconds <= 0.0f
            ? float.PositiveInfinity
            : Time.unscaledTime + Mathf.Max(0.5f, durationSeconds);
        _threatIndicatorArrowText.color = accentColor;
        _threatIndicatorLabelText.text = string.IsNullOrWhiteSpace(label) ? "THREAT" : label;
        _threatIndicatorBackgroundImage.color = new Color(0.03f, 0.035f, 0.045f, 0.86f);

        UpdateThreatIndicator();
        _threatIndicatorRoot.transform.SetAsLastSibling();
    }

    private void UpdateThreatIndicator()
    {
        if (_threatIndicatorTarget == null || _threatIndicatorTarget.gameObject.activeInHierarchy == false)
        {
            _threatIndicatorTarget = null;
            if (_threatIndicatorRoot != null)
                _threatIndicatorRoot.SetActive(false);
            return;
        }

        if (float.IsPositiveInfinity(_threatIndicatorUntil) == false && Time.unscaledTime >= _threatIndicatorUntil)
        {
            _threatIndicatorTarget = null;
            if (_threatIndicatorRoot != null)
                _threatIndicatorRoot.SetActive(false);
            return;
        }

        if (ResolveThreatIndicatorReferences() == false)
            return;

        Camera camera = Camera.main;
        RectTransform rootRect = transform as RectTransform;
        if (camera == null || rootRect == null)
        {
            _threatIndicatorRoot.SetActive(false);
            return;
        }

        Vector3 viewport = camera.WorldToViewportPoint(_threatIndicatorTarget.position);
        bool isInFront = viewport.z > 0.0f;
        bool isOnScreen = isInFront
            && viewport.x >= THREAT_VIEWPORT_MARGIN
            && viewport.x <= 1.0f - THREAT_VIEWPORT_MARGIN
            && viewport.y >= THREAT_VIEWPORT_MARGIN
            && viewport.y <= 1.0f - THREAT_VIEWPORT_MARGIN;

        if (isOnScreen)
        {
            _threatIndicatorRoot.SetActive(false);
            return;
        }

        Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
        if (isInFront == false)
            direction = -direction;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.up;
        direction.Normalize();

        Rect rect = rootRect.rect;
        float edgeHalfWidth = Mathf.Max(1.0f, rect.width * 0.5f - THREAT_EDGE_MARGIN_X);
        float edgeHalfHeight = Mathf.Max(1.0f, rect.height * 0.5f - THREAT_EDGE_MARGIN_Y);
        float xScale = Mathf.Abs(direction.x) <= 0.0001f ? float.MaxValue : edgeHalfWidth / Mathf.Abs(direction.x);
        float yScale = Mathf.Abs(direction.y) <= 0.0001f ? float.MaxValue : edgeHalfHeight / Mathf.Abs(direction.y);
        _threatIndicatorRectTransform.anchoredPosition = direction * Mathf.Min(xScale, yScale);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _threatIndicatorArrowRectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, angle);

        _threatIndicatorRoot.SetActive(true);
        _threatIndicatorRoot.transform.SetAsLastSibling();
    }

    private bool ResolveThreatIndicatorReferences()
    {
        _threatIndicatorRoot ??= Utils.FindChild(gameObject, "ThreatDirectionIndicator", true);
        _threatIndicatorRectTransform ??= _threatIndicatorRoot != null ? _threatIndicatorRoot.transform as RectTransform : null;
        _threatIndicatorBackgroundImage ??= _threatIndicatorRoot != null ? _threatIndicatorRoot.GetComponent<Image>() : null;
        _threatIndicatorArrowRectTransform ??= _threatIndicatorRoot != null ? Utils.FindChild<RectTransform>(_threatIndicatorRoot, "Arrow", true) : null;
        _threatIndicatorArrowText ??= _threatIndicatorRoot != null ? Utils.FindChild<TMP_Text>(_threatIndicatorRoot, "Arrow", true) : null;
        _threatIndicatorLabelText ??= _threatIndicatorRoot != null ? Utils.FindChild<TMP_Text>(_threatIndicatorRoot, "Label", true) : null;

        if (_threatIndicatorRoot != null && _threatIndicatorRectTransform != null && _threatIndicatorBackgroundImage != null && _threatIndicatorArrowRectTransform != null && _threatIndicatorArrowText != null && _threatIndicatorLabelText != null)
            return true;

        Debug.LogError("[HUD] UI_GameScene is missing authored threat indicator references. Runtime UI creation is disabled.", this);
        return false;
    }
}
