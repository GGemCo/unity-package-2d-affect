namespace GGemCo2DAffect
{
    
    /// <summary>
    /// 스탯 변화 라인 Smart String에 전달되는 인자 모델입니다.
    /// </summary>
    public sealed class AffectStatLineArgs
    {
        /// <summary>대상 스탯의 표시 이름입니다.</summary>
        public string StatName { get; set; }

        /// <summary>연산 종류(Add/Multiply/Override 등)의 문자열 표현입니다.</summary>
        public string Operation { get; set; } // Add/Multiply/Override...

        /// <summary>값 타입(Flat/Percent)의 문자열 표현입니다.</summary>
        public string ValueType { get; set; }  // Flat/Percent

        /// <summary>표시용 수치 텍스트입니다.</summary>
        public string ValueText { get; set; }
    }

}