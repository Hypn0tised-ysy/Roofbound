# Abilities
ability slot button:点击时将该技能记录为当前技能，并在玩家状态机中生效。
将Border子对象激活
movement/utility各自只有一个技能位，点击时切换技能，点击已选技能取消选择。

# main menu 
- play:隐藏main menu 激活level select panel
- abilities: 隐藏main menu 激活abilities panel
- options：隐藏main menu 激活options panel
- quit ：退出游戏

# level select panel 
- 关卡选择slot 
- back返回上一级菜单
- play
选择关卡
点击play按钮则隐藏level select panel，激活main menu（以便返回主界面时是正常状态），根据选择关卡load对应的关卡scene（由level controller负责）
# 关卡内
这部分ui逻辑继续由ui manager控制（其在不同游戏场景间不销毁）
- death menu（死亡激活，半透明，不停止游戏时间但是停止响应玩家运动控制）
- pause menu（暂停时激活，半透明）
- finish menu（过关激活）
## death menu 
- level failed文本
- press any key to restart文本
检测到任意输入则restart level
## pause menu 
- resume 继续
- restart level 重新开始关卡
- level select 关卡选择
- main menu 主菜单


# 实现
UI manager 
UIManager 应该是全局唯一对象：

游戏启动时创建。

使用 DontDestroyOnLoad 保持跨场景不销毁。

如果新场景中重复出现 UIManager，应该销毁重复实例。

UIManager 应该监听场景加载完成事件，并根据当前场景类型恢复正确 UI 状态

时机	行为
游戏启动	初始化 UI 引用、按钮监听、默认进入主菜单
进入主菜单 Scene	显示 MainMenuPanel，隐藏所有关卡内菜单
进入关卡 Scene	隐藏主菜单类 UI，进入 Playing 状态
玩家死亡	显示 DeathMenu
玩家暂停	显示 PauseMenu
关卡完成	显示 FinishMenu
返回关卡选择	显示 LevelSelectPanel
返回主菜单	显示 MainMenuPanel

UIManager 内部维护一个 UI 状态枚举。
UIState ├── MainMenu ├── LevelSelect ├── AbilitySelect ├── Options ├── Playing ├── Paused ├── Dead └── Finished
5.2 状态说明
状态	显示内容	玩家控制	时间缩放
MainMenu	MainMenuPanel	禁用	正常
LevelSelect	LevelSelectPanel	禁用	正常
AbilitySelect	AbilityPanel	禁用	正常
Options	OptionsPanel	禁用	正常
Playing	无菜单或仅 HUD	启用	正常
Paused	PauseMenu	禁用	暂停
Dead	DeathMenu	禁用移动控制	不暂停时间
Finished	FinishMenu	禁用	暂停

任何时候只允许一个主流程面板处于激活状态：

MainMenuPanel

LevelSelectPanel

AbilityPanel

OptionsPanel

关卡内菜单也应该互斥：

DeathMenu

PauseMenu

FinishMenu

切换状态时应先隐藏所有相关面板，再激活目标面板。