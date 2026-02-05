using System.Numerics;
using ImGuiNET;

namespace SharpFAI_Player;

/// <summary>
/// 封装常用的ImGui控件，减少重复代码
/// </summary>
public static class ImGuiControls
{
    /// <summary>
    /// 按钮点击事件委托
    /// </summary>
    public delegate void ButtonClickedHandler();

    /// <summary>
    /// 输入值改变事件委托
    /// </summary>
    public delegate void ValueChangedHandler<T>(T newValue);

    /// <summary>
    /// 创建一个带图标的垂直标签
    /// </summary>
    public static bool VerticalTab(string id, string icon, string tooltipText, float x, float y, float width, float height, ButtonClickedHandler? onClick = null)
    {
        ImGui.SetNextWindowPos(new Vector2(x, y));
        ImGui.SetNextWindowSize(new Vector2(width, height));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2, 5));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.1f, 0.95f));

        bool result = false;
        
        if (ImGui.Begin($"##VerticalTab{id}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
        {
            float buttonSize = width - 4;
            
            if (ImGui.Button($"{icon}##{id}", new Vector2(buttonSize, buttonSize)))
            {
                result = true;
                onClick?.Invoke();
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"{tooltipText} (点击)");
            }
        }
        
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
        
        return result;
    }

    /// <summary>
    /// 创建一个输入文本控件
    /// </summary>
    public static bool TextInput(string label, string id, ref string value, uint maxLength = 256, bool readOnly = false, ValueChangedHandler<string>? onValueChanged = null)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        
        ImGui.PushItemWidth(-1);
        
        var flags = ImGuiInputTextFlags.None;
        if (readOnly)
        {
            flags |= ImGuiInputTextFlags.ReadOnly;
        }
        
        string tempValue = value;
        bool changed = ImGui.InputText($"##{id}", ref tempValue, maxLength, flags);
        
        if (changed)
        {
            value = tempValue;
            onValueChanged?.Invoke(value);
        }
        
        ImGui.PopItemWidth();
        
        return changed;
    }

    /// <summary>
    /// 创建一个输入浮点数控件
    /// </summary>
    public static bool FloatInput(string label, string id, ref float value, float step = 0.1f, float stepFast = 1.0f, string format = "%.2f", ValueChangedHandler<float>? onValueChanged = null)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        
        ImGui.PushItemWidth(-1);
        
        float tempValue = value;
        bool changed = ImGui.InputFloat($"##{id}", ref tempValue, step, stepFast, format);
        
        if (changed)
        {
            // 根据需要添加边界限制
            value = tempValue;
            onValueChanged?.Invoke(value);
        }
        
        ImGui.PopItemWidth();
        
        return changed;
    }

    /// <summary>
    /// 创建一个输入整数控件
    /// </summary>
    public static bool IntInput(string label, string id, ref int value, int step = 1, int stepFast = 10, ValueChangedHandler<int>? onValueChanged = null)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        
        ImGui.PushItemWidth(-1);
        
        int tempValue = value;
        bool changed = ImGui.InputInt($"##{id}", ref tempValue, step, stepFast);
        
        if (changed)
        {
            // 根据需要添加边界限制
            value = tempValue;
            onValueChanged?.Invoke(value);
        }
        
        ImGui.PopItemWidth();
        
        return changed;
    }

    /// <summary>
    /// 创建一个按钮
    /// </summary>
    public static bool Button(string text, Vector2? size = null, ButtonClickedHandler? onClick = null)
    {
        bool clicked = size.HasValue ? 
            ImGui.Button(text, size.Value) : 
            ImGui.Button(text);
        
        if (clicked)
        {
            onClick?.Invoke();
        }
        
        return clicked;
    }

    /// <summary>
    /// 创建一个固定大小的按钮
    /// </summary>
    public static bool FixedButton(string text, float width, float height, ButtonClickedHandler? onClick = null)
    {
        return Button(text, new Vector2(width, height), onClick);
    }

    /// <summary>
    /// 创建一个窗口
    /// </summary>
    public static bool BeginWindow(string title, ref bool isOpen, Vector2 pos, Vector2 size, Vector2 minSize, Vector2 maxSize, ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse)
    {
        ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(minSize, maxSize);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.12f, 0.12f, 0.15f, 0.95f));

        bool result = ImGui.Begin(title, ref isOpen, flags);
        
        return result;
    }

    /// <summary>
    /// 结束窗口
    /// </summary>
    public static void EndWindow()
    {
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    /// <summary>
    /// 创建一个带样式的区域
    /// </summary>
    public static void BeginStyleBlock(Vector4 bgColor, Vector2 padding = new Vector2(), Vector2 rounding = new Vector2())
    {
        if (padding != Vector2.Zero)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);
        }
        
        if (rounding != Vector2.Zero)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, rounding.X);
        }
        
        if (bgColor != Vector4.Zero)
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);
        }
    }

    /// <summary>
    /// 结束样式块
    /// </summary>
    public static void EndStyleBlock(int colorCount = 1, int varCount = 0)
    {
        for (int i = 0; i < colorCount; i++)
        {
            ImGui.PopStyleColor();
        }
        
        for (int i = 0; i < varCount; i++)
        {
            ImGui.PopStyleVar();
        }
    }

    /// <summary>
    /// 创建一个进度条
    /// </summary>
    public static void ProgressBar(float fraction, Vector2 sizeArg, string overlay = "")
    {
        ImGui.ProgressBar(fraction, sizeArg, overlay);
    }

    /// <summary>
    /// 创建一个滑块控件
    /// </summary>
    public static bool SliderFloat(string label, string id, ref float value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        return ImGui.SliderFloat($"{label}##{id}", ref value, min, max, format, flags);
    }
    
    /// <summary>
    /// 创建一个滑块控件（整数）
    /// </summary>
    public static bool SliderInt(string label, string id, ref int value, int min, int max, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        return ImGui.SliderInt($"{label}##{id}", ref value, min, max, format, flags);
    }
}