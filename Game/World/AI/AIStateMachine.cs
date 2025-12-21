using Server.Game.Contracts.Actor;
using Server.Game.World.AI;
using Server.Game.World.AI.State;

public enum AIStateType
{
    Idle,
    Chase,
    Attack
}

public class AIStateMachine
{
    public AIStateType CurrentState { get; private set; }
    public List<AIBaseIntent> Intents { get; } = new();

    private readonly Dictionary<AIStateType, AIStateBase> states = new();
    private readonly AIAgent agent;

    public AIStateMachine(AIAgent agent)
    {
        this.agent = agent;
    }

    public void Init(AIStateType initType)
    {
        states[AIStateType.Idle] = new IdleAIState(agent, this);
        states[AIStateType.Chase] = new ChaseAIState(agent, this);
        states[AIStateType.Attack] = new AttackAIState(agent, this);

        CurrentState = initType;
        states[CurrentState].Enter();
    }

    public void Tick(float dt)
    {
        states[CurrentState].Tick(dt);
    }

    public void ChangeState(AIStateType newState)
    {
        if (newState == CurrentState) return;
        Console.WriteLine($"[AIStateMachine] {agent.Entity.Identity.EntityId} : {CurrentState} => {newState}");
        states[CurrentState].Exit();
        CurrentState = newState;
        states[CurrentState].Enter();
    }
}
