using System;

namespace SharpFAI.Editor.Platform.Audio
{
    /// <summary>
    /// 平台特定的音频提供者接口
    /// </summary>
    public interface IAudioProvider : IDisposable
    {
        /// <summary>
        /// 加载音频文件
        /// </summary>
        uint LoadAudio(string filePath);

        /// <summary>
        /// 播放音频
        /// </summary>
        void PlayAudio(uint audioId);

        /// <summary>
        /// 暂停音频
        /// </summary>
        void PauseAudio(uint audioId);

        /// <summary>
        /// 停止音频
        /// </summary>
        void StopAudio(uint audioId);

        /// <summary>
        /// 设置音量 (0.0f - 1.0f)
        /// </summary>
        void SetVolume(uint audioId, float volume);

        /// <summary>
        /// 获取音频播放位置（秒）
        /// </summary>
        float GetPlaybackTime(uint audioId);

        /// <summary>
        /// 设置音频播放位置（秒）
        /// </summary>
        void SetPlaybackTime(uint audioId, float time);

        /// <summary>
        /// 获取音频总长度（秒）
        /// </summary>
        float GetDuration(uint audioId);
    }
}
