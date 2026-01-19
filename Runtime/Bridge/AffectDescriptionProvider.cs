using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 패키지에서 제공하는 실제 설명 Provider 구현체입니다.
    /// </summary>
    /// <remarks>
    /// Core 계층은 IAffectDescriptionProvider 인터페이스만 의존하며,
    /// 본 구현은 내부적으로 AffectDescriptionService를 통해 설명 문자열을 생성합니다.
    /// </remarks>
    public sealed class AffectDescriptionProvider : IAffectDescriptionProvider
    {
        /// <summary>
        /// 지정된 Affect UID에 대한 설명 문자열을 반환합니다.
        /// </summary>
        /// <param name="affectUid">설명을 조회할 Affect의 고유 식별자입니다.</param>
        /// <returns>로컬라이징이 적용된 Affect 설명 문자열을 반환합니다.</returns>
        public string GetDescription(int affectUid)
        {
            return AffectDescriptionService.Instance.GetDescription(affectUid);
        }

        /// <summary>
        /// 확률(Chance) 정보를 접두사로 포함한 Affect 설명 문자열을 반환합니다.
        /// </summary>
        /// <param name="affectUid">설명을 조회할 Affect의 고유 식별자입니다.</param>
        /// <param name="chancePercent">표시할 확률 값(%)입니다.</param>
        /// <returns>
        /// 확률 접두사가 포함된 로컬라이징 Affect 설명 문자열을 반환합니다.
        /// </returns>
        public string GetDescriptionWithChancePrefix(int affectUid, float chancePercent)
        {
            return AffectDescriptionService.Instance.GetDescriptionWithChancePrefix(affectUid, chancePercent);
        }
    }
}