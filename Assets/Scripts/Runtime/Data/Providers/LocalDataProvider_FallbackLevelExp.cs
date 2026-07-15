namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private void SeedFallbackLevelExp()
        {
            AddLevelExp(1, 8);
            AddLevelExp(2, 14);
            AddLevelExp(3, 22);
            AddLevelExp(4, 32);
            AddLevelExp(5, 46);
        }

        private void AddLevelExp(int level, int requiredExp)
        {
            LevelExp[level] = requiredExp;
        }
    }
}
