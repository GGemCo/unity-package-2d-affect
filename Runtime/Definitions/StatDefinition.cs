using System;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 스탯(Stat)의 메타데이터 정의.
    /// </summary>
    /// <remarks>
    /// - 테이블에서 로드된 스탯 정보를 런타임에서 참조하기 위한 순수 데이터 객체(DTO)이다.
    /// - 기본값(<see cref="defaultValue"/>)은 초기화 또는 기준값 계산에 사용될 수 있다.
    /// - 실제 스탯 누적, 보정, 계산 로직은 별도의 스탯 시스템에서 처리된다.
    /// </remarks>
    [Serializable]
    public sealed class StatDefinition
    {
        /// <summary>
        /// 스탯의 고유 식별자.
        /// </summary>
        /// <remarks>
        /// 예: HP, ATK, DEF, MoveSpeed 등
        /// </remarks>
        public string id;

        /// <summary>
        /// 로컬라이즈된 스탯 이름을 조회하기 위한 키.
        /// </summary>
        public string nameKey;

        /// <summary>
        /// 스탯의 기본값.
        /// </summary>
        /// <remarks>
        /// - 캐릭터/엔티티 생성 시 초기값 또는 기준값으로 사용된다.
        /// - Modifier 적용 전의 베이스 값으로 해석하는 것이 일반적이다.
        /// </remarks>
        public float defaultValue;
    }
}