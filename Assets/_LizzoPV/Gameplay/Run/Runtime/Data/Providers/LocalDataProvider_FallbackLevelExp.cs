namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private void SeedFallbackLevelExp()
        {
            for (int level = 1; level <= 21; level++)
                AddLevelExp(level, 5 * level * level);
        }

        private void AddLevelExp(int level, int requiredExp)
        {
            LevelExp[level] = requiredExp;
        }
    }
}
