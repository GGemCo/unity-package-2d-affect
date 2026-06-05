using System.Threading.Tasks;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect Addressables 아틀라스에서 버프/디버프 UI 아이콘 Sprite를 제공하는 Provider입니다.
    /// </summary>
    public sealed class AddressableLoaderAffectIconSpriteProvider : IAddressableIconSpriteProvider
    {
        /// <summary>
        /// Affect 아이콘 아틀라스 요청인지 확인합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>Affect 아이콘 요청이면 <see langword="true"/>입니다.</returns>
        public bool CanHandle(AddressableIconSpriteRequest request)
        {
            return request.AtlasType == AddressableIconAtlasType.AffectIcon;
        }

        /// <summary>
        /// 이미 준비된 Affect 아이콘 캐시에서 Sprite를 즉시 조회합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>캐시에서 찾은 Sprite입니다.</returns>
        public Sprite GetCachedSprite(AddressableIconSpriteRequest request)
        {
            AddressableLoaderAffect loader = AddressableLoaderAffect.Instance;
            if (loader == null)
            {
                return null;
            }

            return loader.GetCachedImageIconByName(request.SpriteName);
        }

        /// <summary>
        /// Affect 아이콘 Atlas를 필요한 시점에 로드한 뒤 Sprite를 조회합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        /// <returns>로드 후 찾은 Sprite입니다.</returns>
        public Task<Sprite> LoadSpriteAsync(AddressableIconSpriteRequest request)
        {
            AddressableLoaderAffect loader = AddressableLoaderAffect.Instance;
            if (loader == null)
            {
                return Task.FromResult<Sprite>(null);
            }

            return loader.LoadImageIconByNameAsync(request.SpriteName);
        }
    }
}
