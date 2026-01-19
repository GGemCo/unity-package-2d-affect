using System;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 상태(State)의 메타데이터 정의.
    /// </summary>
    /// <remarks>
    /// - 테이블에서 로드된 상태 정보를 런타임에서 참조하기 위한 순수 데이터 객체(DTO)이다.
    /// - 상태 자체의 적용/면역/중첩 로직은 Affect/State 시스템에서 처리된다.
    /// - 분류 정보(<see cref="category"/>)는 UI 표시나 필터링, 규칙 처리에 활용될 수 있다.
    /// </remarks>
    [Serializable]
    public sealed class StateDefinition
    {
        /// <summary>
        /// 상태의 고유 식별자.
        /// </summary>
        /// <remarks>
        /// 예: Stun, Freeze, Silence 등
        /// </remarks>
        public string id;

        /// <summary>
        /// 로컬라이즈된 상태 이름을 조회하기 위한 키.
        /// </summary>
        public string nameKey;

        /// <summary>
        /// 상태의 기본 Dispel 분류.
        /// </summary>
        /// <remarks>
        /// 상태 해제(정화/면역) 규칙에서 기본 분류로 사용된다.
        /// </remarks>
        public DispelType dispelType;

        /// <summary>
        /// 상태의 보조 분류 카테고리.
        /// </summary>
        /// <remarks>
        /// - CC, Immune, Utility 등과 같은 논리적 분류 태그를 표현한다.
        /// - 선택 항목이며, 비어 있을 경우 카테고리 분류를 사용하지 않는 것으로 간주한다.
        /// </remarks>
        public string category;
    }
}