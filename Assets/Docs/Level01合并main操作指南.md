# Level01 合并到 main — 操作指南（第二版）

> 方案：**main 选关 → LoadScene("Level01")**，UI 全部由 main 的 `UIManager`（DontDestroyOnLoad）负责。  
> Level01 只保留**关卡世界**，删除/不保留旧 UI，**无需**代码运行时 `SetActive(false)` 忽略。

---

## 1. 加载流程

```text
main (Build index 0)
  └─ UIManager [DontDestroyOnLoad]
       ├─ MainMenu / LevelSelect
       ├─ HUDPanel / PauseMenu
       └─ LevelComplete / GameOver

用户选 Level1 → Play
  └─ SceneManager.LoadScene("Level01")

Level01 (Build index 1)
  └─ 仅关卡世界 + LevelController + Fleet_Spawner + destination ...
  └─ 胜负 → levelController → UIManager.TriggerVictory / TriggerGameOver
```

---

## 2. 代码侧（已完成）

| 文件 | 变更 |
|------|------|
| `Assets/levelController.cs` | 胜负/重开走 UIManager；Fleet 存在时跳过生成主角 |
| `Assets/UIManager.cs` | 默认关卡名 `Level01`, `level1-2` |
| `ProjectSettings/EditorBuildSettings.asset` | 第二场景改为 `Level01` |
| `Assets/Scenes/main.unity` | UIManager / LevelSelect 的 `levelSceneNames` |
| `Assets/Prefabs/UIManager.prefab` | 同上 |
| `Assets/Scenes/Level01.unity` | LevelController 的 winPanel/losePanel/finishMenuPanel 引用已清空 |

**注意**：本版 **不再** 使用 `DisableLegacySceneUiWhenUsingUIManager()`，请在场景中物理删除冗余 UI。

---

## 3. Level01 中应删除的对象

### 3.1 必须删除（根物体，删父带子）

| 根物体名 | 原因 | 子物体示例（会一并删除） |
|----------|------|--------------------------|
| **HUD** | 计时 HUD 由 main 的 `HUDPanel` + `Panel_HUD` 负责 | `Crosshair`、`TimeText` |
| **LevelComplete** | 通关 UI 由 main 的 `LevelComplete` 负责 | `CompleteMenuBtn`、`NextLevelBtn`、`FinalTime` |
| **GameOver** | 失败 UI 由 main 的 `GameOver` 负责 | `Die`、`ReStart`、`Main_Menu` |

### 3.2 若存在则删除

| 根物体名 | 原因 |
|----------|------|
| **PauseMenu** | 暂停由 main 的 UIManager 管理 |
| **MainMenu** / **Main_Menu** | 主菜单只在 main 场景 |
| **Die** | 若未挂在 GameOver 下、单独成根物体时删除 |
| **UIManager** | 关卡场景不应再有第二套 UIManager |

### 3.3 必须保留

| 根物体 / 预制体 | 作用 |
|-----------------|------|
| **Buildings** | 关卡建筑 |
| **Terrain** | 地形 |
| **TrackPath** | 路径点 WP1~WP8 |
| **Fleet_Spawner** | 生成卡车与玩家 |
| **LevelController** | 胜负逻辑（不绑 winPanel/losePanel） |
| **destination** | 终点触发器 |
| **Main Camera** | 关卡摄像机 |
| **Directional Light** | 光照 |
| **BGM** | 可选，背景音乐 |
| **EventSystem** | **建议保留或添加**（main 切场景后原 EventSystem 会卸载，关卡内 UI 按钮需要它） |

### 3.4 LevelController Inspector 检查

- `winPanel` / `losePanel` / `finishMenuPanel` → **None（空）**
- `truckRowCount` / `truckColumnCount` → **0**（由 Fleet_Spawner 生成）
- `triggerDeadOnGroundHit` → 按设计（Level01 原为 1）

---

## 4. Unity 编辑器操作步骤

### 方式 A：菜单一键清理（推荐）

1. 菜单 **Roofbound → Level01 集成 → 清理关卡内冗余 UI**
2. 菜单 **Roofbound → Level01 集成 → 校验 Level01 场景结构**（看 Console）
3. 保存 Level01 场景

### 方式 B：手动在 Hierarchy 删除

1. 打开 `Assets/Scenes/Level01.unity`
2. 删除 §3.1、§3.2 中的根物体
3. 选中 **LevelController**，清空 Win Panel / Lose Panel / Finish Menu Panel
4. 若无 **EventSystem**，创建：`GameObject → UI → Event System`
5. 保存场景

---

## 5. main 场景检查

1. **File → Build Settings**：`main` + `Level01`（不要依赖 `level1-1`）
2. 选中 **UIManager** → **Level Scene Names**：`Level01`, `level1-2`
3. 确认绑定：`hudPanel`→HUDPanel，`finishMenuPanel`→LevelComplete，`deadMenuPanel`→GameOver

---

## 6. 验证清单

- [ ] 从 **main** Play（不要单独 Play Level01）
- [ ] 选 Level1 → 开始 → 进入 Level01 完整关卡
- [ ] 仅 **一个** 玩家（Fleet_Spawner 生成）
- [ ] 到达 destination → 弹出 **UIManager 的 LevelComplete**
- [ ] 死亡 → **UIManager 的 GameOver**
- [ ] ESC 暂停正常；重开 / 回主菜单正常
- [ ] Level01 Hierarchy 中 **无** HUD / LevelComplete / GameOver 根物体

---

## 7. 常见问题

| 现象 | 处理 |
|------|------|
| 两套 UI 叠层 | Level01 未删干净 HUD/LevelComplete/GameOver |
| 通关无面板 | 未从 main 进关，或 LevelController 仍绑旧 winPanel |
| 按钮点不了 | Level01 缺少 EventSystem |
| 两个主角 | Fleet_Spawner 与 levelController 同时生成（代码已互斥，检查 Fleet 配置） |
| 仍加载 level1-1 | main / UIManager.prefab 的 levelSceneNames 未改为 Level01 |

---

*文档路径：`Assets/Docs/Level01合并main操作指南.md`*
