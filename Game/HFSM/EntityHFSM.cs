using Server.Game.Contracts.Server;
using Server.Game.HFSM.HStates;
using Server.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM
{
    public class EntityHFSM
    {
        public readonly HStateMachine Machine = new HStateMachine();
        public readonly EntityFsmContext Ctx;

        private RootState root;

        public EntityHFSM(EntityRuntime entity, ICombatContext combat)
        {
            Ctx = new EntityFsmContext(entity, combat);

            root = new RootState(Ctx, Machine, null);

            Machine.SetRoot(root);
            Machine.Start();
        }

        public void Update(float deltaTime) => Machine.Update(deltaTime);
    }
}
