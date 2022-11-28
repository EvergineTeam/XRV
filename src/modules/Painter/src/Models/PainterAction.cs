// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Primitives;
using Evergine.Framework;
using System.Collections.Generic;
using Xrv.Painter.Components;

namespace Xrv.Painter.Models
{
    /// <summary>
    /// Painter Action.
    /// </summary>
    public class PainterAction
    {
        /// <summary>
        /// Gets or sets action made.
        /// </summary>
        public PainterModes Mode { get; set; }

        /// <summary>
        /// Gets or sets entity result.
        /// </summary>
        public List<LineInfo> Line { get; set; }

        /// <summary>
        /// Gets or sets entity related.
        /// </summary>
        public Entity Entity { get; set; }
    }
}
