using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;
using canadditionalmetals.src.be;
using Vintagestory.API.Util;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;

namespace canadditionalmetals.src.Blocks
{
    public class CANBlockBellows: Block, ITexPositionSource
    {
        private ITexPositionSource tmpTextureSource;
        private string curType;
        private ITextureAtlasAPI curAtlas;
        public Dictionary<string, AssetLocation> tmpAssets = new Dictionary<string, AssetLocation>();
        public Size2i AtlasSize { get; set; }
        private TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition texPos = curAtlas[texturePath];
            if (texPos == null)
            {
                IAsset asset = (this.api as ICoreClientAPI).Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                if (asset != null)
                {
                    BitmapRef bitmap = asset.ToBitmap((this.api as ICoreClientAPI));
                    (this.api as ICoreClientAPI).BlockTextureAtlas.InsertTextureCached(texturePath, (IBitmap)bitmap, out int _, out texPos);
                }
                else
                {
                    (this.api as ICoreClientAPI).World.Logger.Warning("For render in block " + this.Code?.ToString() + ", item {0} defined texture {1}, not no such texture found.", "", (object)texturePath);
                }
            }
            return texPos;
        }
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return false;
            }
            if (!this.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }
            BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
            horVer[0] = horVer[0].GetCW();
            BlockPos secondPos = blockSel.Position.AddCopy(horVer[0]);
            BlockSelection secondBlockSel = new BlockSelection
            {
                Position = secondPos,
                Face = BlockFacing.UP
            };
            if (!this.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }
            string code = horVer[0].Code;
            world.BlockAccessor.GetBlock(base.CodeWithParts(new string[]
            {
                code
            })).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            return true;
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(this, 1);
            CANBlockEntityBellows be = world.BlockAccessor.GetBlockEntity(pos) as CANBlockEntityBellows;
            if (be != null)
            {
                stack.Attributes.SetString("type", be.type);
            }
            else
            {
                stack.Attributes.SetString("type", "tinbronze");
            }
            return stack;
        }
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (tmpAssets.TryGetValue(textureCode, out var assetCode))
                {
                    return this.getOrCreateTexPos(assetCode);
                }
                TextureAtlasPosition pos = this.tmpTextureSource[this.curType + "-" + textureCode];
                if (pos == null)
                {
                    pos = this.tmpTextureSource[textureCode];
                }
                if (pos == null)
                {
                    pos = (this.api as ICoreClientAPI).BlockTextureAtlas.UnknownTexturePosition;
                }
                return pos;
            }
        }
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack); ;
        }
        /* public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
         {
             bool flag = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
             if (flag && !this.tryConnect(world, byPlayer, blockSel.Position, BlockFacing.UP))
             {
                 this.tryConnect(world, byPlayer, blockSel.Position, BlockFacing.DOWN);
             }
             return flag;
         }*/
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }
            CANBlockEntityBellows beQuern = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CANBlockEntityBellows;
            if (beQuern != null)
            {
                return beQuern.UseBellowsOnce(byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            CANBlockEntityBellows beQuern = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CANBlockEntityBellows;
            if (beQuern != null)
            {
                beQuern.SetPlayerGrinding(byPlayer, false);
            }
        }
        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            CANBlockEntityBellows beQuern = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CANBlockEntityBellows;
            if (beQuern != null)
            {
                //beQuern.UseBellowsOnce(byPlayer);
                //beQuern.SetPlayerGrinding(byPlayer, false);
            }
            return true;
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            AddAllTypesToCreativeInventory();
        }
        private JsonItemStack genJstack(string json)
        {
            JsonItemStack jsonItemStack = new JsonItemStack();
            jsonItemStack.Code = this.Code;
            jsonItemStack.Type = EnumItemClass.Block;
            jsonItemStack.Attributes = new JsonObject(JToken.Parse(json));
            jsonItemStack.Resolve(this.api.World, "can wire drawing bench type", true);
            return jsonItemStack;
        }
        public void AddAllTypesToCreativeInventory()
        {
            List<JsonItemStack> stacks = new List<JsonItemStack>();
            Dictionary<string, string[]> vg = this.Attributes["variantGroups"].AsObject<Dictionary<string, string[]>>(null);
            Random r = new Random();

            string[] woodType = vg["metalType"];
            foreach (string loop in woodType)
            {
                stacks.Add(this.genJstack(string.Format("{{ type: \"{0}\" }}", loop)));
            }
            this.CreativeInventoryStacks = new CreativeTabAndStackList[]
            {
                new CreativeTabAndStackList
                {
                    Stacks = stacks.ToArray(),
                    Tabs = new string[]
                    {
                        "general",
                        "decorative"
                    }
                }
            };
        }
        public MeshData GenMesh(ICoreClientAPI capi, Shape shape = null, ITesselatorAPI tesselator = null, ITexPositionSource textureSource = null, Vec3f rotationDeg = null)
        {
            if (tesselator == null)
            {
                tesselator = capi.Tesselator;
            }
            curAtlas = capi.BlockTextureAtlas;
            if (textureSource != null)
            {
                tmpTextureSource = textureSource;
            }
            else
            {
                tmpTextureSource = tesselator.GetTextureSource(this);
            }
            if (shape == null)
            {
                shape = Vintagestory.API.Common.Shape.TryGet(capi, "canadditionalmetals:shapes/block/bellows.json");
            }

            if (shape == null)
            {
                return null;
            }

            AtlasSize = capi.BlockTextureAtlas.Size;
            //var f = (BlockFacing.FromCode(base.LastCodePart(0)).HorizontalAngleIndex - 1) * 90;
            tesselator.TesselateShape("blocklantern", shape, out var modeldata, this, rotationDeg, 0, 0, 0);
            return modeldata;
            /*if (tesselator == null)
            {
                tesselator = capi.Tesselator;
            }
            curAtlas = capi.BlockTextureAtlas;
            if (textureSource != null)
            {
                tmpTextureSource = textureSource;
            }
            else
            {
                tmpTextureSource = tesselator.GetTextureSource(this);
            }
            Shape shape = this.GetShape(capi, type, cshape);

            curAtlas = capi.BlockTextureAtlas;
            this.tmpAssets["tinbronze"] = new AssetLocation("game:block/metal/sheet/" + this.curType + "1.png");
            this.tmpAssets["plain"] = new AssetLocation("canadditionalmetals:block/plain.png");
            this.tmpAssets["inside"] = new AssetLocation("canadditionalmetals:block/inside.png");
            
            if (shape == null)
            {
                return new MeshData(true);
            }
            this.curType = type;
            MeshData mesh;
            //string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f meshRotationDeg = null, int generalGlowLevel = 0, byte climateColorMapId = 0, byte seasonColorMapId = 0, int? quantityElements = null, string[] selectiveElements = null
            tesselator.TesselateShape("bellows", shape, out mesh, (ITexPositionSource)this, (rotation == null) 
                                                                            ? new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ) 
                                                                            : rotation);


            return mesh;*/
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string cacheKey = "bellowsMeshRefs" + base.FirstCodePart(0);
            Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MultiTextureMeshRef>>(capi, cacheKey, () => new Dictionary<string, MultiTextureMeshRef>());
            string woodType = itemstack.Attributes.GetString("type", "tinbronze");
            this.tmpAssets["tinbronze"] = new AssetLocation("game:block/metal/sheet/" + woodType + "1.png");
            this.tmpAssets["plain"] = new AssetLocation("canadditionalmetals:block/plain.png");
            this.tmpAssets["inside"] = new AssetLocation("canadditionalmetals:block/inside.png");
            string type = itemstack.Attributes.GetString("type", "tinbronze");
            string key = string.Concat(new string[]
            {
                type
            });
            if (!meshrefs.TryGetValue(key, out renderinfo.ModelRef))
            {
                AssetLocation shapeloc = new AssetLocation("canadditionalmetals:shapes/block/bellows.json");
                Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
                Vec3f rot = (this.ShapeInventory == null) ? null : new Vec3f(this.ShapeInventory.rotateX, this.ShapeInventory.rotateY, this.ShapeInventory.rotateZ);

                MeshData mesh = this.GenMesh(capi, shape, null, this);
                meshrefs[key] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(mesh));
            }
        }
        public Shape GetShape(ICoreClientAPI capi, string type, CompositeShape cshape)
        {
            if (((cshape != null) ? cshape.Base : null) == null)
            {
                return null;
            }
            ITesselatorAPI tesselator = capi.Tesselator;
            this.tmpTextureSource = tesselator.GetTextureSource(this, 0, true);
            AssetLocation shapeloc = cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            Shape result = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
            this.curType = type;
            return result;
        }
        /* public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
         {
         }
         public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
         {
             BlockFacing ownFacing = BlockFacing.FromCode(this.Variant["side"]);
             BlockFacing leftFacing = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(ownFacing.HorizontalAngleIndex - 1, 4)];
             BlockFacing rightFacing = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(ownFacing.HorizontalAngleIndex + 1, 4)];
             return face == leftFacing || face == rightFacing;
         }*/
    }
}
