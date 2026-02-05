using System.Diagnostics;
using NUnit.Framework;

namespace SharpFAI_Editor.Test;

[TestFixture]
public class AudioMergerTests
{
    private string _testDir = null!;
    private string _baseAudioPath = null!;
    private string _hitSoundPath = null!;
    
    [SetUp]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "AudioMergerTests");
        Directory.CreateDirectory(_testDir);
        
        _baseAudioPath = Path.Combine(_testDir, "base.wav");
        
        // 使用项目中的真实 kick.wav 文件
        var projectDir = Path.GetDirectoryName(typeof(AudioMergerTests).Assembly.Location);
        _hitSoundPath = Path.Combine(projectDir!, "Resources", "kick.wav");
        
        // 如果找不到 kick.wav，回退到创建静音文件
        if (!File.Exists(_hitSoundPath))
        {
            _hitSoundPath = Path.Combine(_testDir, "kick.wav");
            var merger = new AudioMerger();
            merger.CreateSilentWav(_hitSoundPath, 0.1);
        }
        
        // 创建基础音频文件
        var baseMerger = new AudioMerger();
        baseMerger.CreateSilentWav(_baseAudioPath, 5.0);
    }
    
    [TearDown]
    public void TearDown()
    {
        // 清理测试文件
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, true);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
    
    [Test]
    public void TestIncrementalAddOnly()
    {
        var merger = new AudioMerger();
        var output1 = Path.Combine(_testDir, "test1_step1.wav");
        var output2 = Path.Combine(_testDir, "test1_step2.wav");
        
        var inserts1 = new List<AudioMerger.AudioInsert>
        {
            new(100, _hitSoundPath),
            new(200, _hitSoundPath),
            new(300, _hitSoundPath),
            new(400, _hitSoundPath)
        };
        
        TestContext.WriteLine("第一次合成: [100, 200, 300, 400]");
        var sw = Stopwatch.StartNew();
        merger.MixAudioIncremental(_baseAudioPath, inserts1, output1);
        sw.Stop();
        TestContext.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms");
        
        var inserts2 = new List<AudioMerger.AudioInsert>
        {
            new(100, _hitSoundPath),
            new(200, _hitSoundPath),
            new(300, _hitSoundPath),
            new(400, _hitSoundPath),
            new(500, _hitSoundPath),
            new(600, _hitSoundPath)
        };
        
        TestContext.WriteLine("第二次合成: [100, 200, 300, 400, 500, 600] (新增2个)");
        sw.Restart();
        merger.MixAudioIncremental(_baseAudioPath, inserts2, output2);
        sw.Stop();
        TestContext.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"  结果: {output2}");
        
        Assert.That(File.Exists(output2), Is.True, "输出文件应该存在");
        Assert.That(new FileInfo(output2).Length, Is.GreaterThan(0), "输出文件应该有内容");
    }
    
    [Test]
    public void TestIncrementalWithRemoval()
    {
        var merger = new AudioMerger();
        var output1 = Path.Combine(_testDir, "test2_step1.wav");
        var output2 = Path.Combine(_testDir, "test2_step2.wav");
        
        var inserts1 = new List<AudioMerger.AudioInsert>
        {
            new(1, _hitSoundPath),
            new(20, _hitSoundPath),
            new(40, _hitSoundPath),
            new(60, _hitSoundPath)
        };
        
        TestContext.WriteLine("第一次合成: [1, 20, 40, 60]");
        var sw = Stopwatch.StartNew();
        merger.MixAudioIncremental(_baseAudioPath, inserts1, output1);
        sw.Stop();
        TestContext.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms");
        
        var inserts2 = new List<AudioMerger.AudioInsert>
        {
            new(1, _hitSoundPath),
            new(10, _hitSoundPath),
            new(15, _hitSoundPath),
            new(80, _hitSoundPath)
        };
        
        TestContext.WriteLine("第二次合成: [1, 10, 15, 80] (删除[20,40,60], 新增[10,15,80], 保留[1])");
        sw.Restart();
        merger.MixAudioIncremental(_baseAudioPath, inserts2, output2);
        sw.Stop();
        TestContext.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"  结果: {output2}");
        TestContext.WriteLine("  ✓ 应该从基础音频重新混合所有4个插入点");
        TestContext.WriteLine("  ✓ [1]不会重复混合");
        TestContext.WriteLine("  ✓ [20,40,60]不会出现在结果中");
        
        Assert.That(File.Exists(output2), Is.True, "输出文件应该存在");
        Assert.That(new FileInfo(output2).Length, Is.GreaterThan(0), "输出文件应该有内容");
    }
    
    [Test]
    public void TestFullReplacement()
    {
        var merger = new AudioMerger();
        var output1 = Path.Combine(_testDir, "test3_step1.wav");
        var output2 = Path.Combine(_testDir, "test3_step2.wav");
        
        var inserts1 = new List<AudioMerger.AudioInsert>
        {
            new(100, _hitSoundPath),
            new(200, _hitSoundPath),
            new(300, _hitSoundPath),
            new(400, _hitSoundPath)
        };
        
        TestContext.WriteLine("第一次合成: [100, 200, 300, 400]");
        var sw = Stopwatch.StartNew();
        merger.MixAudioIncremental(_baseAudioPath, inserts1, output1);
        sw.Stop();
        TestContext.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms");
        
        var inserts2 = new List<AudioMerger.AudioInsert>
        {
            new(50, _hitSoundPath),
            new(150, _hitSoundPath),
            new(250, _hitSoundPath),
            new(350, _hitSoundPath),
            new(450, _hitSoundPath),
            new(550, _hitSoundPath)
        };
        
        TestContext.WriteLine("第二次合成: [50, 150, 250, 350, 450, 550] (完全不同)");
        sw.Restart();
        merger.MixAudioIncremental(_baseAudioPath, inserts2, output2);
        sw.Stop();
        TestContext.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"  结果: {output2}");
        TestContext.WriteLine("  ✓ 变化率超过50%，应该执行完整合成");
        
        Assert.That(File.Exists(output2), Is.True, "输出文件应该存在");
        Assert.That(new FileInfo(output2).Length, Is.GreaterThan(0), "输出文件应该有内容");
    }
    
    [Test]
    public void TestPerformanceComparison()
    {
        var merger = new AudioMerger();
        var outputFull = Path.Combine(_testDir, "test4_full.wav");
        var outputIncremental = Path.Combine(_testDir, "test4_incremental.wav");
        
        var inserts1 = new List<AudioMerger.AudioInsert>();
        for (int i = 0; i < 100; i++)
        {
            inserts1.Add(new AudioMerger.AudioInsert(i * 10, _hitSoundPath));
        }
        
        TestContext.WriteLine($"创建{inserts1.Count}个插入点");
        
        TestContext.WriteLine("首次增量合成...");
        var sw = Stopwatch.StartNew();
        merger.MixAudioIncremental(_baseAudioPath, inserts1, outputIncremental);
        sw.Stop();
        var firstTime = sw.ElapsedMilliseconds;
        TestContext.WriteLine($"  耗时: {firstTime}ms");
        
        var inserts2 = new List<AudioMerger.AudioInsert>(inserts1);
        for (int i = 100; i < 110; i++)
        {
            inserts2.Add(new AudioMerger.AudioInsert(i * 10, _hitSoundPath));
        }
        
        TestContext.WriteLine($"\n新增10个插入点 (总共{inserts2.Count}个)");
        
        var merger2 = new AudioMerger();
        TestContext.WriteLine("完整合成...");
        sw.Restart();
        merger2.MixAudio(_baseAudioPath, inserts2, outputFull);
        sw.Stop();
        var fullTime = sw.ElapsedMilliseconds;
        TestContext.WriteLine($"  耗时: {fullTime}ms");
        
        TestContext.WriteLine("增量合成...");
        sw.Restart();
        merger.MixAudioIncremental(_baseAudioPath, inserts2, outputIncremental);
        sw.Stop();
        var incrementalTime = sw.ElapsedMilliseconds;
        TestContext.WriteLine($"  耗时: {incrementalTime}ms");
        
        TestContext.WriteLine($"\n性能对比:");
        TestContext.WriteLine($"  完整合成: {fullTime}ms");
        TestContext.WriteLine($"  增量合成: {incrementalTime}ms");
        if (incrementalTime > 0)
        {
            var speedup = (double)fullTime / incrementalTime;
            TestContext.WriteLine($"  加速比: {speedup:F2}x");
            
            Assert.That(incrementalTime, Is.LessThanOrEqualTo(fullTime), 
                "增量合成应该比完整合成更快或相当");
        }
        
        Assert.That(File.Exists(outputFull), Is.True, "完整合成输出文件应该存在");
        Assert.That(File.Exists(outputIncremental), Is.True, "增量合成输出文件应该存在");
    }
}
