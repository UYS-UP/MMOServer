using Server.Game.HFSM;
using Server.Game.HFSM.HStates;
using Server.Game.World.AI.HStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI
{
    public class AIHFSM
    {
        public readonly HStateMachine Machine;
        public readonly AIFsmContext Ctx;
        public readonly AIRootState Root;

        public AIHFSM(AIAgent agent, ICombatContext combat)
        {
            Machine = new HStateMachine();
            
            Ctx = new AIFsmContext(agent, combat);

            // 初始化根状态
            var root = new AIRootState(Ctx, Machine, null);
            Machine.SetRoot(root);
            Machine.Start();
        }

        public void Update(float deltaTime)
        {
            Ctx.ClearIntents(); 
            Machine.Update(deltaTime);
        }
    }
}
