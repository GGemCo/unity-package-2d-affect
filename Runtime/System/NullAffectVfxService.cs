namespace GGemCo2DAffect
{
    public sealed class NullAffectVfxService : IAffectVfxService
    {
        public object Play(int vfxUid, IAffectTarget target, float scale, float offsetY, float duration) => null;
        public void Stop(object token) { }
    }
}
