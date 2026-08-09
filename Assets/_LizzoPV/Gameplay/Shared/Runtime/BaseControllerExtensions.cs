public static class BaseControllerExtensions
{
	public static bool IsValid(this BaseController bc)
	{
		return bc != null && bc.isActiveAndEnabled;
	}
}
