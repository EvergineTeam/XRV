using Evergine.Framework;
using System;

namespace Xrv.Core.Settings
{
    public class Section
    {
        public string Name { get; set; }

        public Func<Entity> Contents { get; set; }
    }
}
