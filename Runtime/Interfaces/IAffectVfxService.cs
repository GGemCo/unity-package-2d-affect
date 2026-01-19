namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 시스템에서 비주얼 이펙트(VFX)를 재생·중지하기 위한 추상 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// - Core/게임 프로젝트에서 EffectManager 등 실제 이펙트 시스템에 연결하는 어댑터(Adapter)를 구현합니다.
    /// - Affect 패키지 자체는 특정 이펙트 구현이나 엔진 기능에 직접 의존하지 않도록 설계되었습니다.
    /// </remarks>
    public interface IAffectVfxService
    {
        /// <summary>
        /// 지정된 대상에 비주얼 이펙트(VFX)를 재생합니다.
        /// </summary>
        /// <param name="vfxUid">재생할 VFX를 식별하는 고유 ID입니다.</param>
        /// <param name="target">VFX가 부착되거나 기준이 되는 Affect 대상입니다.</param>
        /// <param name="scale">VFX의 전체 스케일 배율입니다.</param>
        /// <param name="offsetY">대상의 기준 위치에서 Y축으로 이동할 오프셋 값입니다.</param>
        /// <param name="duration">VFX가 유지될 시간(초)이며, 음수 또는 0이면 구현체의 기본 규칙을 따릅니다.</param>
        /// <returns>
        /// 재생 중인 VFX를 식별하기 위한 토큰 객체입니다.
        /// 이후 <see cref="Stop"/> 호출 시 해당 토큰을 전달하여 중지합니다.
        /// </returns>
        object Play(int vfxUid, IAffectTarget target, float scale, float offsetY, float duration);

        /// <summary>
        /// <see cref="Play"/>로 재생된 비주얼 이펙트(VFX)를 중지합니다.
        /// </summary>
        /// <param name="token">
        /// <see cref="Play"/> 호출 시 반환된 토큰 객체입니다.
        /// 유효하지 않거나 이미 중지된 토큰인 경우, 구현체에서 안전하게 무시할 수 있습니다.
        /// </param>
        void Stop(object token);
    }
}