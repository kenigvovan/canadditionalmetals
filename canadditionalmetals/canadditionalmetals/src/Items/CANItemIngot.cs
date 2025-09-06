using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace canadditionalmetals.src.Items
{
    public class CANItemIngot : ItemIngot, IAnvilWorkable
    {
        bool isBlisterSteel;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            isBlisterSteel = Variant["metal"] == "blistersteel";
        }



        public new string GetMetalType()
        {
            return LastCodePart();
        }

        public new int GetRequiredAnvilTier(ItemStack stack)
        {
            string metalcode = Variant["metal"];
            int tier = 0;

            if (api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(metalcode, out MetalPropertyVariant var))
            {
                tier = var.Tier - 1;
            }

            if (stack.Collectible.Attributes?["requiresAnvilTier"].Exists == true)
            {
                tier = stack.Collectible.Attributes["requiresAnvilTier"].AsInt(tier);
            }

            return tier;
        }


        public new List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
        {
            return api.GetSmithingRecipes()
                .Where(r => r.Ingredient.SatisfiesAsIngredient(stack))
                .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code) // Cannot sort by name, thats language dependent!
                .ToList()
            ;
        }

        public new bool CanWork(ItemStack stack)
        {
            float temperature = stack.Collectible.GetTemperature(api.World, stack);
            float meltingpoint = stack.Collectible.GetMeltingPoint(api.World, null, new DummySlot(stack));

            if (stack.Collectible.Attributes?["workableTemperature"].Exists == true)
            {
                return stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingpoint / 2) <= temperature;
            }

            return temperature >= meltingpoint / 2;
        }

        public new ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
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
        public virtual int VoxelCountForHandbook(ItemStack stack) => 42;

        public static void CreateVoxelsFromIngot(ICoreAPI api, ref byte[,,] voxels, bool isBlisterSteel = false)
        {
            voxels = new byte[16, 6, 16];

            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        voxels[4 + x, y, 6 + z] = (byte)EnumVoxelMaterial.Metal;

                        if (isBlisterSteel)
                        {
                            if (api.World.Rand.NextDouble() < 0.5)
                            {
                                voxels[4 + x, y + 1, 6 + z] = (byte)EnumVoxelMaterial.Metal;
                            }
                            if (api.World.Rand.NextDouble() < 0.5)
                            {
                                voxels[4 + x, y + 1, 6 + z] = (byte)EnumVoxelMaterial.Slag;
                            }
                        }
                    }
                }
            }
        }

        public static int AddVoxelsFromIngot(ref byte[,,] voxels)
        {
            int totalAdded = 0;
            for (int x = 0; x < 7; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    int y = 0;
                    int added = 0;
                    while (y < 6 && added < 2)
                    {
                        if (voxels[4 + x, y, 6 + z] == (byte)EnumVoxelMaterial.Empty)
                        {
                            voxels[4 + x, y, 6 + z] = (byte)EnumVoxelMaterial.Metal;
                            added++;
                            totalAdded++;
                        }

                        y++;
                    }
                }
            }
            return totalAdded;
        }

        public ItemStack GetBaseMaterial(ItemStack stack)
        {
            return stack;
        }

        public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
        {
            return EnumHelveWorkableMode.NotWorkable;
        }
    }
}
