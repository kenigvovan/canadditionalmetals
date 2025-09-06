using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace canadditionalmetals.src
{
    public interface IInBloomeryRenderer : IRenderer, IDisposable
    {
        void OnUpdate(float temperature);

        void OnCookingComplete();
    }
}
