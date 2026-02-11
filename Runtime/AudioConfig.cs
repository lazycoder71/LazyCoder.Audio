using Sirenix.OdinInspector;
using UnityEngine;

namespace LazyCoder.Audio
{
    [System.Serializable]
    public class AudioConfig : ScriptableObject
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField] private AudioType _type;

        [Range(0f, 1f)]
        [SerializeField] private float _volumeScale = 1f;

        [SerializeField] private bool _is3D;

        [ShowIf("@_is3D")]
        [MinMaxSlider(0f, 500f, ShowFields = true)]
        [SerializeField] private Vector2 _distance = new Vector2(1f, 10f);

        [Space]
        [SerializeField] private bool _pitchVariation;

        [ShowIf("@_pitchVariation")]
        [MinMaxSlider(-3f, 3f, ShowFields = true)]
        [SerializeField] private Vector2 _pitchVariationRange = new Vector2(1.0f, 1.0f);

        public AudioClip Clip
        {
            get => _clip;
            set => _clip = value;
        }

        public AudioType Type => _type;

        public bool Is3D => _is3D;

        public Vector2 Distance => _distance;

        public float VolumeScale => _volumeScale;

        public bool PitchVariation => _pitchVariation;

        public Vector2 PitchVariationRange => _pitchVariationRange;
    }
}