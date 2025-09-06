using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace canadditionalmetals.src.Items
{
    public class CANItemBloom : Item, IAnvilWorkable
    {
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }
        public override string GetHeldItemName(ItemStack itemStack)
        {
            if (itemStack.Attributes.HasAttribute("voxels"))
            {
                return Lang.Get("Partially worked black steel bloom", Array.Empty<object>());
            }
            return base.GetHeldItemName(itemStack);
        }
        public MeshData GenMesh(ICoreClientAPI capi, ItemStack stack)
        {
            return null;
        }
        public int GetWorkItemHashCode(ItemStack stack)
        {
            return stack.Attributes.GetHashCode();
        }
        public int GetRequiredAnvilTier(ItemStack stack)
        {
            return 2;
        }
        public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
        {
            return (from r in this.api.GetSmithingRecipes()
                    where r.Ingredient.SatisfiesAsIngredient(stack, true)
                    orderby r.Output.ResolvedItemstack.Collectible.Code
                    select r).ToList<SmithingRecipe>();
        }
        public bool CanWork(ItemStack stack)
        {
            float temperature = stack.Collectible.GetTemperature(this.api.World, stack);
            float meltingpoint = stack.Collectible.GetMeltingPoint(this.api.World, null, new DummySlot(stack));
            JsonObject attributes = stack.Collectible.Attributes;
            if (attributes != null && attributes["workableTemperature"].Exists)
            {
                return stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingpoint / 2f) <= temperature;
            }
            return temperature >= meltingpoint / 2f;
        }
        public ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
        {
            if (beAnvil.WorkItemStack != null)
            {
                return null;
            }
            if (stack.Attributes.HasAttribute("voxels"))
            {
                try
                {
                    beAnvil.Voxels = BlockEntityAnvil.deserializeVoxels(stack.Attributes.GetBytes("voxels", null));
                    beAnvil.SelectedRecipeId = stack.Attributes.GetInt("selectedRecipeId", 0);
                    goto IL_006C;
                }
                catch (Exception)
                {
                    this.CreateVoxelsFromIronBloom(ref beAnvil.Voxels);
                    goto IL_006C;
                }
            }
            this.CreateVoxelsFromIronBloom(ref beAnvil.Voxels);
        IL_006C:
            ItemStack workItemStack = stack.Clone();
            workItemStack.StackSize = 1;
            workItemStack.Collectible.SetTemperature(this.api.World, workItemStack, stack.Collectible.GetTemperature(this.api.World, stack), true);
            return workItemStack.Clone();
        }
        public virtual int VoxelCountForHandbook(ItemStack stack)
        {
            return 42;
        }
        private void CreateVoxelsFromIronBloom(ref byte[,,] voxels)
        {
            ItemIngot.CreateVoxelsFromIngot(this.api, ref voxels, false);
            Random rand = this.api.World.Rand;
            for (int dx = -1; dx < 8; dx++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int dz = -1; dz < 5; dz++)
                    {
                        int x = 4 + dx;
                        int z = 6 + dz;
                        if (y != 0 || voxels[x, y, z] != 1)
                        {
                            float dist = (float)(Math.Max(0, Math.Abs(x - 7) - 1) + Math.Max(0, Math.Abs(z - 8) - 1)) + Math.Max(0f, (float)y - 1f);
                            if (rand.NextDouble() >= (double)(dist / 3f - 0.4f + ((float)y - 1.5f) / 4f))
                            {
                                if (rand.NextDouble() > (double)(dist / 2f))
                                {
                                    voxels[x, y, z] = 1;
                                }
                                else
                                {
                                    voxels[x, y, z] = 2;
                                }
                            }
                        }
                    }
                }
            }
        }
        public ItemStack GetBaseMaterial(ItemStack stack)
        {
            return stack;
        }
        public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
        {
            return EnumHelveWorkableMode.FullyWorkable;
        }
    }
}
