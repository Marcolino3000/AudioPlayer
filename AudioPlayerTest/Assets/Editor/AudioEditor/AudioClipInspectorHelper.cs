using System;
using System.Reflection;
using Codice.CM.SEIDInfo;
using UnityEditor;
using UnityEngine;

namespace Editor.AudioEditor
{
    public static class AudioClipInspectorHelper
    {
        public static UnityEditor.Editor CreateAudioClipInspector(AudioClip clip)
        {
            // Get the type of AudioClipInspector
            Type audioClipInspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AudioClipInspector");

            if (audioClipInspectorType == null)
            {
                Debug.LogError("AudioClipInspector type not found.");
                return null;
            }

            // Create an instance of AudioClipInspector
            UnityEditor.Editor audioClipInspector = Activator.CreateInstance(audioClipInspectorType, true) as UnityEditor.Editor;

            if (audioClipInspector == null)
            {
                Debug.LogError("Failed to create an instance of AudioClipInspector.");
                return null;
            }

            FieldInfo targetField = audioClipInspectorType.GetField("target", BindingFlags.Public | BindingFlags.Instance);
            if (targetField != null)
            {
                targetField.SetValue(audioClipInspector, clip);
            }
            else
            {
                Debug.LogError("Failed to set the target AudioClip on AudioClipInspector.");
            }

            return audioClipInspector;
        }
    }
}
