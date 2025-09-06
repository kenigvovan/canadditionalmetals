using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using canadditionalmetals.src.Guis;
using canadditionalmetals.src.Inventories;
using canadditionalmetals.src.render;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace canadditionalmetals.src.be
{
    public class CANBlockEntityBloomery: BlockEntityOpenableContainer, IHeatSource, IFirePit, ITemperatureSensitive
    {
        private float rotAngleY;
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
        private static Vec3f origin = new Vec3f(0.5f, 0f, 0.5f);
        public bool IsHot
        {
            get
            {
                return this.IsBurning;
            }
        }
        public virtual bool BurnsAllFuell
        {
            get
            {
                return true;
            }
        }
        public virtual float HeatModifier
        {
            get
            {
                return 1f;
            }
        }
        public virtual float BurnDurationModifier
        {
            get
            {
                return 1f;
            }
        }
        public virtual int enviromentTemperature()
        {
            return 20;
        }
        public virtual float maxCookingTime()
        {
            if (this.inputSlot.Itemstack != null)
            {
                return this.inputSlot.Itemstack.Collectible.GetMeltingDuration(this.Api.World, this.inventory, this.inputSlot);
            }
            return 30f;
        }
        public override string InventoryClassName
        {
            get
            {
                return "stove";
            }
        }
        public virtual string DialogTitle
        {
            get
            {
                return Lang.Get("Firepit", Array.Empty<object>());
            }
        }
        public override InventoryBase Inventory
        {
            get
            {
                return this.inventory;
            }
        }
        public CANBlockEntityBloomery()
        {
            this.inventory = new CANInventorySmelting(null, null);
            this.inventory.SlotModified += this.OnSlotModifid;
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.inventory.pos = this.Pos;
            this.inventory.LateInitialize(string.Concat(new string[]
            {
                "smelting-",
                this.Pos.X.ToString(),
                "/",
                this.Pos.Y.ToString(),
                "/",
                this.Pos.Z.ToString()
            }), api);
            this.RegisterGameTickListener(new Action<float>(this.OnBurnTick), 100, 0);
            this.RegisterGameTickListener(new Action<float>(this.On500msTick), 500, 0);
            if (api is ICoreClientAPI)
            {
                this.renderer = new CANBloomeryContentsRenderer(api as ICoreClientAPI, this.Pos);
                (api as ICoreClientAPI).Event.RegisterRenderer(this.renderer, EnumRenderStage.Opaque, "firepit");
                this.UpdateRenderer();
            }
        }
        private void OnSlotModifid(int slotid)
        {
            base.Block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
            this.UpdateRenderer();
            this.MarkDirty(this.Api.Side == EnumAppSide.Server, null);
            this.shouldRedraw = true;
            if (this.Api is ICoreClientAPI && this.clientDialog != null)
            {
                this.SetDialogValues(this.clientDialog.Attributes);
            }
            IWorldChunk chunkAtBlockPos = this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos);
            if (chunkAtBlockPos == null)
            {
                return;
            }
            chunkAtBlockPos.MarkModified();
        }
        public bool IsSmoldering
        {
            get
            {
                return this.canIgniteFuel;
            }
        }
        public bool IsBurning
        {
            get
            {
                return this.fuelBurnTime > 0f;
            }
        }
        private void On500msTick(float dt)
        {
            if (this.Api is ICoreServerAPI && (this.IsBurning || this.prevFurnaceTemperature != this.furnaceTemperature))
            {
                this.MarkDirty(false, null);
            }
            this.prevFurnaceTemperature = this.furnaceTemperature;
        }
        private void OnBurnTick(float dt)
        {
            if (base.Block.Code.Path.Contains("construct"))
            {
                return;
            }
            if (!(this.Api is ICoreClientAPI))
            {
                if (this.fuelBurnTime > 0f)
                {
                    bool lowFuelConsumption = Math.Abs(this.furnaceTemperature - (float)this.maxTemperature) < 50f && this.inputSlot.Empty;
                    this.fuelBurnTime -= dt / (lowFuelConsumption ? this.emptyFirepitBurnTimeMulBonus : 1f);
                    if (this.fuelBurnTime <= 0f)
                    {
                        this.fuelBurnTime = 0f;
                        this.maxFuelBurnTime = 0f;
                        if (!this.canSmelt())
                        {
                            this.setBlockState("extinct");
                            this.extinguishedTotalHours = this.Api.World.Calendar.TotalHours;
                        }
                    }
                }
                if (!this.IsBurning && base.Block.Variant["burnstate"] == "extinct" && this.Api.World.Calendar.TotalHours - this.extinguishedTotalHours > 2.0)
                {
                    this.canIgniteFuel = false;
                    this.setBlockState("cold");
                }
                if (this.IsBurning)
                {
                    this.furnaceTemperature = this.changeTemperature(this.furnaceTemperature, (float)this.maxTemperature, dt);
                }
                if (this.canHeatInput())
                {
                    this.heatInput(dt);
                }
                else
                {
                    this.inputStackCookingTime = 0f;
                }
                if (this.canHeatOutput())
                {
                    this.heatOutput(dt);
                }
                if (this.canSmeltInput() && this.inputStackCookingTime > this.maxCookingTime())
                {
                    this.smeltItems();
                }
                if (!this.IsBurning && this.canIgniteFuel && this.canSmelt())
                {
                    this.igniteFuel();
                }
                if (!this.IsBurning)
                {
                    this.furnaceTemperature = this.changeTemperature(this.furnaceTemperature, (float)this.enviromentTemperature(), dt);
                }
                return;
            }
            CANBloomeryContentsRenderer firepitContentsRenderer = this.renderer;
            if (firepitContentsRenderer == null)
            {
                return;
            }
            IInBloomeryRenderer contentStackRenderer = firepitContentsRenderer.contentStackRenderer;
            if (contentStackRenderer == null)
            {
                return;
            }
            contentStackRenderer.OnUpdate(this.InputStackTemp);
        }
        public EnumIgniteState GetIgnitableState(float secondsIgniting)
        {
            if (this.fuelSlot.Empty)
            {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }
            if (this.IsBurning)
            {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }
            if (secondsIgniting <= 3f)
            {
                return EnumIgniteState.Ignitable;
            }
            return EnumIgniteState.IgniteNow;
        }
        public float changeTemperature(float fromTemp, float toTemp, float dt)
        {
            float diff = Math.Abs(fromTemp - toTemp);
            dt += dt * (diff / 28f);
            if (diff < dt)
            {
                return toTemp;
            }
            if (fromTemp > toTemp)
            {
                dt = -dt;
            }
            if (Math.Abs(fromTemp - toTemp) < 1f)
            {
                return toTemp;
            }
            return fromTemp + dt;
        }

        // Token: 0x06000D20 RID: 3360 RVA: 0x0008A8F0 File Offset: 0x00088AF0
        private bool canSmelt()
        {
            CombustibleProperties fuelCopts = this.fuelCombustibleOpts;
            if (fuelCopts == null)
            {
                return false;
            }
            bool smeltableInput = this.canHeatInput();
            return (this.BurnsAllFuell || smeltableInput) && (float)fuelCopts.BurnTemperature * this.HeatModifier > 0f;
        }

        // Token: 0x06000D21 RID: 3361 RVA: 0x0008A934 File Offset: 0x00088B34
        public void heatInput(float dt)
        {
            float oldTemp = this.InputStackTemp;
            float nowTemp = oldTemp;
            float meltingPoint = this.inputSlot.Itemstack.Collectible.GetMeltingPoint(this.Api.World, this.inventory, this.inputSlot);
            if (oldTemp < this.furnaceTemperature)
            {
                float f = (1f + GameMath.Clamp((this.furnaceTemperature - oldTemp) / 30f, 0f, 1.6f)) * dt;
                if (nowTemp >= meltingPoint)
                {
                    f /= 11f;
                }
                float newTemp = this.changeTemperature(oldTemp, this.furnaceTemperature, f);
                int num = ((this.inputStack.Collectible.CombustibleProps == null) ? 0 : this.inputStack.Collectible.CombustibleProps.MaxTemperature);
                JsonObject itemAttributes = this.inputStack.ItemAttributes;
                int maxTemp = Math.Max(num, (((itemAttributes != null) ? itemAttributes["maxTemperature"] : null) == null) ? 0 : this.inputStack.ItemAttributes["maxTemperature"].AsInt(0));
                if (maxTemp > 0)
                {
                    newTemp = Math.Min((float)maxTemp, newTemp);
                }
                if (oldTemp != newTemp)
                {
                    this.InputStackTemp = newTemp;
                    nowTemp = newTemp;
                }
            }
            if (nowTemp >= meltingPoint)
            {
                float diff = nowTemp / meltingPoint;
                this.inputStackCookingTime += (float)GameMath.Clamp((int)diff, 1, 30) * dt;
                return;
            }
            if (this.inputStackCookingTime > 0f)
            {
                this.inputStackCookingTime -= 1f;
            }
        }

        // Token: 0x06000D22 RID: 3362 RVA: 0x0008AA98 File Offset: 0x00088C98
        public void heatOutput(float dt)
        {
            float oldTemp = this.OutputStackTemp;
            if (oldTemp < this.furnaceTemperature)
            {
                float newTemp = this.changeTemperature(oldTemp, this.furnaceTemperature, 2f * dt);
                int num = ((this.outputStack.Collectible.CombustibleProps == null) ? 0 : this.outputStack.Collectible.CombustibleProps.MaxTemperature);
                JsonObject itemAttributes = this.outputStack.ItemAttributes;
                int maxTemp = Math.Max(num, (((itemAttributes != null) ? itemAttributes["maxTemperature"] : null) == null) ? 0 : this.outputStack.ItemAttributes["maxTemperature"].AsInt(0));
                if (maxTemp > 0)
                {
                    newTemp = Math.Min((float)maxTemp, newTemp);
                }
                if (oldTemp != newTemp)
                {
                    this.OutputStackTemp = newTemp;
                }
            }
        }

        // Token: 0x06000D23 RID: 3363 RVA: 0x0008AB54 File Offset: 0x00088D54
        public void CoolNow(float amountRel)
        {
            this.Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), this.Pos, -0.5, null, false, 16f, 1f);
            this.fuelBurnTime -= amountRel / 10f;
            if (this.Api.World.Rand.NextDouble() < (double)(amountRel / 5f) || this.fuelBurnTime <= 0f)
            {
                this.setBlockState("cold");
                this.extinguishedTotalHours = -99.0;
                this.canIgniteFuel = false;
                this.fuelBurnTime = 0f;
                this.maxFuelBurnTime = 0f;
            }
            this.MarkDirty(true, null);
        }

        // Token: 0x170001CF RID: 463
        // (get) Token: 0x06000D24 RID: 3364 RVA: 0x0008AC16 File Offset: 0x00088E16
        // (set) Token: 0x06000D25 RID: 3365 RVA: 0x0008AC24 File Offset: 0x00088E24
        public float InputStackTemp
        {
            get
            {
                return this.GetTemp(this.inputStack);
            }
            set
            {
                this.SetTemp(this.inputStack, value);
            }
        }

        // Token: 0x170001D0 RID: 464
        // (get) Token: 0x06000D26 RID: 3366 RVA: 0x0008AC33 File Offset: 0x00088E33
        // (set) Token: 0x06000D27 RID: 3367 RVA: 0x0008AC41 File Offset: 0x00088E41
        public float OutputStackTemp
        {
            get
            {
                return this.GetTemp(this.outputStack);
            }
            set
            {
                this.SetTemp(this.outputStack, value);
            }
        }

        // Token: 0x06000D28 RID: 3368 RVA: 0x0008AC50 File Offset: 0x00088E50
        private float GetTemp(ItemStack stack)
        {
            if (stack == null)
            {
                return (float)this.enviromentTemperature();
            }
            if (this.inventory.CookingSlots.Length != 0)
            {
                bool haveStack = false;
                float lowestTemp = 0f;
                for (int i = 0; i < this.inventory.CookingSlots.Length; i++)
                {
                    ItemStack cookingStack = this.inventory.CookingSlots[i].Itemstack;
                    if (cookingStack != null)
                    {
                        float stackTemp = cookingStack.Collectible.GetTemperature(this.Api.World, cookingStack);
                        lowestTemp = (haveStack ? Math.Min(lowestTemp, stackTemp) : stackTemp);
                        haveStack = true;
                    }
                }
                return lowestTemp;
            }
            return stack.Collectible.GetTemperature(this.Api.World, stack);
        }

        // Token: 0x06000D29 RID: 3369 RVA: 0x0008ACF0 File Offset: 0x00088EF0
        private void SetTemp(ItemStack stack, float value)
        {
            if (stack == null)
            {
                return;
            }
            if (this.inventory.CookingSlots.Length != 0)
            {
                for (int i = 0; i < this.inventory.CookingSlots.Length; i++)
                {
                    ItemStack itemstack = this.inventory.CookingSlots[i].Itemstack;
                    if (itemstack != null)
                    {
                        itemstack.Collectible.SetTemperature(this.Api.World, this.inventory.CookingSlots[i].Itemstack, value, true);
                    }
                }
                return;
            }
            stack.Collectible.SetTemperature(this.Api.World, stack, value, true);
        }

        // Token: 0x06000D2A RID: 3370 RVA: 0x0008AD82 File Offset: 0x00088F82
        public void igniteFuel()
        {
            this.igniteWithFuel(this.fuelStack);
            this.fuelStack.StackSize--;
            if (this.fuelStack.StackSize <= 0)
            {
                this.fuelStack = null;
            }
        }
        public void igniteWithFuel(IItemStack stack)
        {
            CombustibleProperties fuelCopts = stack.Collectible.CombustibleProps;
            this.maxFuelBurnTime = (this.fuelBurnTime = fuelCopts.BurnDuration * this.BurnDurationModifier);
            this.maxTemperature = (int)((float)fuelCopts.BurnTemperature * this.HeatModifier);
            this.smokeLevel = fuelCopts.SmokeLevel;
            this.setBlockState("lit");
            this.MarkDirty(true, null);
        }

        // Token: 0x06000D2C RID: 3372 RVA: 0x0008AE24 File Offset: 0x00089024
        public void setBlockState(string state)
        {
            AssetLocation loc = base.Block.CodeWithVariant("burnstate", state);
            Block block = this.Api.World.GetBlock(loc);
            if (block == null)
            {
                return;
            }
            this.Api.World.BlockAccessor.ExchangeBlock(block.Id, this.Pos);
            base.Block = block;
        }

        // Token: 0x06000D2D RID: 3373 RVA: 0x0008AE84 File Offset: 0x00089084
        public bool canHeatInput()
        {
            if (!this.canSmeltInput())
            {
                ItemStack inputStack = this.inputStack;
                bool flag;
                if (inputStack == null)
                {
                    flag = null != null;
                }
                else
                {
                    JsonObject itemAttributes = inputStack.ItemAttributes;
                    flag = ((itemAttributes != null) ? itemAttributes["allowHeating"] : null) != null;
                }
                return flag && this.inputStack.ItemAttributes["allowHeating"].AsBool(false);
            }
            return true;
        }

        // Token: 0x06000D2E RID: 3374 RVA: 0x0008AEE0 File Offset: 0x000890E0
        public bool canHeatOutput()
        {
            ItemStack outputStack = this.outputStack;
            bool flag;
            if (outputStack == null)
            {
                flag = null != null;
            }
            else
            {
                JsonObject itemAttributes = outputStack.ItemAttributes;
                flag = ((itemAttributes != null) ? itemAttributes["allowHeating"] : null) != null;
            }
            return flag && this.outputStack.ItemAttributes["allowHeating"].AsBool(false);
        }

        // Token: 0x06000D2F RID: 3375 RVA: 0x0008AF30 File Offset: 0x00089130
        public bool canSmeltInput()
        {
            if (this.inputStack == null)
            {
                return false;
            }
            if (this.inputStack.Collectible.OnSmeltAttempt(this.inventory))
            {
                this.MarkDirty(true, null);
            }
            return this.inputStack.Collectible.CanSmelt(this.Api.World, this.inventory, this.inputSlot.Itemstack, this.outputSlot.Itemstack) && (this.inputStack.Collectible.CombustibleProps == null || !this.inputStack.Collectible.CombustibleProps.RequiresContainer);
        }

        // Token: 0x06000D30 RID: 3376 RVA: 0x0008AFD0 File Offset: 0x000891D0
        public void smeltItems()
        {
            this.inputStack.Collectible.DoSmelt(this.Api.World, this.inventory, this.inputSlot, this.outputSlot);
            this.InputStackTemp = (float)this.enviromentTemperature();
            this.inputStackCookingTime = 0f;
            this.MarkDirty(true, null);
            this.inputSlot.MarkDirty();
        }

        // Token: 0x06000D31 RID: 3377 RVA: 0x0008B035 File Offset: 0x00089235
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (this.Api.Side == EnumAppSide.Client)
            {
                base.toggleInventoryDialogClient(byPlayer, delegate
                {
                    SyncedTreeAttribute dtree = new SyncedTreeAttribute();
                    this.SetDialogValues(dtree);
                    this.clientDialog = new CANGuiDialogBlockEntityFirepit(this.DialogTitle, this.Inventory, this.Pos, dtree, this.Api as ICoreClientAPI);
                    return this.clientDialog;
                });
            }
            return true;
        }

        // Token: 0x06000D32 RID: 3378 RVA: 0x0008B059 File Offset: 0x00089259
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
        }

        // Token: 0x06000D33 RID: 3379 RVA: 0x0008B064 File Offset: 0x00089264
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == 1001)
            {
                (this.Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(this.Inventory);
                GuiDialogBlockEntity invDialog = this.invDialog;
                if (invDialog != null)
                {
                    invDialog.TryClose();
                }
                GuiDialogBlockEntity invDialog2 = this.invDialog;
                if (invDialog2 != null)
                {
                    invDialog2.Dispose();
                }
                this.invDialog = null;
            }
        }

        // Token: 0x06000D34 RID: 3380 RVA: 0x0008B0CC File Offset: 0x000892CC
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            if (this.Api != null)
            {
                this.Inventory.AfterBlocksLoaded(this.Api.World);
            }
            this.furnaceTemperature = tree.GetFloat("furnaceTemperature", 0f);
            this.maxTemperature = tree.GetInt("maxTemperature", 0);
            this.inputStackCookingTime = tree.GetFloat("oreCookingTime", 0f);
            this.fuelBurnTime = tree.GetFloat("fuelBurnTime", 0f);
            this.maxFuelBurnTime = tree.GetFloat("maxFuelBurnTime", 0f);
            this.extinguishedTotalHours = tree.GetDouble("extinguishedTotalHours", 0.0);
            this.canIgniteFuel = tree.GetBool("canIgniteFuel", true);
            this.cachedFuel = tree.GetFloat("cachedFuel", 0f);
            this.MeshAngle = tree.GetFloat("meshAngle", this.MeshAngle);
            ICoreAPI api = this.Api;
            if (api != null && api.Side == EnumAppSide.Client)
            {
                this.UpdateRenderer();
                if (this.clientDialog != null)
                {
                    this.SetDialogValues(this.clientDialog.Attributes);
                }
            }
            ICoreAPI api2 = this.Api;
            if (api2 != null && api2.Side == EnumAppSide.Client && (this.clientSidePrevBurning != this.IsBurning || this.shouldRedraw))
            {
                BEBehaviorFirepitAmbient behavior = base.GetBehavior<BEBehaviorFirepitAmbient>();
                if (behavior != null)
                {
                    behavior.ToggleAmbientSounds(this.IsBurning);
                }
                this.clientSidePrevBurning = this.IsBurning;
                this.MarkDirty(true, null);
                this.shouldRedraw = false;
            }
        }
        private void UpdateRenderer()
        {
            if (this.renderer == null)
            {
                return;
            }
            ItemStack contentStack = ((this.inputStack == null) ? this.outputStack : this.inputStack);
            if (this.renderer.ContentStack != null && this.renderer.contentStackRenderer != null && ((contentStack != null) ? contentStack.Collectible : null) is IInBloomeryRendererSupplier && this.renderer.ContentStack.Equals(this.Api.World, contentStack, GlobalConstants.IgnoredStackAttributes))
            {
                return;
            }
            IInBloomeryRenderer contentStackRenderer = this.renderer.contentStackRenderer;
            if (contentStackRenderer != null)
            {
                contentStackRenderer.Dispose();
            }
            this.renderer.contentStackRenderer = null;
            if (((contentStack != null) ? contentStack.Collectible : null) is IInBloomeryRendererSupplier)
            {
                IInBloomeryRenderer childrenderer = (((contentStack != null) ? contentStack.Collectible : null) as IInBloomeryRendererSupplier).GetRendererWhenInFirepit(contentStack, this, contentStack == this.outputStack);
                if (childrenderer != null)
                {
                    this.renderer.SetChildRenderer(contentStack, childrenderer);
                    return;
                }
            }
            InFirePitProps props = this.GetRenderProps(contentStack);
            if (((contentStack != null) ? contentStack.Collectible : null) != null && !(((contentStack != null) ? contentStack.Collectible : null) is IInFirepitMeshSupplier) && props != null)
            {
                this.renderer.SetContents(contentStack, props.Transform);
                return;
            }
            this.renderer.SetContents(null, null);
        }
        private void SetDialogValues(ITreeAttribute dialogTree)
        {
            dialogTree.SetFloat("furnaceTemperature", this.furnaceTemperature);
            dialogTree.SetInt("maxTemperature", this.maxTemperature);
            dialogTree.SetFloat("oreCookingTime", this.inputStackCookingTime);
            dialogTree.SetFloat("maxFuelBurnTime", this.maxFuelBurnTime);
            dialogTree.SetFloat("fuelBurnTime", this.fuelBurnTime);
            if (this.inputSlot.Itemstack != null)
            {
                float meltingDuration = this.inputSlot.Itemstack.Collectible.GetMeltingDuration(this.Api.World, this.inventory, this.inputSlot);
                dialogTree.SetFloat("oreTemperature", this.InputStackTemp);
                dialogTree.SetFloat("maxOreCookingTime", meltingDuration);
            }
            else
            {
                dialogTree.RemoveAttribute("oreTemperature");
            }
            dialogTree.SetString("outputText", this.inventory.GetOutputText());
            dialogTree.SetInt("haveCookingContainer", (this.inventory.HaveCookingContainer != false) ? 1 : 0);
            dialogTree.SetInt("quantityCookingSlots", this.inventory.CookingSlots.Length);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            this.Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;
            tree.SetFloat("furnaceTemperature", this.furnaceTemperature);
            tree.SetInt("maxTemperature", this.maxTemperature);
            tree.SetFloat("oreCookingTime", this.inputStackCookingTime);
            tree.SetFloat("fuelBurnTime", this.fuelBurnTime);
            tree.SetFloat("maxFuelBurnTime", this.maxFuelBurnTime);
            tree.SetDouble("extinguishedTotalHours", this.extinguishedTotalHours);
            tree.SetBool("canIgniteFuel", this.canIgniteFuel);
            tree.SetFloat("cachedFuel", this.cachedFuel);
            tree.SetFloat("meshAngle", this.MeshAngle);
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            CANBloomeryContentsRenderer firepitContentsRenderer = this.renderer;
            if (firepitContentsRenderer != null)
            {
                firepitContentsRenderer.Dispose();
            }
            this.renderer = null;
            if (this.clientDialog != null)
            {
                this.clientDialog.TryClose();
                CANGuiDialogBlockEntityFirepit guiDialogBlockEntityFirepit = this.clientDialog;
                if (guiDialogBlockEntityFirepit != null)
                {
                    guiDialogBlockEntityFirepit.Dispose();
                }
                this.clientDialog = null;
            }
        }
        public ItemSlot fuelSlot
        {
            get
            {
                return this.inventory[0];
            }
        }
        public ItemSlot inputSlot
        {
            get
            {
                return this.inventory[1];
            }
        }
        public ItemSlot outputSlot
        {
            get
            {
                return this.inventory[2];
            }
        }
        public ItemSlot[] otherCookingSlots
        {
            get
            {
                return this.inventory.CookingSlots;
            }
        }

        public ItemStack fuelStack
        {
            get
            {
                return this.inventory[0].Itemstack;
            }
            set
            {
                this.inventory[0].Itemstack = value;
                this.inventory[0].MarkDirty();
            }
        }

        public ItemStack inputStack
        {
            get
            {
                return this.inventory[1].Itemstack;
            }
            set
            {
                this.inventory[1].Itemstack = value;
                this.inventory[1].MarkDirty();
            }
        }
        public ItemStack outputStack
        {
            get
            {
                return this.inventory[2].Itemstack;
            }
            set
            {
                this.inventory[2].Itemstack = value;
                this.inventory[2].MarkDirty();
            }
        }
        public CombustibleProperties fuelCombustibleOpts
        {
            get
            {
                return this.getCombustibleOpts(0);
            }
        }

        public CombustibleProperties getCombustibleOpts(int slotid)
        {
            ItemSlot slot = this.inventory[slotid];
            if (slot.Itemstack == null)
            {
                return null;
            }
            return slot.Itemstack.Collectible.CombustibleProps;
        }

        public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            foreach (ItemSlot slot in this.Inventory)
            {
                if (slot.Itemstack != null)
                {
                    if (slot.Itemstack.Class == EnumItemClass.Item)
                    {
                        itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
                    }
                    else
                    {
                        blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
                    }
                    slot.Itemstack.Collectible.OnStoreCollectibleMappings(this.Api.World, slot, blockIdMapping, itemIdMapping);
                }
            }
            foreach (ItemSlot slot2 in this.inventory.CookingSlots)
            {
                if (slot2.Itemstack != null)
                {
                    if (slot2.Itemstack.Class == EnumItemClass.Item)
                    {
                        itemIdMapping[slot2.Itemstack.Item.Id] = slot2.Itemstack.Item.Code;
                    }
                    else
                    {
                        blockIdMapping[slot2.Itemstack.Block.BlockId] = slot2.Itemstack.Block.Code;
                    }
                    slot2.Itemstack.Collectible.OnStoreCollectibleMappings(this.Api.World, slot2, blockIdMapping, itemIdMapping);
                }
            }
        }

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
        {
            base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
        }
        public EnumFirepitModel CurrentModel { get; private set; }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (base.Block == null || base.Block.Code.Path.Contains("construct"))
            {
                return false;
            }
            ItemStack contentStack = ((this.inputStack == null) ? this.outputStack : this.inputStack);
            MeshData contentmesh = this.getContentMesh(contentStack, tesselator);
            if (contentmesh != null)
            {
                mesher.AddMeshData(contentmesh, 1);
            }
            string burnState = base.Block.Variant["burnstate"];
            string contentState = this.CurrentModel.ToString().ToLowerInvariant();
            if (burnState == "cold" && this.fuelSlot.Empty)
            {
                burnState = "extinct";
            }
            if (burnState == null)
            {
                return true;
            }
            mesher.AddMeshData(this.getOrCreateMesh(burnState, contentState), 1);
            return true;
        }
        private MeshData getContentMesh(ItemStack contentStack, ITesselatorAPI tesselator)
        {
            this.CurrentModel = EnumFirepitModel.Normal;
            if (contentStack == null)
            {
                return null;
            }
            if (contentStack.Collectible is IInFirepitMeshSupplier)
            {
                EnumFirepitModel model = EnumFirepitModel.Normal;
                MeshData mesh = (contentStack.Collectible as IInFirepitMeshSupplier).GetMeshWhenInFirepit(contentStack, this.Api.World, this.Pos, ref model);
                this.CurrentModel = model;
                if (mesh != null)
                {
                    return mesh;
                }
            }
            if (contentStack.Collectible is IInFirepitRendererSupplier)
            {
                EnumFirepitModel model2 = (contentStack.Collectible as IInBloomeryRendererSupplier).GetDesiredFirepitModel(contentStack, this, contentStack == this.outputStack);
                this.CurrentModel = model2;
                return null;
            }
            InFirePitProps renderProps = this.GetRenderProps(contentStack);
            if (renderProps == null)
            {
                if (this.renderer.RequireSpit)
                {
                    this.CurrentModel = EnumFirepitModel.Spit;
                }
                return null;
            }
            this.CurrentModel = renderProps.UseFirepitModel;
            if (contentStack.Class != EnumItemClass.Item)
            {
                MeshData ingredientMesh;
                tesselator.TesselateBlock(contentStack.Block, out ingredientMesh);
                ingredientMesh.ModelTransform(renderProps.Transform);
                if (!this.IsBurning && renderProps.UseFirepitModel != EnumFirepitModel.Spit)
                {
                    ingredientMesh.Translate(0f, -0.0625f, 0f);
                }
                return ingredientMesh;
            }
            return null;
        }
        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            CANBloomeryContentsRenderer firepitContentsRenderer = this.renderer;
            if (firepitContentsRenderer == null)
            {
                return;
            }
            firepitContentsRenderer.Dispose();
        }
        private InFirePitProps GetRenderProps(ItemStack contentStack)
        {
            if (contentStack != null)
            {
                JsonObject itemAttributes = contentStack.ItemAttributes;
                if (((itemAttributes != null) ? new bool?(itemAttributes.KeyExists("inFirePitProps")) : null).GetValueOrDefault())
                {
                    InFirePitProps inFirePitProps = contentStack.ItemAttributes["inFirePitProps"].AsObject<InFirePitProps>(null);
                    inFirePitProps.Transform.EnsureDefaultValues();
                    inFirePitProps.Transform.Translation.Add(0, 0.5f, 0);
                    return inFirePitProps;
                }
            }
            return null;
        }
        public MeshData getOrCreateMesh(string burnstate, string contentstate)
        {
            Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "bloomery-meshes", () => new Dictionary<string, MeshData>());
            
            string key = burnstate + "-normal";
            if (!Meshes.TryGetValue(key + this.MeshAngle.ToString(), out MeshData meshdata))
            {
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                if (block.BlockId == 0) return null;

                MeshData[] meshes = new MeshData[17];
                ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;
                //.Rotate(CANBlockEntityBloomery.origin,0f , this.MeshAngle, 0f)
                mesher.TesselateShape(block, Shape.TryGet(Api, "canadditionalmetals:shapes/block/" + key + ".json"), out meshdata);
                meshdata.Rotate(CANBlockEntityBloomery.origin, 0f, this.MeshAngle, 0f);
            }
            return meshdata;
        }
        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            if (this.IsBurning)
            {
                return 10f;
            }
            if (!this.IsSmoldering)
            {
                return 0f;
            }
            return 0.25f;
        }
        internal CANInventorySmelting inventory;
        public float prevFurnaceTemperature = 20f;
        public float furnaceTemperature = 20f;
        public int maxTemperature;
        public float inputStackCookingTime;
        public float fuelBurnTime;
        public float maxFuelBurnTime;
        public float smokeLevel;
        public bool canIgniteFuel;
        public float cachedFuel;
        public double extinguishedTotalHours;
        private CANGuiDialogBlockEntityFirepit clientDialog;
        private bool clientSidePrevBurning;
        private CANBloomeryContentsRenderer renderer;
        private bool shouldRedraw;
        public float emptyFirepitBurnTimeMulBonus = 4f;
    }
}
