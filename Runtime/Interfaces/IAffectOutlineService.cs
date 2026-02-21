using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 시스템에서 타겟의 외형(Outline 등)을 데코레이션하는 서비스 인터페이스.
    /// </summary>
    /// <remarks>
    /// - Affect 패키지는 렌더링 구현에 직접 의존하지 않는다.
    /// - Core/게임 프로젝트에서 실제 렌더러 조작(스프라이트 복제, 셰이더 파라미터 등)을 수행하는 구현체를 제공한다.
    /// </remarks>
    public interface IAffectOutlineService
    {
        /// <summary>
        /// 타겟에 Outline을 적용한다.
        /// </summary>
        /// <param name="target">Outline을 적용할 Affect 타겟.</param>
        /// <param name="pixelSize">두께(픽셀). 1 이상을 권장한다.</param>
        /// <param name="color">색상. HEX 코드.</param>
        /// <returns>해제를 위한 토큰 객체. 적용 불가 시 null.</returns>
        object Apply(IAffectTarget target, int pixelSize, Color color);

        /// <summary>
        /// <see cref="Apply"/>로 적용된 Outline을 해제한다.
        /// </summary>
        /// <param name="token">Apply가 반환한 토큰.</param>
        void Remove(object token);
    }
}
