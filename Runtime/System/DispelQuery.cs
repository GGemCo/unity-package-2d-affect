using System.Collections.Generic;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Dispel(해제) 대상 어펙트를 선별하기 위한 조건 집합.
    /// </summary>
    /// <remarks>
    /// - DispelType, 태그 조건을 조합하여 제거 대상을 필터링한다.
    /// - 실제 제거 개수 제한은 MaxRemoveCount로 제어한다.
    /// - AffectComponent.Dispel()에서 사용된다.
    /// </remarks>
    public sealed class DispelQuery
    {
        /// <summary>
        /// 해제할 어펙트의 DispelType 조건.
        /// None이면 타입 조건을 적용하지 않는다.
        /// </summary>
        public DispelType DispelType = DispelType.None;

        /// <summary>
        /// 최대 제거 개수.
        /// 기본값은 int.MaxValue로 사실상 제한이 없다.
        /// </summary>
        public int MaxRemoveCount = int.MaxValue;

        /// <summary>
        /// 반드시 포함되어야 하는 태그 목록.
        /// 모든 태그가 AffectDefinition에 존재해야 매칭된다(AND 조건).
        /// </summary>
        public readonly List<string> RequireTags = new();

        /// <summary>
        /// 포함되면 안 되는 태그 목록.
        /// 하나라도 AffectDefinition에 존재하면 매칭에서 제외된다.
        /// </summary>
        public readonly List<string> ExcludeTags = new();

        /// <summary>
        /// 주어진 어펙트 정의가 이 쿼리 조건에 부합하는지 판단한다.
        /// </summary>
        /// <param name="def">검사할 어펙트 정의.</param>
        /// <returns>모든 조건을 만족하면 true, 하나라도 만족하지 않으면 false.</returns>
        /// <remarks>
        /// 평가 순서:
        /// 1) DispelType 검사
        /// 2) RequireTags 전체 포함 여부 검사
        /// 3) ExcludeTags 포함 여부 검사
        /// </remarks>
        public bool Match(AffectDefinition def)
        {
            if (def == null) return false;

            if (DispelType != DispelType.None && def.dispelType != DispelType)
                return false;

            for (int i = 0; i < RequireTags.Count; i++)
            {
                if (!def.HasTag(RequireTags[i])) return false;
            }

            for (int i = 0; i < ExcludeTags.Count; i++)
            {
                if (def.HasTag(ExcludeTags[i])) return false;
            }

            return true;
        }
    }
}