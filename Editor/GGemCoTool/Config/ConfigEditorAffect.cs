using GGemCo2DCore;

namespace GGemCo2DAffectEditor
{
    public static class ConfigEditorAffect
    {
        public enum ToolOrdering
        {
            AutoSetting = 1,
            DefaultSetting,
            SettingAddressable,
            SettingScenePreIntro,
            SettingSceneLoading,
            SettingSceneGame,
            Development = 100,
            CreateSampleAiDeck,
            CreateAbilityLocalization,
            CreateLifetimeLocalization,
            Test = 200,
            PreviewShuffle,
            Etc = 900,
        }
        private const string NameToolGGemCo = ConfigDefine.NameSDK+"ToolAffect/";
        // 기본 셋팅하기
        private const string NameToolSettings = NameToolGGemCo + "설정하기/";
        public const string NameToolSettingAuto = NameToolSettings + "자동 셋팅하기";
        public const string NameToolSettingDefault = NameToolSettings + "기본 셋팅하기";
        public const string NameToolSettingAddressable = NameToolSettings + "Addressable 셋팅하기";
        public const string NameToolSettingScenePreIntro = NameToolSettings + "Pre 인트로 씬 셋팅하기";
        public const string NameToolSettingSceneLoading = NameToolSettings + "로딩 씬 셋팅하기";
        public const string NameToolSettingSceneGame = NameToolSettings + "게임 씬 셋팅하기";
        
        // 개발툴
        private const string NameToolDevelopment = NameToolGGemCo + "개발툴/";
        
        // 테스트
        private const string NameToolTest = NameToolGGemCo + "태스트툴/";
        
        // etc
        private const string NameToolEtc = NameToolGGemCo + "기타/";
        
        // 에디터에서 사용되는 프리팹 경로
        public const string PathPackageCore = "Packages/com.ggemco.2d.affect";
    }
}