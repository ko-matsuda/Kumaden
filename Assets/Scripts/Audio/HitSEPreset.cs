using UnityEngine;

[CreateAssetMenu(fileName = "HitSEPreset", menuName = "Kumaden/Hit SE Preset")]
public class HitSEPreset : ScriptableObject
{
    [Header("PERFECT")]
    public AudioClip perfectClip;
    [Range(0f, 1f)] public float perfectVolume = 0.9f;
    [Range(0.5f, 2f)] public float perfectPitchMin = 0.98f;
    [Range(0.5f, 2f)] public float perfectPitchMax = 1.02f;

    [Header("GOOD")]
    public AudioClip goodClip;
    [Range(0f, 1f)] public float goodVolume = 0.8f;
    [Range(0.5f, 2f)] public float goodPitchMin = 0.98f;
    [Range(0.5f, 2f)] public float goodPitchMax = 1.02f;

    [Header("Common")]
    [Tooltip("同時発音の上限（ポリフォニー）")]
    [Range(1, 32)] public int polyphony = 12;
}
