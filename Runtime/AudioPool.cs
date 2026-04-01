using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace LazyCoder.Audio
{
    /// <summary>
    /// Object pool for reusing AudioSource GameObjects to avoid runtime allocations.
    /// </summary>
    public class AudioPool : ObjectPool<AudioSource>
    {
        private const int DefaultCapacity = 10;
        private const int MaxSize = 100;

        public AudioPool() : base(Create, OnGet, OnRelease, OnDestroy, true, DefaultCapacity, MaxSize)
        {
        }

        private static AudioSource Create()
        {
            var obj = new GameObject(nameof(AudioPlayer));
            Object.DontDestroyOnLoad(obj);
            return obj.AddComponent<AudioSource>();
        }

        private static void OnGet(AudioSource source) => source.gameObject.SetActive(true);

        private static void OnRelease(AudioSource source) => source.gameObject.SetActive(false);

        private static void OnDestroy(AudioSource source) => Object.Destroy(source.gameObject);
    }
}