using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.AudioEditor
{
    public class AudioPlayerWindow : EditorWindow
    {
        private Label label;
        private AudioClip currentClip;
        private Image previewImage;
        private Texture2D previewTexture;
        private Texture2D waveformTexture;
        private int waveformWidth;
        private int waveformHeight = 128;
        private Color waveformColor = Color.white;
        private float scale = 1.0f;

        private VisualElement playheadElement;
        private int playheadSample = 0; // sample index for playhead
        private Color playheadColor = Color.red;
        private int playheadWidth = 2;

        private Button playButton;
        private bool isPlaying = false;

        private AudioPlayerSettings settings;

        public void CreateGUI()
        {
            // Find AudioPlayerSettings asset in project if not assigned
            if (settings == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioPlayerSettings");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioPlayerSettings>(path);
                }
                else
                {
                    Debug.LogWarning("AudioPlayerSettings asset not found in project.");
                }
            }

            label = new Label(Selection.activeObject != null ? Selection.activeObject.name : "(none)");
            rootVisualElement.Add(label);

            // Add scale slider
            var scaleSlider = new Slider("Scale", 0.1f, 5.0f);
            scaleSlider.value = scale;
            scaleSlider.RegisterValueChangedCallback(evt => {
                scale = evt.newValue;
                UpdateWaveformTexture();
            });
            rootVisualElement.Add(scaleSlider);

            playButton = new Button(OnPlayButtonClicked) { text = "Play" };
            rootVisualElement.Add(playButton);

            var imageContainer = new VisualElement();
            imageContainer.style.flexGrow = 1;
            imageContainer.style.height = waveformHeight;
            rootVisualElement.Add(imageContainer);
            
            previewImage = new Image();
            previewImage.style.flexGrow = 1;
            previewImage.style.position = Position.Absolute;
            imageContainer.Add(previewImage);

            // Register mouse down event for playhead movement
            previewImage.RegisterCallback<PointerDownEvent>(OnWaveformClicked);

            // Create playhead element
            playheadElement = new VisualElement();
            playheadElement.style.position = Position.Absolute;
            playheadElement.style.top = 0;
            playheadElement.style.height = waveformHeight;
            playheadElement.style.width = playheadWidth;
            playheadElement.style.backgroundColor = playheadColor;
            imageContainer.Add(playheadElement);

            playheadSample = 0;
        }

        private void OnWaveformClicked(PointerDownEvent evt)
        {
            if (currentClip == null || waveformTexture == null)
                return;
            // Only respond to left mouse button
            if (evt.button != 0)
                return;
            float localX = evt.localPosition.x;
            localX = Mathf.Clamp(localX, 0, waveformWidth - 1);
            float normalized = localX / (float)waveformWidth;
            int sample = Mathf.RoundToInt(normalized * (currentClip.samples - 1));

            // Move playhead to clicked position
            playheadSample = sample;
            RenderPlayhead();

            // If playing, stop current playback and start at new position
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
                isPlaying = true;
                playButton.text = "Stop";
                // Stop any current playback before starting new
                AudioUtilWrapper.StopAllPreviewClips();
                AudioUtilWrapper.PlayPreviewClip(currentClip, playheadSample, false);
                EditorApplication.update += UpdatePlayheadDuringPlayback;
            }
            else
            {
                isPlaying = false;
                playButton.text = "Play";
                AudioUtilWrapper.StopAllPreviewClips();
                EditorApplication.update -= UpdatePlayheadDuringPlayback;
            }
        }

        private void UpdatePlayheadDuringPlayback()
        {
            if (!isPlaying || currentClip == null)
                return;

            // Get current sample position from AudioUtilWrapper
            int samplePos = AudioUtilWrapper.GetPreviewClipSamplePosition();
            playheadSample = samplePos;
            RenderPlayhead();

            // Stop when finished
            if (!AudioUtilWrapper.IsPreviewClipPlaying())
            {
                isPlaying = false;
                playButton.text = "Play";
                EditorApplication.update -= UpdatePlayheadDuringPlayback;
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
            // Use settings for waveform dimensions and color
            waveformWidth = Mathf.RoundToInt(settings.waveformWidth * scale);
            waveformHeight = settings.waveformHeight;
            waveformColor = settings.waveformColor;

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

            // Update playhead after waveform is drawn
            RenderPlayhead();
        }

        public void OnSelectionChange()
        {
            if (Selection.activeObject == null)
            {
                label.text = "(none)";
                if (previewImage != null)
                    previewImage.image = null;
                currentClip = null;
                return;
            }

            label.text = Selection.activeObject.name;
            currentClip = Selection.activeObject as AudioClip;

            if (currentClip == null)
                return;
            
            isPlaying = false;
            playButton.text = "Play";
            AudioUtilWrapper.StopAllPreviewClips();
            EditorApplication.update -= UpdatePlayheadDuringPlayback;
            UpdateWaveformTexture();
            RenderPlayhead();
        }

        private void GetPreviewTexture()
        {
            // Try to get the asset preview; if none, create a fallback green texture
            previewTexture = null;
            if (currentClip != null)
            {
                previewTexture = AssetPreview.GetAssetPreview(currentClip);
                if (previewTexture == null)
                {
                    // Fallback: simple green texture with configured size
                    int w = 256, h = 128;
                    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    Color[] cols = new Color[w * h];
                    for (int i = 0; i < cols.Length; i++) cols[i] = Color.green;
                    tex.SetPixels(cols);
                    tex.Apply();
                    previewTexture = tex;
                }
            }

            previewImage.image = previewTexture;
        }

        [MenuItem("Tools/AudioPlayer")]
        public static void Show()
        {
            var window = GetWindow<AudioPlayerWindow>();
            window.titleContent = new GUIContent("Audio Player");
        }

        private void TryingToUseAudioCurveRendering()
        {
            var frame = new VisualElement();
            frame.style.height = 200;
            frame.style.width = 300;
            var rect = frame.contentRect;


            Color curveColor = new Color(1f, 0.54901963f, 0.0f, 1f);
            AudioCurveRendering.AudioMinMaxCurveAndColorEvaluator eval =
                (AudioCurveRendering.AudioMinMaxCurveAndColorEvaluator)((float x, out Color col, out float minValue,
                    out float maxValue) =>
                {
                    col = curveColor;
                    if (10000 <= 0)
                    {
                        minValue = 0.0f;
                        maxValue = 0.0f;
                    }
                    else
                    {
                        int index1 = ((int)Mathf.Floor(Mathf.Clamp(x * (float)(10000 - 2), 0.0f, (float)(10000 - 2)))) *
                                     2;
                        int index2 = index1 + 1 * 2;
                        minValue = 0;
                        maxValue = 10;
                        if ((double)minValue > (double)maxValue)
                        {
                            float num = minValue;
                            minValue = maxValue;
                            maxValue = num;
                        }
                    }
                });
            AudioCurveRendering.DrawMinMaxFilledCurve(rect, eval);

            rootVisualElement.Add(frame);
        }
    }
}
