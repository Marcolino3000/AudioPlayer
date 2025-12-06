using UnityEngine;

namespace Editor.AudioEditor
{
    [CreateAssetMenu(fileName = "AudioPlayerSettings", menuName = "AudioEditor/AudioPlayerSettings", order = 1)]
    public class AudioPlayerSettings : ScriptableObject
    {
        [Header("Waveform Display Settings")]
        [Min(1)]
        public int waveformWidth;
        [Min(1)]
        public int waveformHeight;
        public Color waveformColor;
        public float scale;
    }
}

