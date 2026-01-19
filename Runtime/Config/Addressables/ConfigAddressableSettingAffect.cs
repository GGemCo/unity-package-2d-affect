using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// TCG(Addressables) 설정 에셋 정보를 정의하는 클래스입니다.
    /// 게임 전반에서 참조되는 설정용 AddressableAssetInfo를 중앙에서 관리합니다.
    /// </summary>
    public static class ConfigAddressableSettingAffect
    {
        /// <summary>
        /// TCG 전반 설정 데이터를 담고 있는 Addressables 에셋 정보입니다.
        /// 카드 규칙, 밸런스, 공통 옵션 등의 설정 로딩에 사용됩니다.
        /// </summary>
        public static readonly AddressableAssetInfo TcgSettings =
            ConfigAddressableSetting.Make(nameof(TcgSettings));

        /// <summary>
        /// TCG UI 컷신(Cutscene) 관련 설정 데이터를 담고 있는 Addressables 에셋 정보입니다.
        /// UI 연출, 흐름 제어 등에 필요한 설정을 포함합니다.
        /// </summary>
        public static readonly AddressableAssetInfo TcgUICutsceneSettings =
            ConfigAddressableSetting.Make(nameof(TcgUICutsceneSettings));

        /// <summary>
        /// 로딩 씬 진입 시 반드시 선행 로드되어야 하는 Addressables 설정 에셋 목록입니다.
        /// 이후 씬 전환 과정에서 즉시 사용될 설정 데이터를 미리 메모리에 적재합니다.
        /// </summary>
        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
            TcgSettings,
            TcgUICutsceneSettings
        };
    }
}