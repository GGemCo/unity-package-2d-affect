using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect Damage Modifier가 Core 데미지 파이프라인으로 원인 정보를 전달하기 위한 컨텍스트입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="AffectApplyContext.Source"/>는 공격자 같은 원천 객체를 의미하고,
    /// 이 컨텍스트는 해당 원천에 더해 어떤 Affect가 데미지를 만들었는지와 사망 연출 후보를 함께 전달합니다.
    /// </remarks>
    internal sealed class AffectDamageSourceContext
    {
        /// <summary>
        /// 데미지를 발생시킨 Affect UID입니다.
        /// </summary>
        public int AffectUid;

        /// <summary>
        /// Affect를 유발한 원천 객체입니다. 일반적으로 공격자 GameObject입니다.
        /// </summary>
        public object Source;

        /// <summary>
        /// 이 데미지로 대상이 사망했을 때 사용할 사망 연출 요청입니다.
        /// </summary>
        public DeathPresentationRequest DeathPresentation;
    }
}
