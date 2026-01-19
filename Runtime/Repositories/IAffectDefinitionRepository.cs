using System.Collections.Generic;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 정의(AffectDefinition)와 모디파이어(AffectModifierDefinition)를 조회하기 위한 저장소 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// - Affect 시스템은 데이터의 실제 저장 방식(ScriptableObject, JSON, 서버 데이터 등)에 의존하지 않습니다.
    /// - 이 인터페이스를 통해 Affect UID 기반으로 정의 및 모디파이어 목록을 조회합니다.
    /// </remarks>
    public interface IAffectDefinitionRepository
    {
        /// <summary>
        /// 지정한 Affect UID에 해당하는 Affect 정의를 조회합니다.
        /// </summary>
        /// <param name="affectUid">조회할 Affect를 식별하는 고유 UID입니다.</param>
        /// <param name="definition">조회에 성공한 경우 반환되는 Affect 정의입니다.</param>
        /// <returns>
        /// 정의가 존재하면 true, 존재하지 않으면 false를 반환합니다.
        /// </returns>
        bool TryGetAffect(int affectUid, out AffectDefinition definition);

        /// <summary>
        /// 지정한 Affect UID에 연결된 모든 모디파이어 정의 목록을 반환합니다.
        /// </summary>
        /// <param name="affectUid">모디파이어를 조회할 Affect를 식별하는 고유 UID입니다.</param>
        /// <returns>
        /// Affect에 속한 모디파이어 정의의 읽기 전용 목록입니다.
        /// UID가 유효하지 않은 경우 빈 목록을 반환할 수 있습니다.
        /// </returns>
        IReadOnlyList<AffectModifierDefinition> GetModifiers(int affectUid);
    }
}