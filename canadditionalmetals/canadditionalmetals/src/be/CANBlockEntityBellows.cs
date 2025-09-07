using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using canadditionalmetals.src.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace canadditionalmetals.src.be
{
    public class CANBlockEntityBellows: BlockEntity, ITexPositionSource
    {
        /* static CANBlockEntityBellows()
         {
             BlockEntityQuern.FlourParticles.AddPos.Set(1.0625, 0.0, 1.0625);
             BlockEntityQuern.FlourParticles.AddQuantity = 20f;
             BlockEntityQuern.FlourParticles.MinVelocity.Set(-0.25f, 0f, -0.25f);
             BlockEntityQuern.FlourParticles.AddVelocity.Set(0.5f, 1f, 0.5f);
             BlockEntityQuern.FlourParticles.WithTerrainCollision = true;
             BlockEntityQuern.FlourParticles.ParticleModel = EnumParticleModel.Cube;
             BlockEntityQuern.FlourParticles.LifeLength = 1.5f;
             BlockEntityQuern.FlourParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.4f);
             BlockEntityQuern.FlourDustParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.3f, EnumParticleModel.Quad);
             BlockEntityQuern.FlourDustParticles.AddPos.Set(1.0625, 0.0, 1.0625);
             BlockEntityQuern.FlourDustParticles.AddQuantity = 5f;
             BlockEntityQuern.FlourDustParticles.MinVelocity.Set(-0.05f, 0f, -0.05f);
             BlockEntityQuern.FlourDustParticles.AddVelocity.Set(0.1f, 0.2f, 0.1f);
             BlockEntityQuern.FlourDustParticles.WithTerrainCollision = false;
             BlockEntityQuern.FlourDustParticles.ParticleModel = EnumParticleModel.Quad;
             BlockEntityQuern.FlourDustParticles.LifeLength = 1.5f;
             BlockEntityQuern.FlourDustParticles.SelfPropelled = true;
             BlockEntityQuern.FlourDustParticles.GravityEffect = 0f;
             BlockEntityQuern.FlourDustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 0.4f);
             BlockEntityQuern.FlourDustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
         }*/
        private CANBlockBellows ownBlock;
        public string type = "tinbronze";
        private static Vec3f origin = new Vec3f(0.5f, 0f, 0.5f);
        private float meshangle;
        public Size2i AtlasSize => this.capi.BlockTextureAtlas.Size;
        public virtual float MeshAngle
        {
            get
            {
                return this.meshangle;
            }
            set
            {
                this.meshangle = value;
                this.animRot.Y = value;
            }
        }
        private float rotAngleY;
        private ICoreClientAPI capi;
        private ICoreServerAPI sapi;
        MeshData defaultMesh = null;
        private Vec3f animRot = new Vec3f();
        public Dictionary<string, AssetLocation> tmpAssets = new Dictionary<string, AssetLocation>();
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (tmpAssets.TryGetValue(textureCode, out var assetCode))
                {
                    return this.getOrCreateTexPos(assetCode);
                }

                Dictionary<string, CompositeTexture> dictionary;
                dictionary = new Dictionary<string, CompositeTexture>();
                foreach (var it in this.Block.Textures)
                {
                    dictionary.Add(it.Key, it.Value);
                }
                AssetLocation texturePath = (AssetLocation)null;
                CompositeTexture compositeTexture;
                if (dictionary.TryGetValue(textureCode, out compositeTexture))
                    texturePath = compositeTexture.Baked.BakedName;
                if ((object)texturePath == null && dictionary.TryGetValue("all", out compositeTexture))
                    texturePath = compositeTexture.Baked.BakedName;

                return this.getOrCreateTexPos(texturePath);
            }
        }
        private TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition texPos = this.capi.BlockTextureAtlas[texturePath];
            if (texPos == null)
            {
                IAsset asset = this.capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                if (asset != null)
                {
                    BitmapRef bitmap = asset.ToBitmap(this.capi);
                    this.capi.BlockTextureAtlas.InsertTextureCached(texturePath, (IBitmap)bitmap, out int _, out texPos);
                }
                else
                {
                    this.capi.World.Logger.Warning("For render in block " + this.Block.Code?.ToString() + ", item {0} defined texture {1}, not no such texture found.", "", (object)texturePath);
                }
            }
            return texPos;
        }
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (((byItemStack != null) ? byItemStack.Attributes : null) != null)
            {
                this.type = byItemStack.Attributes.GetString("type", "tinbronze");
                string orient = this.Block.LastCodePart(0);

                if (this.Api.Side == EnumAppSide.Client && this.defaultMesh == null)
                {
                    this.defaultMesh = animUtil.InitializeAnimator("wiring2" + string.Concat(new string[]
                    {
                     orient, type
                    }), Vintagestory.API.Common.Shape.TryGet(this.capi, "canadditionalmetals:shapes/block/bellows.json"), this, this.animRot);                 
                }
            }
        }
        private BlockEntityAnimationUtil animUtil
        {
            get
            {
                return base.GetBehavior<BEBehaviorAnimatable>().animUtil;
            }
        }
        public bool triggerServer = false;
        public float GrindSpeed
        {
            get
            {
                if (this.automated && this.mpc.Network != null)
                {
                    return this.mpc.TrueSpeed;
                }
                return 0f;
            }
        }
        public bool AlreadyBlows { get; set; } = false;
        MeshData bellowsMesh
        {
            get
            {
                Api.ObjectCache.TryGetValue("bellowsmesh-" + Material, out object value);
                return (MeshData)value;
            }
            set { Api.ObjectCache["quernbasemesh-" + Material] = value; }
        }
        public string Material { get; set; }
        public virtual float maxGrindingTime()
        {
            return 4f;
        }
        public virtual string DialogTitle
        {
            get
            {
                return Lang.Get("Quern", Array.Empty<object>());
            }
        }
        public CANBlockEntityBellows()
        {

        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Server)
            {
                this.sapi = api as ICoreServerAPI;
                this.MeshAngle = (BlockFacing.FromCode(base.Block.LastCodePart(0)).HorizontalAngleIndex - 1) * 90;
            }
            else
                this.capi = api as ICoreClientAPI;
            this.ownBlock = (base.Block as CANBlockBellows);
            if (api.Side == EnumAppSide.Client)
            {
                BlockEntityAnimationUtil animUtil = this.animUtil;
                if (animUtil == null)
                {
                    return;
                }
                //var rotatedIndex = (BlockFacing.FromCode(base.Block.LastCodePart(0)).HorizontalAngleIndex - 1) * 90;
                var c2 = base.Block.LastCodePart(0);
                this.MeshAngle = (BlockFacing.FromCode(base.Block.LastCodePart(0)).HorizontalAngleIndex - 1) * 90;
                this.defaultMesh = this.getMesh(this.capi.Tesselator, this.animRot);
                this.animUtil.InitializeAnimator("bellows", null, this, this.animRot);
                this.animUtil.StartAnimation(new AnimationMetaData
                {
                    Animation = "idle",
                    Code = "idle",
                    AnimationSpeed = 1f,
                    EaseInSpeed = 1f,
                    EaseOutSpeed = 1f,
                    Weight = 1f,
                    BlendMode = EnumAnimationBlendMode.Average
                });               
            }
            this.RegisterGameTickListener(new Action<float>(this.Every500ms), 500, 0);
        }
        public MeshData GenMesh(ICoreClientAPI capi, Shape shape = null, ITesselatorAPI tesselator = null, ITexPositionSource textureSource = null, Vec3f rotationDeg = null)
        {
            if (shape == null)
            {
                shape = Vintagestory.API.Common.Shape.TryGet(this.capi, "canadditionalmetals:shapes/block/bellows.json");
            }

            if (shape == null)
            {
                return null;
            }

            tesselator.TesselateShape("blockbellows", shape, out var modeldata, this, this.animRot, 0, 0, 0);
            return modeldata;
        }
        public void updateSoundState(bool nowGrinding)
        {
            if (nowGrinding)
            {
                this.startSound();
                return;
            }
            this.stopSound();
        }
        public void startSound()
        {
            if (this.ambientSound == null)
            {
                ICoreAPI api = this.Api;
                if (api != null && api.Side == EnumAppSide.Client)
                {
                    this.ambientSound = (this.Api as ICoreClientAPI).World.LoadSound(new SoundParams
                    {
                        Location = new AssetLocation("sounds/bellows.ogg"),
                        ShouldLoop = true,
                        Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0.75f
                    });
                    this.ambientSound.Start();
                }
            }
        }
        public void stopSound()
        {
            if (this.ambientSound != null)
            {
                this.ambientSound.Stop();
                this.ambientSound.Dispose();
                this.ambientSound = null;
            }
        }
        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);
            this.mpc = base.GetBehavior<BEBehaviorMPConsumer>();
            if (this.mpc != null)
            {
                this.mpc.OnConnected = delegate
                {
                    this.automated = true;
                };
                this.mpc.OnDisconnected = delegate
                {
                    this.automated = false;
                };
            }
        }
        public void IsGrinding(IPlayer byPlayer)
        {
            this.SetPlayerGrinding(byPlayer, true);
        }
        private void Every500ms(float dt)
        {           
            if(!this.automated || this.mpc.TrueSpeed < 1)
            {
                //this.animUtil.StopAnimation("blow");
            }
        }
        public bool UseBellowsOnce(IPlayer player)
        {
            if (!this.animUtil.activeAnimationsByAnimCode.TryGetValue("blow", out var _) || this.Api.Side == EnumAppSide.Server)
            {
                AnimationMetaData meta = new AnimationMetaData
                {
                    Animation = "blow",
                    Code = "blow",
                    AnimationSpeed = .5f,
                    EaseInSpeed = 5f,
                    EaseOutSpeed = 3f
                };
                this.animUtil.StartAnimation(meta);
                this.Api.World.PlaySoundAt(new AssetLocation("canadditionalmetals:sounds/bellows"), this.Pos.X, this.Pos.InternalY, this.Pos.Z, player, true, 32f, 1f);
                if(this.Api.Side == EnumAppSide.Client)
                {
                    return true;
                }
                else
                {
                    IncreaseTemparature();
                    var c = 3;
                }
            }
            return false;
            //this.MarkDirty(true);
        }
        public void IncreaseTemparature()
        {
            triggerServer = false;
            BlockFacing facing = BlockFacing.FromCode(this.Block.Variant["side"]);
            BlockPos secondPos = this.Pos.AddCopy(facing);
            if (this.Api.World.BlockAccessor.GetBlockEntity(secondPos) is CANBlockEntityBloomery bloomery)
            {
                if(!bloomery.IsBurning)
                {
                    return;
                }
                if(!bloomery.inputSlot.Empty)
                {
                    return;
                }
                var fuelStack = bloomery.fuelCombustibleOpts;
                var maxTemp = fuelStack.BurnTemperature;
                int additionalPerBlow = maxTemp / 25;
                int newMaxTemp = (int)(fuelStack.BurnTemperature * 1.5);

                var newTemp = Math.Min((bloomery.furnaceTemperature + additionalPerBlow), newMaxTemp);
                bloomery.furnaceTemperature = newTemp;

            }
        }
        public void SetPlayerGrinding(IPlayer player, bool playerGrinding)
        {
            if (!this.automated)
            {
               
            }
            this.updateGrindingState();
        }
        private void updateGrindingState()
        {
            ICoreAPI api = this.Api;
            if (((api != null) ? api.World : null) == null)
            {
                return;
            }

            /*if (nowGrinding )
            {
                this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos, new Action(this.OnRetesselated));
                this.updateSoundState(nowGrinding);
                if (this.Api.Side == EnumAppSide.Server)
                {
                    this.MarkDirty(false, null);
                }
            }*/
        }
        internal MeshData getMesh(ITesselatorAPI tesselator, Vec3f rotationDeg = null)
        {
            Dictionary<string, MeshData> lanternMeshes = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshData>>(this.Api, "bellowsBlockMeshes", () => new Dictionary<string, MeshData>());
            MeshData mesh = null;
            CANBlockBellows block = this.Api.World.BlockAccessor.GetBlock(this.Pos) as CANBlockBellows;
            if (block == null)
            {
                return null;
            }
            //lanternMeshes.Clear();
            string orient = block.LastCodePart(0);
            this.tmpAssets["tinbronze"] = new AssetLocation("game:block/metal/sheet/" + this.type + "1.png");
            this.tmpAssets["plain"] = new AssetLocation("canadditionalmetals:block/plain.png");
            this.tmpAssets["inside"] = new AssetLocation("canadditionalmetals:block/inside.png");

            if (lanternMeshes.TryGetValue(string.Concat(new string[]
            {
                orient, type
            }), out mesh))
            {
                return mesh;
            }

            return lanternMeshes[string.Concat(new string[]
            {
                orient, type
            })] = GenMesh(this.Api as ICoreClientAPI, null, tesselator, this, rotationDeg);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.type = tree.GetString("type", "tinbronze");
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);
            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                this.defaultMesh = this.getMesh(this.capi.Tesselator, this.animRot);
                string orient = this.Block.LastCodePart(0);
                animUtil.StopAnimation("idle");
                this.defaultMesh = animUtil.InitializeAnimator("wiring2" + string.Concat(new string[]
                    {
                     orient, type
                    }), Vintagestory.API.Common.Shape.TryGet(this.capi, "canadditionalmetals:shapes/block/bellows.json"), this, this.animRot);
                this.animUtil.StartAnimation(new AnimationMetaData
                {
                    Animation = "idle",
                    Code = "idle",
                    AnimationSpeed = 1f,
                    EaseInSpeed = 1f,
                    EaseOutSpeed = 1f,
                    Weight = 1f,
                    BlendMode = EnumAnimationBlendMode.Average
                });
                // animUtil.InitializeAnimator("wiring2" + key, Vintagestory.API.Common.Shape.TryGet(canjewelry.capi, "canjewelry:shapes/block/wiretable.json"), this, this.animRot);
                this.MarkDirty(true, null);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (base.Block != null)
            {
                tree.SetString("blockCode", base.Block.Code.ToShortString());
            }
            tree.SetString("type", this.type);
            tree.SetFloat("meshAngle", this.MeshAngle);
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.ambientSound != null)
            {
                this.ambientSound.Stop();
                this.ambientSound.Dispose();
            }
        }
        ~CANBlockEntityBellows()
        {
            if (this.ambientSound != null)
            {
                this.ambientSound.Dispose();
            }
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            string part = this.Block.LastCodePart(1);
            if (!base.OnTesselation(mesher, tesselator))
            {
                if (this.defaultMesh == null)
                {
                    this.defaultMesh = this.getMesh(tesselator);
                    if (this.defaultMesh == null)
                    {
                        return false;
                    }
                }
                mesher.AddMeshData(this.defaultMesh.Clone());
            }
            return true;
        }
        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.ambientSound != null)
            {
                this.ambientSound.Stop();
                this.ambientSound.Dispose();
                this.ambientSound = null;
            }
        }
        private static SimpleParticleProperties FlourParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.3f, EnumParticleModel.Quad);
        private static SimpleParticleProperties FlourDustParticles;
        private ILoadedSound ambientSound;
        private bool automated;
        private BEBehaviorMPConsumer mpc;
        private int nowOutputFace;
    }
}
