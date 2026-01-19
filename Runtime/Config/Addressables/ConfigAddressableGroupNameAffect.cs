using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 도메인에서 사용하는 Addressables Group 이름 규칙을 정의한다.
    /// </summary>
    /// <remarks>
    /// - TCG(Addressables) 리소스의 빌드/패키징 단위인 Group 이름을 중앙에서 관리한다.
    /// - 문자열 하드코딩을 방지하고, SDK 공통 접두사(<see cref="ConfigDefine.NameSDK"/>)를 일관되게 적용한다.
    /// - Group 이름 변경 시 이 클래스만 수정하면 되도록 단일 진입점(Single Source of Truth)으로 사용한다.
    /// </remarks>
    public static class ConfigAddressableGroupNameAffect
    {
        /// <summary>
        /// Affect 아이콘 이미지(Addressables) 리소스가 속한 Group 이름.
        /// </summary>
        /// <remarks>
        /// - Affect 버프/디버프 UI에서 사용하는 아이콘 이미지 묶음.
        /// - 실제 Addressables 설정의 Group Name과 반드시 일치해야 한다.
        /// </remarks>
        public const string AffectIcon = ConfigDefine.NameSDK + "_Affect_IconImage";
    }
}