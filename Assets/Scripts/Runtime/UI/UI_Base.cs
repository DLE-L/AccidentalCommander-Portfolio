using TMPro;
using UnityEngine;

public abstract class UI_Base : MonoBehaviour
{
	protected bool _init;

	public virtual bool Init()
	{
		if (_init)
			return false;

		_init = true;
		ApplyDefaultFontInChildren();
		return true;
	}

	protected void ApplyDefaultFontInChildren()
	{
		TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
		if (defaultFont == null)
			return;

		TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
		foreach (TMP_Text text in texts)
		{
			if (text.font == null)
				text.font = defaultFont;
		}
	}

	private void Start()
	{
		Init();
	}

}
