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
        private AudioSource _source;
        private readonly AudioConfig _config;

        private bool _isDisposed;
        private bool _isPaused;

        // Bind target tracking
        private bool _isBinding;
        private Transform _bindTarget;

        // Internal volume (0-1), independent of config/global volume
        private float _volume = 1.0f;

        private Tween _fadeTween;
        private CancelToken _cancelToken = new();

        public AudioConfig Config => _config;

        public float PlayTime { get; private set; }

        public bool IsPlaying => !_isDisposed && _source && _source.isPlaying;

        public bool IsIsPaused => _isPaused;

        public AudioPlayer(AudioSource source, AudioConfig config, bool isLoop)
        {
            _source = source;
            _config = config;

            // Setup AudioSource
            _source.clip = config.Clip;
            _source.time = 0f;
            _source.loop = isLoop;
            _source.spatialBlend = config.Is3D ? 1.0f : 0f;
            _source.minDistance = config.Distance.x;
            _source.maxDistance = config.Distance.y;
            _source.pitch = config.PitchVariation ? config.PitchVariationRange.RandomWithin() : 1.0f;

            PlayTime = Time.time;

            UpdateVolume();
            _source.Play();

            // Auto-cleanup for non-looping clips
            AutoCleanupAsync(_cancelToken.Token).Forget();

            // Register & subscribe events
            AudioManager.Register(this);
            MonoCallback.SafeInstance.EventLateUpdate += OnLateUpdate;
            AudioManager.VolumeMaster.EventValueChanged += OnVolumeChanged;
            AudioManager.GetCategoryVolume(_config.Category).EventValueChanged += OnVolumeChanged;
        }

        /// <summary>
        /// Set the world position of the audio source.
        /// </summary>
        public AudioPlayer SetPosition(Vector3 position)
        {
            if (_source)
                _source.transform.position = position;

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
            _volume = volume;
            UpdateVolume();

            return this;
        }

        /// <summary>
        /// Override the pitch of the audio source.
        /// </summary>
        public AudioPlayer SetPitch(float pitch)
        {
            if (_source)
                _source.pitch = pitch;

            return this;
        }

        /// <summary>
        /// Pause playback. Can be resumed with <see cref="Resume"/>.
        /// </summary>
        public AudioPlayer Pause()
        {
            if (_isDisposed || !_source || _isPaused)
                return this;

            _isPaused = true;
            _source.Pause();

            return this;
        }

        /// <summary>
        /// Resume playback after <see cref="Pause"/>.
        /// </summary>
        public AudioPlayer Resume()
        {
            if (_isDisposed || !_source || !_isPaused)
                return this;

            _isPaused = false;
            _source.UnPause();

            return this;
        }

        /// <summary>
        /// Fade in from silence to the target volume over the specified duration.
        /// </summary>
        public AudioPlayer FadeIn(float duration, float targetVolume = 1.0f)
        {
            if (_isDisposed || !_source)
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
            if (_isDisposed || !_source)
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

            if (_source)
            {
                _source.Stop();
                AudioManager.Pool.Release(_source);
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
            AudioManager.GetCategoryVolume(_config.Category).EventValueChanged -= OnVolumeChanged;

            _source = null;

            GC.SuppressFinalize(this);
        }

        private void OnVolumeChanged(float oldValue, float newValue) => UpdateVolume();

        private void OnLateUpdate() => FollowBindTarget();

        private void UpdateVolume()
        {
            if (!_source)
                return;

            float categoryVolume = AudioManager.GetCategoryVolume(_config.Category).Value;
            _source.volume = _volume * _config.VolumeScale * categoryVolume * AudioManager.VolumeMaster.Value;
        }

        private void FollowBindTarget()
        {
            if (!_isBinding)
                return;

            if (_bindTarget && _source)
                _source.transform.position = _bindTarget.position;
            else
                Stop();
        }

        private void AnimateVolume(float targetVolume, float duration, Action onComplete = null)
        {
            KillFadeTween();

            _fadeTween = DOTween.To(() => _volume, x =>
                {
                    _volume = x;
                    UpdateVolume();
                }, targetVolume, duration)
                .SetTarget(_source)
                .SetLink(_source.gameObject);

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

                if (_source && !_source.isPlaying && !_source.loop && !_isPaused)
                {
                    Stop();
                    return;
                }
            }
        }
    }
}