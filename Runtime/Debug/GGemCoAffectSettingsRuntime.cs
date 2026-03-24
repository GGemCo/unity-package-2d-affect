using GGemCo2DCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 전용 설정 런타임 접근 지점입니다.
    /// 가능하면 Core 외부 SettingsRegistry 를 통해 로드하고,
    /// 그렇지 않은 환경에서는 Addressables 직접 로드를 시도합니다.
    /// </summary>
    public static class GGemCoAffectSettingsRuntime
    {
        private const string ExternalSettingId = "affect.settings";

        private static bool _externalRegistered;
        private static bool _loadRequested;
        private static AsyncOperationHandle<GGemCoAffectSettings> _loadHandle;

        public static GGemCoAffectSettings Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterExternalSetting()
        {
            if (_externalRegistered)
                return;

            _externalRegistered = true;
            AddressableLoaderSettingsRegist.SettingsRegistry.Register(
                ExternalSettingId,
                ConfigAddressableSettingAffect.AffectSettings.Key,
                obj => Current = obj as GGemCoAffectSettings);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Current = null;
            _loadRequested = false;
            _externalRegistered = false;
            _loadHandle = default;
        }

        public static GGemCoAffectSettings GetOrLoad()
        {
            if (Current != null)
                return Current;

            if (!_externalRegistered)
                RegisterExternalSetting();

            if (_loadRequested)
                return Current;

            _loadRequested = true;
            _loadHandle = Addressables.LoadAssetAsync<GGemCoAffectSettings>(ConfigAddressableSettingAffect.AffectSettings.Key);
            _loadHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Current = handle.Result;
                }
            };

            return Current;
        }
    }
}
