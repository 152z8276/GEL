# 手机测试与构建清单

## 当前已准备

- Android Build Support 的 AndroidPlayer 已存在。
- 项目默认横屏：`Landscape Left`。
- 旧 Input Manager 已启用；触屏代码使用 `Input.touches`。
- 左下虚拟摇杆、右侧手势、右上 TILT/CAL 按钮已接入。
- 当前机器没有找到 Unity Android SDK/NDK；需要先安装。

## 1. 安装 Android SDK/NDK

Unity Hub → Installs → 当前 `2022.3.62f3c1` → Add modules：

- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

安装后在 Unity → Preferences → External Tools 确认 SDK、NDK、JDK 路径。

## 2. 切换平台

Unity 打开项目：

`File → Build Settings → Android → Switch Platform`

## 3. Player Settings

- Default Orientation：`Landscape Left`
- Auto Rotation：关闭
- Package Name：`com.Portfolio.GELPrototype`
- Minimum API：22
- Target API：Automatic
- Scripting Backend：IL2CPP
- Target Architectures：ARM64
- Development Build：第一次测试勾选

## 4. 手机连接

手机打开开发者选项和 USB 调试，连接后：

```powershell
adb devices
```

确认设备出现在列表中。

## 5. 构建

Unity：

`File → Build Settings → Build And Run`

或先 `Build` 出 APK，再复制到手机安装。

## 6. 真机检查项

- 左下摇杆能否转向和前后移动。
- 摇杆手指按住时，右侧手势是否还能同时触发。
- 从球上开始拖动是否进入射门。
- 从球外开始拖动是否进入过人/防守爆发。
- 右上 TILT 和 CALIBRATE 按钮是否可点，是否不会误触发射门。
- 手机横屏左右方向下，倾斜 X/Y 是否需要 `InvertX/InvertY`。
- 手势轨迹是否稳定，球命中圈是否好点。

## 7. 回退点

Git 仓库已初始化，当前提交和标签：

- `checkpoint-00-p0-mobile-input`
- `checkpoint-01-weight-core`
- `checkpoint-02-device-tilt`
- `checkpoint-03-gesture-recognition`
- `checkpoint-04-dribble-animations`

回退示例：

```powershell
git checkout checkpoint-03-gesture-recognition
```

或只回退某个文件：

```powershell
git restore --source checkpoint-03-gesture-recognition Assets/Scripts/Input/GestureInput.cs
```

## 8. 日志

```powershell
adb logcat -s Unity
```

重点看：

- `Exception`
- `NullReferenceException`
- `Touch`/输入相关错误
- 材质/Shader 错误
