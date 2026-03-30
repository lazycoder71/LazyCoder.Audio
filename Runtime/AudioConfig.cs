using Sirenix.OdinInspector;
using UnityEngine;

namespace LazyCoder.Audio
{
    /// <summary>
    /// ScriptableObject that defines audio playback settings for a single clip.
    /// </summary>
    public class AudioConfig : ScriptableObject
    {
        [SerializeField] private AudioClip _clip;

        [SerializeField] private AudioCategory _category;

        [Range(0f, 1f)]
        [SerializeField] private float _volumeScale = 1f;

        [Title("3D Settings")]
        [SerializeField] private bool _is3D;

        [ShowIf("@_is3D")]
        [MinMaxSlider(0f, 500f, ShowFields = true)]
        [SerializeField] private Vector2 _distance = new Vector2(1f, 10f);

        [Title("Pitch Settings")]
        [SerializeField] private bool _pitchVariation;

        [ShowIf("@_pitchVariation")]
        [MinMaxSlider(-3f, 3f, ShowFields = true)]
        [SerializeField] private Vector2 _pitchVariationRange = new Vector2(1.0f, 1.0f);

        public AudioClip Clip => _clip;

        public AudioCategory Category => _category;

        public float VolumeScale => _volumeScale;

        public bool Is3D => _is3D;

        public Vector2 Distance => _distance;

        public bool PitchVariation => _pitchVariation;

        public Vector2 PitchVariationRange => _pitchVariationRange;

        public void SetClip(AudioClip clip)
        {
            _clip = clip;
        }
    }
}