using UnityEngine;

public static class Extension
{
	public static T GetOrAddComponent<T>(this GameObject go) where T : UnityEngine.Component
	{
		return Utils.GetOrAddComponent<T>(go);
	}

	public static bool IsValid(this GameObject go)
	{
		return go != null && go.activeSelf;
	}

	public static bool IsValid(this BaseController bc)
	{
		return bc != null && bc.isActiveAndEnabled;
	}
}
