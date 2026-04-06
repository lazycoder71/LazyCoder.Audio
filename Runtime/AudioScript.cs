using UnityEngine;

namespace LazyCoder.Audio
{
    public class AudioScript : MonoBehaviour
    {
        public void PlayOneShot(AudioConfig config)
        {
            AudioManager.Play(config, false);
        }
    }
}