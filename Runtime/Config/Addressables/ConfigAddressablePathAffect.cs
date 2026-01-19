using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 도메인에서 사용하는 Addressables 경로 규칙을 생성·관리하는 단일 진실 소스(Single Source of Truth).
    /// </summary>
    /// <remarks>
    /// - Addressables 경로는 반드시 이 클래스에서만 생성/관리한다.
    /// - OS 간 슬래시 및 특수문자 차이를 <see cref="ConfigAddressablePath.Combine"/>으로 정규화한다.
    /// - 다른 Config* 클래스(키/그룹/라벨)는 경로 문자열을 직접 만들지 말고 본 클래스를 통해 참조한다.
    /// </remarks>
    public static class ConfigAddressablePathAffect
    {
        // -------------------------
        // Images (Icons, Parts, etc.)
        // -------------------------
        /// <summary>
        /// 이미지 리소스 경로 묶음.
        /// </summary>
        public static class Images
        {
            /// <summary>
            /// 아이콘 이미지 경로 묶음.
            /// </summary>
            public static class Icon
            {
                /// <summary>
                /// Affect 아이콘 이미지가 위치한 Addressables 경로.
                /// </summary>
                /// <remarks>
                /// - 기준 경로: <c>Images/Icon</c>
                /// - 최종 경로 예: <c>Images/Icon/Affect</c>
                /// - 실제 폴더 구조 및 Addressables 설정과 반드시 일치해야 한다.
                /// </remarks>
                public static string Affect =>
                    ConfigAddressablePath.Combine(
                        ConfigAddressablePath.Images.Icon.RootIcon,
                        "Affect");
            }
        }
    }
}