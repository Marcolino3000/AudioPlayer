using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public static class AudioUtilWrapper
    {
        private static readonly Type AudioUtilType;

        static AudioUtilWrapper()
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            AudioUtilType = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        }

        public static bool ResetAllAudioClipPlayCountsOnPlay
        {
            get => (bool)GetProperty("resetAllAudioClipPlayCountsOnPlay");
            set => SetProperty("resetAllAudioClipPlayCountsOnPlay", value);
        }

        public static void PlayPreviewClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            InvokeMethod("PlayPreviewClip", new object[] { clip, startSample, loop });
        }

        public static void PausePreviewClip()
        {
            InvokeMethod("PausePreviewClip", null);
        }

        public static void ResumePreviewClip()
        {
            InvokeMethod("ResumePreviewClip", null);
        }

        public static void LoopPreviewClip(bool on)
        {
            InvokeMethod("LoopPreviewClip", new object[] { on });
        }

        public static bool IsPreviewClipPlaying()
        {
            return (bool)InvokeMethod("IsPreviewClipPlaying", null);
        }

        public static void StopAllPreviewClips()
        {
            InvokeMethod("StopAllPreviewClips", null);
        }

        public static float GetPreviewClipPosition()
        {
            return (float)InvokeMethod("GetPreviewClipPosition", null);
        }

        public static int GetPreviewClipSamplePosition()
        {
            return (int)InvokeMethod("GetPreviewClipSamplePosition", null);
        }

        public static void SetPreviewClipSamplePosition(AudioClip clip, int samplePosition)
        {
            InvokeMethod("SetPreviewClipSamplePosition", new object[] { clip, samplePosition });
        }

        private static object InvokeMethod(string methodName, object[] parameters)
        {
            MethodInfo method = AudioUtilType?.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                Debug.LogError($"Method {methodName} not found in AudioUtil.");
                return null;
            }
            return method.Invoke(null, parameters);
        }

        private static object GetProperty(string propertyName)
        {
            PropertyInfo property = AudioUtilType?.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
            if (property == null)
            {
                Debug.LogError($"Property {propertyName} not found in AudioUtil.");
                return null;
            }
            return property.GetValue(null);
        }

        private static void SetProperty(string propertyName, object value)
        {
            PropertyInfo property = AudioUtilType?.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
            if (property == null)
            {
                Debug.LogError($"Property {propertyName} not found in AudioUtil.");
                return;
            }
            property.SetValue(null, value);
        }
    }
}
