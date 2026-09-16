在gpt和deepseek的帮助下，完成了对freecam的移植
Terkoiz Freecam — SPT 5.0 BE

适用于 SPT 5.0.0 BE / EFT 1.1.5.0.47242 的 Freecam 移植版。

✨功能
+：开启/关闭自由视角
.：切换相机控制权
Enter：将玩家传送到当前相机位置
*：隐藏/显示 UI
WASD / 方向键：移动相机
鼠标：旋转视角
Shift：加速移动
Q/E和R/F：上下移动
鼠标滚轮：前后移动相机

改动：
重新添加回免摔落伤害功能
安装完，首次启动后，会在插件文件内生成config文件夹，内部FreecamConfig.json和NoFallDamage.json，分别控制相机视角的记忆功能（默认关闭）和免摔落伤害功能（默认开启）。

安装

将Terkoiz.Freecam.zip解压

放入：
SPT根目录\BepInEx\plugins\

从源码编译

项目提供了 build.bat，无需手动输入编译命令。

使用方法
确认已经安装对应的 .NET SDK。
确认项目中的 SPT 5.0 BE DLL 引用路径正确。
双击：
build.bat

或者在项目目录打开 CMD：

build.bat

脚本会自动执行编译。

编译成功后，将生成的：

Terkoiz.Freecam.dll

不过我觉得什么都不改的话，路径应该会报错

本项目基于 TerkoizLT/SPT-Freecam 修改。

原版针对旧版 Mono SPT，本版本针对 SPT 5.0 BE 的 IL2CPP 环境进行了移植。

主要修改：

将 FreecamController 改为 IL2CPP 兼容的 MonoBehaviour
增加 IL2CPP 类型注册
适配 SPT 5.0 的 PlayerCameraController
适配新的 GameWorld / MainPlayer 获取方式
使用 PlayerCameraController.ExternalControl 接管相机
适配 GamePlayerOwner 的控制
修改初始化时机，使其等待 MainPlayer 和相机创建完成
保留原版 Freecam 的主要操作逻辑和功能

v2改动
1. 挂载方式重构
挂载时机：
Plugin.Load() 里 new GameObject + DontDestroyOnLoad  →  Harmony 补丁 GameWorld.OnGameStarted 后挂到 GameWorld.gameObject 上

相机移动脚本	：
逻辑全在 FreecamController 里                         → 拆出独立 Freecam 组件，运行时 AddComponent 到主相机

初始化：
每 0.5s 轮询 TryInitialize()	                         → Start 里一次性完成

2. 相机接管方式改变
不再依赖 PlayerCameraController.ExternalControl，改为走游戏自身的 POV 状态机：
csharp
localPlayer.PointOfView = EPointOfView.ThirdPerson;
playerBody.PointOfView.Value = EPointOfView.FreeCamera;
pcc.UpdatePointOfView();

3. 新增 4 个 Harmony 补丁（关键修复）
CameraDistancePatch
PlayerCameraControllerLateUpdatePatch
ForceSetCameraPositionPatch
FreecamPatch

5. 新增免摔伤模块
独立文件 NoFallDamage.cs，补丁 ActiveHealthController.HandleFall

只对 GameWorld.MainPlayer 生效，AI / 队友照常受伤

致谢

原项目：TerkoizLT/SPT-Freecam

感谢原作者的 Freecam 实现。
