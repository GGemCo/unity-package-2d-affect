using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 패키지의 실제 설명 Provider 구현.
    /// 내부적으로 AffectDescriptionService를 사용합니다.
    /// </summary>
    public sealed class AffectDescriptionProvider : IAffectDescriptionProvider
    {
        public string GetDescription(int affectUid)
        {
            return AffectDescriptionService.Instance.GetDescription(affectUid);
        }

        public string GetDescriptionWithChancePrefix(int affectUid, float chancePercent)
        {
            return AffectDescriptionService.Instance.GetDescriptionWithChancePrefix(affectUid, chancePercent);
        }
    }
}