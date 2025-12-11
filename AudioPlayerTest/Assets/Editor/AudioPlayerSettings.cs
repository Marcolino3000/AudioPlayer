using UnityEngine;
using UnityEngine.UIElements;

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
        public Color waveformBackgroundColor;
        public Color gridColor;
        public Color playheadColor;
        public int playheadWidth;
        public Color markerColor;
        public int markerWidth;
        public float waveformScale;
    }
}

