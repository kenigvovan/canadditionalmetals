using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using canadditionalmetals.src.be;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace canadditionalmetals.src
{
    public interface IInBloomeryRendererSupplier
    {
        IInBloomeryRenderer GetRendererWhenInFirepit(ItemStack stack, CANBlockEntityBloomery firepit, bool forOutputSlot);

        EnumFirepitModel GetDesiredFirepitModel(ItemStack stack, CANBlockEntityBloomery firepit, bool forOutputSlot);
    }
}
