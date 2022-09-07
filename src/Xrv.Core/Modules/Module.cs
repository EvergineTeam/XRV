// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;

namespace Xrv.Core.Modules
{
    /// <summary>
    /// Module definition.
    /// </summary>
    public abstract class Module
    {
        /// <summary>
        /// Gets module name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets module hand menu button description. If null, no button is added at all.
        /// </summary>
        public abstract MenuButtonDescription HandMenuButton { get; }

        /// <summary>
        /// Gets help section item. If null, no item is added at all.
        /// </summary>
        public abstract TabItem Help { get; }

        /// <summary>
        /// Gets settings section item. If null, no item is added at all.
        /// </summary>
        public abstract TabItem Settings { get; }

        /// <summary>
        /// Invoked when hand menu button is released.
        /// </summary>
        /// <param name="turnOn">For toggle buttons, it indicates the state of the toggle (On = true, Off = false).
        /// For standard buttons, it will always be true.</param>
        public abstract void Run(bool turnOn);

        /// <summary>
        /// Module initialization. It allows developers to add required entities for
        /// their modules.
        /// </summary>
        /// <param name="scene">Scene that is about to be loaded.</param>
        public abstract void Initialize(Scene scene);
    }
}
