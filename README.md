# GEL — P0 灰盒原型

《绿茵·星能联盟》的 1v1 足球手感原型。当前目标是验证“越肩视角 + 鼠标手势射门 + 子弹时间”是否好玩，不追求美术。

## 打开方式

- 编辑器：Unity **2022.3.62f3c1**
- 项目路径：`C:\Users\pc\Desktop\作品集\GEL`
- 直接运行 `Launch Editor.bat`，或在 Unity Hub 里 Add project from disk 选这个目录。

首次打开会自动导入并生成：

- `Assets/Scenes/P0_Prototype.unity`

如果没有自动生成，点菜单 **Tools → GEL → Build P0 Prototype Scene**。

## 操作

| 输入 | 行为 |
|---|---|
| `W A S D` / 方向键 | `W` 前进；`A/D` 缓慢转满 270° 并前进；`S` 根据敌方是否在正面 180° 扇面内选择后撤或转身 180° |
| 手机左下虚拟摇杆 | 左右推杆转向，上下推杆前进/后退 |
| 手机屏幕右侧单指拖动 | 从球上开始为射门，从球外开始为过人/防守爆发；左下摇杆区域不会误触发手势 |
| 鼠标左键拖动（球命中圈内） | 手势射门：手势先拟合成平滑二次曲线，再按方向/弧度出球 |
| 鼠标左键拖动（球命中圈外） | 过人/拨球：屏幕手势方向直接对应短促快速移动方向 |
| `Q` / 鼠标右键 | 抢断（双方都能抢，成功率见下） |
| `F` | 固定力度测试射门（不用画手势） |
| `T` | AI 自动行为开关（调试用） |
| 按住 `Space` | 手动子弹时间；只有在球有归属时才生效，靠近 AI（3m）或从球上开始画射门手势时自动触发 |
| `R` | 重开一局 |

规则：先进 3 球或 120 秒内比分领先者获胜。对手会站位、上抢、铲球，拿球后会反击和射门；按 `T` 可让 AI 停住，方便单独练射门/带球。

持球时不会被贴身自动抢走——要夺走球权必须**抢断**；主动射门/传球会把球释放成松散球。球场是密闭体，没有出界规则：球撞到边线/底线内壳、球门后墙或穹顶顶棚都会回弹场内。

## 移动端输入

- `MobileControls` 生成左下虚拟摇杆，输出到 `PlayerMotor.VirtualInput`；摇杆左右对应 A/D 转向，上下对应 W/S。
- `GestureInput` 已支持 `Input.touches`：第一根非摇杆区域的手指会驱动完整手势，摇杆手指和手势手指可以同时存在。
- 触屏起点使用 `Screen.height` 缩放命中圈和最小滑动距离，避免只适配 1440p 桌面分辨率。
- 桌面鼠标和键盘输入保留；移动端不依赖鼠标。
- `MobileControls.ShowInEditor` 可临时在编辑器显示摇杆，方便检查布局。
- 手机右上角有 `TILT` 开关和 `CALIBRATE` 按钮；桌面调试可用 `Y` 切换、`U` 重新校准。

## 设备倾斜（可选）

- `TiltInput` 默认关闭，开启后把设备相对中性姿态的倾斜写入 `WeightSystem.Tilt`，只辅助重心，不改变动作类型和最终拨球方向。
- 使用加速度计、低通滤波、2.5° 死区和 22° 最大有效倾角；进入时会进行约 1s 自动校准。
- 射门瞄准时不会施加倾斜；`InvertX/InvertY` 用于适配不同横屏方向。

## 当前范围

- 球员模型改用 **`free module` 里的 Mixamo ybot/xbot**，Humanoid 骨骼；物理仍是胶囊碰撞体。
- 玩家用 `ybot`，AI 用 `xbot`；模型在 `Assets/Models/FreeModule/Characters`，动作在 `Assets/Models/FreeModule/Actions`。
- 比赛场地回到 **42 × 26 m**，球门线 `x = ±18`、边线 `z = ±12`；球半径约 **0.11 m**（直径 0.22m，接近真实足球）。
- 外围不再有可见围墙，整座球场由**半胶囊穹顶**直接覆盖；可活动范围约 **44 × 30 m**，穹顶基座落在地面 `x = ±23`、`z = ±16`，最高约 12m。
- 穹顶主体是半透明蓝色，**不发光**；外层叠加一层深色电路纹路，纹路做呼吸式明暗变化（`StadiumGlow`）。
- 四角保留三段式科技柱：下部三角形棱台基座、中部悬浮发光能量核心、上部悬浮六棱柱。
- 没有粗灯带，也不再使用粗网格/铁丝网纹路。
- 球场地面使用程序生成的草坪纹理和割草条纹，包含中圈、中点、禁行区、球门区、点球点、角球弧等标准画线。
- 整座球场是密闭体：外围只有不可见边界碰撞体 `x = ±22`、`z = ±15`、高 12m，顶部有不可见顶棚碰撞体；球门前还有球网/后墙，球不会飞出场景。
- **边线和球门线本身也有碰撞体**：触线 `z = ±12` 全段回弹；底线 `x = ±18` 在球门区 `z = ±3` 之外回弹，球门口保持开放让球进门。球不会再越过边线进入缓冲区后才撞墙。
- 碰撞参数按接近真实足球调整：边界 `bounciness = 0.66`、`friction = 0.12`；草皮 `bounciness = 0.35`、`friction = 0.40`；球自身 `bounciness = 0.35`、`drag = 0.025`、`angularDrag = 0.18`。反弹会明显衰减，地面滚动也会自然减速。
- 穹顶视觉是**半胶囊穹顶**：沿 X 轴的半圆柱 + 两端半椭圆封头，从地面直接起拱，覆盖整个球场；物理边界仍用顶部顶棚近似，避免球穿出。
- Built-in Render Pipeline；URP/Toon 留到 P1。
- AI 是单层状态逻辑：追球、站位、铲球、射门；按 `T` 关闭自动行为，AI 会原地停住。
- 镜头按思维导图重建：方向始终跟随玩家身体朝向；A/D 转身并前进，S 根据敌方位置选择后撤或 180° 转身。
- 子弹时间镜头降到足球中心高度并上仰，同时保留球、球门、AI 和双方球员的脚。
- 射门轨迹是二次贝塞尔曲线；曲率上限 `1.0/m`（与 `y=x²/2` 顶点曲率同量级），横向偏移上限 8m。
- 初速统一 22 m/s，速度衰减 0.08/s；射门前有 0.35s 起脚前摇，球在这段时间内缓慢向前移动，球员速度锁到 0.8m/s。
- 双方都有抢断；被抢断者 2s 内不能再次抢断。
- 镜头 yaw 始终取玩家身体 yaw；A/D 转身时镜头同步转，S 后撤时镜头不动。
- 子弹时间镜头会快速拉近，退出后恢复；球权松散时保持正常速度。
- 画手势时会在 3m 内锁定球权，移动中射门不会丢球。
- 没有门将、没有犯规，也没有出界规则；球始终被封闭球场边界挡回场内。

## 模型与动画

- 玩家 `ybot`，AI `xbot`，FBX 导入为 **Humanoid**；原 Mixamo 材质/贴图保留，两队靠模型本身区分。
- 两个模型使用不同 Idle：玩家 `Dwarf Idle`，AI `Warrior Idle`；分别由 `FreePlayer.controller` 和 `FreeAI.controller` 驱动。
- 动作：`Dwarf Idle`、`Warrior Idle`、`Standard Run`、`Slow Jog Backwards`、`Run To Stop`、`Strike Forward Jog`、`Tackle`、`Turn 180`。
- 不再使用左右跑/左右跑转身：`Running Right Turn`、`Left Turn`、`Right Turn`、`Turn To Running` 已从 controller 移除。
- `Idle`：玩家 `Dwarf Idle`，AI `Warrior Idle`。
- `JogForward`：`Standard Run`；`JogBackward`：`Slow Jog Backwards`。
- `Turn180`：S 的 180° 转身状态。
- `Brake`：`Run To Stop`。
- `Shoot`：`Strike Forward Jog`；`F`/手势射门触发。
- `Tackle`：`Tackle.fbx`，抢断判定成功/失败都会触发；动画播放速度 1.5x，动作锁 0.35s。
- `S` 的两种状态：
  - 敌方在正面 180° 扇面内：后撤，身体/镜头不变。
  - 敌方不在正面：转身 180°，镜头跟着转；转完后如果敌方在正面，再回到后撤状态。
- Animator Controller：
  - 玩家 `Assets/Animations/FreePlayer.controller`
  - AI `Assets/Animations/FreeAI.controller`
  - 参数：`Speed`、`Backward`、`S180`、`Braking`、`Shoot`、`Tackle`
- [PlayerAnimationDriver.cs](GEL/Assets/Scripts/Player/PlayerAnimationDriver.cs) 驱动 `Speed/Backward/S180/Braking/Shoot/Tackle`。
- AI 驱动 `Speed/Backward`，不接摄像机；玩家转向/后撤判定只作用于玩家。AI 抢断也会触发 `Tackle` 动作。
- `Dribble`、`SoccerSpin`、`SoccerTackle`、`Running Right Turn`、`Left Turn`、`Right Turn`、`Turn To Running` 作为备用文件，当前不绑定。
- 关闭 root motion，位移仍由 Rigidbody 控制。
- 如果免费模型资源缺失，`P0SceneBuilder` 回退到程序化人体。

## 射门与慢动作

- 回退到上一版的“手势终点 + 贝塞尔曲线”射门，不再使用竖直/水平弹道实验。
- 手势屏幕线不再投影到地面；蓝线仍投到相机前方 `GesturePlaneDistance = 12m` 的相机朝向平面，只用于稳定显示。
- **手势起点会平移到球的屏幕位置**，保证拟合曲线从球出发。
- 手势曲线用屏幕点做二次拟合，再乘 `CurveGain = 1.0`；屏幕 Y-up 的正负号已翻转，往右画弧会变成向右的球。
- 最后加一步轨迹修正：读取**当前屏幕上的手势终点**和**球门的屏幕位置**（不做空间投影），用两者 Y 差生成 `verticalBias`，直接加到贝塞尔控制点的 Y 上，让射门弧线在画面里更贴近手势终点。`VerticalClimbScale = 2.5`，范围约 `-0.8 ~ +1.5m`。
- 手势终点投影到同一个相机平面，得到世界终点；从球指向终点决定方向，球到终点的距离决定射程。
- 射程限制在 **8 ~ 30m**。
- 球使用旧 `LaunchCurve`：二次贝塞尔轨迹，`ShotSpeed = 22`、`ShotSpeedDecay = 0.08`、`ShotArc` 和 `MaxCurveOffset` 控制弧线与横向偏移。
- 随机弧度扰动从 ±8% 降到 ±4%，避免画好的弧线被随机拉直。
- 射门有 `ShotWindupTime = 0.35s` 前摇：球保持球权，以 `ShotWindupBallSpeed = 1.2m/s` 缓慢向前移动；球员速度锁到 `ShotWindupSpeed = 0.8m/s`，前摇结束才真正飞出。
- 手势完成、`IsShooting` 变 true 的瞬间，`SlowMotionController` 立刻把 `Time.timeScale` 设回 1，退出子弹时间。
- 自动慢动作：玩家与 AI 距离 ≤ `DuelDistance = 3m`（且 AI 自动行为开启），或玩家持球从球上开始画射门手势时触发；从球外开始的过人/变向手势**不会主动触发子弹时间**，并且过人手势、爆发和 0.05s 恢复期间会暂停近身自动慢动作，除非手动 Space。
- 过人/防守爆发在 commit 瞬间会强制退出子弹时间，保证爆发按真实速度完成；0.35s 内不会重新被近身判定拉回慢动作。
- **球权前提**：`Possession.CurrentOwner != null` 才允许子弹时间；球在双方之间松散时，`Space`、距离判定、画手势都不会触发或维持慢动作。
- 画手势时 `PossessionManager.BeginAim` 会锁定球权（3m 内），松手后球先缓慢前移，前摇结束再释放。
- 正常镜头水平右移，不再把球员放在视线中心：`Offset = (-3.0, 1.35, -1.1)`，FOV 58；玩家模型保持可见，位于画面左侧。
- 镜头以 `LookOffset.y = -0.55` 向下俯视，只看角度、不改相机高度。
- 子弹时间镜头拉近到 `(-1.7, 1.35, -0.6)`，FOV 42，`ZoomSpeed = 12`；进入时相机高度降到足球中心高度，视角上仰，同时看到球、球门和双方球员的脚。
- 脚部/球焦点：`focus = 0.7 * 脚部锚点 + 0.3 * 球`，并叠加 `velocity * 0.12s` 的转播式前馈。

## 镜头方向

- 未在子弹时间时，镜头方向始终与玩家身体朝向一致。
- 镜头 yaw 取 `bodyYaw - 90°`，再用很短的 `SmoothDampAngle` 去掉 fixed-step 抖动；A/D 转身、S 的 180° 转身都会带着镜头转。
- 相机位置/旋转/速度前馈都做了阻尼，避免 Rigidbody 固定步长造成的高频抖动。
- S 后撤时身体不动，所以镜头也不动。
- 始终以玩家为主体、保持越肩。
- 看向点加上与相机相同的肩偏移，保证视线水平方向与 rig yaw 平行。
- 按你的决定，暂不做遮挡候选机位/半透明。
- 开球/进球重置时 `ResetDirection()` 按玩家当前身体 yaw 重新贴到身后。
- 子弹时间镜头高度取球心、视角上仰，镜头方向仍跟随身体。

## 移动惯性

- 状态机：`Idle / Run / Brake`；动画状态额外有 Turn180 / Shoot / Tackle。
- A/D：以 `BodyTurnSpeed = 90°/s` 缓慢转向，每次最多 **270°**，同时向前移动。
- 转向完成后身体停止；松开后再次按 A/D 可以继续转。
- 身体朝向改变时镜头同步跟随；速度仍是沿身体前向/后向的矢量。
- W：沿身体当前朝向前进。
- S：按一次判断敌方是否在身体正面 180° 扇面内：
  - 在扇面内：后撤，身体和镜头不变。
  - 不在扇面内：开始时长为 `Turn180Duration` 的 180° 转身；转身完成后若 S 仍按住，进入后撤。
- 急停：无输入且速度 > 1.5 时进入 `Brake`，减速度 30，带一点滑行；速度 < 0.4 回 `Idle`。
- AI 反应：`ReactionTime = 0.18s`，`Acceleration = 18`、`TurnSharpness = 5`；玩家先做动作，防守者会慢一拍调整。
- 子弹时间玩家倍率不变；靠 AI 迟滞 + 距离拉开后自然退出 3m 判定。

## 带球脚绑定

- **已删除 `PreferredFoot`**，球权判定不再看惯用脚；左右脚触发器都能触发 `TryClaimFromFoot`。
- `BallFoot` 绑定 `mixamorig:RightFoot` 用于脚部判定，`BallToe` 绑定 `mixamorig:RightToe_End` 作为带球参考点。
- 控球时以**球员中心**为圆心，在地面投影出 **0.5m** 和 **1.0m** 两个圆。
- 球始终在**身体朝向的直线**上，在 `0.5 ~ 1.0m` 两个圆之间缓慢正弦往复；`DribbleFrequency = 1.4Hz`，对应跑步循环步频。
- `MinDribbleDistance = 0.5`、`DribbleDistance = 1.0`、`DribbleSpring = 70`、`DribbleMaxSpeed = 12`。
- 球模型使用 `Assets/Models/FreeModule/Creature/Itm_Scr_Ball00.fbx`，大小仍按直径 0.22m 处理。
- 球贴图使用同目录的 `mitm_scr_ball00_alb.png`（Albedo）和 `mitm_scr_ball00_nrm.png`（Normal），由 `P0SceneBuilder` 生成 `Assets/Materials/BallModel.mat` 并赋给球模型。
- 松球且没有任何抢断 attempt 时，**球员平面位置距球** ≤ `LoosePickupRadius = 1.0m` 会直接吸附球权；发生抢断动作期间不吸附。
- 吸附用身体平面距离而不是脚尖，避免球员胶囊先把中圈球撞走、脚尖来不及触发的间歇性问题。
- 控球时临时忽略球与当前持球者胶囊的碰撞，避免球被身体挤飞；换权/释放/重置时恢复碰撞。
- **松球不靠距离吸附**：双方脚下各有一个 `FootTouch` 触发器；球保持自然物理运动，直到撞到任一脚的触发器才获得球权。
- 球是正常 SphereCollider + Rigidbody，不是 Trigger；只有脚部判定器是 Trigger。

## 重心系统（第一版）

- 新增 `WeightSystem`，状态：`Balanced / Lean / Commit / Recover`。
- 过人手势拖动期间，手势前段会写入局部二维重心 `Weight`；身体/髋部由 `PlayerVisual` 程序化倾斜，root 和摄像机不转。
- 手势最终方向 commit 时记录 `CommitDirection` 和 `LastLeanDirection`，重心在 `RecoverTime` 内回正。
- 防守 AI 会以已有 `ReactionTime` 延迟读取玩家重心，并把目标点向该方向偏移；防守者被骗时，过人判定会获得 `GetFeintAdvantage` 加成。
- 当前还没有设备倾斜输入；`WeightSystem.Tilt` 接口已预留。

## 过人动作（第一版）

- `FreePlayer/FreeAI.controller` 已接入 `Dribble`、`Spin`、`FeintLeft`、`FeintRight`、`StepOver` 状态，使用 `free module` 里的现成动作占位。
- 识别结果映射：
  - `Simple`：不触发额外动作
  - `Feint`：按重心左右触发 `FeintLeft/FeintRight`
  - `Roulette`：触发 `Spin`
  - `DoubleTouch`：触发 `Dribble`
  - `StepOver`：触发 `StepOver`
- 动画 `applyRootMotion = false`，位移仍由爆发逻辑负责，root 和摄像机不跟随动画旋转。

## 牛尾巴（Elastico）

- 唯一的专项花式动作。
- 手势：划一个短促的 `>` 或 `<`，从低往高（也兼容反方向）；识别器要求整体有足够竖直位移、路径比净位移长、存在一个足够尖的顶点，最少 3 个采样点即可。
- 识别为 `DribbleStyle.Elastico`，仍走现有高速爆发。
- 动作不依赖新素材：`PlayerVisual` 在 `LateUpdate` 里对 Humanoid 的髋/膝/踝做程序化骨骼旋转，向斜前方伸腿再落脚，持续 `ElasticoPoseDuration = 0.34s`。
- 伸腿方向由最终拨球方向的左右侧决定；root 和球权逻辑不跟随骨骼旋转。
- **起点在球命中圈内时始终执行射门，不做任何花式切换**；Elastico 必须从球命中圈外起手。
- Elastico commit 会调用 `SlowMotionController.ForceEnter(0.60s)`，自动进入子弹时间并触发 `ShoulderCamera` 的拉近/FOV 变化；动作在慢动作中完成，然后回到正常速度。
- 编辑器快捷键 `Ctrl+R` 不生效时，使用菜单 `GEL → Force Recompile Scripts` 强制刷新脚本。

## 复杂手势识别（第一版）

- 过人手势在松手时做规则识别，不依赖机器学习。
- 识别类型：
  - `Simple`：普通短滑
  - `Feint`：前段方向与最终方向夹角 > 70°
  - `Roulette`：累计转角 > 180°
  - `DoubleTouch`：两次以上反向
  - `StepOver`：闭合面积较大、路径明显长于净位移
- 识别只作用于 `Dribble` 模式，不参与射门；识别失败统一回退到 `Simple`。
- 手势前 32% 作为重心诱导方向，最终位移方向作为 exit；`Feint/DoubleTouch` 会提高过人成功率，但最终结果仍受防守者 commit 和速度差影响。
- 当前只验证识别和重心收益，动作动画还未接入。

## 过人（第一版）

- 按下时判定触点：球屏幕命中圈内（`BallHitRadius = 95px`）进入射门，圈外进入过人。
- 手势按住期间会锁定 A/D 转向，避免同时按方向键时插进几帧转弯。
- 过人手势会额外锁住**全部移动输入**：按下到拨球爆发结束后的 `DribbleRecoveryDuration = 0.05s` 内，不响应 W/A/S/D，身体朝向和镜头都不变；只做独立于方向键的水平爆发。射门手势仍保留 W/S，只锁 A/D。
- 过人方向按屏幕手势方向：屏幕向上 = 相机前方，屏幕向右 = 相机右方，屏幕斜向 = 斜向短促移动；不做地面 raycast 或空间投影。
- 手势方向直接调用 `PlayerMotor.TryDribble`，身体朝向不跟随手势旋转。
- **非控球状态**下同一个手势会变成防守爆发（`Burst`）：不拨球、不判定过人成功，只做同样方向和时长的短促位移，用于补位/回追；冷却与过人手势共用，因此攻防都有一次快速位移窗口。
- 拨球/爆发期间球权被硬锁在持球者身上，不会因为 burst 球速或距离瞬间超过 `MaxControlSpeed` 而变成 `Loose ball`；抢断仍可正常发生。
- 拨球爆发期间**不旋转身体/摄像机**，只做身体相对侧步，避免镜头瞬转；身体朝向保持原方向。
- 当前版本没有拨球动画，先用玩家爆发位移 + 球侧向跟随验证手感：
  - `DribbleInstantOffset = 0.5m`：commit 瞬间先位移一小段
  - `DribbleBurstDuration = 0.12s`
  - `DribbleBurstPeakSpeed = 16m/s`
  - `DefensiveBurstPeakSpeed = 18m/s`
  - 总距离约 2.4~2.7m，明显快于奔跑 7.5m/s
  - `DribbleRecoveryDuration = 0.05s`
  - `DribbleCooldown = 0.55s`
  - 过人最小滑动 `MinDribblePixels = 10px`
- 防守者在 `DribbleRange = 1.6m` 内、且不在身体正后方（`dot(forward, toDefender) > -0.2`）时参与判定。
- **速度能力值**：
  - 无球速度 `MoveSpeed = 7.5m/s`
  - 带球速度 `DribbleSpeed = 6m/s`
  - 能力上限 `DribbleAbilityCap = 0.9`
  - 人球结合能力 `BallControlAbility = DribbleSpeed / MoveSpeed × DribbleAbilityCap = 0.72`
- 过人完成后的判定：
  - 双方身体朝向不一致（`dot(player.forward, defender.forward) < 0.85`）：**直接成功**。
  - 朝向一致：计算 `防守者速度 - DribbleSpeed`；超过 `SpeedGapThreshold / BallControlAbility` 则失败，否则成功。
  - `SpeedGapThreshold = 0.8`
- 成功：防守者 `Stun(0.28s)`、禁止抢断 `0.5s`、过人免疫 `1.2s`，取消防守者的抢断 attempt；HUD 显示 `Beat!`。
- 过人/变向本身**不会主动进入子弹时间**；commit 时会强制退出当前子弹时间，让爆发在正常速度下完成。射门板块的子弹时间进入逻辑未改。
- 全屏绿光/蓝光提示尚未实现，等手势起始触点范围最终确定后再加。

## 抢断

- 玩家 `Q` 或鼠标右键；AI 在 `TackleRange` 内自动尝试。
- 成功率：`clamp01( (1 - 脚部离球距离 / TackleRange) * TackleAbility * Random(0.7, 0.9) )`。
- 脚部点 = 球员位置 + 前向 0.45m；`TackleAbility` 暂定 1.0，`TackleRange` 暂定 1.6m。
- 触发抢断动作时，抢断者速度强制归零，动作锁 **0.35s**，动画以 1.5x 播放，动作结束后立刻恢复。
- 抢断不再在按键瞬间结算，而是在 0.35s 动作过程中持续记录**脚尖离球的最近距离**，动作结束时结算。
- 结算公式：`clamp(TackleAbility * BallControlAbility * closeness * Random(0.8, 1.2), 0.05, 0.9)`。
  - `closeness = clamp01(1 - 最近脚尖距离 / TackleRange)`；否则直接乘绝对距离会变成“离得越远越容易抢”。
  - `TackleAbility` 默认 **0.75**，建议范围 **0.5 ~ 1.0**。
  - `BallControlAbility` 默认 **0.75**，建议范围 **0.5 ~ 1.0**。
  - 随机波动 **0.8 ~ 1.2**，最终概率限制在 **5% ~ 90%**。
- 成功：球权给抢断者，被抢断者硬直 **0.2s**，并且 **2s 内不能再次抢断**；失败：抢断者硬直 0.15s，球权不变。
- 硬直暂时不接动作，直接冻结当前动画帧，硬直结束后恢复。
- 抢断者自身有 0.8s 冷却，防止连点。
- 当前持球人不会被贴身自动换权，必须通过抢断；松散的球仍可由最近的人捡起。

## AI 射门

- AI 射门也走二次曲线，曲率分 5 档：`大弧左(-1) / 小弧左(-0.5) / 直线(0) / 小弧右(0.5) / 大弧右(1)`。
- 概率按钟形分布：直线峰值 `0.6 * ShotAbility`（限制在 0.3~0.85），剩余概率按 75% 小弧、25% 大弧分到两侧。
- `ShotAbility = 1.0` 时：`5% / 15% / 60% / 15% / 5%`。
- 大弧横向偏移 6m，小弧 3m；AI 初速 20m/s，衰减 0.08/s。
- 能力值越高，直线概率越高（更稳）；后续可反过来做成“能力越高越敢打大弧”。

可调参数：

| 参数 | 默认 | 作用 |
|---|---|---|
| `MinShotDistance` | 8 | 手势射门最短射程（m） |
| `MaxShotDistance` | 30 | 手势射门最远射程（m） |
| `CurveGain` | 1.0 | 屏幕手势弧度放大倍率 |
| `ShotWindupTime` | 0.35 | 射门前摇（秒） |
| `ShotWindupSpeed` | 0.8 | 前摇期间球员速度（m/s） |
| `ShotWindupBallSpeed` | 1.2 | 前摇期间球缓慢前移速度（m/s） |
| `ShotSpeed` | 22 | 旧曲线/F 测试回退初速 |
| `ShotSpeedDecay` | 0.08 | 旧曲线/F 测试衰减 |
| `MaxCurvature` | 0.22 | 曲率上限（1/m） |
| `MaxCurveOffset` | 1.8 | 横向偏移上限（m） |
| `VerticalClimbScale` | 2.5 | 手势终点与球门屏幕 Y 差 → 弧高修正 |
| `ShotArc` | 1.0 | 旧曲线弧高 |
| `CurveSkill` | 1.0 | 旧曲线球员弧度参数 |
| `GesturePlaneDistance` | 12 | 手势屏幕点投影平面距相机的距离（m） |
| `DuelDistance` | 3 | 自动慢动作触发距离 |
| `Acceleration` | 22 | 前向加速度 |
| `Deceleration` | 30 | 刹车减速度 |
| `LateralGrip` | 24 | 侧向抓地 |
| `MinDribbleDistance` | 0.5 | 球离球员中心的最小半径（m） |
| `DribbleDistance` | 1.0 | 球离球员中心的最大半径（m） |
| `DribbleFrequency` | 1.4 | 球在 0.5~1.0m 圆之间往复的频率（Hz） |
| `DribbleSpring` | 70 | 球跟随目标半径的弹簧强度 |
| `ReactionTime` | 0.18 | AI 反应延迟 |
| `DuelFov` | 42 | 子弹时间 FOV |
| `BodyTurnSpeed` | 90 | A/D 转身速度（°/s） |
| `MaxTurnPerPress` | 270 | 每次按 A/D 的最大转角 |
| `TackleAbility` | 0.75 | 抢断能力，建议 0.5~1.0 |
| `BallControlAbility` | 0.75 | 被抢断者护球能力，建议 0.5~1.0 |
| `TackleActionSeconds` | 0.35 | 抢断动作/判定时长 |
| `LoosePickupRadius` | 1.0 | 松球自动吸附半径（球员平面距离，m） |
| `TackleRange` | 1.6 | 抢断距离（m） |
| `VictimLockSeconds` | 2.0 | 被抢断后禁止抢断时间 |
| `TacklerCooldownSeconds` | 0.8 | 抢断者冷却 |
| `ShotAbility` | 1.0 | AI 射门能力占位 |
| `ShotSpeed`（AI） | 20 | AI 初速 |
| `ShotDistance`（AI） | 26 | AI 射门曲线长度 |

## AI 调试

- `T`：切换 `AIController.Automatic`，关闭后 AI 不再追球/铲球/射门。
- AI 的目标点每 `ReactionTime` 才刷新一次，防守时会慢一拍；`Acceleration/TurnSharpness` 也比玩家低。
- 选中 AI 时，Scene 视图会画出 `TackleRange`（红圈）和 `ShootRange`（黄圈），以及当前目标点的黄线。
- 也可以在 Inspector 里直接取消 `AIController` 的 `Automatic`，在 Play 前就禁用它。

## 目录

```
Assets/
  Editor/P0SceneBuilder.cs     # 一键生成灰盒球场和全部对象
  Scripts/
    Ball/BallSimulator.cs      # 自定义球物理 + 二次曲线跟随
    Player/PlayerAgent.cs      # 球员身份、硬直、传送
    Player/PlayerMotor.cs      # 移动 + 前摇 + 拟合曲线射门
    AI/AIController.cs         # 防守站位/抢断/正态分布弧线射门
    Core/MatchDirector.cs      # 比分、计时、开球、重开
    Core/PossessionManager.cs  # 球权判定、带球吸附、瞄准锁球
    Core/TackleSystem.cs       # 双方共用的抢断判定
    Core/SlowMotionController.cs # 手动/自动子弹时间
    Input/GestureInput.cs      # 手势采样、二次拟合、射门参数、抢断输入
    Camera/ShoulderCamera.cs   # 越肩跟随 + 球权换向 + 慢动作拉近
    Match/GoalTrigger.cs
    UI/PrototypeHUD.cs         # OnGUI 临时 HUD
  Materials/                   # 首次生成
  Scenes/P0_Prototype.unity    # 首次生成
```

## 下一步

1. P0 试玩：先确认“拖鼠标射门 + 慢动作”是否顺手。
2. P1：换 URP + Toon、加脚部 IK 和 6 个动作、把 AI 换成行为树。
3. Android：用 Unity Hub 中国版补 Android SDK & NDK Tools，再切平台。
