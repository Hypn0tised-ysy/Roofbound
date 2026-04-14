using System.Collections.Generic;

/// <summary>
/// 状态驱动协调器：统一调用状态节点的 Enter/Tick/Exit 生命周期。
/// </summary>
public sealed class PlayerLocomotionStateDriver
{
    private readonly Dictionary<PlayerLocomotionState, IPlayerLocomotionStateNode> stateNodes
        = new Dictionary<PlayerLocomotionState, IPlayerLocomotionStateNode>();

    public PlayerLocomotionState CurrentState { get; private set; }
    public bool IsInitialized { get; private set; }

    public void Register(PlayerLocomotionState state, IPlayerLocomotionStateNode stateNode)
    {
        stateNodes[state] = stateNode;
    }

    public void Initialize(PlayerLocomotionState initialState, PlayerLocomotionFrameContext context)
    {
        CurrentState = initialState;
        IsInitialized = true;

        if (stateNodes.TryGetValue(CurrentState, out IPlayerLocomotionStateNode node))
        {
            node.OnEnter(context);
        }
    }

    public void ChangeState(PlayerLocomotionState nextState, PlayerLocomotionFrameContext context)
    {
        if (!IsInitialized)
        {
            Initialize(nextState, context);
            return;
        }

        if (CurrentState == nextState)
        {
            return;
        }

        if (stateNodes.TryGetValue(CurrentState, out IPlayerLocomotionStateNode currentNode))
        {
            currentNode.OnExit(context);
        }

        CurrentState = nextState;

        if (stateNodes.TryGetValue(CurrentState, out IPlayerLocomotionStateNode nextNode))
        {
            nextNode.OnEnter(context);
        }
    }

    public void Tick(PlayerLocomotionFrameContext context, float deltaTime)
    {
        if (!IsInitialized)
        {
            return;
        }

        if (stateNodes.TryGetValue(CurrentState, out IPlayerLocomotionStateNode node))
        {
            node.Tick(context, deltaTime);
        }
    }

    public static PlayerLocomotionStateDriver CreateDefaultNoopDriver()
    {
        PlayerLocomotionStateDriver driver = new PlayerLocomotionStateDriver();
        IPlayerLocomotionStateNode noopNode = new PlayerNoopLocomotionStateNode();

        driver.Register(PlayerLocomotionState.Grounded, noopNode);
        driver.Register(PlayerLocomotionState.OnPlatform, noopNode);
        driver.Register(PlayerLocomotionState.Airborne, noopNode);
        driver.Register(PlayerLocomotionState.PostSprint, noopNode);

        return driver;
    }
}

/// <summary>
/// 默认空状态节点：当前阶段仅建立状态驱动入口，不改变已有行为。
/// </summary>
public sealed class PlayerNoopLocomotionStateNode : IPlayerLocomotionStateNode
{
    public void OnEnter(PlayerLocomotionFrameContext context)
    {
    }

    public void Tick(PlayerLocomotionFrameContext context, float deltaTime)
    {
    }

    public void OnExit(PlayerLocomotionFrameContext context)
    {
    }
}
