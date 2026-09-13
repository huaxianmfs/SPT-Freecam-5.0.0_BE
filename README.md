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
记忆上次自由相机位置（单局内）

⚠️
提醒：我没有移植免摔落功能

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


致谢

原项目：TerkoizLT/SPT-Freecam

感谢原作者的 Freecam 实现。
