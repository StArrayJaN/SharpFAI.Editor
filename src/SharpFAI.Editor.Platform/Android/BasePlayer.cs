using Android.Content;
using Android.Opengl;
using Android.Util;
using Javax.Microedition.Khronos.Opengles;
using OpenTK.Graphics.ES30;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Platform.Android;

/// <summary>
/// BasePlayer - 基础玩家渲染器类
/// 继承自 GLSurfaceView 并实现 IPlayer 接口
/// </summary>
public abstract class BasePlayer : GLSurfaceView, GLSurfaceView.IRenderer, IPlayer
{
    private static readonly string Tag = nameof(BasePlayer);
    
    // 渲染状态
    protected bool isPlaying;
    protected bool isPaused;
    protected bool isCreated;
    
    // 时间相关
    protected double deltaTime;
    protected long lastFrameTime;
    
    // 视图尺寸
    protected int surfaceWidth;
    protected int surfaceHeight;
    
    // 当前楼层（用于 MoveToNextFloor）
    protected object? currentFloor;
    
    public BasePlayer(Context context) : base(context)
    {
        Init();
    }
    
    public BasePlayer(Context context, IAttributeSet attrs) : base(context, attrs)
    {
        Init();
    }
    
    /// <summary>
    /// 初始化 BasePlayer
    /// </summary>
    private void Init()
    {
        // 设置 EGL 配置
        SetEGLContextClientVersion(3);
        // 设置 EGL 配置选择器
        SetEGLConfigChooser(8, 8, 8, 8, 16, 0); // RGB各8位，Alpha 8位，深度16位
        // 设置渲染器
        SetRenderer(this);
        
        // 设置渲染模式（按需渲染）
        RenderMode = Rendermode.Continuously;
        
        // 保持屏幕常亮
        KeepScreenOn = true;
        
        ResetPlayer();
    }
    
    #region GLSurfaceView.IRenderer Implementation
    
    /// <summary>
    /// 当表面创建时调用
    /// </summary>
    public virtual void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
    {
        Log.Info(Tag, "OnSurfaceCreated");
        
        // 设置 OpenGL 状态
        GLEnable(gl);
        
        // 创建玩家资源
        CreatePlayer();
        isCreated = true;
    }
    
    /// <summary>
    /// 当表面尺寸改变时调用
    /// </summary>
    public virtual void OnSurfaceChanged(IGL10 gl, int width, int height)
    {
        Log.Info(Tag, $"OnSurfaceChanged: {width}x{height}");
        
        surfaceWidth = width;
        surfaceHeight = height;
        
        // 设置视口
        GL.Viewport(0, 0, width, height);
        
        // 更新投影矩阵等
        OnViewportChanged(width, height);
    }
    
    /// <summary>
    /// 每帧绘制时调用
    /// </summary>
    public virtual void OnDrawFrame(IGL10 gl)
    {
        // 计算帧时间
        CalculateDeltaTime();
        
        // 更新逻辑
        if (isPlaying && !isPaused)
        {
            UpdatePlayer(deltaTime);
        }
        
        // 渲染画面
        RenderPlayer(deltaTime);
    }
    
    #endregion
    
    #region IPlayer Implementation
    
    /// <summary>
    /// 创建玩家资源
    /// </summary>
    public abstract void CreatePlayer();
    
    /// <summary>
    /// 更新玩家逻辑
    /// </summary>
    public abstract void UpdatePlayer(double delta);
    
    /// <summary>
    /// 渲染玩家画面
    /// </summary>
    public abstract void RenderPlayer(double delta);

    /// <summary>
    /// 移动到下一层
    /// </summary>
    public abstract void MoveToNextFloor(Floor next);
    
    /// <summary>
    /// 开始播放
    /// </summary>
    public virtual void StartPlay()
    {
        Log.Info(Tag, "StartPlay");
        isPlaying = true;
        isPaused = false;
        lastFrameTime = System.Environment.TickCount64;
    }
    
    /// <summary>
    /// 停止播放
    /// </summary>
    public virtual void StopPlay()
    {
        Log.Info(Tag, "StopPlay");
        isPlaying = false;
        isPaused = false;
    }
    
    /// <summary>
    /// 暂停播放
    /// </summary>
    public virtual void PausePlay()
    {
        Log.Info(Tag, "PausePlay");
        isPaused = true;
    }
    
    /// <summary>
    /// 恢复播放
    /// </summary>
    public virtual void ResumePlay()
    {
        Log.Info(Tag, "ResumePlay");
        isPaused = false;
        lastFrameTime = System.Environment.TickCount64;
    }
    
    /// <summary>
    /// 重置玩家
    /// </summary>
    public virtual void ResetPlayer()
    {
        Log.Info(Tag, "ResetPlayer");
        isPlaying = false;
        isPaused = false;
        isCreated = false;
        deltaTime = 0;
        lastFrameTime = 0;
        currentFloor = null;
    }
    
    /// <summary>
    /// 销毁玩家资源
    /// </summary>
    public virtual void DestroyPlayer()
    {
        Log.Info(Tag, "DestroyPlayer");
        ResetPlayer();
        // 子类可以在这里释放 OpenGL 资源
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 计算帧间隔时间
    /// </summary>
    protected void CalculateDeltaTime()
    {
        long currentTime = System.Environment.TickCount64;
        deltaTime = (currentTime - lastFrameTime) / 1000.0; // 转换为秒
        lastFrameTime = currentTime;
    }
    
    /// <summary>
    /// 启用 OpenGL 功能
    /// </summary>
    protected virtual void GLEnable(IGL10 gl)
    {
        // 启用深度测试
        GL.Enable(EnableCap.DepthTest);
        
        // 设置背景颜色
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
    }
    
    /// <summary>
    /// 视口改变时的回调
    /// </summary>
    protected virtual void OnViewportChanged(int width, int height)
    {
        // 子类可以重写此方法来处理投影矩阵等
    }
    
    /// <summary>
    /// 请求重新渲染
    /// </summary>
    protected void RequestRender()
    {
        ((GLSurfaceView)this).RequestRender();
    }
    
    #endregion
    
    /// <summary>
    /// 释放资源
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DestroyPlayer();
        }
        base.Dispose(disposing);
    }
}