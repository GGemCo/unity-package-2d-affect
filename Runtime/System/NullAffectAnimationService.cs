namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 애니메이션 기능을 사용하지 않는 환경을 위한 Null 구현체입니다.
    /// </summary>
    public sealed class NullAffectAnimationService : IAffectAnimationService
    {
        public object Play(IAffectTarget target, AffectAnimationDefinition definition) => null;

        public void Stop(object token)
        {
        }
    }
}
