using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo2DCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 아이콘 SpriteAtlas를 Addressables에서 로드하고 캐싱합니다.
    /// </summary>
    public class AddressableLoaderAffect : MonoBehaviour
    {
        public static AddressableLoaderAffect Instance { get; private set; }

        private readonly List<SpriteAtlas> _atlases = new();
        private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.Ordinal);
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new();
        private readonly AddressableLoaderAffectIconSpriteProvider _iconSpriteProvider = new();
        private float _prefabLoadProgress;
        private bool _isAtlasLoaded;
        private Task _atlasLoadTask;

        private void Awake()
        {
            _prefabLoadProgress = 0f;

            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                AddressableIconSpriteProviderRegistry.Register(_iconSpriteProvider);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                AddressableIconSpriteProviderRegistry.Unregister(_iconSpriteProvider);
                Instance = null;
            }

            ReleaseAll();
        }

        /// <summary>
        /// 현재 보관 중인 모든 Addressables 핸들을 해제합니다.
        /// </summary>
        private void ReleaseAll()
        {
            AddressableLoaderController.ReleaseByHandles(_activeHandles);
        }

        /// <summary>
        /// Affect 아이콘 SpriteAtlas들을 비동기로 선로드합니다.
        /// </summary>
        public async Task LoadAtlasesAsync()
        {
            if (_isAtlasLoaded)
            {
                _prefabLoadProgress = 1f;
                return;
            }

            if (_atlasLoadTask != null)
            {
                await _atlasLoadTask;
                return;
            }

            _atlasLoadTask = LoadAtlasesOnceAsync();
            try
            {
                await _atlasLoadTask;
            }
            finally
            {
                _atlasLoadTask = null;
            }
        }

        /// <summary>
        /// Affect 아이콘 Atlas 로딩을 1회 수행합니다.
        /// </summary>
        /// <remarks>
        /// 여러 버프 아이콘이 같은 프레임에 로딩을 요청할 수 있으므로,
        /// 외부 진입점(<see cref="LoadAtlasesAsync"/>)에서 중복 실행을 막고 이 메서드는 실제 로딩만 담당합니다.
        /// </remarks>
        private async Task LoadAtlasesOnceAsync()
        {
            try
            {
                _atlases.Clear();
                _spriteCache.Clear();

                await LoadAtlasesInternalAsync();
                _isAtlasLoaded = true;
                _prefabLoadProgress = 1f;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"[AffectIcon] 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 아이콘 키로 Sprite를 조회합니다.
        /// 선로드되지 않은 경우 최초 1회 지연 로드를 수행합니다.
        /// </summary>
        public Sprite GetImageIconByName(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return null;

            EnsureAtlasLoadedSync();

            if (_spriteCache.TryGetValue(iconKey, out var cached) && cached != null)
                return cached;

            for (int i = 0; i < _atlases.Count; i++)
            {
                var atlas = _atlases[i];
                if (atlas == null) continue;

                var sprite = atlas.GetSprite(iconKey);
                if (sprite != null)
                {
                    _spriteCache[iconKey] = sprite;
                    return sprite;
                }
            }

            _spriteCache[iconKey] = null;
            GcLogger.LogError($"Addressables에서 {iconKey} 아이콘 이미지를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 이미 로드된 Affect 아이콘 캐시 또는 Atlas에서 Sprite를 즉시 조회합니다.
        /// </summary>
        /// <param name="iconKey">Atlas 내부 Sprite 이름입니다.</param>
        /// <returns>캐시 또는 로드 완료된 Atlas에서 찾은 Sprite입니다.</returns>
        /// <remarks>
        /// 이 메서드는 Addressables 동기 로드를 발생시키지 않습니다.
        /// UI는 먼저 이 경로로 캐시를 확인하고, 없으면 비동기 로딩 경로를 사용합니다.
        /// </remarks>
        public Sprite GetCachedImageIconByName(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return null;

            if (_spriteCache.TryGetValue(iconKey, out var cached) && cached != null)
            {
                return cached;
            }

            // Atlas가 아직 준비되지 않았다면 UI 비동기 로딩 단계로 넘긴다.
            if (!_isAtlasLoaded && _atlases.Count <= 0)
            {
                return null;
            }

            for (int i = 0; i < _atlases.Count; i++)
            {
                var atlas = _atlases[i];
                if (atlas == null) continue;

                var sprite = atlas.GetSprite(iconKey);
                if (sprite != null)
                {
                    _spriteCache[iconKey] = sprite;
                    return sprite;
                }
            }

            return null;
        }

        /// <summary>
        /// Affect 아이콘 Atlas를 비동기로 준비한 뒤 Sprite를 조회합니다.
        /// </summary>
        /// <param name="iconKey">Atlas 내부 Sprite 이름입니다.</param>
        /// <returns>로드 후 찾은 Sprite입니다. 찾지 못하면 null입니다.</returns>
        public async Task<Sprite> LoadImageIconByNameAsync(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return null;

            if (!_isAtlasLoaded)
            {
                await LoadAtlasesAsync();
            }

            return GetCachedImageIconByName(iconKey);
        }

        /// <summary>
        /// 시작 로딩에서 제외된 Atlas를 최초 접근 시 동기적으로 로드합니다.
        /// </summary>
        private void EnsureAtlasLoadedSync()
        {
            if (_isAtlasLoaded)
            {
                return;
            }

            LoadAtlasesInternalSync();
            _isAtlasLoaded = true;
        }

        /// <summary>
        /// Affect 아이콘 Atlas 그룹을 비동기로 로드합니다.
        /// </summary>
        private async Task LoadAtlasesInternalAsync()
        {
            var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabelAffect.ImageAffectIcon);
            await locationHandle.Task;

            if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
            {
                GcLogger.LogError($"{ConfigAddressableLabelAffect.ImageAffectIcon} 레이블을 가진 리소스를 찾을 수 없습니다.");
                return;
            }

            int totalCount = Mathf.Max(1, locationHandle.Result.Count);
            int loadedCount = 0;

            foreach (var location in locationHandle.Result)
            {
                string address = location.PrimaryKey;
                var loadHandle = Addressables.LoadAssetAsync<SpriteAtlas>(address);

                while (!loadHandle.IsDone)
                {
                    _prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                    await Task.Yield();
                }

                _activeHandles.Add(loadHandle);
                var atlas = await loadHandle.Task;
                if (atlas == null) continue;

                _atlases.Add(atlas);
                loadedCount++;
            }

            Addressables.Release(locationHandle);
        }

        /// <summary>
        /// Affect 아이콘 Atlas 그룹을 동기적으로 로드합니다.
        /// </summary>
        private void LoadAtlasesInternalSync()
        {
            if (_atlases.Count > 0)
            {
                return;
            }

            var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabelAffect.ImageAffectIcon);
            var locations = locationHandle.WaitForCompletion();

            if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded || locations == null)
            {
                GcLogger.LogError($"{ConfigAddressableLabelAffect.ImageAffectIcon} 레이블을 가진 리소스를 찾을 수 없습니다.");
                Addressables.Release(locationHandle);
                return;
            }

            foreach (var location in locations)
            {
                string address = location.PrimaryKey;
                var loadHandle = Addressables.LoadAssetAsync<SpriteAtlas>(address);
                _activeHandles.Add(loadHandle);
                var atlas = loadHandle.WaitForCompletion();
                if (loadHandle.Status == AsyncOperationStatus.Succeeded && atlas != null)
                {
                    _atlases.Add(atlas);
                }
            }

            Addressables.Release(locationHandle);
        }

        /// <summary>
        /// Atlas 로딩 진행률(0~1)을 반환합니다.
        /// </summary>
        public float GetPrefabLoadProgress() => _prefabLoadProgress;
    }
}
