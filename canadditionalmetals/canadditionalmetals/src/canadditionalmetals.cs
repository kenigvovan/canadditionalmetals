using canadditionalmetals.src.be;
using canadditionalmetals.src.Blocks;
using canadditionalmetals.src.Items;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canadditionalmetals.src
{
    public class canadditionalmetals : ModSystem
    {
        public Harmony harmonyInstance;
        public const string harmonyID = "canadditionalmetals.Patches";
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass("CANItemBloom", typeof(Items.CANItemBloom));
            api.RegisterBlockClass("CANBlockBloomery", typeof(Blocks.CANBlockBloomery));
            api.RegisterBlockEntityClass("CANBlockEntityBloomery", typeof(CANBlockEntityBloomery));
            api.RegisterBlockEntityClass("CANBlockEntityBellows", typeof(CANBlockEntityBellows));
            api.RegisterBlockClass("CANBlockBellows", typeof(CANBlockBellows));

            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(ItemIngot).GetMethod("TryPlaceOn"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ItemIngot_TryPlaceOn")));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {

        }

        public override void StartClientSide(ICoreClientAPI api)
        {

        }

    }
}
