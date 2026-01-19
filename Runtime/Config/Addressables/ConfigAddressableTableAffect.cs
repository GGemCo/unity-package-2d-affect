using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    public static class ConfigAddressableTableAffect
    {
        public const string Affect = "affect";

        public const string AffectModifier = "affect_modifier";

        public static readonly AddressableAssetInfo TableAffect =
            ConfigAddressableTable.Make(Affect);

        public static readonly AddressableAssetInfo TableAffectModifier =
            ConfigAddressableTable.Make(AffectModifier);

        public static readonly List<AddressableAssetInfo> All = new()
        {
            TableAffect,
            TableAffectModifier,
        };
    }
}
