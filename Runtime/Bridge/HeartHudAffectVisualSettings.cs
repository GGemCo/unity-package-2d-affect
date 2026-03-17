using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

/// <summary>
/// Heart 기반 HUD 연출용 Affect 시각 상태 설정입니다.
/// </summary>
[CreateAssetMenu(fileName = "HeartHudAffectVisualSettings", menuName = "GGemCo/UI/Heart HUD Affect Visual Settings")]
public class HeartHudAffectVisualSettings : HudAffectVisualSettings
{
    [SerializeField] private List<HeartHudVisualProfile> profiles = new();

    protected override HudAffectVisualProfileBase GetProfile(string stateKey)
    {
        if (profiles == null || profiles.Count == 0)
            return null;

        string normalized = NormalizeStateKey(stateKey);
        for (int i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            if (profile == null || string.IsNullOrWhiteSpace(profile.StateKey))
                continue;

            if (string.Equals(profile.StateKey, normalized, StringComparison.OrdinalIgnoreCase))
                return profile;
        }

        return null;
    }
}

/// <summary>
/// 특정 Affect 상태 키에 대응하는 Heart HUD 시각 프로필입니다.
/// </summary>
[Serializable]
public sealed class HeartHudVisualProfile : HudAffectVisualProfileBase
{
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private bool overrideBaseSprites = false;
    [SerializeField] private List<Sprite> baseSprites = new();
    [SerializeField] private bool overrideTempSprites = false;
    [SerializeField] private List<Sprite> tempSprites = new();
    [SerializeField] private bool usePulse = false;
    [SerializeField] private float pulseScaleMultiplier = 1.05f;
    [SerializeField] private float pulseSpeed = 3.0f;

    public Color Tint => tint;
    public bool OverrideBaseSprites => overrideBaseSprites;
    public IReadOnlyList<Sprite> BaseSprites => baseSprites;
    public bool OverrideTempSprites => overrideTempSprites;
    public IReadOnlyList<Sprite> TempSprites => tempSprites;
    public bool UsePulse => usePulse;
    public float PulseScaleMultiplier => pulseScaleMultiplier;
    public float PulseSpeed => pulseSpeed;
}
