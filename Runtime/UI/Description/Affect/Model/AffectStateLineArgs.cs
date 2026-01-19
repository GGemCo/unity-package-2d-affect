namespace GGemCo2DAffect
{
    /// <summary>
    /// 상태이상(State) 라인 Smart String에 전달되는 인자 모델입니다.
    /// </summary>
    public sealed class AffectStateLineArgs
    {
        /// <summary>상태이상의 표시 이름입니다.</summary>
        public string StateName { get; set; }

        /// <summary>적용 확률(퍼센트) 텍스트입니다.</summary>
        public string ChancePercentText { get; set; }

        /// <summary>지속시간이 존재하는지 여부입니다.</summary>
        public bool HasDuration { get; set; }

        /// <summary>지속시간(초) 텍스트입니다.</summary>
        public string DurationSecondsText { get; set; }
    }
}