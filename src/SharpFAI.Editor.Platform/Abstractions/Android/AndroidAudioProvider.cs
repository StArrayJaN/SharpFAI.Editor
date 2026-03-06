using System;
using System.Collections.Generic;
using SharpFAI.Editor.Platform.Audio;

namespace SharpFAI.Editor.Platform.Android
{
    public class AndroidAudioProvider : IAudioProvider
    {
        private Dictionary<uint, object> _audioSources = new();
        private uint _nextId = 1;

        public uint LoadAudio(string filePath)
        {
            var id = _nextId++;
            _audioSources[id] = null;
            return id;
        }

        public void PlayAudio(uint audioId) { }
        public void PauseAudio(uint audioId) { }
        public void StopAudio(uint audioId) { }
        public void SetVolume(uint audioId, float volume) { }
        public float GetPlaybackTime(uint audioId) => 0.0f;
        public void SetPlaybackTime(uint audioId, float time) { }
        public float GetDuration(uint audioId) => 0.0f;
        public void Dispose() => _audioSources.Clear();
    }
}
