using System;
using System.Collections.Generic;
using LazyCoder.Core;

namespace LazyCoder.Audio
{
    /// <summary>
    /// Central static manager for audio playback, volume control, and active player tracking.
    /// </summary>
    public static class AudioManager
    {
        public static readonly LzValue<float> VolumeMaster = new(1.0f);
        public static readonly LzValue<float> VolumeMusic = new(1.0f);
        public static readonly LzValue<float> VolumeSound = new(1.0f);

        public static readonly AudioPool Pool = new();

        private static readonly List<AudioPlayer> _activePlayers = new();

        /// <summary>
        /// Play an audio config and return the player handle.
        /// </summary>
        public static AudioPlayer Play(AudioConfig config, bool isLoop = false)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new AudioPlayer(Pool.Get(), config, isLoop);
        }

        /// <summary>
        /// Stop all active players that match the given config.
        /// </summary>
        public static void Stop(AudioConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            for (int i = _activePlayers.Count - 1; i >= 0; i--)
            {
                if (_activePlayers[i].Config == config)
                    _activePlayers[i].Stop();
            }
        }

        /// <summary>
        /// Stop all currently active audio players.
        /// </summary>
        public static void StopAll()
        {
            for (int i = _activePlayers.Count - 1; i >= 0; i--)
            {
                _activePlayers[i].Stop();
            }
        }

        /// <summary>
        /// Get the volume LzValue for the given audio category.
        /// </summary>
        public static LzValue<float> GetCategoryVolume(AudioCategory category)
        {
            return category == AudioCategory.Music ? VolumeMusic : VolumeSound;
        }

        internal static void Register(AudioPlayer player) => _activePlayers.Add(player);

        internal static void Unregister(AudioPlayer player) => _activePlayers.Remove(player);
    }
}