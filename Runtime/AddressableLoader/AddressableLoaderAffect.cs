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
    /// Affect 아이콘(SpriteAtlas)을 Addressables로부터 로드하고, 아이콘 키 기반으로 Sprite를 조회/캐싱하는 로더입니다.
    /// </summary>
    /// <remarks>
    /// - Addressables 라벨(ConfigAddressableLabelAffect.ImageAffectIcon)로 SpriteAtlas 로케이션을 조회한 뒤 Atlas들을 로드합니다.
    /// - 로드된 Atlas에서 iconKey로 Sprite를 찾아 캐시에 저장하며, 동일 키 조회 시 재탐색을 방지합니다.
    /// - 로딩 진행률은 Atlas 로딩 퍼센트를 기반으로 계산하여 제공합니다.
    /// - 생성된 핸들은 내부 컬렉션에 보관하며, 파괴 시 일괄 해제합니다.
    /// </remarks>
    public class AddressableLoaderAffect : MonoBehaviour
    {
        /// <summary>
        /// 전역에서 접근 가능한 싱글턴 인스턴스입니다.
        /// </summary>
        public static AddressableLoaderAffect Instance { get; private set; }

        /// <summary>
        /// 로드된 SpriteAtlas 목록입니다.
        /// </summary>
        private readonly List<SpriteAtlas> _atlases = new();

        /// <summary>
        /// 아이콘 키(iconKey)로 조회한 Sprite를 캐싱합니다.
        /// </summary>
        /// <remarks>
        /// 키 비교는 StringComparer.Ordinal을 사용하여 문화권 영향 없이 고정 비교합니다.
        /// null도 캐싱하여 반복 로그/탐색을 방지합니다.
        /// </remarks>
        private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.Ordinal);

        /// <summary>
        /// Addressables 로딩에서 생성된 활성 핸들 목록입니다(해제 대상).
        /// </summary>
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new();

        /// <summary>
        /// Atlas 로딩 진행률(0~1)입니다.
        /// </summary>
        private float _prefabLoadProgress;

        /// <summary>
        /// 싱글턴을 초기화하고 파괴되지 않는 오브젝트로 등록합니다.
        /// </summary>
        private void Awake()
        {
            _prefabLoadProgress = 0f;

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

        /// <summary>
        /// 오브젝트 파괴 시 보유 중인 Addressables 핸들을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
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
        /// Affect 아이콘 SpriteAtlas들을 Addressables 라벨 기반으로 비동기 로드합니다.
        /// </summary>
        /// <returns>로딩이 완료될 때까지 대기하는 Task입니다.</returns>
        /// <remarks>
        /// 처리 흐름:
        /// - 라벨로 리소스 로케이션 목록을 조회합니다.
        /// - 각 로케이션의 PrimaryKey(address)로 SpriteAtlas를 로드합니다.
        /// - 로딩 중에는 PercentComplete 기반으로 진행률을 갱신합니다.
        /// - 로딩이 끝나면 Atlas 목록/캐시를 갱신하고 핸들을 추적합니다.
        /// </remarks>
        /// <exception cref="Exception">
        /// Addressables 내부 로딩 중 예외가 발생할 수 있으며, 본 메서드는 catch에서 로그만 남기고 종료합니다.
        /// </exception>
        public async Task LoadAtlasesAsync()
        {
            try
            {
                _atlases.Clear();
                _spriteCache.Clear();

                // 아이콘 Atlas 로케이션 조회
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabelAffect.ImageAffectIcon);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressableLabelAffect.ImageAffectIcon} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = locationHandle.Result.Count;
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    var loadHandle = Addressables.LoadAssetAsync<SpriteAtlas>(address);

                    // 로딩 진행률 갱신(현재 로드 중인 항목 PercentComplete 포함)
                    while (!loadHandle.IsDone)
                    {
                        _prefabLoadProgress =
                            (loadedCount + loadHandle.PercentComplete) / Mathf.Max(1, totalCount);

                        await Task.Yield();
                    }

                    _activeHandles.Add(loadHandle);

                    var atlas = await loadHandle.Task;
                    if (atlas == null) continue;

                    _atlases.Add(atlas);
                    loadedCount++;
                }

                _activeHandles.Add(locationHandle);

                _prefabLoadProgress = 1f;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"[AffectIcon] 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 아이콘 키로 Sprite를 조회합니다. 없으면 로드된 Atlas들을 순회하여 검색하고 캐싱합니다.
        /// </summary>
        /// <param name="iconKey">Atlas 내 Sprite 이름(아이콘 키)입니다.</param>
        /// <returns>조회된 Sprite. 찾을 수 없으면 null을 반환합니다.</returns>
        public Sprite GetImageIconByName(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return null;

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

            // 1회만 로그를 남기기 위해 null도 캐싱
            _spriteCache[iconKey] = null;
            GcLogger.LogError($"Addressables에서 {iconKey} 아이콘 이미지를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// Atlas 로딩 진행률(0~1)을 반환합니다.
        /// </summary>
        /// <returns>현재 로딩 진행률입니다.</returns>
        public float GetPrefabLoadProgress() => _prefabLoadProgress;
    }
}
