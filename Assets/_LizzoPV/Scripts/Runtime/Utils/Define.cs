public static class Define
{
	public enum ObjectType
	{
		Player,
		Monster,
		Projectile,
		Env
	}

	public enum StageType
	{
		Normal,
		Boss,
	}

	public enum CreatureState
	{
		Idle,
		Moving,
		Skill,
		Dead
	}

	public const int GOBLIN_ID = 1;
	public const int SNAKE_ID = 2;
	public const int BOSS_ID = 3;
	public const int ORC_ID = 4;
	public const int RED_CHARGER_ID = 5;

	public const string EXP_GEM_PREFAB = "EXPGem.prefab";
}
