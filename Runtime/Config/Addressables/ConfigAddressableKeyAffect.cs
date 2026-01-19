using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 도메인에서 사용하는 Addressables Key 규칙을 정의한다.
    /// </summary>
    /// <remarks>
    /// - Addressables 로드 시 사용하는 Key 문자열을 중앙에서 관리한다.
    /// - 문자열 하드코딩을 방지하고, SDK 공통 접두사(<see cref="ConfigDefine.NameSDK"/>)를 일관되게 적용한다.
    /// - Group 이름(<see cref="ConfigAddressableGroupNameAffect"/>)과는 역할이 다르므로 혼용하지 않도록 한다.
    /// </remarks>
    public static class ConfigAddressableKeyAffect
    {
        /// <summary>
        /// Affect 아이콘(Addressables) 리소스를 로드할 때 사용하는 Key 접두사.
        /// </summary>
        /// <remarks>
        /// - 실제 로드 시에는 이 Key 뒤에 개별 아이콘 식별자(예: 아이콘 ID)를 결합해 사용한다.
        /// - Addressables 설정의 Key 규칙과 반드시 일치해야 한다.
        /// </remarks>
        public const string AffectIcon = ConfigDefine.NameSDK + "_Affect_Icon";
    }
}