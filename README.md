# SharpFAI Editor 多平台架构

这是 SharpFAI 项目的新架构重构。本项目将编辑器和播放器合并为单一可执行文件，同时支持多平台部署。

## 项目结构

```
SharpFAI.Editor/
├── src/
│   ├── SharpFAI.Editor.Core/           # 核心游戏逻辑（编辑器+播放器共享）
│   └── SharpFAI.Editor.Platform/       # 跨平台抽象层和应用程序
│       ├── Abstractions/               # 平台抽象接口和实现
│       ├── Windows/                    # Windows 应用程序
│       ├── Linux/                      # Linux 应用程序
│       ├── macOS/                      # macOS 应用程序
│       └── Android/                    # Android 应用程序 (已实现)
├── tests/                              # 单元测试（待实现）
└── docs/                               # 文档（待实现）
```

## 编译条件

每个平台应用通过 `-DEFINE` 预处理器符号来选择正确的实现：

- **Windows**: `WINDOWS`
- **Linux**: `LINUX`
- **macOS**: `MACOS`
- **Android**: `ANDROID`

## 运行方式

### Windows
```bash
# 编辑器模式（默认）
SharpFAI.Editor.Platform.Windows.exe --mode=editor

# 仅播放器模式
SharpFAI.Editor.Platform.Windows.exe --mode=player

# 编辑器+播放器合并模式
SharpFAI.Editor.Platform.Windows.exe --mode=combined
```

### Linux/macOS
```bash
# 编辑器模式（默认）
./SharpFAI.Editor.Platform.Linux --mode=editor
./SharpFAI.Editor.Platform.macOS --mode=editor

# 仅播放器模式
./SharpFAI.Editor.Platform.Linux --mode=player
./SharpFAI.Editor.Platform.macOS --mode=player

# 编辑器+播放器合并模式
./SharpFAI.Editor.Platform.Linux --mode=combined
./SharpFAI.Editor.Platform.macOS --mode=combined
```

### Android
通过 UI 菜单切换编辑器和播放器模式

### 架构设计

1. **平台抽象层** (`SharpFAI.Editor.Platform.Abstractions`)
   - `IGraphicsContext` - 图形上下文（OpenTK GameWindow / GLSurfaceView）
   - `IAudioProvider` - 音频播放
   - `IInputManager` - 输入处理
   - `IFileProvider` - 文件系统

2. **核心库** (`SharpFAI.Editor.Core`)
   - 游戏逻辑
   - 数据结构
   - 编辑功能
   - `SharpFAIApplication` - 应用程序基类

3. **平台实现**
   - `DesktopGraphicsContext` - 桌面平台图形上下文
   - `DesktopAudioProvider` - 桌面平台音频提供者
   - `AndroidGraphicsContext` - Android 平台图形上下文
   - `AndroidAudioProvider` - Android 平台音频提供者

4. **平台应用** (`SharpFAI.Editor.Platform.{Windows,Linux,macOS,Android}`)
   - 仅包含平台入口点（`Program.cs` 或 `MainActivity.cs`）
   - 创建平台特定的实现并注入到核心应用程序

## 编译

```bash
# 构建 Windows 版本
dotnet build src/SharpFAI.Editor.Platform/Windows/SharpFAI.Editor.Platform.Windows.csproj

# 构建 Linux 版本
dotnet build src/SharpFAI.Editor.Platform/Linux/SharpFAI.Editor.Platform.Linux.csproj

# 构建 macOS 版本
dotnet build src/SharpFAI.Editor.Platform/macOS/SharpFAI.Editor.Platform.macOS.csproj

# 构建 Android 版本
dotnet build src/SharpFAI.Editor.Platform/Android/SharpFAI.Editor.Platform.Android.csproj

# 构建所有平台
dotnet build SharpFAI.Editor.sln
```

## 项目状态

### ✅ 已完成
- 多平台架构实现（Windows、Linux、macOS、Android）
- 平台抽象层（IGraphicsContext、IAudioProvider等）
- 各平台入口点和实现
- 应用程序模式支持（EditorOnly、PlayerOnly、Combined）
- 统一的解决方案文件

### 🔄 进行中
- ImGUI 集成
- 编辑器功能完善
- 播放器功能完善

### 📋 待完成
- 测试框架建立
- 文档编写
- 性能优化
