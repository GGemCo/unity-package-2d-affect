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

        /// <summary>
        /// Affect Settings를 반환하거나 로드를 요청합니다.
        /// 개발용 Settings Override가 등록되어 있으면 Addressables 직접 로드보다 먼저 사용합니다.
        /// </summary>
        /// <returns>현재 즉시 사용할 수 있는 Affect Settings입니다. 아직 비동기 로드가 완료되지 않았으면 null입니다.</returns>
        public static GGemCoAffectSettings GetOrLoad()
        {
            if (Current != null)
                return Current;

            if (!_externalRegistered)
                RegisterExternalSetting();

            if (TryApplyOverrideSettings())
                return Current;

            if (_loadRequested)
                return Current;

            _loadRequested = true;
            _loadHandle = Addressables.LoadAssetAsync<GGemCoAffectSettings>(ConfigAddressableSettingAffect.AffectSettings.Key);
            _loadHandle.Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                    return;

                // 비동기 로드 완료 시점에 개발용 Settings가 준비되어 있을 수 있으므로 한 번 더 우선 확인합니다.
                if (TryApplyOverrideSettings())
                    return;

                Current = handle.Result;
            };

            return Current;
        }

        /// <summary>
        /// 공용 Settings Runtime Resolver에서 Affect 개발용 Settings Override를 조회하여 현재 설정에 반영합니다.
        /// </summary>
        /// <returns>개발용 Affect Settings를 찾아 Current에 반영했으면 true입니다.</returns>
        private static bool TryApplyOverrideSettings()
        {
            if (!SettingsRuntimeResolver.TryGetOverride(
                    ConfigAddressableSettingAffect.AffectSettings.Key,
                    out GGemCoAffectSettings overrideSettings))
            {
                return false;
            }

            Current = overrideSettings;
            return Current != null;
        }
    }
}
