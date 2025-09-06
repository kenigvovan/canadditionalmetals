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

namespace canadditionalmetals.src.Blocks
{
    public class CANBlockBellows: Block, ITexPositionSource
    {
        private ITexPositionSource tmpTextureSource;
        private string curType;
        private ITextureAtlasAPI curAtlas;
        public Dictionary<string, AssetLocation> tmpAssets = new Dictionary<string, AssetLocation>();
        public Size2i AtlasSize
        {
            get
            {
                return this.tmpTextureSource.AtlasSize;
            }
        }
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
            bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (flag)
            {
                CANBlockEntityBellows bect = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CANBlockEntityBellows;
                if (bect != null)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
                    double dz = (double)((float)byPlayer.Entity.Pos.Z) - ((double)targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(y, dz);
                    string type = bect.type;
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
                beQuern.UseBellowsOnce(byPlayer);
                return true;
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
                beQuern.UseBellowsOnce(byPlayer);
                //beQuern.SetPlayerGrinding(byPlayer, false);
            }
            return true;
        }
        public MeshData GenMesh(ICoreClientAPI capi, string type, CompositeShape cshape, Vec3f rotation = null)
        {
            Shape shape = this.GetShape(capi, type, cshape);
            ITesselatorAPI tesselator = capi.Tesselator;
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


            return mesh;
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string cacheKey = "bellowsMeshRefs" + base.FirstCodePart(0);
            Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MultiTextureMeshRef>>(capi, cacheKey, () => new Dictionary<string, MultiTextureMeshRef>());
            this.tmpAssets["tinbronze"] = new AssetLocation("game:block/metal/sheet/" + this.curType + "1.png");
            this.tmpAssets["plain"] = new AssetLocation("canadditionalmetals:block/plain.png");
            this.tmpAssets["inside"] = new AssetLocation("canadditionalmetals:block/inside.png");
            string type = itemstack.Attributes.GetString("type", "tinbronze");
            string key = string.Concat(new string[]
            {
                type
            });
            if (!meshrefs.TryGetValue(key, out renderinfo.ModelRef))
            {
                CompositeShape cshape = this.Shape;
                Vec3f rot = (this.ShapeInventory == null) ? null : new Vec3f(this.ShapeInventory.rotateX, this.ShapeInventory.rotateY, this.ShapeInventory.rotateZ);

                MeshData mesh = this.GenMesh(capi, type, cshape, rot);
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
