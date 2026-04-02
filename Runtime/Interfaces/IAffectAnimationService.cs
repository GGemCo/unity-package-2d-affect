namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 수명주기에 연결되는 캐릭터 애니메이션 재생/해제를 담당하는 서비스 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Affect 패키지는 구체 애니메이션 구현을 직접 알지 않고, Core/게임 프로젝트의 어댑터가 이를 처리합니다.
    /// </remarks>
    public interface IAffectAnimationService
    {
        /// <summary>
        /// 지정 대상에 Affect 애니메이션 정의를 적용합니다.
        /// </summary>
        /// <param name="target">애니메이션을 재생할 Affect 타깃입니다.</param>
        /// <param name="definition">재생할 애니메이션 정의입니다.</param>
        /// <returns>이후 해제 시 사용할 토큰 객체입니다.</returns>
        object Play(IAffectTarget target, AffectAnimationDefinition definition);

        /// <summary>
        /// <see cref="Play"/>로 등록된 Affect 애니메이션을 해제합니다.
        /// </summary>
        /// <param name="token">Play가 반환한 토큰입니다.</param>
        void Stop(object token);
    }
}
