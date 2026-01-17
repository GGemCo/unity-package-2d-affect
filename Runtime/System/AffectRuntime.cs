namespace GGemCo2DAffect
{
    public static class AffectRuntime
    {
        public static IAffectDefinitionRepository AffectRepository { get; set; } = new InMemoryAffectRepository();
        public static IStatusDefinitionRepository StatusRepository { get; set; } = new InMemoryStatusRepository();
        public static IAffectVfxService VfxService { get; set; } = new NullAffectVfxService();
    }
}
