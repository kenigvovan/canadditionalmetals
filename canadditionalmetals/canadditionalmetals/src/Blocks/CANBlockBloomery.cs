using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using canadditionalmetals.src.be;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canadditionalmetals.src.Blocks
{
    public class CANBlockBloomery : Block, IIgnitable, ISmokeEmitter
    {
        public bool IsExtinct;

        AdvancedParticleProperties[] ringParticles;
        Vec3f[] basePos;
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            IsExtinct = LastCodePart() != "lit";

            if (!IsExtinct && api.Side == EnumAppSide.Client)
            {
                ringParticles = new AdvancedParticleProperties[this.ParticleProperties.Length * 4];
                basePos = new Vec3f[ringParticles.Length];

                Cuboidf[] spawnBoxes = new Cuboidf[]
                {
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.3125f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.7125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.3125f),
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.7125f, x2: 0.875f, y2: 0.5f, z2: 0.875f)
                };

                for (int i = 0; i < ParticleProperties.Length; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        AdvancedParticleProperties props = ParticleProperties[i].Clone();

                        Cuboidf box = spawnBoxes[j];
                        basePos[i * 4 + j] = new Vec3f(0, 0, 0);

                        props.PosOffset[0].avg = box.MidX;
                        props.PosOffset[0].var = box.Width / 2;

                        props.PosOffset[1].avg = 0.1f;
                        props.PosOffset[1].var = 0.05f;

                        props.PosOffset[2].avg = box.MidZ;
                        props.PosOffset[2].var = box.Length / 2;

                        props.Quantity.avg /= 4f;
                        props.Quantity.var /= 4f;

                        ringParticles[i * 4 + j] = props;
                    }
                }
            }


            interactions = ObjectCacheUtil.GetOrCreate(api, "firepitInteractions", () =>
            {
                List<ItemStack> canIgniteStacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, true);

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-ignite",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = canIgniteStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            CANBlockEntityBloomery bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as CANBlockEntityBloomery;
                            if (bef?.fuelSlot != null && !bef.fuelSlot.Empty && !bef.IsBurning)
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-refuel",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift"
                    }
                };
            });
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (flag)
            {
                CANBlockEntityBloomery bect = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CANBlockEntityBloomery;
                if (bect != null)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
                    double dz = (double)((float)byPlayer.Entity.Pos.Z) - ((double)targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(y, dz);

                    string rotatatableInterval = "22.5degnot45deg";
                    if (rotatatableInterval == "22.5degnot45deg")
                    {
                        float rounded90degRad = (float)((int)Math.Round((double)(angleHor / 1.5707964f))) * 1.5707964f;
                        float deg45rad = 0.3926991f;
                        if (Math.Abs(angleHor - rounded90degRad) >= deg45rad)
                        {
                            bect.MeshAngle = rounded90degRad + 0.3926991f * (float)Math.Sign(angleHor - rounded90degRad);
                        }
                        else
                        {
                            bect.MeshAngle = rounded90degRad;
                        }
                    }
                    if (rotatatableInterval == "22.5deg")
                    {
                        float deg22dot5rad = 0.3926991f;
                        float roundRad = (float)((int)Math.Round((double)(angleHor / deg22dot5rad))) * deg22dot5rad;
                        bect.MeshAngle = roundRad;
                    }
                }
            }
            return flag;
        }
        public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
        {
            if (world.Rand.NextDouble() < 0.05 && GetBlockEntity<CANBlockEntityBloomery>(pos)?.IsBurning == true)
            {
                entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, SourceBlock = this, Type = EnumDamageType.Fire, SourcePos = pos.ToVec3d() }, 0.5f);
            }

            base.OnEntityInside(world, entity, pos);
        }


        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            CANBlockEntityBloomery bef = api.World.BlockAccessor.GetBlockEntity(pos) as CANBlockEntityBloomery;
            if (bef.IsBurning) return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
            return EnumIgniteState.NotIgnitable;
        }
        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            CANBlockEntityBloomery bef = api.World.BlockAccessor.GetBlockEntity(pos) as CANBlockEntityBloomery;
            if (bef == null) return EnumIgniteState.NotIgnitable;
            return bef.GetIgnitableState(secondsIgniting);
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            CANBlockEntityBloomery bef = api.World.BlockAccessor.GetBlockEntity(pos) as CANBlockEntityBloomery;
            if (bef != null && !bef.canIgniteFuel)
            {
                bef.canIgniteFuel = true;
                bef.extinguishedTotalHours = api.World.Calendar.TotalHours;
            }

            handling = EnumHandling.PreventDefault;
        }


        public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
        {
            bool val = base.ShouldReceiveClientParticleTicks(world, player, pos, out _);
            isWindAffected = true;

            return val;
        }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (IsExtinct)
            {
                base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
                return;
            }

            CANBlockEntityBloomery bef = manager.BlockAccess.GetBlockEntity(pos) as CANBlockEntityBloomery;
            if (bef != null && bef.CurrentModel == EnumFirepitModel.Wide)
            {
                for (int i = 0; i < ringParticles.Length; i++)
                {
                    AdvancedParticleProperties bps = ringParticles[i];
                    bps.WindAffectednesAtPos = windAffectednessAtPos;
                    bps.basePos.X = pos.X + basePos[i].X;
                    bps.basePos.Y = pos.InternalY + basePos[i].Y;
                    bps.basePos.Z = pos.Z + basePos[i].Z;

                    manager.Spawn(bps);
                }

                return;
            }

            base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }
            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

            CANBlockEntityBloomery bef = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CANBlockEntityBloomery;

            if (bef != null && stack?.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>() && bef.GetIgnitableState(0) == EnumIgniteState.Ignitable)
            {
                return false;
            }

            if (bef != null && stack != null)
            {
                bool activated = false;

                if (byPlayer.Entity.Controls.ShiftKey)
                {
                    if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.MeltingPoint > 0)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, 1);
                        byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.inputSlot, ref op);
                        if (op.MovedQuantity > 0) activated = true;
                    }

                    if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, 1);
                        byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.fuelSlot, ref op);
                        if (op.MovedQuantity > 0) activated = true;
                    }
                }

                if (stack.Collectible.Attributes?.IsTrue("mealContainer") == true && !activated)
                {
                    ItemSlot potSlot = null;
                    if (bef.inputStack?.Collectible is BlockCookedContainer)
                    {
                        potSlot = bef.inputSlot;
                    }
                    if (bef.outputStack?.Collectible is BlockCookedContainer)
                    {
                        potSlot = bef.outputSlot;
                    }

                    if (potSlot != null)
                    {
                        BlockCookedContainer blockPot = potSlot.Itemstack.Collectible as BlockCookedContainer;
                        ItemSlot targetSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                        if (byPlayer.InventoryManager.ActiveHotbarSlot.StackSize > 1)
                        {
                            targetSlot = new DummySlot(targetSlot.TakeOut(1));
                            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                            blockPot.ServeIntoStack(targetSlot, potSlot, world);
                            if (!byPlayer.InventoryManager.TryGiveItemstack(targetSlot.Itemstack, true))
                            {
                                world.SpawnItemEntity(targetSlot.Itemstack, byPlayer.Entity.ServerPos.XYZ);
                            }
                        }
                        else blockPot.ServeIntoStack(targetSlot, potSlot, world);
                    }
                    else if (!bef.inputSlot.Empty || byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(api.World, bef.inputSlot, 1) == 0)
                    {
                        bef.OnPlayerRightClick(byPlayer, blockSel);
                    }

                    activated = true;
                }

                if (stack?.Collectible is BlockSmeltingContainer or BlockSmeltedContainer && !activated)
                {
                    if (byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(api.World, bef.inputSlot, 1) > 0) activated = true;
                }

                if (activated)
                {
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                    var loc = stack.ItemAttributes?["placeSound"].Exists == true ? AssetLocation.Create(stack.ItemAttributes["placeSound"].AsString(), stack.Collectible.Code.Domain) : null;

                    if (loc != null)
                    {
                        api.World.PlaySoundAt(loc.WithPathPrefixOnce("sounds/"), blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, byPlayer, 0.88f + (float)api.World.Rand.NextDouble() * 0.24f, 16);
                    }

                    return true;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public static bool IsFirewoodPile(IWorldAccessor world, BlockPos pos)
        {
            var beg = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos);
            return beg != null && beg.Inventory[0]?.Itemstack?.Collectible is ItemFirewood;
        }

        public static int GetFireWoodQuanity(IWorldAccessor world, BlockPos pos)
        {
            var beg = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos);
            return beg?.Inventory[0]?.StackSize ?? 0;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
        {
            if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
            {
                return GetBlockEntity<CANBlockEntityBloomery>(pos)?.IsBurning == true ? 10000f : 1f;
            }

            return base.GetTraversalCost(pos, creatureType);
        }

        public bool EmitsSmoke(BlockPos pos)
        {
            var befirepit = api.World.BlockAccessor.GetBlockEntity(pos) as CANBlockEntityBloomery;
            return befirepit?.IsBurning == true;
        }
    }
}
