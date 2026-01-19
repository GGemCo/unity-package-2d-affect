using UnityEditor;
using UnityEngine;
using GGemCo2DTcgEditor;

namespace GGemCo2DAffectEditor
{
    public class AddressableEditorAffect : DefaultEditorWindowAffect
    {
        private const string Title = "Addressable 셋팅하기";
        public float buttonWidth;
        public float buttonHeight;
        
        private SettingScriptableObjectAffect _settingScriptableObjectAffect;
        private SettingTableAffect _settingTableAffect;
        private SettingAffectImage _settingAffectImage;
        
        private Vector2 _scrollPosition;

        [MenuItem(ConfigEditorAffect.NameToolSettingAddressable, false, (int)ConfigEditorAffect.ToolOrdering.SettingAddressable)]
        public static void ShowWindow()
        {
            GetWindow<AddressableEditorAffect>(Title);
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            
            buttonHeight = 40f;
            
            _settingScriptableObjectAffect = new SettingScriptableObjectAffect(this);
            _settingTableAffect = new SettingTableAffect(this);
            _settingAffectImage = new SettingAffectImage(this);
        }

        private void OnGUI()
        {
            buttonWidth = position.width / 2f - 10f;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.BeginHorizontal();
            // _settingScriptableObjectAffect.OnGUI();
            _settingTableAffect.OnGUI();
            _settingAffectImage.OnGUI();
            EditorGUILayout.EndHorizontal();
            
            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }
    }
}