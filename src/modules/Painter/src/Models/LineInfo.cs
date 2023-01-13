// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.
using Evergine.Framework.XR;
using Evergine.Mathematics;
using Xrv.Painter.Components;
using Xrv.Painter.Enums;

namespace Xrv.Painter.Models
{
    /// <summary>
    /// Line Info for painter.
    /// </summary>
    public class LineInfo
    {
        /// <summary>
        /// Gets or sets Color.
        /// </summary>
        public ColorEnum Color { get; set; }

        /// <summary>
        /// Gets or sets Position.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets Thickness.
        /// </summary>
        public float Thickness { get; set; }

        /// <summary>
        /// Gets or sets PainterThickness.
        /// </summary>
        public PainterThickness PainterThickness { get; set; }

        /// <summary>
        /// Gets or sets Hand.
        /// </summary>
        public XRHandedness Hand { get; set; }
    }
}
