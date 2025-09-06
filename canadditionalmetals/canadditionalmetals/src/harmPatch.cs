using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canadditionalmetals.src
{
    [HarmonyPatch]
    public class harmPatch
    {
        public static bool Prefix_ItemIngot_TryPlaceOn(ItemIngot __instance, ItemStack stack, BlockEntityAnvil beAnvil, ICoreAPI ___api, ref ItemStack __result)
        {
            if(stack == null || !stack.Collectible.Code.Path.StartsWith("ingot-weak"))
            {
                return true;
            }
            if (beAnvil.WorkItemStack != null)
            {
                __result = null;
                return false;
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
                    CreateVoxelsFromIronBloom(__instance, ref beAnvil.Voxels, ___api);
                    goto IL_006C;
                }
            }
            CreateVoxelsFromIronBloom(__instance, ref beAnvil.Voxels, ___api);
        IL_006C:
            ItemStack workItemStack = stack.Clone();
            workItemStack.StackSize = 1;
            workItemStack.Collectible.SetTemperature(___api.World, workItemStack, stack.Collectible.GetTemperature(___api.World, stack), true);
            __result = workItemStack.Clone();
            return false;
        }
        private static void CreateVoxelsFromIronBloom(ItemIngot __instance, ref byte[,,] voxels, ICoreAPI api)
        {
            ItemIngot.CreateVoxelsFromIngot(api, ref voxels, false);
            Random rand = api.World.Rand;
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
    }
}
