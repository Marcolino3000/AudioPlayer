using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.AudioEditor
{
    [UnityEditor.CustomEditor(typeof(AudioClipWrapper))]
    public class CustomEditor : UnityEditor.Editor
    {
        private Texture2D previewTexture;
        private AudioClip clip;
        
        public override bool HasPreviewGUI() => true;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var t = (AudioClipWrapper) this.target;
            clip = t.audioClip; 
            // GenerateAudioPreview(clip);
        }
        
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            // GUI.Label(r, (target as MyObject).text);
            
        }

        public override Texture2D RenderStaticPreview(
            string assetPath, Object[] subAssets, int width, int height)
        {
            // Ensure we have the target clip reference
            // clip = (AudioClip)target;

            // Create a new texture of the requested size and fill it with green
            var tex = new Texture2D(Math.Max(1, width), Math.Max(1, height), TextureFormat.RGBA32, false);
            Color fill = Color.green;
            Color[] cols = new Color[tex.width * tex.height];
            for (int i = 0; i < cols.Length; i++) cols[i] = fill;
            tex.SetPixels(cols);
            tex.Apply();

            return tex;
        }
    
        private void GenerateAudioPreview(AudioClip clip)
        {

            previewTexture =  AssetPreview.GetAssetPreview(clip);
            
            if (previewTexture == null)
            {
                Debug.LogError("Failed to generate static preview for the AudioClip.");
            }
        
            if (previewTexture != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("AudioClip Preview:", EditorStyles.boldLabel);
                
                GUILayout.Label(previewTexture, GUILayout.Width(256), GUILayout.Height(128));
                
            }
            
        }
    }
}
