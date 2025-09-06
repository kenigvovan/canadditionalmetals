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
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace canadditionalmetals.src.be
{
    public class CANBlockEntityBellows: BlockEntity
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
        public virtual float MeshAngle
        {
            get
            {
                return this.rotAngleY;
            }
            set
            {
                this.rotAngleY = value;
            }
        }
        private float rotAngleY;
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (((byItemStack != null) ? byItemStack.Attributes : null) != null)
            {
                string nowType = byItemStack.Attributes.GetString("type", "tinbronze");
                if (nowType != this.type)
                {
                    this.type = nowType;
                    this.MarkDirty(false, null);
                }
            }
            base.OnBlockPlaced(null);
        }
        private BlockEntityAnimationUtil animUtil
        {
            get
            {
                return base.GetBehavior<BEBehaviorAnimatable>().animUtil;
            }
        }
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
            this.ownBlock = (base.Block as CANBlockBellows);
            if (api.Side == EnumAppSide.Client)
            {
                if (bellowsMesh == null)
                {
                    bellowsMesh = GenMesh();
                }
                this.animUtil.InitializeAnimator("bellows", null, this.ownBlock, new Vec3f(0f, 0f, 0f));
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
                        Location = new AssetLocation("sounds/block/quern.ogg"),
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
        public void UseBellowsOnce(IPlayer player)
        {
            AnimationMetaData meta = new AnimationMetaData
            {
                Animation = "blow",
                Code = "blow",
                AnimationSpeed = .8f,
                EaseInSpeed = 5f,
                EaseOutSpeed = 3f
            };
            this.animUtil.StartAnimation(meta);
            //this.MarkDirty(true);
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
        internal MeshData GenMesh()
        {
            CANBlockBellows block = base.Block as CANBlockBellows;
            if (base.Block == null)
            {
                block = (this.Api.World.BlockAccessor.GetBlock(this.Pos) as CANBlockBellows);
                base.Block = block;
            }
            if (block == null)
            {
                return null;
            }
            string cacheKey = "bellowsMeshes" + block.FirstCodePart(0);
            Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshData>>(this.Api, cacheKey, () => new Dictionary<string, MeshData>());
            CompositeShape cshape = this.ownBlock.Shape;
            if (((cshape != null) ? cshape.Base : null) == null)
            {
                return null;
            }
            string meshKey = string.Concat(new string[]
            {
                this.type
            });
            MeshData mesh;
            if (!meshes.TryGetValue(meshKey, out mesh))
            {
                mesh = block.GenMesh(this.Api as ICoreClientAPI, this.type, cshape, new Vec3f(cshape.rotateX, cshape.rotateY, cshape.rotateZ));
                meshes[meshKey] = mesh;
            }
            return mesh;
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.type = tree.GetString("type", "tinbronze");
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);
            if (this.Api != null && this.Api.Side == EnumAppSide.Client)
            {
                this.GenMesh();
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
            if (base.Block == null)
            {
                return false;
            }
            mesher.AddMeshData(this.bellowsMesh, 1);
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
