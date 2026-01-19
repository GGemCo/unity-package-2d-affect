using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 메모리 상에서 Affect 정의와 모디파이어를 관리하는 간단한 구현체입니다.
    /// </summary>
    /// <remarks>
    /// - 런타임 중 동적으로 로드/생성된 Affect 데이터를 보관하는 용도로 사용됩니다.
    /// - 디스크나 서버와의 동기화 없이, 단일 실행 컨텍스트 내에서만 유효합니다.
    /// - 테스트, 프로토타이핑, 또는 에디터/부트스트랩 단계에서의 임시 저장소로 적합합니다.
    /// </remarks>
    public sealed class InMemoryAffectRepository : IAffectDefinitionRepository
    {
        /// <summary>
        /// Affect UID를 키로 하는 Affect 정의 캐시입니다.
        /// </summary>
        private readonly Dictionary<int, AffectDefinition> _affects = new();

        /// <summary>
        /// Affect UID를 키로 하는 모디파이어 정의 목록 캐시입니다.
        /// </summary>
        private readonly Dictionary<int, List<AffectModifierDefinition>> _modifiers = new();

        /// <summary>
        /// 저장된 모든 Affect 정의와 모디파이어를 제거합니다.
        /// </summary>
        public void Clear()
        {
            _affects.Clear();
            _modifiers.Clear();
        }

        /// <summary>
        /// Affect 정의와 해당 Affect에 속한 모디파이어 목록을 등록합니다.
        /// </summary>
        /// <param name="definition">등록할 Affect 정의입니다.</param>
        /// <param name="modifiers">
        /// Affect에 연결된 모디파이어 정의 목록입니다.
        /// null인 경우 빈 목록으로 등록됩니다.
        /// </param>
        public void Register(AffectDefinition definition, List<AffectModifierDefinition> modifiers)
        {
            // NOTE: 중복 UID가 등록되면 기존 정의를 덮어씁니다.
            Debug.Log($"affect 등록. Uid: {definition.uid}");

            _affects[definition.uid] = definition;
            _modifiers[definition.uid] = modifiers ?? new List<AffectModifierDefinition>(0);
        }

        /// <summary>
        /// 지정한 UID에 해당하는 Affect 정의를 조회합니다.
        /// </summary>
        /// <param name="affectUid">조회할 Affect를 식별하는 고유 UID입니다.</param>
        /// <param name="definition">조회에 성공한 경우 반환되는 Affect 정의입니다.</param>
        /// <returns>
        /// 정의가 존재하면 true, 존재하지 않으면 false를 반환합니다.
        /// </returns>
        public bool TryGetAffect(int affectUid, out AffectDefinition definition)
        {
            Debug.Log($"affect 가져오기. count: {_affects.Count}, uid: {affectUid}");
            return _affects.TryGetValue(affectUid, out definition);
        }

        /// <summary>
        /// 지정한 Affect UID에 연결된 모디파이어 정의 목록을 반환합니다.
        /// </summary>
        /// <param name="affectUid">모디파이어를 조회할 Affect를 식별하는 고유 UID입니다.</param>
        /// <returns>
        /// Affect에 속한 모디파이어 정의의 읽기 전용 목록입니다.
        /// UID가 등록되지 않은 경우 빈 목록을 반환합니다.
        /// </returns>
        public IReadOnlyList<AffectModifierDefinition> GetModifiers(int affectUid)
        {
            if (_modifiers.TryGetValue(affectUid, out var list))
                return list;

            return System.Array.Empty<AffectModifierDefinition>();
        }
    }
}
