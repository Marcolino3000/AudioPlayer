using UnityEngine;

namespace Editor.AudioEditor
{
    [CreateAssetMenu(menuName = "AudioEditor/AudioClip Wrapper")]
    public class AudioClipWrapper : ScriptableObject
    {
        public AudioClip audioClip;
    }
}