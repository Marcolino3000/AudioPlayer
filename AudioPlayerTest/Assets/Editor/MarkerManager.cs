using System;
using System.Collections.Generic;

namespace Editor.AudioEditor
{
    public class MarkerManager
    {
    
        public class Marker
        {
            public int id;
            public int sample;
            public Marker(int id, int sample)
            {
                this.id = id;
                this.sample = sample;
            }
        }

        private int lastPlayheadSample = -1;
        private List<Marker> markers = new List<Marker>();
        private int nextId = 1;

        public event Action<int> OnMarkerReached;

        public Marker AddMarker(int sample)
        {
            var marker = new Marker(nextId++, sample);
            markers.Add(marker);
            return marker;
        }

        public void RemoveMarker(int id)
        {
            markers.RemoveAll(m => m.id == id);
        }

        public List<Marker> GetMarkers()
        {
            return markers;
        }

        public void CheckPlayhead(int playheadSample)
        {
            foreach (var marker in markers)
            {
                // Fire event if playhead crosses marker (forward only)
                if (lastPlayheadSample < marker.sample && playheadSample >= marker.sample)
                {
                    OnMarkerReached?.Invoke(marker.id);
                }
            }
            lastPlayheadSample = playheadSample;
        }

        public void ResetPlayheadCheck()
        {
            lastPlayheadSample = -1;
        }

        public int? GetMarkerAtSample(int sample)
        {
            var marker = markers.Find(m => m.sample == sample);
            return marker?.id;
        }
    }
}
