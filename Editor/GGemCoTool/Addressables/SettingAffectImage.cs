using System.Collections.Generic;
using System.IO;
using GGemCo2DAffect;
using GGemCo2DCore;
using GGemCo2DCoreEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DAffectEditor
{
    /// <summary>
    /// 어펙트(Effect) 아이콘 이미지 및 스프라이트 아틀라스를 Addressables 그룹에 등록/재구성하는 에디터 설정 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 테이블(ConfigAddressableTableAffect) 기반으로 아이콘 경로를 계산하여 엔트리를 생성하고,
    /// 동일 자산을 스프라이트 아틀라스에 묶은 뒤 아틀라스 자체도 Addressable로 등록합니다.
    /// </remarks>
    public class SettingAffectImage : DefaultAddressable
    {
        /// <summary>
        /// 버튼에 표시되는 텍스트(기능 실행 제목)입니다.
        /// </summary>
        private const string Title = "어펙트 아이콘 이미지 추가하기";

        /// <summary>
        /// UI 레이아웃(버튼 폭/높이 등) 정보를 제공하는 부모 에디터 윈도우 참조입니다.
        /// </summary>
        private readonly AddressableEditorAffect _addressableEditor;

        /// <summary>
        /// 어펙트 아이콘 Addressable 설정 UI를 초기화합니다.
        /// </summary>
        /// <param name="addressableEditorWindow">버튼 크기 등 UI 정보를 참조할 에디터 윈도우입니다.</param>
        public SettingAffectImage(AddressableEditorAffect addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;

            // NOTE: DefaultAddressable(부모)에서 사용하는 대상 그룹 이름을 어펙트 아이콘 그룹으로 고정합니다.
            targetGroupName = ConfigAddressableGroupNameAffect.AffectIcon;
        }

        /// <summary>
        /// 에디터 윈도우에 표시될 UI를 그립니다.
        /// </summary>
        /// <remarks>
        /// 필수 테이블 파일이 없으면 안내 메시지를 출력하고,
        /// 존재할 경우 버튼을 통해 Setup 실행을 트리거합니다.
        /// </remarks>
        public void OnGUI()
        {
            if (!File.Exists($"{ConfigAddressableTableAffect.TableAffect.Path}"))
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTableAffect.Affect} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    try
                    {
                        Setup();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.DisplayDialog(
                            Title,
                            "어펙트 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.",
                            "OK");
                    }
                }
            }
        }

        /// <summary>
        /// 어펙트 아이콘 이미지(Addressable 엔트리)와 스프라이트 아틀라스를 생성/정리한 뒤 Addressables 설정에 반영합니다.
        /// </summary>
        /// <param name="ctx">
        /// 배치/자동화 실행 컨텍스트입니다. null이면 사용자 확인 다이얼로그 및 완료 다이얼로그를 표시합니다.
        /// </param>
        /// <remarks>
        /// 동작 개요:
        /// - AddressableAssetSettings가 없으면 생성합니다.
        /// - 대상 그룹을 가져오거나 생성합니다.
        /// - 그룹 엔트리를 초기화한 뒤(스키마 유지) 테이블 기반으로 엔트리를 재구성합니다.
        /// - 아이콘들을 아틀라스에 묶고, 아틀라스 자체도 Addressable로 등록합니다.
        /// </remarks>
        public void Setup(EditorSetupContext ctx = null)
        {
            if (ctx == null)
            {
                bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
                if (!result) return;
            }

            // 어펙트 테이블 로드 (Uid, IconKey 등 이미지 매핑에 사용)
            Dictionary<int, StruckTableAffect> dictionary = TableLoaderManagerAffect.LoadAffectTable().GetDatas();

            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // 타겟 그룹 가져오기/생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            // 1) 그룹 엔트리 전체 초기화 (스키마/설정은 유지)
            ClearGroupEntries(settings, group);

            // 스프라이트 아틀라스 준비(폴더가 없으면 생성)
            string atlasFolderPath = ConfigAddressablePath.SpriteAtlas;
            Directory.CreateDirectory(atlasFolderPath);
            var atlas = GetOrCreateSpriteAtlas($"{atlasFolderPath}/AffectIconAtlas.spriteatlas");

            // 2) 테이블 기반으로 엔트리 재구성
            // - 각 아이콘 PNG를 개별 Addressable 엔트리로 등록
            // - 동시에 아틀라스에 포함될 실제 에셋 목록(존재하는 파일만)을 수집
            List<Object> assets = new();
            foreach (KeyValuePair<int, StruckTableAffect> outerPair in dictionary)
            {
                var info = outerPair.Value;
                if (info.Uid <= 0) continue;

                string key = $"{ConfigAddressableKeyAffect.AffectIcon}_{info.Uid}";
                string assetPath = $"{ConfigAddressablePath.Images.RootImage}";
                assetPath = $"{assetPath}/{info.IconKey}.png";

                Add(settings, group, key, assetPath);
                AddToListIfExists(assets, assetPath);
            }

            // 아틀라스 재구성(기존 구성 초기화 후 수집된 에셋을 다시 등록)
            ClearAndAddToAtlas(atlas, assets);

            // 아틀라스 자체도 Addressable 로 등록(공용 키/라벨)
            if (assets.Count > 0)
            {
                Add(
                    settings,
                    group,
                    ConfigAddressableKeyAffect.AffectIcon,
                    AssetDatabase.GetAssetPath(atlas),
                    ConfigAddressableLabelAffect.ImageAffectIcon);
            }

            // 적용/저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 어펙트 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "[Addressable] 어펙트 설정 완료", "OK");
            }
        }
    }
}
