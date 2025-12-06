using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Editor.AudioEditor
{
    [CreateAssetMenu(menuName = "AudioEditor/MarkerManager")]
    public class MarkerManager : SerializedScriptableObject
    {
        public int lastPlayheadSample = -1;
        public Dictionary<AudioClip, List<Marker>> clipsToMarkers = new();
        public int nextId = 1;

        public event Action<int> OnMarkerReached;

        public int AddMarker(AudioClip clip, int sample)
        {
            if (!clipsToMarkers.TryGetValue(clip, out var markers))
            {
                markers = new List<Marker>();
                clipsToMarkers[clip] = markers;
            }
            var marker = new Marker(nextId++, sample);
            markers.Add(marker);
            return marker.Id;
        }

        public void RemoveMarker(AudioClip clip, int id)
        {
            if (clipsToMarkers.TryGetValue(clip, out var markers))
            {
                markers.RemoveAll(m => m.Id == id);
            }
        }
        
        public void RemoveMarkerBySample(AudioClip clip, int sample)
        {
            if (clipsToMarkers.TryGetValue(clip, out var markers))
            {
                markers.RemoveAll(m => m.Sample == sample);
            }
        }

        public List<int> GetMarkerPositions(AudioClip clip)
        {
            if (clipsToMarkers.TryGetValue(clip, out var markers))
                return markers.Select(m => m.Sample).ToList();
            return new List<int>();
        }

        public void CheckPlayhead(AudioClip clip, int playheadSample)
        {
            if (clipsToMarkers.TryGetValue(clip, out var markers))
            {
                foreach (var marker in markers)
                {
                    if (lastPlayheadSample < marker.Sample && playheadSample >= marker.Sample)
                    {
                        OnMarkerReached?.Invoke(marker.Id);
                        Debug.Log("marker reached: " + marker.Id);
                    }
                }
            }
            lastPlayheadSample = playheadSample;
        }

        public void ResetPlayheadCheck()
        {
            lastPlayheadSample = -1;
        }

        public bool ExistsMarkerAtSample(AudioClip clip, int sample)
        {
            if (clipsToMarkers.TryGetValue(clip, out var markers))
            {
                var marker = markers.Find(m => m.Sample == sample);
                return marker != null;
            }
            return false;
        }
        
        public class Marker
        {
            public readonly int Id;
            public readonly int Sample;
            public Marker(int id, int sample)
            {
                Id = id;
                Sample = sample;
            }
        }
    }
}
