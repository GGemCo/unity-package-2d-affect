namespace GGemCo2DAffect
{
    /// <summary>
    /// 회복(Heal) 라인 Smart String에 전달되는 인자 모델입니다.
    /// </summary>
    public sealed class AffectHealLineArgs
    {
        /// <summary>기본 회복 값 텍스트입니다.</summary>
        public string BaseValueText { get; set; }

        /// <summary>스케일링(계수/스탯 기반)이 존재하는지 여부입니다.</summary>
        public bool HasScaling { get; set; }

        /// <summary>스케일링 기준 스탯의 표시 이름입니다.</summary>
        public string ScalingStatName { get; set; }

        /// <summary>스케일링 계수 텍스트입니다.</summary>
        public string ScalingCoefText { get; set; }
    }
}
