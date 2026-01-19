using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    public static class ConfigScriptableObjectAffect
    {
        private const string BaseName = ConfigDefine.NameSDK + ConfigPackageInfo.NamePackageAffect;

        private const string BasePath = ConfigDefine.NameSDK + "/" + ConfigPackageInfo.NamePackageAffect + "/";

        private enum MenuOrdering
        {
            None,
        }

        public static readonly Dictionary<string, Type> SettingsTypes = new()
        {
        };
    }
}
