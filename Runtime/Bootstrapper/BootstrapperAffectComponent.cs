using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Core 캐릭터 활성화 이벤트를 기준으로 Affect 컴포넌트 초기화 시점을 연결합니다.
    /// </summary>
    /// <remarks>
    /// 캐릭터의 Awake/Start 순서에 직접 의존하지 않고, Core의 CharacterBase 초기화 완료 이후에
    /// AffectComponent가 런타임 저장소와 표시 서비스를 바인딩하도록 보장합니다.
    /// </remarks>
    public sealed class BootstrapperAffectComponent : MonoBehaviour
    {
        /// <summary>
        /// 대상 캐릭터에 AffectComponent가 없을 때 자동으로 추가할지 여부입니다.
        /// </summary>
        [SerializeField] private bool addIfMissing = true;

        /// <summary>
        /// 오브젝트가 활성화될 때 캐릭터 활성화/파괴 이벤트를 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            CharacterManager.OnCharacterActivated += OnCharacterActivated;
            CharacterManager.OnCharacterDestroyed += OnCharacterDestroyed;
        }

        /// <summary>
        /// 오브젝트가 비활성화될 때 등록한 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            CharacterManager.OnCharacterActivated -= OnCharacterActivated;
            CharacterManager.OnCharacterDestroyed -= OnCharacterDestroyed;
        }

        /// <summary>
        /// 캐릭터가 Core 초기화를 마친 뒤 AffectComponent를 초기화하고 활성화합니다.
        /// </summary>
        /// <param name="character">초기화가 완료된 캐릭터입니다.</param>
        private void OnCharacterActivated(CharacterBase character)
        {
            if (character == null)
                return;

            AffectComponent component = character.GetComponent<AffectComponent>();
            if (component == null && addIfMissing)
                component = character.gameObject.AddComponent<AffectComponent>();

            if (component == null)
                return;

            component.Initialize(null);
            component.Activate(null);
        }

        /// <summary>
        /// 캐릭터 파괴 시 AffectComponent의 명시적 정리 루틴을 호출합니다.
        /// </summary>
        /// <param name="character">파괴되는 캐릭터입니다.</param>
        private void OnCharacterDestroyed(CharacterBase character)
        {
            if (character == null)
                return;

            AffectComponent component = character.GetComponent<AffectComponent>();
            component?.Deinitialize();
        }
    }
}
