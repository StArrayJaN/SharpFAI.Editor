// Pure C# WAV audio merger without platform dependencies
// 纯 C# WAV 音频合成器，无平台依赖

using System.Runtime.CompilerServices;

public class AudioMerger
{
    // 增量合成状态
    private byte[]? _cachedMixedAudio;
    private WaveFormat? _cachedFormat;
    private int _lastProcessedInsertCount;
    private List<AudioInsert>? _lastInserts;

    public class WaveFormat
    {
        public int SampleRate { get; set; } = 44100;
        public short BitsPerSample { get; set; } = 16;
        public short Channels { get; set; } = 2;
        public int BlockAlign => Channels * (BitsPerSample / 8);
        public int ByteRate => SampleRate * BlockAlign;
    }

    private class AudioData
    {
        public WaveFormat Format { get; set; }
        public byte[] Data { get; set; }
    }

    public class AudioInsert
    {
        public double Timestamp { get; set; }
        public string FilePath { get; set; }
        public int Position { get; set; }

        public AudioInsert(double timestamp, string filePath)
        {
            Timestamp = timestamp;
            FilePath = filePath;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MixAudioWithProgressMapping(string baseFile, List<AudioInsert> inserts, string outputPath, int minProgress, int maxProgress)
    {
        int num = inserts.Count + 4;
        AudioData audioData = ReadWav(baseFile);
        WaveFormat format = audioData.Format;
        int sampleRate = format.SampleRate;
        int num2 = format.BitsPerSample / 8;
        int channels = format.Channels;
        int num3 = num2 * channels;
        foreach (AudioInsert insert in inserts)
        {
            insert.Position = (int)(insert.Timestamp * sampleRate / 1000.0) * num3;
            insert.Position = Math.Min(insert.Position, audioData.Data.Length);
        }
        inserts.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        byte[] array = new byte[audioData.Data.Length];
        Array.Copy(audioData.Data, array, audioData.Data.Length);
        Dictionary<string, AudioData> dictionary = new Dictionary<string, AudioData>();
        List<string> list = (from i in inserts
            where !string.IsNullOrEmpty(i.FilePath)
            select i.FilePath).Distinct().ToList();
        int num4 = list.Count;
        int num5 = 0;
        for (int num6 = 0; num6 < list.Count; num6++)
        {
            string text = list[num6];
            if (!dictionary.ContainsKey(text))
            {
                dictionary[text] = ReadWav(text);
                num5++;
            }
        }
        for (int num8 = 0; num8 < inserts.Count; num8++)
        {
            AudioInsert audioInsert = inserts[num8];
            AudioData audioData2 = dictionary[audioInsert.FilePath];
            int position = audioInsert.Position;
            int num9 = format.BitsPerSample / 8;
            int num10 = num9 * format.Channels;
            position = position / num10 * num10;
            int num11 = Math.Min(audioData2.Data.Length, audioData.Data.Length - position);
            if (num11 > 0)
            {
                MixSamples(array, audioData2.Data, position, format);
            }
        }
        WriteWav(outputPath, array, format);
    }

    public void MixAudio(string baseFile, List<AudioInsert> inserts, string outputPath)
    {
        int num = inserts.Count + 4;
        AudioData baseAudio = ReadWav(baseFile);
        MixAudioCommon(baseAudio, inserts, outputPath, num);
    }

    public void MixAudio(MemoryStream baseMemoryStream, List<AudioInsert> inserts, string outputPath)
    {
        int num = inserts.Count + 4;
        AudioData baseAudio = ReadWav(baseMemoryStream);
        MixAudioCommon(baseAudio, inserts, outputPath, num);
    }
    
    /// <summary>
    /// 增量合成音频 - 智能检测变化并只处理差异
    /// Incremental audio mixing - intelligently detect changes and only process differences
    /// </summary>
    public void MixAudioIncremental(string baseFile, List<AudioInsert> inserts, string outputPath)
    {
        AudioData baseAudio = ReadWav(baseFile);
        MixAudioIncrementalCommon(baseAudio, inserts, outputPath);
    }
    
    /// <summary>
    /// 增量合成音频（使用内存流）- 智能检测变化并只处理差异
    /// Incremental audio mixing (from memory stream) - intelligently detect changes and only process differences
    /// </summary>
    public void MixAudioIncremental(MemoryStream baseMemoryStream, List<AudioInsert> inserts, string outputPath)
    {
        AudioData baseAudio = ReadWav(baseMemoryStream);
        MixAudioIncrementalCommon(baseAudio, inserts, outputPath);
    }
    
    /// <summary>
    /// 重置增量合成缓存
    /// Reset incremental mixing cache
    /// </summary>
    public void ResetIncrementalCache()
    {
        _cachedMixedAudio = null;
        _cachedFormat = null;
        _lastProcessedInsertCount = 0;
        _lastInserts = null;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MixAudioIncrementalCommon(AudioData baseAudio, List<AudioInsert> inserts, string outputPath)
    {
        WaveFormat format = baseAudio.Format;
        int sampleRate = format.SampleRate;
        int bytesPerSample = format.BitsPerSample / 8;
        int channels = format.Channels;
        int blockAlign = bytesPerSample * channels;
        
        // 计算所有插入点的位置
        foreach (AudioInsert insert in inserts)
        {
            insert.Position = (int)(insert.Timestamp * sampleRate / 1000.0) * blockAlign;
            insert.Position = Math.Min(insert.Position, baseAudio.Data.Length);
        }
        inserts.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        
        byte[] mixedAudio;
        
        // 检查是否可以使用增量合成
        bool canUseIncremental = _cachedMixedAudio != null && 
                                  _cachedFormat != null &&
                                  _lastInserts != null &&
                                  _cachedMixedAudio.Length == baseAudio.Data.Length &&
                                  _cachedFormat.SampleRate == format.SampleRate &&
                                  _cachedFormat.BitsPerSample == format.BitsPerSample &&
                                  _cachedFormat.Channels == format.Channels;
        
        if (canUseIncremental)
        {
            // 检测插入点的变化
            var insertChanges = DetectInsertChanges(_lastInserts!, inserts);
            
            // 如果变化太大（超过50%），直接完整合成
            if (insertChanges.RemovedCount + insertChanges.AddedCount > inserts.Count * 0.5)
            {
                // 变化太大，执行完整合成
                mixedAudio = new byte[baseAudio.Data.Length];
                Array.Copy(baseAudio.Data, mixedAudio, baseAudio.Data.Length);
                
                Dictionary<string, AudioData> audioCache = LoadAudioFiles(inserts);
                MixAllInserts(mixedAudio, inserts, audioCache, format, blockAlign, baseAudio.Data.Length);
            }
            else if (insertChanges.RemovedCount > 0)
            {
                // 有删除的插入点，需要从基础音频重新开始
                mixedAudio = new byte[baseAudio.Data.Length];
                Array.Copy(baseAudio.Data, mixedAudio, baseAudio.Data.Length);
                
                // 加载所有需要的音频文件
                Dictionary<string, AudioData> audioCache = LoadAudioFiles(inserts);
                
                // 混合所有插入点
                MixAllInserts(mixedAudio, inserts, audioCache, format, blockAlign, baseAudio.Data.Length);
            }
            else if (insertChanges.AddedCount > 0)
            {
                // 只有新增，可以在缓存基础上增量混合
                mixedAudio = new byte[_cachedMixedAudio.Length];
                Array.Copy(_cachedMixedAudio, mixedAudio, _cachedMixedAudio.Length);
                
                // 只加载新增的音频文件
                Dictionary<string, AudioData> audioCache = LoadAudioFiles(insertChanges.AddedInserts);
                
                // 只混合新增的插入点
                MixAllInserts(mixedAudio, insertChanges.AddedInserts, audioCache, format, blockAlign, baseAudio.Data.Length);
            }
            else
            {
                // 没有变化，直接使用缓存
                mixedAudio = _cachedMixedAudio;
            }
        }
        else
        {
            // 首次合成或格式变化，执行完整合成
            mixedAudio = new byte[baseAudio.Data.Length];
            Array.Copy(baseAudio.Data, mixedAudio, baseAudio.Data.Length);
            
            // 加载所有音频文件
            Dictionary<string, AudioData> audioCache = LoadAudioFiles(inserts);
            
            // 混合所有插入点
            MixAllInserts(mixedAudio, inserts, audioCache, format, blockAlign, baseAudio.Data.Length);
        }
        
        // 更新缓存
        _cachedMixedAudio = mixedAudio;
        _cachedFormat = format;
        _lastProcessedInsertCount = inserts.Count;
        _lastInserts = new List<AudioInsert>(inserts);
        
        // 写入输出文件
        WriteWav(outputPath, mixedAudio, format);
    }
    
    /// <summary>
    /// 检测插入点的变化
    /// </summary>
    private class InsertChanges
    {
        public List<AudioInsert> AddedInserts { get; set; } = new();
        public List<AudioInsert> RemovedInserts { get; set; } = new();
        public int AddedCount => AddedInserts.Count;
        public int RemovedCount => RemovedInserts.Count;
    }
    
    private InsertChanges DetectInsertChanges(List<AudioInsert> oldInserts, List<AudioInsert> newInserts)
    {
        var changes = new InsertChanges();
        
        // 创建旧插入点的哈希集合（使用时间戳和文件路径作为键）
        var oldSet = new HashSet<string>(
            oldInserts.Select(i => $"{i.Timestamp}:{i.FilePath}")
        );
        
        // 创建新插入点的哈希集合
        var newSet = new HashSet<string>(
            newInserts.Select(i => $"{i.Timestamp}:{i.FilePath}")
        );
        
        // 找出新增的插入点
        foreach (var insert in newInserts)
        {
            string key = $"{insert.Timestamp}:{insert.FilePath}";
            if (!oldSet.Contains(key))
            {
                changes.AddedInserts.Add(insert);
            }
        }
        
        // 找出删除的插入点
        foreach (var insert in oldInserts)
        {
            string key = $"{insert.Timestamp}:{insert.FilePath}";
            if (!newSet.Contains(key))
            {
                changes.RemovedInserts.Add(insert);
            }
        }
        
        return changes;
    }
    
    /// <summary>
    /// 加载音频文件到缓存
    /// </summary>
    private Dictionary<string, AudioData> LoadAudioFiles(List<AudioInsert> inserts)
    {
        var audioCache = new Dictionary<string, AudioData>();
        var uniqueFiles = inserts
            .Where(i => !string.IsNullOrEmpty(i.FilePath))
            .Select(i => i.FilePath)
            .Distinct()
            .ToList();
        
        foreach (string filePath in uniqueFiles)
        {
            if (!audioCache.ContainsKey(filePath))
            {
                audioCache[filePath] = ReadWav(filePath);
            }
        }
        
        return audioCache;
    }
    
    /// <summary>
    /// 混合所有插入点
    /// </summary>
    private void MixAllInserts(byte[] mixedAudio, List<AudioInsert> inserts, 
        Dictionary<string, AudioData> audioCache, WaveFormat format, int blockAlign, int maxLength)
    {
        foreach (AudioInsert insert in inserts)
        {
            if (!audioCache.ContainsKey(insert.FilePath))
                continue;
                
            AudioData insertAudio = audioCache[insert.FilePath];
            int position = insert.Position;
            position = position / blockAlign * blockAlign;
            int mixLength = Math.Min(insertAudio.Data.Length, maxLength - position);
            
            if (mixLength > 0)
            {
                MixSamples(mixedAudio, insertAudio.Data, position, format);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MixAudioCommon(AudioData baseAudio, List<AudioInsert> inserts, string outputPath, int totalSteps)
    {
        WaveFormat format = baseAudio.Format;
        int sampleRate = format.SampleRate;
        int num = format.BitsPerSample / 8;
        int channels = format.Channels;
        int num2 = num * channels;
        foreach (AudioInsert insert in inserts)
        {
            insert.Position = (int)(insert.Timestamp * sampleRate / 1000.0) * num2;
            insert.Position = Math.Min(insert.Position, baseAudio.Data.Length);
        }
        inserts.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        byte[] array = new byte[baseAudio.Data.Length];
        Array.Copy(baseAudio.Data, array, baseAudio.Data.Length);
        Dictionary<string, AudioData> dictionary = new Dictionary<string, AudioData>();
        List<string> list = (from i in inserts
            where !string.IsNullOrEmpty(i.FilePath)
            select i.FilePath).Distinct().ToList();
        int num3 = list.Count;
        int num4 = 0;
        for (int num5 = 0; num5 < list.Count; num5++)
        {
            string text = list[num5];
            if (!dictionary.ContainsKey(text))
            {
                dictionary[text] = ReadWav(text);
                num4++;
            }
        }
        for (int num7 = 0; num7 < inserts.Count; num7++)
        {
            AudioInsert audioInsert = inserts[num7];
            AudioData audioData = dictionary[audioInsert.FilePath];
            int position = audioInsert.Position;
            int num8 = format.BitsPerSample / 8;
            int num9 = num8 * format.Channels;
            position = position / num9 * num9;
            int num10 = Math.Min(audioData.Data.Length, baseAudio.Data.Length - position);
            if (num10 > 0)
            {
                MixSamples(array, audioData.Data, position, format);
            }
        }
        WriteWav(outputPath, array, format);
    }

    public void CreateSilentWav(string filePath, double duration, WaveFormat? format = null)
    {
        format ??= new WaveFormat { SampleRate = 44100, BitsPerSample = 16, Channels = 2 };
		
        int numSamples = (int)(duration * format.SampleRate);
        if (numSamples <= 0)
        {
            throw new ArgumentException("Duration must be greater than 0");
        }
        int dataSize = numSamples * format.BlockAlign;
        byte[] data = new byte[dataSize];
        WriteWav(filePath, data, format);
    }

    public void Export(string hitSoundPath, List<double> hitSoundTimes, string outputPath)
    {
        string text = Path.GetTempFileName() + ".wav";
        try
        {
            double first = hitSoundTimes[0];
            List<double> list = hitSoundTimes.Select(t => t - first).ToList();
            double duration = list.Last() / 1000.0 + 1.0;
            CreateSilentWav(text, duration);
            List<AudioInsert> list2 = new List<AudioInsert>();
            foreach (double item in list)
            {
                list2.Add(new AudioInsert(item, hitSoundPath));
            }
            MixAudioWithProgressMapping(text, list2, outputPath, 40, 100);
        }
        finally
        {
            if (File.Exists(text))
            {
                File.Delete(text);
            }
        }
    }

    // Pure C# WAV file reader - no external dependencies
    // 纯 C# WAV 文件读取器 - 无外部依赖
    private AudioData ReadWav(string filePath)
    {
        using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return ReadWavFromStream(fs);
    }

    private AudioData ReadWav(MemoryStream memoryStream)
    {
        memoryStream.Position = 0;
        return ReadWavFromStream(memoryStream);
    }

    private AudioData ReadWavFromStream(Stream stream)
    {
        using BinaryReader reader = new BinaryReader(stream);
		
        // Read RIFF header
        string riff = new string(reader.ReadChars(4));
        if (riff != "RIFF")
            throw new FormatException("Not a valid WAV file (missing RIFF header)");
		
        int fileSize = reader.ReadInt32();
        string wave = new string(reader.ReadChars(4));
        if (wave != "WAVE")
            throw new FormatException("Not a valid WAV file (missing WAVE header)");
		
        // Read fmt chunk
        string fmt = new string(reader.ReadChars(4));
        if (fmt != "fmt ")
            throw new FormatException("Not a valid WAV file (missing fmt chunk)");
		
        int fmtSize = reader.ReadInt32();
        short audioFormat = reader.ReadInt16(); // 1 = PCM
        short channels = reader.ReadInt16();
        int sampleRate = reader.ReadInt32();
        int byteRate = reader.ReadInt32();
        short blockAlign = reader.ReadInt16();
        short bitsPerSample = reader.ReadInt16();
		
        // Skip any extra fmt bytes
        if (fmtSize > 16)
        {
            reader.ReadBytes(fmtSize - 16);
        }
		
        // Find data chunk
        string chunkId = new string(reader.ReadChars(4));
        int chunkSize = reader.ReadInt32();
		
        // Skip non-data chunks
        while (chunkId != "data" && stream.Position < stream.Length)
        {
            reader.ReadBytes(chunkSize);
            if (stream.Position >= stream.Length - 8)
                throw new FormatException("No data chunk found in WAV file");
            chunkId = new string(reader.ReadChars(4));
            chunkSize = reader.ReadInt32();
        }
		
        if (chunkId != "data")
            throw new FormatException("No data chunk found in WAV file");
		
        // Read audio data
        byte[] data = reader.ReadBytes(chunkSize);
		
        WaveFormat format = new WaveFormat
        {
            SampleRate = sampleRate,
            BitsPerSample = bitsPerSample,
            Channels = channels
        };
		
        // Convert to standard format if needed (44.1kHz, 16-bit, stereo)
        if (sampleRate != 44100 || bitsPerSample != 16 || channels != 2)
        {
            return ConvertToStandardFormat(data, format);
        }
		
        return new AudioData
        {
            Format = format,
            Data = data
        };
    }

    // Convert audio to standard format (44.1kHz, 16-bit, stereo)
    // 将音频转换为标准格式（44.1kHz, 16位, 立体声）
    private AudioData ConvertToStandardFormat(byte[] sourceData, WaveFormat sourceFormat)
    {
        WaveFormat targetFormat = new WaveFormat
        {
            SampleRate = 44100,
            BitsPerSample = 16,
            Channels = 2
        };
		
        // Simple conversion for common cases
        List<byte> convertedData = new List<byte>();
		
        int sourceBytesPerSample = sourceFormat.BitsPerSample / 8;
        int sourceBlockAlign = sourceFormat.Channels * sourceBytesPerSample;
		
        double sampleRateRatio = (double)sourceFormat.SampleRate / targetFormat.SampleRate;
        int sourceSamples = sourceData.Length / sourceBlockAlign;
        int targetSamples = (int)(sourceSamples / sampleRateRatio);
		
        for (int i = 0; i < targetSamples; i++)
        {
            int sourceIndex = (int)(i * sampleRateRatio) * sourceBlockAlign;
            if (sourceIndex >= sourceData.Length - sourceBlockAlign + 1)
                break;
			
            // Convert to 16-bit stereo
            short leftSample = 0;
            short rightSample = 0;
			
            if (sourceFormat.BitsPerSample == 16)
            {
                leftSample = (short)(sourceData[sourceIndex] | (sourceData[sourceIndex + 1] << 8));
                if (sourceFormat.Channels == 2)
                {
                    rightSample = (short)(sourceData[sourceIndex + 2] | (sourceData[sourceIndex + 3] << 8));
                }
                else
                {
                    rightSample = leftSample; // Mono to stereo
                }
            }
            else if (sourceFormat.BitsPerSample == 8)
            {
                // 8-bit to 16-bit conversion
                leftSample = (short)((sourceData[sourceIndex] - 128) * 256);
                if (sourceFormat.Channels == 2)
                {
                    rightSample = (short)((sourceData[sourceIndex + 1] - 128) * 256);
                }
                else
                {
                    rightSample = leftSample;
                }
            }
			
            // Write stereo 16-bit samples
            convertedData.Add((byte)(leftSample & 0xFF));
            convertedData.Add((byte)((leftSample >> 8) & 0xFF));
            convertedData.Add((byte)(rightSample & 0xFF));
            convertedData.Add((byte)((rightSample >> 8) & 0xFF));
        }
		
        return new AudioData
        {
            Format = targetFormat,
            Data = convertedData.ToArray()
        };
    }

    // Pure C# WAV file writer - no external dependencies
    // 纯 C# WAV 文件写入器 - 无外部依赖
    private void WriteWav(string filePath, byte[] data, WaveFormat format)
    {
        using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new BinaryWriter(fs);
		
        // RIFF header
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + data.Length); // File size - 8
        writer.Write("WAVE".ToCharArray());
		
        // fmt chunk
        writer.Write("fmt ".ToCharArray());
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // Audio format (1 = PCM)
        writer.Write(format.Channels);
        writer.Write(format.SampleRate);
        writer.Write(format.ByteRate);
        writer.Write((short)format.BlockAlign);
        writer.Write(format.BitsPerSample);
		
        // data chunk
        writer.Write("data".ToCharArray());
        writer.Write(data.Length);
        writer.Write(data);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MixSamples(byte[] buffer, byte[] insert, int startPos, WaveFormat format)
    {
        if (format.BitsPerSample == 16)
        {
            for (int i = 0; i < insert.Length - 1; i += 2)
            {
                int num2 = startPos + i;
                if (num2 + 1 >= buffer.Length)
                {
                    break;
                }
                short num3 = (short)(buffer[num2] | (buffer[num2 + 1] << 8));
                short num4 = (short)(insert[i] | (insert[i + 1] << 8));
                int val = num3 + num4;
                val = Math.Max(-32768, Math.Min(32767, val));
                buffer[num2] = (byte)(val & 0xFF);
                buffer[num2 + 1] = (byte)((val >> 8) & 0xFF);
            }
            return;
        }
		
        // 8-bit mixing
        for (int j = 0; j < insert.Length; j++)
        {
            int num5 = startPos + j;
            if (num5 >= buffer.Length)
            {
                break;
            }
            int val2 = buffer[num5] + insert[j];
            buffer[num5] = (byte)Math.Max(0, Math.Min(255, val2));
        }
    }
}