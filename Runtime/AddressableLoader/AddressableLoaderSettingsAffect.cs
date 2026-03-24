using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo2DAffect;
using GGemCo2DCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DControl
{
    /// <summary>
    /// Control 패키지 Settings를 Addressables에서 불러옵니다.
    /// </summary>
    public class AddressableLoaderSettingsAffect : MonoBehaviour
    {
        public static AddressableLoaderSettingsAffect Instance { get; private set; }

        [HideInInspector] public GGemCoAffectSettings affectSettings;

        public delegate void DelegateLoadSettings(GGemCoAffectSettings attackComboSettings);
        public event DelegateLoadSettings OnLoadSettings;
        
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new HashSet<AsyncOperationHandle>();
        private float _loadProgress;

        private void Awake()
        {
            _loadProgress = 0f;
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }

        private void ReleaseAll()
        {
            AddressableLoaderController.ReleaseByHandles(_activeHandles);
        }

        public async Task LoadAllSettingsAsync()
        {
            try
            {
                var affectSettingsTask = LoadSettingsAsync<GGemCoAffectSettings>(ConfigAddressableSettingAffect.AffectSettings.Key);

                await Task.WhenAll(affectSettingsTask);

                affectSettings = affectSettingsTask.Result;

                OnLoadSettings?.Invoke(affectSettings);
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"설정 로딩 중 오류 발생: {ex.Message}");
            }
        }

        private async Task<T> LoadSettingsAsync<T>(string key, bool optional = false) where T : ScriptableObject
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
            await locationsHandle.Task;

            if (!locationsHandle.Status.Equals(AsyncOperationStatus.Succeeded) || locationsHandle.Result.Count == 0)
            {
                if (!optional)
                {
                    GcLogger.LogError($"[AddressableSettingsLoader] '{key}' 가 Addressables에 등록되지 않았습니다. '{key}' 를 생성한 후 {ConfigDefine.NameSDK}Tool > 기본 셋팅하기 메뉴를 열고 Addressable 추가하기 버튼을 클릭해주세요.");
                }

                Addressables.Release(locationsHandle);
                return null;
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            _activeHandles.Add(handle);
            T asset = await handle.Task;

            Addressables.Release(locationsHandle);
            return asset;
        }

        public float GetLoadProgress() => _loadProgress;
    }
}
