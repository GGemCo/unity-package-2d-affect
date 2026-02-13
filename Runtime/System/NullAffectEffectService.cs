using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 VFX가 필요 없는 환경을 위한 더미(Null) 구현체.
    /// </summary>
    /// <remarks>
    /// Null Object 패턴을 적용하여,
    /// VFX 시스템이 비활성화된 환경에서도 null 체크 없이 안전하게 호출할 수 있다.
    /// </remarks>
    public sealed class NullAffectEffectService : IAffectEffectService
    {
        /// <summary>
        /// VFX 재생 요청을 무시하고 null 토큰을 반환한다.
        /// </summary>
        /// <param name="vfxUid">재생할 VFX 식별자(무시됨).</param>
        /// <param name="target">VFX 부착 대상(무시됨).</param>
        /// <param name="scale">스케일 값(무시됨).</param>
        /// <param name="offsetY">Y축 오프셋(무시됨).</param>
        /// <param name="duration">지속 시간(무시됨).</param>
        /// <returns>항상 null을 반환한다.</returns>
        public object Play(
            int vfxUid,
            IAffectTarget target,
            float scale,
            float offsetY,
            float duration,
            AffectEffectPositionType positionType,
            AffectEffectFollowType followType,
            ConfigSortingLayer.Keys sortingLayerKey) => null;

        /// <summary>
        /// VFX 중지 요청을 무시한다.
        /// </summary>
        /// <param name="token">Play에서 반환된 토큰(항상 null일 수 있음).</param>
        public void Stop(object token) { }
    }
}