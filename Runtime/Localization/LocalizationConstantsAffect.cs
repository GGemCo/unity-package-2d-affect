using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 패키지에서 사용하는 Localization 테이블 및 Key 네이밍 규칙을 정의하는 상수 모음입니다.
    /// </summary>
    /// <remarks>
    /// 문자열 리소스의 식별자(Single Source of Truth)를 중앙에서 관리하여
    /// 테이블 이름 및 키 네이밍의 일관성을 유지하고,
    /// 초기화/검증/프리로드 등의 공통 처리를 단순화하는 목적을 가집니다.
    /// </remarks>
    public static class LocalizationConstantsAffect
    {
        /// <summary>
        /// Affect 관련 Localization Table 이름 정의 모음입니다.
        /// </summary>
        /// <remarks>
        /// 각 상수는 Unity Localization 패키지에서 사용하는 테이블 이름과 1:1로 대응됩니다.
        /// </remarks>
        public static class Tables
        {
            /// <summary>
            /// Affect 이름(Name)을 제공하는 Localization 테이블 이름입니다.
            /// </summary>
            public const string AffectName = "GGemCo_Affect_Name";

            /// <summary>
            /// Affect 설명(Description)을 제공하는 Localization 테이블 이름입니다.
            /// </summary>
            public const string AffectDescription = "GGemCo_Affect_Description";

            /// <summary>
            /// Affect 중첩 정책(Stack Policy)을 설명하는 Localization 테이블 이름입니다.
            /// </summary>
            public const string AffectStackPolicy = "GGemCo_Affect_StackPolicy";

            /// <summary>
            /// Affect 패키지에서 사용하는 모든 Localization 테이블 이름 목록입니다.
            /// </summary>
            /// <remarks>
            /// 초기화, 유효성 검증, 프리로드 및 빌드 시 검사 등의
            /// 공통 처리 로직에서 순회 대상으로 사용됩니다.
            /// </remarks>
            public static readonly string[] All =
            {
                AffectName,
                AffectDescription,
                AffectStackPolicy,
            };
        }
    }
}
