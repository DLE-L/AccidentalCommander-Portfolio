public static class Extension
{
	public static bool IsValid(this BaseController bc)
	{
		return bc != null && bc.isActiveAndEnabled;
	}
}
