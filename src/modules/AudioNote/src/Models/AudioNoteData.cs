// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Mathematics;

namespace Xrv.AudioNote.Models
{
    /// <summary>
    /// Audio Note Data.
    /// </summary>
    public class AudioNoteData
    {
        /// <summary>
        /// Gets or sets guid.
        /// </summary>
        public string Guid { get; set; }

        /// <summary>
        /// Gets or sets path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets x.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Gets or sets y.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Gets or sets z.
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// Set position.
        /// </summary>
        /// <param name="position">Position to set.</param>
        public void SetPosition(Vector3 position)
        {
            this.X = position.X;
            this.Y = position.Y;
            this.Z = position.Z;
        }

        /// <summary>
        /// Get position.
        /// </summary>
        /// <returns>Position.</returns>
        public Vector3 GetPosition()
        {
            return new Vector3(this.X, this.Y, this.Z);
        }
    }
}
