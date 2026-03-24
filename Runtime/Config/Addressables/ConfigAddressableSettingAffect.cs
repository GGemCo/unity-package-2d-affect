using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    public static class ConfigAddressableSettingAffect
    {
        public static readonly AddressableAssetInfo AffectSettings = ConfigAddressableSetting.Make(nameof(AffectSettings));

        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
            AffectSettings,
        };
    }
}
