using OpenTK.Graphics.ES30;

namespace SharpFAI.Editor.Platform.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // 创建 EditorView 并设置为当前视图
        GL.LoadBindings(new GLBindingsContext());
        SetContentView(Resource.Layout.activity_main);
    }

    protected override void OnResume()
    {
        base.OnResume();
    }

    protected override void OnPause()
    {
        base.OnPause();
    }
}