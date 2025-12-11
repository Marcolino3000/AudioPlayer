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
        [SerializeField] private MarkerManager markerManager;

        public void CreateGUI()
        {
            Debug.Log("create gui");
            LoadStyleSheets();
            LoadAudioPlayerSettings();
            ApplyAudioPlayerSettings();
            FindMarkerManager();
            

            debugLabel = new Label(Selection.activeObject != null ? Selection.activeObject.name : "(none)");
            rootVisualElement.Add(debugLabel);

            AddScaleSlider();
            AddPlayButton();

            AddWaveformImageContainer();
            rootVisualElement.Add(waveformImageContainer);
            AddWaveformImage();
            waveformImageContainer.Add(previewImage);
            AddPlayhead();
            RenderClipMarkers();
        }


        public void OnSelectionChange()
        {
            Debug.Log("onSelectionChange");

            if (!SetCurrentClip()) return;

            StopPlaying();
            playheadSample = 0;

            ApplyAudioPlayerSettings();
            //
            AddWaveformImageContainer();
            AddWaveformImage();

            UpdateWaveformTexture();
            AddPlayhead();
            RenderPlayhead();
            RenderClipMarkers();
        }

        private void FindMarkerManager()
        {
            if (markerManager == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:MarkerManager");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    markerManager = AssetDatabase.LoadAssetAtPath<MarkerManager>(path);
                }
                else
                {
                        
                }
                {
                    Debug.LogWarning("MarkerManager asset not found in project.");
                }
            }
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
            if(previewImage != null && rootVisualElement.Contains(previewImage))
                rootVisualElement.Remove(previewImage);
            
            previewImage = new Image();
            previewImage.style.flexGrow = 1;
            previewImage.style.position = Position.Absolute;

            waveformImageContainer.styleSheets.Add(borderStylesheet);
            waveformImageContainer.AddToClassList("waveformImage");
            
            waveformImageContainer.Add(previewImage);

            previewImage.RegisterCallback<PointerDownEvent>(OnWaveformClicked);
            previewImage.RegisterCallback<PointerDownEvent>(OnWaveformRightClicked);
        }

        private void AddWaveformImageContainer()
        {
            if(waveformImageContainer != null && rootVisualElement.Contains(waveformImageContainer))
                rootVisualElement.Remove(waveformImageContainer);
            
            waveformImageContainer = new VisualElement();
            // waveformImageContainer.style.flexGrow = 1;
            waveformImageContainer.style.backgroundColor = settings.waveformBackgroundColor;
            
            waveformImageContainer.style.width = settings.waveformWidth + 5;
            waveformImageContainer.style.height = settings.waveformHeight + 5;
            
            waveformImageContainer.style.position = Position.Relative;
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

        private void OnWaveformRightClicked(PointerDownEvent evt)
        {
            if (evt.button != 1) // Only respond to right mouse button
                return;

            Debug.Log("right-clicked waveform");
            
            float localX = evt.localPosition.x;
            localX = Mathf.Clamp(localX, 0, waveformWidth - 1);
            float normalized = localX / waveformWidth;
            int sample = Mathf.RoundToInt(normalized * (currentClip.samples - 1));
            
            // AddMarker(sample, localX);
            int id = markerManager.AddMarker(currentClip, sample);
            Debug.Log("Marker added.");
            RenderClipMarkers();
            
        }

        private void RenderClipMarkers()
        {
            if (currentClip == null || markerManager == null) return;

            foreach (var pos in markerManager.GetMarkerPositions(currentClip))
            {
                float normalized = (float)pos / currentClip.samples;
                float localX = normalized * waveformWidth;
                AddMarkerVisualElement(pos, localX);
            }
        }

        private void AddMarkerVisualElement(int sample, float localX)
        {
            var marker = new VisualElement();
            marker.style.position = Position.Absolute;
            marker.style.left = localX;
            marker.style.top = 0;
            marker.style.width = 4;
            marker.style.height = waveformHeight;
            marker.style.backgroundColor = Color.gold;

            marker.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    Debug.Log("right-clicked marker");
                    waveformImageContainer.Remove(marker);
                    markerManager.RemoveMarkerBySample(currentClip, sample);
                    Debug.Log("Marker removed.");
                }
            });

            waveformImageContainer.Add(marker);
            // markerManager.AddMarker(currentClip, sample);
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
            markerManager.CheckPlayhead(currentClip, playheadSample);
            
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
            // playheadElement.style.height = waveformHeight;
            // playheadElement.style.width = playheadWidth;
        }

        private void UpdateWaveformTexture()
        {
            if (currentClip == null)
            {
                return;
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

            float[] allSamples = new float[samplesCount * channels];
            bool hasRetrievedData = currentClip.GetData(allSamples, 0);
            if (!hasRetrievedData) return;

            int samplesPerPixel = Mathf.Max(1, Mathf.CeilToInt((float)samplesCount / waveformWidth));
            float clipMaxPeak = 0f;
            for (int i = 0; i < allSamples.Length; i++)
            {
                float abs = Mathf.Abs(allSamples[i]);
                if (abs > clipMaxPeak) 
                    clipMaxPeak = abs;
            }
            if (clipMaxPeak < 1e-6f) clipMaxPeak = 1f; // avoid division by zero

            Color clearColor = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[waveformWidth * waveformHeight];
            for (int i = 0; i < pixels.Length; i++) 
                pixels[i] = clearColor;

            int halfHeight = waveformHeight / 2;
            for (int x = 0; x < waveformWidth; x++)
            {
                int startSample = x * samplesPerPixel;
                int endSample = Mathf.Min(samplesCount, startSample + samplesPerPixel);
                float sum = 0f;
                for (int s = startSample; s < endSample; s++)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        float v = Mathf.Abs(allSamples[s * channels + ch]);
                        sum += v;
                    }
                }
                float average = sum / samplesPerPixel;
                average  /= clipMaxPeak * (1 / scale);
                int yTop = Mathf.Clamp(halfHeight + Mathf.RoundToInt(average * halfHeight), 0, waveformHeight - 1);
                int yBottom = Mathf.Clamp(halfHeight - Mathf.RoundToInt(average * halfHeight), 0, waveformHeight - 1);
                // draw vertical line between yBottom and yTop (texture origin is bottom-left)
                for (int y = yBottom; y <= yTop; y++)
                {
                    int idx = y * waveformWidth + x;
                    if (idx >= 0 && idx < pixels.Length)
                        pixels[idx] = settings.waveformColor;
                }
            }

            waveformTexture = new Texture2D(waveformWidth, waveformHeight, TextureFormat.RGBA32, false);
            waveformTexture.SetPixels(pixels);
            waveformTexture.Apply();

            if (previewImage != null)
                previewImage.image = waveformTexture;

            // RenderPlayhead();
        }

        private bool SetCurrentClip()
        {
            if (Selection.activeObject == null)
            {
                debugLabel.text = "(none)";
                // if (previewImage != null)
                // previewImage.image = null;
                currentClip = null;
                return false;
            }
            
            debugLabel.text = Selection.activeObject.name;
            currentClip = Selection.activeObject as AudioClip;
            
            if (currentClip == null)
                return false;
            return true;
        }

        [MenuItem("Tools/AudioPlayer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioPlayerWindow>();
            window.titleContent = new GUIContent("Audio Player");
        }
        
    }
}
