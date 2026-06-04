using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    private static readonly string[] DefaultLevelSceneNames = { "Level01", "level1-2" };

    public enum UIState
    {
        MainMenu,
        LevelSelect,
        AbilitySelect,
        Options,
        Playing,
        Paused,
        Dead,
        Finished
    }

    [Header("主流程面板")]
    public MainMenu MainMenu;
    public LevelSelect LevelSelect;
    [SerializeField] private GameObject abilityPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("关卡选择")]
    [SerializeField] private string[] levelSceneNames = new[] { "Level01", "Level02","Level03","Level04" };
    [SerializeField] private int selectedLevelIndex = 0;

    [Header("关卡内面板")]
    [SerializeField] private GameObject hudPanel; // 🔴 1. 新增：HUD 槽位
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject deadMenuPanel;
    [SerializeField] private GameObject finishMenuPanel;

    public UIState CurrentState { get; private set; } = UIState.MainMenu;
    public int SelectedLevelIndex => selectedLevelIndex;
    public bool IsMenuPaused { get; private set; }
    public bool IsInputLocked { get; private set; }

    private float baseFixedDeltaTime;
    private bool hasAppliedInitialState;
    private bool pendingGameplayStart;
    private bool pendingShowMainMenuAfterLoad;
    private bool pendingShowLevelSelectAfterLoad;
    private playerControl cachedPlayerControl;
    private Panel_HUD cachedHudPanel;
    private float gameplayRunTime;
    private float lastVictoryRunTime;
    private bool hasVictorySnapshot;
    private static EventSystem persistedEventSystem;

    private BGMController cachedBgmController;
    private AudioSource cachedBgmAudioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        baseFixedDeltaTime = Time.fixedDeltaTime;
        EnsureEventSystem();
        TryBindSceneReferences();
        FixBrokenCanvasScales();
        CacheBGM();
    }

    private void FixBrokenCanvasScales()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Transform canvasTransform = canvases[i].transform;
            if (canvasTransform.localScale.sqrMagnitude < 0.001f)
            {
                canvasTransform.localScale = Vector3.one;
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (!hasAppliedInitialState)
        {
            hasAppliedInitialState = true;
            ShowMainMenu();
        }
    }

    private void Update()
    {
        if (CurrentState == UIState.Playing && Time.timeScale > 0f)
        {
            gameplayRunTime += Time.deltaTime;
        }

        if (CurrentState == UIState.Playing && Input.GetKeyDown(KeyCode.Escape))
        {
            ShowPauseMenu();
        }
        else if (CurrentState == UIState.Paused && Input.GetKeyDown(KeyCode.Escape))
        {
            HidePauseMenu();
        }
    }

    public void ShowMainMenu()
    {
        //当前场景是否为main
        //若不是main，先加载main场景
        if (SceneManager.GetActiveScene().name != "main")
        {
            pendingShowMainMenuAfterLoad = true;
            SceneManager.LoadScene("main");
        }
        SetState(UIState.MainMenu);
    }

    public void ShowLevelSelect()
    {
        if (SceneManager.GetActiveScene().name != "main")
        {
            pendingShowLevelSelectAfterLoad = true;
            SceneManager.LoadScene("main");
            return;
        }

        SetState(UIState.LevelSelect);
    }

    public void SelectLevel(int index)
    {
        Debug.Log("button clicked, select level index:" + index);

        if (levelSceneNames == null || levelSceneNames.Length == 0)
        {
            selectedLevelIndex = Mathf.Max(0, index);
            return;
        }

        selectedLevelIndex = Mathf.Clamp(index, 0, levelSceneNames.Length - 1);

        Debug.Log("selected level index: " + selectedLevelIndex);
    }

    public void PlaySelectedLevel()
    {
        pendingGameplayStart = true;
        SetState(UIState.Playing);

        string[] scenes = (levelSceneNames != null && levelSceneNames.Length > 0)
            ? levelSceneNames
            : DefaultLevelSceneNames;

        if (scenes.Length > 0)
        {
            string sceneName = scenes[Mathf.Clamp(selectedLevelIndex, 0, scenes.Length - 1)];
            if (!string.IsNullOrEmpty(sceneName))
            {
                Debug.Log("play scene: " + sceneName);
                SceneManager.LoadScene(sceneName);
                return;
            }
            else
            {
                Debug.Log("invalid scene name for selected level index, loading current active scene: " + sceneName);
            }
        }

        Debug.LogWarning("levelSceneNames not configured or selected scene is invalid; aborting scene load.");

        pendingGameplayStart = false;
    }

    public void ShowAbilityPanel()
    {
        SkillSelectionStore.RefreshAllAbilitySlots();
        SetState(UIState.AbilitySelect);
    }

    public void ShowOptionsPanel()
    {
        SetState(UIState.Options);
    }

    public void StartGameplay()
    {
        pendingGameplayStart = false;
        SetState(UIState.Playing);
    }

    public void ShowPauseMenu()
    {
        SetState(UIState.Paused);
    }

    public void HidePauseMenu()
    {
        SetState(UIState.Playing);
    }

    public void GoBack()
    {
        switch (CurrentState)
        {
            case UIState.LevelSelect:
            case UIState.AbilitySelect:
            case UIState.Options:
                ShowMainMenu();
                break;
            case UIState.Paused:
                HidePauseMenu();
                break;
        }
    }

    public void RestartLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        pendingGameplayStart = true;
        SetState(UIState.Playing);
        SceneManager.LoadScene(sceneName);
    }

    public void ReplayCurrentLevel()
    {
        RestartLevel();
    }

    // Load the next level in the configured `levelSceneNames` list.
    // If the current scene is the last configured level, return to Main Menu.
    public void PlayNextLevel()
    {
        string[] scenes = (levelSceneNames != null && levelSceneNames.Length > 0)
            ? levelSceneNames
            : DefaultLevelSceneNames;

        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogWarning("PlayNextLevel: no configured level scenes; returning to main menu.");
            ShowMainMenu();
            return;
        }

        string current = SceneManager.GetActiveScene().name;
        int idx = System.Array.IndexOf(scenes, current);

        // If we found the current scene and there is a next one, load it.
        if (idx >= 0 && idx + 1 < scenes.Length)
        {
            int nextIdx = idx + 1;
            string nextScene = scenes[nextIdx];
            Debug.Log("Playing next level: " + nextScene);
            selectedLevelIndex = nextIdx;
            pendingGameplayStart = true;
            SetState(UIState.Playing);
            SceneManager.LoadScene(nextScene);
            return;
        }

        // Fallback: if selectedLevelIndex points to a different level, try that
        int fallbackIdx = Mathf.Clamp(selectedLevelIndex + 1, 0, scenes.Length - 1);
        if (fallbackIdx != selectedLevelIndex && fallbackIdx < scenes.Length)
        {
            string nextScene = scenes[fallbackIdx];
            Debug.Log("Playing next level (fallback): " + nextScene);
            selectedLevelIndex = fallbackIdx;
            pendingGameplayStart = true;
            SetState(UIState.Playing);
            SceneManager.LoadScene(nextScene);
            return;
        }

        // No next level: return to main menu
        Debug.Log("No next level available; returning to main menu.");
        ShowMainMenu();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void TriggerVictory()
    {
        CaptureVictorySnapshot();
        SetState(UIState.Finished);
    }

    /// <summary>胜利面板应优先读此值（构建版在 OnEnable 时比 GetRunTime 更可靠）。</summary>
    public float GetVictoryRunTime()
    {
        if (hasVictorySnapshot)
        {
            return lastVictoryRunTime;
        }

        return ReadCurrentRunTime();
    }

    public float GetRunTime()
    {
        return GetVictoryRunTime();
    }

    public void ReportHudTime(float seconds)
    {
        if (CurrentState == UIState.Playing && seconds > gameplayRunTime)
        {
            gameplayRunTime = seconds;
        }
    }

    private void CaptureVictorySnapshot()
    {
        ResolveHudPanel();
        lastVictoryRunTime = ReadCurrentRunTime();
        hasVictorySnapshot = true;
        Debug.Log("[UIManager] 胜利用时快照: " + Panel_HUD.FormatTime(lastVictoryRunTime)
            + " (HUD=" + (cachedHudPanel != null ? Panel_HUD.FormatTime(cachedHudPanel.GetFinalTime()) : "无")
            + ", gameplay=" + Panel_HUD.FormatTime(gameplayRunTime) + ")");
    }

    private float ReadCurrentRunTime()
    {
        ResolveHudPanel();
        float hudTime = cachedHudPanel != null ? cachedHudPanel.GetFinalTime() : 0f;
        return Mathf.Max(hudTime, gameplayRunTime);
    }

    public void TriggerGameOver()
    {
        Debug.Log("show dead panel");
        SetState(UIState.Dead);
    }

    public void SetPlayerUICanvasVisible(bool visible)
    {
        ResolvePlayerControl();
        if (cachedPlayerControl != null)
        {
            cachedPlayerControl.SetPlayerCanvasVisible(visible);
        }
    }

    public void SetMenuPaused(bool paused)
    {
        IsMenuPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
    }

    public void SetInputLocked(bool locked)
    {
        IsInputLocked = locked;

        if (!locked && CurrentState == UIState.Playing)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanupDuplicateEventSystems();
        TryBindSceneReferences();
        cachedPlayerControl = null;
        // If we were requested to show Level Select on the next load, do that now
        if (pendingShowLevelSelectAfterLoad)
        {
            pendingShowLevelSelectAfterLoad = false;
            SetState(UIState.LevelSelect);
        }
        // If we were requested to show the Main Menu on the next load, do that now
        else if (pendingShowMainMenuAfterLoad)
        {
            pendingShowMainMenuAfterLoad = false;
            ShowMainMenu();
        }
        else
        {
            ApplyState(CurrentState, CurrentState);
        }

        if (pendingGameplayStart && CurrentState == UIState.Playing)
        {
            ResetHudTimer();
            pendingGameplayStart = false;
        }

        cachedBgmController = null;
        cachedBgmAudioSource = null;
        CacheBGM();
    }

    // Request that when the next scene loads, UIManager should switch to MainMenu state.
    public void RequestShowMainMenuOnNextSceneLoad()
    {
        pendingShowMainMenuAfterLoad = true;
    }

    private void SetState(UIState newState)
    {
        UIState previousState = CurrentState;
        CurrentState = newState;
        ApplyState(newState, previousState);
    }

    private void ApplyState(UIState state, UIState previousState)
    {
        HideMenuPanels();

        // 暂停时隐藏 HUD，避免同 Canvas 下 HUD 挡住 PauseMenu 射线；计时已在 ResetRunTimer 中单独管理
        bool showHud = state == UIState.Playing;
        SetPanelVisible(hudPanel, showHud);

        bool showPlayerCanvas = state == UIState.Playing;
        SetPlayerUICanvasVisible(showPlayerCanvas);

        switch (state)
        {
            case UIState.MainMenu:
                SetMenuPaused(false);
                SetInputLocked(true);
                SetPanelVisible(MainMenu != null ? MainMenu.gameObject : null, true);
                ShowCursor();
                break;
            case UIState.LevelSelect:
                SetMenuPaused(false);
                SetInputLocked(true);
                SetPanelVisible(LevelSelect != null ? LevelSelect.gameObject : null, true);
                ShowCursor();
                break;
            case UIState.AbilitySelect:
                SetMenuPaused(false);
                SetInputLocked(true);
                SetPanelVisible(abilityPanel, true);
                ShowCursor();
                break;
            case UIState.Options:
                SetMenuPaused(false);
                SetInputLocked(true);
                SetPanelVisible(optionsPanel, true);
                ShowCursor();
                break;
            case UIState.Playing:
                SetMenuPaused(false);
                SetInputLocked(false);
                HideCursor();
                if (previousState == UIState.Paused)
                {
                    ResumeBGM();
                    NotifyPlayerGameplayResumed();
                }
                break;
            case UIState.Paused:
                SetMenuPaused(true);
                SetInputLocked(true);
                SetPanelVisible(pauseMenuPanel, true);
                BringPanelToFront(pauseMenuPanel);
                EnsureEventSystem();
                ShowCursor();
                PauseBGM();
                break;
            case UIState.Dead:
                SetMenuPaused(true);
                SetInputLocked(true);
                SetPanelVisible(deadMenuPanel, true);
                ShowCursor();
                break;
            case UIState.Finished:
                SetMenuPaused(true);
                SetInputLocked(true);
                SetPanelVisible(finishMenuPanel, true);
                ShowCursor();
                break;
        }
    }

    private void HideMenuPanels()
    {
        SetPanelVisible(MainMenu != null ? MainMenu.gameObject : null, false);
        SetPanelVisible(LevelSelect != null ? LevelSelect.gameObject : null, false);
        SetPanelVisible(abilityPanel, false);
        SetPanelVisible(optionsPanel, false);
        SetPanelVisible(pauseMenuPanel, false);
        SetPanelVisible(deadMenuPanel, false);
        SetPanelVisible(finishMenuPanel, false);
    }

    private void ResetHudTimer()
    {
        gameplayRunTime = 0f;
        lastVictoryRunTime = 0f;
        hasVictorySnapshot = false;
        ResolveHudPanel();
        if (cachedHudPanel != null)
        {
            cachedHudPanel.ResetRunTimer();
        }
    }

    private void NotifyPlayerGameplayResumed()
    {
        ResolvePlayerControl();
        if (cachedPlayerControl != null)
        {
            cachedPlayerControl.NotifyGameplayResumed();
        }
    }

    private void HideAllPanels()
    {
        HideMenuPanels();
        SetPanelVisible(hudPanel, false);
    }

    private void SetPanelVisible(GameObject panel, bool visible)
    {
        if (panel != null)
        {
            panel.SetActive(visible);
        }
    }

    private void BringPanelToFront(GameObject panel)
    {
        if (panel != null)
        {
            panel.transform.SetAsLastSibling();
        }
    }

    private void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void TryBindSceneReferences()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        if (MainMenu == null)
        {
            MainMenu = FindComponentInRoots<MainMenu>(roots, "MainMenuPanel");
        }

        if (LevelSelect == null)
        {
            LevelSelect = FindComponentInRoots<LevelSelect>(roots, "LevelSelectPanel");
        }

        if (abilityPanel == null)
        {
            abilityPanel = FindGameObjectInRoots(roots, "AbilityPanel");
        }

        if (optionsPanel == null)
        {
            optionsPanel = FindGameObjectInRoots(roots, "OptionsPanel");
        }

        if (pauseMenuPanel == null)
        {
            pauseMenuPanel = FindGameObjectInRoots(roots, "PauseMenu");
        }

        if (deadMenuPanel == null)
        {
            deadMenuPanel = FindGameObjectInRoots(roots, "DeathMenu");
        }

        BindFinishMenuPanel(roots);
        BindHudPanel(roots);
        ResolvePlayerControl();
    }

    private void BindFinishMenuPanel(GameObject[] sceneRoots)
    {
        if (finishMenuPanel != null)
        {
            return;
        }

        LevelComplete levelComplete = GetComponentInChildren<LevelComplete>(true);
        if (levelComplete != null)
        {
            finishMenuPanel = levelComplete.gameObject;
            return;
        }

        finishMenuPanel = FindGameObjectInRoots(sceneRoots, "LevelComplete");
        if (finishMenuPanel == null)
        {
            finishMenuPanel = FindGameObjectInRoots(sceneRoots, "FinishMenu");
        }
    }

    private void BindHudPanel(GameObject[] sceneRoots)
    {
        if (hudPanel == null)
        {
            hudPanel = FindGameObjectInRoots(sceneRoots, "HUDPanel");
        }

        cachedHudPanel = null;
        ResolveHudPanel();

        if (cachedHudPanel == null)
        {
            cachedHudPanel = GetComponentInChildren<Panel_HUD>(true);
            if (cachedHudPanel != null)
            {
                hudPanel = cachedHudPanel.gameObject;
            }
        }
    }

    private void ResolveHudPanel()
    {
        if (cachedHudPanel != null)
        {
            return;
        }

        if (hudPanel != null)
        {
            cachedHudPanel = hudPanel.GetComponent<Panel_HUD>();
        }

        if (cachedHudPanel == null)
        {
            cachedHudPanel = GetComponentInChildren<Panel_HUD>(true);
            if (cachedHudPanel != null)
            {
                hudPanel = cachedHudPanel.gameObject;
            }
        }
    }

    private void ResolvePlayerControl()
    {
        if (cachedPlayerControl != null)
        {
            return;
        }

        cachedPlayerControl = Object.FindObjectOfType<playerControl>();
    }

    private static T FindComponentInRoots<T>(GameObject[] roots, string objectName) where T : Component
    {
        GameObject target = FindGameObjectInRoots(roots, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static GameObject FindGameObjectInRoots(GameObject[] roots, string objectName)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindDeepChild(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindDeepChild(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = FindDeepChild(parent.GetChild(i), objectName);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private void EnsureEventSystem()
    {
        if (persistedEventSystem != null)
        {
            if (!persistedEventSystem.gameObject.activeInHierarchy)
            {
                persistedEventSystem.gameObject.SetActive(true);
            }

            persistedEventSystem.enabled = true;
            return;
        }

        EventSystem sceneEventSystem = FindObjectOfType<EventSystem>();
        if (sceneEventSystem != null)
        {
            persistedEventSystem = sceneEventSystem;
        }
        else
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            persistedEventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        DontDestroyOnLoad(persistedEventSystem.gameObject);
    }

    private void CleanupDuplicateEventSystems()
    {
        if (persistedEventSystem == null)
        {
            EnsureEventSystem();
        }

        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] == persistedEventSystem)
            {
                continue;
            }

            Destroy(eventSystems[i].gameObject);
        }
    }

    private void PauseBGM()
    {
        CacheBGM();
        if (cachedBgmAudioSource != null && cachedBgmAudioSource.isPlaying)
            cachedBgmAudioSource.Pause();
    }

    private void ResumeBGM()
    {
        CacheBGM();
        if (cachedBgmAudioSource != null && cachedBgmAudioSource.clip != null)
            cachedBgmAudioSource.UnPause();
    }

    // 缓存当前场景的 BGM 引用（自动查找）
    private void CacheBGM()
    {
        if (cachedBgmController != null)
            return;

        // 优先找 BGMController 组件
        cachedBgmController = FindObjectOfType<BGMController>();
        if (cachedBgmController != null && cachedBgmController.audioSource != null)
        {
            cachedBgmAudioSource = cachedBgmController.audioSource;
            return;
        }

        // 如果没有 BGMController，找名为 "BGM" 的物体的 AudioSource
        GameObject bgmObj = GameObject.Find("BGM");
        if (bgmObj != null)
            cachedBgmAudioSource = bgmObj.GetComponent<AudioSource>();

        // 保底：找场景中第一个正在播放的循环 AudioSource
        if (cachedBgmAudioSource == null)
        {
            AudioSource[] sources = FindObjectsOfType<AudioSource>();
            foreach (var s in sources)
            {
                if (s.loop && s.isPlaying)
                {
                    cachedBgmAudioSource = s;
                    break;
                }
            }
        }
    }
}
