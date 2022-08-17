using Evergine.Framework;
using System;
using Xrv.Core.Menu;

namespace Xrv.Core.Modules
{
    public abstract class Module
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract HandMenuButtonDefinition HandMenuButton { get; }

        // Help

        // Configuration

        public abstract void Run(bool turnOff);

        public abstract void Initialize(Scene scene);
    }
}
