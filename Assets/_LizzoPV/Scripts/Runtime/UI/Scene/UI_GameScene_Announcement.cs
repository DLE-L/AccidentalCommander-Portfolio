using Lizzo.PV.P0.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UI_GameScene
{
    [Header("Boss Warning")]
    [SerializeField] private GameObject _bossWarningRoot;
    [SerializeField] private CanvasGroup _bossWarningGroup;
    [SerializeField] private TMP_Text _bossWarningText;
    [SerializeField] private Image[] _bossWarningEdges;

    private Color _bossWarningAccentColor = Color.red;
    private float _bossWarningStartedAt = -999.0f;
    private float _bossWarningDuration;
    private bool _bossWarningShowsEdges;

    private const float BOSS_WARNING_FADE_SECONDS = 0.16f;
    private const float BOSS_WARNING_EDGE_ALPHA = 0.34f;

    public void ShowSpawnAnnouncement(string title, string body, Color accentColor)
    {
        ShowSpawnAnnouncement(string.Empty, title, body, accentColor);
    }

    public void ShowSpawnAnnouncement(string presentationId, string fallbackTitle, string fallbackBody, Color fallbackAccentColor)
    {
        if (ResolveAnnouncementReferences() == false)
            return;

        ApplyAnnouncementPresentation(presentationId, fallbackTitle, fallbackBody, fallbackAccentColor);
        _announcementGroup.alpha = 0.0f;
        _announcementStartedAt = Time.unscaledTime;
        _announcementRoot.SetActive(true);
        _announcementRoot.transform.SetAsLastSibling();
    }

    private void UpdateAnnouncement()
    {
        if (_announcementRoot == null || _announcementRoot.activeSelf == false)
            return;

        float elapsed = Time.unscaledTime - _announcementStartedAt;
        if (elapsed >= _announcementDuration)
        {
            _announcementRoot.SetActive(false);
            return;
        }

        float alpha = 1.0f;
        if (elapsed < ANNOUNCEMENT_FADE_IN)
            alpha = Mathf.Clamp01(elapsed / ANNOUNCEMENT_FADE_IN);
        else if (elapsed > _announcementDuration - ANNOUNCEMENT_FADE_OUT)
            alpha = Mathf.Clamp01((_announcementDuration - elapsed) / ANNOUNCEMENT_FADE_OUT);

        _announcementGroup.alpha = alpha;
    }

    private bool ResolveAnnouncementReferences()
    {
        if (_announcementRoot != null && _announcementGroup != null && _announcementPanelImage != null && _announcementBodyText != null)
            return true;

        Debug.LogError("[HUD] UI_GameScene is missing authored announcement references. Runtime UI creation is disabled.", this);
        return false;
    }

    private void ApplyAnnouncementPresentation(string presentationId, string fallbackTitle, string fallbackBody, Color fallbackAccentColor)
    {
        _announcementDuration = ANNOUNCEMENT_DURATION;
        if (_announcementTitleText != null)
            _announcementTitleText.text = fallbackTitle;
        _announcementBodyText.text = fallbackBody;
        if (_announcementTitleText != null)
            _announcementTitleText.color = Color.white;
        _announcementBodyText.color = new Color(0.90f, 0.92f, 0.96f, 1.0f);
        if (_announcementTitleText != null)
            _announcementTitleText.fontSize = 34.0f;
        _announcementBodyText.fontSize = 22.0f;
        if (_announcementAccentImage != null)
            _announcementAccentImage.color = fallbackAccentColor;

        _announcementPanelImage.sprite = null;
        _announcementPanelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.86f);

        if (PresentationCatalogProvider.TryGetAnnouncement(presentationId, out AnnouncementPresentationSet.Entry entry) == false)
            return;

        if (_announcementTitleText != null)
            _announcementTitleText.text = string.IsNullOrWhiteSpace(entry.Title) ? fallbackTitle : entry.Title;
        _announcementBodyText.text = string.IsNullOrWhiteSpace(entry.Body) ? fallbackBody : entry.Body;
        if (_announcementTitleText != null)
            _announcementTitleText.color = entry.TitleColor;
        _announcementBodyText.color = entry.BodyColor;
        if (_announcementTitleText != null)
            _announcementTitleText.fontSize = entry.TitleSize;
        _announcementBodyText.fontSize = entry.BodySize;
        if (_announcementAccentImage != null)
            _announcementAccentImage.color = entry.AccentColor;
        _announcementDuration = Mathf.Max(0.1f, entry.Duration);
        _announcementPanelImage.sprite = entry.BackgroundSprite;
        _announcementPanelImage.color = entry.PanelColor;
    }

    public void ShowBossPreWarning(string text, Color accentColor, float durationSeconds, bool showEdges)
    {
        if (ResolveBossWarningReferences() == false)
            return;

        _bossWarningText.text = string.IsNullOrWhiteSpace(text) ? "WARNING" : text;
        _bossWarningText.color = Color.Lerp(accentColor, Color.white, 0.18f);
        _bossWarningText.fontSize = 58.0f;
        _bossWarningText.fontSizeMax = 58.0f;
        _bossWarningAccentColor = accentColor;
        _bossWarningStartedAt = Time.unscaledTime;
        _bossWarningDuration = Mathf.Max(0.1f, durationSeconds);
        _bossWarningShowsEdges = showEdges;

        SetBossWarningEdgesVisible(showEdges);
        _bossWarningGroup.alpha = 0.0f;
        _bossWarningRoot.SetActive(true);
        _bossWarningRoot.transform.SetAsLastSibling();
    }

    public void ShowBossCountdown(int seconds)
    {
        ShowBossPreWarning(Mathf.Clamp(seconds, 1, 5).ToString(), new Color(1.0f, 0.18f, 0.08f, 1.0f), 1.05f, true);
        _bossWarningText.fontSize = 88.0f;
        _bossWarningText.fontSizeMax = 88.0f;
    }

    public void HideBossPreWarning()
    {
        if (_bossWarningRoot != null)
            _bossWarningRoot.SetActive(false);
    }

    private void UpdateBossWarningOverlay()
    {
        if (_bossWarningRoot == null || _bossWarningRoot.activeSelf == false)
            return;

        float elapsed = Time.unscaledTime - _bossWarningStartedAt;
        if (elapsed >= _bossWarningDuration)
        {
            _bossWarningRoot.SetActive(false);
            return;
        }

        float fade = Mathf.Min(BOSS_WARNING_FADE_SECONDS, _bossWarningDuration * 0.45f);
        float alpha = 1.0f;
        if (elapsed < fade)
            alpha = Mathf.Clamp01(elapsed / fade);
        else if (elapsed > _bossWarningDuration - fade)
            alpha = Mathf.Clamp01((_bossWarningDuration - elapsed) / fade);

        _bossWarningGroup.alpha = alpha;
        if (_bossWarningShowsEdges)
        {
            float pulse = 0.82f + Mathf.Sin(Time.unscaledTime * 16.0f) * 0.18f;
            Color edgeColor = _bossWarningAccentColor;
            edgeColor.a = BOSS_WARNING_EDGE_ALPHA * Mathf.Clamp01(pulse);
            for (int i = 0; i < _bossWarningEdges.Length; i++)
            {
                if (_bossWarningEdges[i] != null)
                    _bossWarningEdges[i].color = edgeColor;
            }
        }
    }

    private bool ResolveBossWarningReferences()
    {
        bool hasAuthoredEdges = _bossWarningEdges != null
            && _bossWarningEdges.Length == 4
            && _bossWarningEdges[0] != null
            && _bossWarningEdges[1] != null
            && _bossWarningEdges[2] != null
            && _bossWarningEdges[3] != null;
        if (_bossWarningRoot != null && _bossWarningGroup != null && _bossWarningText != null && hasAuthoredEdges)
            return true;

        Debug.LogError("[HUD] UI_GameScene is missing authored boss warning references. Runtime UI creation is disabled.", this);
        return false;
    }

    private void SetBossWarningEdgesVisible(bool visible)
    {
        if (_bossWarningEdges == null)
            return;

        for (int i = 0; i < _bossWarningEdges.Length; i++)
        {
            if (_bossWarningEdges[i] != null)
                _bossWarningEdges[i].gameObject.SetActive(visible);
        }
    }
}
