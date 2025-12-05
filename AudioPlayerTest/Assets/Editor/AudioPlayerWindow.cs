using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.AudioEditor
{
    public class AudioPlayerWindow : EditorWindow
    {
        private Label debugLabel;
        private AudioClip currentClip;
        private Image previewImage;
        private Texture2D waveformTexture;
        private int waveformWidth;
        private int waveformHeight = 128;
        private Color waveformColor = Color.white;
        private float scale = 1.0f;
        private VisualElement waveformImageContainer;
        
        private VisualElement playheadElement;
        private int playheadSample = 0;
        private Color playheadColor = Color.red;
        private int playheadWidth = 2;

        private Button playButton;
        private bool isPlaying;
        
        private StyleSheet borderStylesheet;

        private AudioPlayerSettings settings;

        public void CreateGUI()
        {
            Debug.Log("CreateGUI");
            
            LoadStyleSheets();
            LoadAudioPlayerSettings();
            ApplyAudioPlayerSettings();

            debugLabel = new Label(Selection.activeObject != null ? Selection.activeObject.name : "(none)");
            rootVisualElement.Add(debugLabel);

            AddScaleSlider();
            AddPlayButton();

            AddWaveformImageContainer();

            AddWaveformImage();

            AddPlayhead();

            
        }

  

#region Visual Elements
        
        private void LoadStyleSheets()
        {
            string[] guids = AssetDatabase.FindAssets("t:StyleSheet");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                borderStylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);    
            }
            else
            {
                Debug.Log("no styles found");
            }
        }
        private void AddPlayhead()
        {
            playheadElement = new VisualElement();
            playheadElement.style.position = Position.Absolute;
            playheadElement.style.top = 0;
            playheadElement.style.height = waveformHeight;
            playheadElement.style.width = playheadWidth;
            playheadElement.style.backgroundColor = playheadColor;
            waveformImageContainer.Add(playheadElement);
            
            playheadSample = 0;
        }

        private void AddWaveformImage()
        {
            previewImage = new Image();
            previewImage.style.flexGrow = 1;
            previewImage.style.position = Position.Absolute;
            waveformImageContainer.Add(previewImage);
            
            waveformImageContainer.styleSheets.Add(borderStylesheet);
            
            previewImage.RegisterCallback<PointerDownEvent>(OnWaveformClicked);
        }

        private void AddWaveformImageContainer()
        {
            waveformImageContainer = new VisualElement();
            waveformImageContainer.style.flexGrow = 1;
            waveformImageContainer.style.height = waveformHeight;

            rootVisualElement.Add(waveformImageContainer);
        }

        private void AddPlayButton()
        {
            playButton = new Button(OnPlayButtonClicked) { text = "Play" };
            rootVisualElement.Add(playButton);
        }

        private void AddScaleSlider()
        {
            var scaleSlider = new Slider("Scale", 0.1f, 5.0f);
            scaleSlider.value = scale;
            scaleSlider.RegisterValueChangedCallback(evt => {
                scale = evt.newValue;
                UpdateWaveformTexture();
            });
            rootVisualElement.Add(scaleSlider);
        }
        #endregion

        private void LoadAudioPlayerSettings()
        {
            if (settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:AudioPlayerSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<AudioPlayerSettings>(path);
                }
                else
                {
                    Debug.LogWarning("AudioPlayerSettings asset not found in project.");
                }
            }
        }

        private void ApplyAudioPlayerSettings()
        {
            waveformWidth = Mathf.RoundToInt(settings.waveformWidth * scale);
            waveformHeight = settings.waveformHeight;
            waveformColor = settings.waveformColor;
        }

        private void OnWaveformClicked(PointerDownEvent evt)
        {
            if (currentClip == null || waveformTexture == null) return;
            
            if (evt.button != 0) return;
            
            float localX = evt.localPosition.x;
            // localX = Mathf.Clamp(localX, 0, waveformWidth - 1);
            float normalized = localX / waveformWidth;
            playheadSample = Mathf.RoundToInt(normalized * (currentClip.samples - 1));

            RenderPlayhead();

            Debug.Log("waveform clicked");
            
            if (isPlaying)
            {
                AudioUtilWrapper.StopAllPreviewClips();
                AudioUtilWrapper.PlayPreviewClip(currentClip, playheadSample, false);
            }
        }

        private void OnPlayButtonClicked()
        {
            if (currentClip == null)
                return;

            if (!isPlaying)
            {
                StartPlaying();
            }
            else
            {
                StopPlaying();
            }
        }

        private void StartPlaying()
        {
            isPlaying = true;
            playButton.text = "Stop";
            AudioUtilWrapper.StopAllPreviewClips();
            AudioUtilWrapper.PlayPreviewClip(currentClip, playheadSample, false);
            EditorApplication.update += UpdatePlayheadDuringPlayback;
        }

        private void StopPlaying()
        {
            isPlaying = false;
            playButton.text = "Play";
            AudioUtilWrapper.StopAllPreviewClips();
            EditorApplication.update -= UpdatePlayheadDuringPlayback;
        }

        private void UpdatePlayheadDuringPlayback()
        {
            if (!isPlaying || currentClip == null)
                return;

            playheadSample = AudioUtilWrapper.GetPreviewClipSamplePosition();
            RenderPlayhead();

            // Stop when finished
            if (!AudioUtilWrapper.IsPreviewClipPlaying())
            {
                // isPlaying = false;
                // playButton.text = "Play";
                // EditorApplication.update -= UpdatePlayheadDuringPlayback;
                StopPlaying();
            }
        }

        private void RenderPlayhead()
        {
            // Only show playhead if waveform is present
            if (waveformTexture == null || previewImage == null)
            {
                playheadElement.style.display = DisplayStyle.None;
                return;
            }
            playheadElement.style.display = DisplayStyle.Flex;

            // Compute playhead X position based on sample
            int samplesCount = currentClip != null ? currentClip.samples : 1;
            float normalized = samplesCount > 1 ? (float)playheadSample / (float)samplesCount : 0f;
            int x = Mathf.Clamp(Mathf.RoundToInt(normalized * waveformWidth), 0, waveformWidth - playheadWidth);
            playheadElement.style.left = x;
            playheadElement.style.height = waveformHeight;
            playheadElement.style.width = playheadWidth;
        }

        private void UpdateWaveformTexture()
        {
            // clear if no clip selected
            if (currentClip == null)
            {
                if (previewImage != null)
                    previewImage.image = null;

                if (waveformTexture != null)
                {
                    Object.DestroyImmediate(waveformTexture);
                    waveformTexture = null;
                }

                return;
            }

            int samplesCount = currentClip.samples;
            int channels = currentClip.channels;
            if (samplesCount <= 0 || channels <= 0)
                return;

            // read samples (interleaved channels)
            float[] allSamples = new float[samplesCount * channels];
            bool ok = currentClip.GetData(allSamples, 0);
            if (!ok)
                return;

            int width = waveformWidth;
            int height = waveformHeight;

            int samplesPerPixel = Mathf.Max(1, Mathf.CeilToInt((float)samplesCount / width));

            // --- NORMALIZATION ---
            float maxAbs = 0f;
            for (int i = 0; i < allSamples.Length; i++)
            {
                float abs = Mathf.Abs(allSamples[i]);
                if (abs > maxAbs) maxAbs = abs;
            }
            if (maxAbs < 1e-6f) maxAbs = 1f; // avoid division by zero

            // prepare pixel buffer (clear with transparent background)
            Color clearColor = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clearColor;

            int halfH = height / 2;

            // for each column compute peak amplitude
            for (int x = 0; x < width; x++)
            {
                int startSample = x * samplesPerPixel;
                int endSample = Mathf.Min(samplesCount, startSample + samplesPerPixel);
                float peak = 0f;

                for (int s = startSample; s < endSample; s++)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        float v = Mathf.Abs(allSamples[s * channels + ch]);
                        if (v > peak) peak = v;
                    }
                }

                // normalize peak
                peak /= maxAbs;

                int yTop = Mathf.Clamp(halfH + Mathf.RoundToInt(peak * halfH), 0, height - 1);
                int yBottom = Mathf.Clamp(halfH - Mathf.RoundToInt(peak * halfH), 0, height - 1);

                // draw vertical line between yBottom and yTop (texture origin is bottom-left)
                for (int y = yBottom; y <= yTop; y++)
                {
                    int idx = y * width + x;
                    if (idx >= 0 && idx < pixels.Length)
                        pixels[idx] = waveformColor;
                }
            }

            // replace existing texture
            if (waveformTexture != null)
            {
                Object.DestroyImmediate(waveformTexture);
                waveformTexture = null;
            }

            waveformTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            waveformTexture.SetPixels(pixels);
            waveformTexture.Apply();

            if (previewImage != null)
                previewImage.image = waveformTexture;

            RenderPlayhead();
        }

        public void OnSelectionChange()
        {
            Debug.Log("onSelectionChange");
            
            if (SetCurrentClip()) return;

            StopPlaying();
            playheadSample = 0;
            
            ApplyAudioPlayerSettings();

            UpdateWaveformTexture();
            RenderPlayhead();
        }

        private bool SetCurrentClip()
        {
            if (Selection.activeObject == null)
            {
                debugLabel.text = "(none)";
                // if (previewImage != null)
                previewImage.image = null;
                currentClip = null;
                return true;
            }
            
            debugLabel.text = Selection.activeObject.name;
            currentClip = Selection.activeObject as AudioClip;
            
            if (currentClip == null)
                return true;
            return false;
        }

        [MenuItem("Tools/AudioPlayer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioPlayerWindow>();
            window.titleContent = new GUIContent("Audio Player");
        }
        
    }
}
