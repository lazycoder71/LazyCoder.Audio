using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LazyCoder.Core;
using UnityEngine;

namespace LazyCoder.Audio
{
    /// <summary>
    /// Represents an active audio playback instance with fluent API for control.
    /// Implements <see cref="IDisposable"/> for safe resource cleanup.
    /// </summary>
    public class AudioPlayer : IDisposable
    {
        private bool _isDisposed;

        // Bind target tracking
        private bool _isBinding;
        private Transform _bindTarget;

        // Internal volume (0-1), independent of config/global volume
        private float _volume = 1.0f;

        private Tween _fadeTween;
        private readonly CancelToken _cancelToken = new();

        public AudioConfig Config { get; private set; }

        public AudioSource Source { get; private set; }

        public float PlayTime { get; private set; }

        public bool IsPaused { get; private set; }

        public AudioPlayer(AudioSource source, AudioConfig config, bool isLoop)
        {
            Source = source;
            Config = config;

            // Setup AudioSource
            Source.clip = config.Clip;
            Source.time = 0f;
            Source.loop = isLoop;
            Source.spatialBlend = config.Is3D ? 1.0f : 0f;
            Source.minDistance = config.Distance.x;
            Source.maxDistance = config.Distance.y;
            Source.pitch = config.PitchVariation ? config.PitchVariationRange.RandomWithin() : 1.0f;

            PlayTime = Time.time;

            UpdateVolume();

            Source.Play();

            // Auto-cleanup for non-looping clips
            if (!isLoop)
                AutoCleanupAsync(_cancelToken.Token).Forget();

            // Register & subscribe events
            AudioManager.Register(this);
            MonoCallback.SafeInstance.EventLateUpdate += OnLateUpdate;
            AudioManager.VolumeMaster.EventValueChanged += OnVolumeChanged;
            AudioManager.GetCategoryVolume(Config.Category).EventValueChanged += OnVolumeChanged;
        }

        /// <summary>
        /// Set the world position of the audio source.
        /// </summary>
        public AudioPlayer SetPosition(Vector3 position)
        {
            if (Source)
                Source.transform.position = position;

            return this;
        }

        /// <summary>
        /// Bind the audio source to follow a transform every frame.
        /// Automatically stops if the target is destroyed.
        /// </summary>
        public AudioPlayer BindTo(Transform target)
        {
            _isBinding = true;
            _bindTarget = target;

            return this;
        }

        /// <summary>
        /// Set the local volume multiplier (0-1). Does not affect config or global volume.
        /// </summary>
        public AudioPlayer SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            
            UpdateVolume();

            return this;
        }

        /// <summary>
        /// Override the pitch of the audio source.
        /// </summary>
        public AudioPlayer SetPitch(float pitch)
        {
            if (Source)
                Source.pitch = pitch;

            return this;
        }

        /// <summary>
        /// Pause playback. Can be resumed with <see cref="Resume"/>.
        /// </summary>
        public AudioPlayer Pause()
        {
            if (_isDisposed || !Source || IsPaused)
                return this;

            IsPaused = true;
            Source.Pause();

            return this;
        }

        /// <summary>
        /// Resume playback after <see cref="Pause"/>.
        /// </summary>
        public AudioPlayer Resume()
        {
            if (_isDisposed || !Source || !IsPaused)
                return this;

            IsPaused = false;
            Source.UnPause();

            return this;
        }

        /// <summary>
        /// Fade in from silence to the target volume over the specified duration.
        /// </summary>
        public AudioPlayer FadeIn(float duration, float targetVolume = 1.0f)
        {
            if (_isDisposed || !Source)
                return this;

            _volume = 0f;
            UpdateVolume();

            AnimateVolume(targetVolume, duration);

            return this;
        }

        /// <summary>
        /// Fade out to silence over the specified duration. Optionally stops playback on complete.
        /// </summary>
        public AudioPlayer FadeOut(float duration, bool stopOnComplete = true)
        {
            if (_isDisposed || !Source)
                return this;

            AnimateVolume(0f, duration, stopOnComplete ? Stop : null);

            return this;
        }

        /// <summary>
        /// Stop playback and release the audio source back to the pool.
        /// </summary>
        public void Stop()
        {
            if (_isDisposed)
                return;

            if (Source)
            {
                Source.Stop();
                AudioManager.Pool.Release(Source);
            }

            Dispose();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            KillFadeTween();

            _cancelToken.Cancel();

            AudioManager.Unregister(this);

            MonoCallback.SafeInstance.EventLateUpdate -= OnLateUpdate;
            AudioManager.VolumeMaster.EventValueChanged -= OnVolumeChanged;
            AudioManager.GetCategoryVolume(Config.Category).EventValueChanged -= OnVolumeChanged;

            Source = null;

            GC.SuppressFinalize(this);
        }

        private void OnVolumeChanged(float oldValue, float newValue) => UpdateVolume();

        private void OnLateUpdate() => FollowBindTarget();

        private void UpdateVolume()
        {
            if (!Source)
                return;

            float categoryVolume = AudioManager.GetCategoryVolume(Config.Category).Value;
            Source.volume = _volume * Config.VolumeScale * categoryVolume * AudioManager.VolumeMaster.Value;
        }

        private void FollowBindTarget()
        {
            if (!_isBinding)
                return;

            if (_bindTarget && Source)
                Source.transform.position = _bindTarget.position;
            else
                Stop();
        }

        private void AnimateVolume(float targetVolume, float duration, Action onComplete = null)
        {
            KillFadeTween();

            targetVolume = Mathf.Clamp01(targetVolume);

            _fadeTween = DOTween.To(() => _volume, x =>
                {
                    _volume = x;
                    UpdateVolume();
                }, targetVolume, duration)
                .SetTarget(Source)
                .SetLink(Source.gameObject);

            if (onComplete != null)
                _fadeTween.OnComplete(() => onComplete());
        }

        private void KillFadeTween()
        {
            if (_fadeTween != null && _fadeTween.IsActive())
                _fadeTween.Kill();

            _fadeTween = null;
        }

        private async UniTaskVoid AutoCleanupAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(cancellationToken);

                if (Source && !Source.isPlaying && !Source.loop && !IsPaused)
                {
                    Stop();
                    return;
                }
            }
        }
    }
}