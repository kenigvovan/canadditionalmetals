using canadditionalmetals.src.be;
using canadditionalmetals.src.Blocks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
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
            api.RegisterBlockClass("CANBlockBloomery", typeof(Blocks.CANBlockBloomery));
            api.RegisterBlockEntityClass("CANBlockEntityBloomery", typeof(CANBlockEntityBloomery));
            api.RegisterBlockEntityClass("CANBlockEntityBellows", typeof(CANBlockEntityBellows));
            api.RegisterBlockClass("CANBlockBellows", typeof(CANBlockBellows));

            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(ItemIngot).GetMethod("TryPlaceOn"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ItemIngot_TryPlaceOn")));
            
            //harmonyInstance.Patch(typeof(BlockAnvil).GetMethod("OnLoaded"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ItemIngot_TryPlaceOn")));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.ServerRunPhase(EnumServerRunPhase.AssetsFinalize, () =>
            {
                var c = api.ModLoader.GetModSystem<SurvivalCoreSystem>(true).metalsByCode;
                //if (!c.TryGetValue("blacksteel", out var _))
                {
                    var metals = new[]
                       {
                            new MetalPropertyVariant { Code = "blacksteel",   Tier = 5 },
                            new MetalPropertyVariant { Code = "redsteel",     Tier = 6 },
                            new MetalPropertyVariant { Code = "bluesteel",    Tier = 6 },
                            new MetalPropertyVariant { Code = "weakblacksteel", Tier = 5 },
                            new MetalPropertyVariant { Code = "weakredsteel",   Tier = 6 },
                            new MetalPropertyVariant { Code = "weakbluesteel",  Tier = 6 }
                        };

                    foreach (var metal in metals)
                    {
                        c.Add(metal.Code.Path, metal);
                    }
                }
            });
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                var c = api.ModLoader.GetModSystem<SurvivalCoreSystem>(true).metalsByCode;
                //if (!c.TryGetValue("blacksteel", out var _))
                {
                    var metals = new[]
                    {
                            new MetalPropertyVariant { Code = "blacksteel",   Tier = 5 },
                            new MetalPropertyVariant { Code = "redsteel",     Tier = 6 },
                            new MetalPropertyVariant { Code = "bluesteel",    Tier = 6 },
                            new MetalPropertyVariant { Code = "weakblacksteel", Tier = 5 },
                            new MetalPropertyVariant { Code = "weakredsteel",   Tier = 6 },
                            new MetalPropertyVariant { Code = "weakbluesteel",  Tier = 6 }
                    };

                    foreach (var metal in metals)
                    {
                        c.Add(metal.Code.Path, metal);
                    }
                }
            };
        }

    }
}
