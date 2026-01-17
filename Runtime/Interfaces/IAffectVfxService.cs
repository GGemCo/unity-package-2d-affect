namespace GGemCo2DAffect
{
    /// <summary>
    /// 비주얼 이펙트 재생 인터페이스.
    /// - Core/게임 프로젝트에서 EffectManager 등에 연결하는 어댑터를 구현한다.
    /// - Affect 패키지 자체는 특정 이펙트 시스템에 의존하지 않는다.
    /// </summary>
    public interface IAffectVfxService
    {
        object Play(int vfxUid, IAffectTarget target, float scale, float offsetY, float duration);
        void Stop(object token);
    }
}
