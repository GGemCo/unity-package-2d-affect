using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// TCG 패키지에서 사용하는 Localization 테이블 및 Key 네이밍 규칙을 정의합니다.
    /// 문자열 리소스의 식별자(Single Source of Truth)를 중앙에서 관리합니다.
    /// </summary>
    public static class LocalizationConstantsAffect
    {
        /// <summary>
        /// Localization Table 이름 정의 모음입니다.
        /// 각 상수는 Unity Localization 패키지에서 사용하는 테이블 이름과 1:1로 대응됩니다.
        /// </summary>
        public static class Tables
        {
            /// <summary>
            /// TCG 게임 메뉴 UI(Window)에서 사용하는 Localization 테이블입니다.
            /// </summary>
            public const string UIWindowGameMenu = ConfigDefine.NameSDK + "_UIWindowTcgGameMenu";

            /// <summary>
            /// 카드 상세 정보 UI(Window)에서 사용하는 Localization 테이블입니다.
            /// </summary>
            public const string UIWindowCardInfo = ConfigDefine.NameSDK + "_UIWindowTcgCardInfo";

            /// <summary>
            /// 내 덱(My Deck) UI(Window)에서 사용하는 Localization 테이블입니다.
            /// </summary>
            public const string UIWindowMyDeck = ConfigDefine.NameSDK + "_UIWindowTcgMyDeck";

            /// <summary>
            /// 카드 이름(Name)을 관리하는 Localization 테이블입니다.
            /// 카드 ID를 Key로 사용하여 다국어 이름을 제공합니다.
            /// </summary>
            public const string CardName = ConfigDefine.NameSDK + "_Tcg_Card_Name";

            /// <summary>
            /// 카드 능력 설명(Ability Description)을 관리하는 Localization 테이블입니다.
            /// </summary>
            public const string AbilityDescription = ConfigDefine.NameSDK + "_Tcg_Ability_Description";

            /// <summary>
            /// 카드 능력 발동 조건(Trigger)을 관리하는 Localization 테이블입니다.
            /// </summary>
            public const string AbilityTrigger = ConfigDefine.NameSDK + "_Tcg_Ability_Trigger";

            /// <summary>
            /// 카드 능력 대상(Target)을 관리하는 Localization 테이블입니다.
            /// </summary>
            public const string AbilityTarget = ConfigDefine.NameSDK + "_Tcg_Ability_Target";

            /// <summary>
            /// 카드 지속 시간/수명(Lifetime)에 대한 설명을 관리하는 Localization 테이블입니다.
            /// </summary>
            public const string LifetimeDescription = ConfigDefine.NameSDK + "_Tcg_Lifetime_Description";

            /// <summary>
            /// TCG에서 사용하는 모든 Localization 테이블 이름 목록입니다.
            /// 초기화, 검증, 프리로드 등의 공통 처리에 사용됩니다.
            /// </summary>
            public static readonly string[] All =
            {
                UIWindowGameMenu,
                UIWindowCardInfo,
                UIWindowMyDeck,
                CardName,
                AbilityDescription,
                AbilityTrigger,
                AbilityTarget,
                LifetimeDescription,
            };
        }

        /// <summary>
        /// Localization Key 네이밍 규칙 정의 모음입니다.
        /// 실제 Key 문자열을 조합하거나 규칙화할 때 공통 접두사/카테고리로 사용됩니다.
        /// </summary>
        public static class Keys
        {
            /// <summary>
            /// 버튼(Button) UI 요소에서 사용하는 Key 접두사입니다.
            /// 예: Button_OK, Button_Cancel
            /// </summary>
            private const string NameButton = "Button";

            /// <summary>
            /// 일반 텍스트(Text) UI 요소에서 사용하는 Key 접두사입니다.
            /// 예: Text_Title, Text_Description
            /// </summary>
            private const string NameText = "Text";
        }
    }
}
