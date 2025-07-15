using LazyCoder.SO;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LazyCoder.Audio.Editor
{
    public static class AudioEditor 
    {
        [MenuItem("LazyCoder/Audio/Create config from audio file")]
        private static void CreateConfig()
        {
            foreach (var s in Selection.objects)
            {
                var assetPath = AssetDatabase.GetAssetPath(s);

                if (s.GetType() != typeof(AudioClip))
                    continue;

                AudioClip clip = (AudioClip)s;

                AudioConfig config = ScriptableObjectHelper.LoadOrCreateNewAsset<AudioConfig>(Path.GetDirectoryName(assetPath), clip.name);

                if (config == null)
                    continue;

                config.Clip = clip;

                ScriptableObjectHelper.SaveAsset(config);
            }
        }
    }
}
