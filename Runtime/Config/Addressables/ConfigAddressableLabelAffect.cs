using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 도메인에서 사용하는 Addressables Label 네이밍 규칙을 정의한다.
    /// </summary>
    /// <remarks>
    /// - Label은 여러 에셋을 논리적으로 묶어 조회하거나 일괄 로딩하기 위한 식별자이다.
    /// - 문자열 하드코딩을 방지하고, SDK 공통 접두사(<see cref="ConfigDefine.NameSDK"/>)를 일관되게 적용한다.
    /// - Group(<see cref="ConfigAddressableGroupNameAffect"/>) 및 Key(<see cref="ConfigAddressableKeyAffect"/>)와
    ///   역할이 다르므로 혼용하지 않도록 한다.
    /// </remarks>
    public static class ConfigAddressableLabelAffect
    {
        /// <summary>
        /// Affect 아이콘 이미지 에셋에 부여되는 Addressables Label.
        /// </summary>
        /// <remarks>
        /// - Affect 버프/디버프 UI에서 사용하는 아이콘 이미지들을 논리적으로 묶는다.
        /// - Addressables 설정의 Label 이름과 반드시 일치해야 한다.
        /// </remarks>
        public const string ImageAffectIcon = ConfigDefine.NameSDK + "_Affect_Icon";
    }
}