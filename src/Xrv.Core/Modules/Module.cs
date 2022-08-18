using Evergine.Framework;
using System;
using Xrv.Core.Menu;

namespace Xrv.Core.Modules
{
    public abstract class Module
    {
        public abstract string Name { get; }

        public abstract HandMenuButtonDescription HandMenuButton { get; }

        // Help

        // Configuration

        public abstract void Run(bool turnOn);

        public abstract void Initialize(Scene scene);
    }
}
