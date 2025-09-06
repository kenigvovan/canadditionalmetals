using canadditionalmetals.src.be;
using canadditionalmetals.src.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace canadditionalmetals.src
{
    public class canadditionalmetals : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass("CANItemBloom", typeof(Items.CANItemBloom));
            api.RegisterBlockClass("CANBlockBloomery", typeof(Blocks.CANBlockBloomery));
            api.RegisterBlockEntityClass("CANBlockEntityBloomery", typeof(CANBlockEntityBloomery));
            api.RegisterBlockEntityClass("CANBlockEntityBellows", typeof(CANBlockEntityBellows));
            api.RegisterBlockClass("CANBlockBellows", typeof(CANBlockBellows));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {

        }

        public override void StartClientSide(ICoreClientAPI api)
        {

        }

    }
}
