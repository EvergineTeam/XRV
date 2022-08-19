using Evergine.Framework;
using Xrv.Core.Menu;
using Xrv.Core.Settings;

namespace Xrv.Core.Modules
{
    public abstract class Module
    {
        public abstract string Name { get; }

        public abstract HandMenuButtonDescription HandMenuButton { get; }

        public abstract Section Help { get; }

        public abstract Section Settings { get; }

        public abstract void Run(bool turnOn);

        public abstract void Initialize(Scene scene);
    }
}
