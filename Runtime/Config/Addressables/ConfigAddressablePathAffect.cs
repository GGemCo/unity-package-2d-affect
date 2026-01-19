using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// TCG 패키지의 Addressables 경로 규칙을 생성/관리하는 단일 소스(SoT)입니다.
    /// - 경로는 반드시 여기서만 생성/관리합니다.
    /// - OS 간 슬래시/특수문자 정규화를 제공합니다.
    /// - 다른 Config* 클래스는 본 클래스를 통해 경로를 참조하세요.
    /// </summary>
    public static class ConfigAddressablePathAffect
    {
        /// <summary>
        /// TCG 프리팹(Prefab) 리소스의 루트 경로입니다.
        /// </summary>
        private static string RootPrefabTcg =>
            ConfigAddressablePath.Combine(ConfigAddressablePath.Prefab.RootPrefab, ConfigPackageInfo.NamePackageTcg);

        /// <summary>
        /// TCG 이미지(Image) 리소스의 루트 경로입니다.
        /// </summary>
        private static string RootImageTcg =>
            ConfigAddressablePath.Combine(ConfigAddressablePath.Images.RootImage, ConfigPackageInfo.NamePackageTcg);

        /// <summary>
        /// 카드 리소스 경로 모음입니다.
        /// 카드 관련 에셋을 용도별 하위 경로로 구조화합니다.
        /// </summary>
        public static class Card
        {
            /// <summary>
            /// 카드 일러스트(Artwork) 이미지가 위치한 경로입니다.
            /// </summary>
            public static string ImageArt =>
                ConfigAddressablePath.Combine(RootImageTcg, "CardArt");

            /// <summary>
            /// 핸드 영역에 표시되는 카드 테두리(Border) 이미지가 위치한 경로입니다.
            /// </summary>
            public static string ImageBorderHand =>
                ConfigAddressablePath.Combine(RootImageTcg, "CardBorderHand");

            /// <summary>
            /// 필드 영역에 표시되는 카드 테두리(Border) 이미지가 위치한 경로입니다.
            /// </summary>
            public static string ImageBorderField =>
                ConfigAddressablePath.Combine(RootImageTcg, "CardBorderField");
        }
    }
}