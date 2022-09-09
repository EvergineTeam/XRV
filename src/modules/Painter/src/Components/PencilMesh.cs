// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Components.Primitives;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LineVertexFormat = Evergine.Common.Graphics.VertexFormats.VertexPositionNormalTexture;

namespace Xrv.Painter.Components
{
    /// <summary>
    /// Pencil mesh.
    /// </summary>
    public class PencilMesh : MeshComponent
    {
        /// <summary>
        /// The line points list.
        /// </summary>
        [IgnoreEvergine]
        public List<LinePointInfo> LinePoints = new List<LinePointInfo>();

        internal Mesh mesh;

        /// <summary>
        /// Graphics Context.
        /// </summary>
        [BindService]
        protected GraphicsContext graphicsContext;

        private LineBatch3D debugLineBatch;
        private List<BoundingOrientedBox> bblist = new List<BoundingOrientedBox>();
        private BoundingBox boundingBox = new BoundingBox();

        /// <summary>
        /// Gets or sets vertices per point.
        /// </summary>
        public int VerticesPerPoint { get; set; } = 6;

        /// <summary>
        /// Gets or sets a value indicating whether is debug mode.
        /// </summary>
        public bool IsDebugMode { get; set; } = false;

        /// <inheritdoc/>
        [DontRenderProperty]
        public override Model Model { get => base.Model; set => base.Model = value; }

        /// <inheritdoc/>
        [DontRenderProperty]
        public override string ModelMeshName
        {
            get
            {
                return base.ModelMeshName;
            }
        }

        /// <summary>
        /// Gets the mesh bounding box.
        /// </summary>
        /// <returns>The bounding box.</returns>
        public override BoundingBox? BoundingBox
        {
            get
            {
                return this.boundingBox;
            }
        }

        /// <summary>
        /// Refresh meshes method.
        /// </summary>
        public void RefreshMeshes()
        {
            this.DisposeMeshes();

            if (this.LinePoints?.Count < 2)
            {
                return;
            }

            this.ResetBoundingBox();
            this.FillStripLines();
            this.GenerateInternalModel();
        }

        /// <summary>
        /// Check line collision.
        /// </summary>
        /// <param name="pencilBounding">bounding for deletion.</param>
        /// <returns>true if collides.</returns>
        public bool CheckLineCollision(BoundingSphere pencilBounding)
        {
            if (this.BoundingBox.HasValue &&
               this.BoundingBox.Value.Contains(pencilBounding) != ContainmentType.Disjoint)
            {
                foreach (var item in this.bblist)
                {
                    if (item.Contains(ref pencilBounding) != ContainmentType.Disjoint)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            this.RefreshMeshes();
            if (this.IsDebugMode)
            {
                RenderLayerDescription opaqueLayerDescription = new RenderLayerDescription()
                {
                    RenderState = new RenderStateDescription()
                    {
                        RasterizerState = RasterizerStates.CullBack,
                        BlendState = BlendStates.Opaque,
                        DepthStencilState = DepthStencilStates.ReadWrite,
                    },
                };

                this.debugLineBatch = new LineBatch3D(this.graphicsContext, opaqueLayerDescription)
                {
                    ResetAfterRender = false,
                };

                this.Managers.RenderManager.AddRenderObject(this.debugLineBatch);
            }

            return true;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            if (this.debugLineBatch != null)
            {
                this.Managers.RenderManager.RemoveRenderObject(this.debugLineBatch);
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            this.DisposeMeshes();
            base.OnDestroy();
        }

        /// <summary>
        /// Resets the current bounding box.
        /// </summary>
        private void ResetBoundingBox()
        {
            this.boundingBox.Max = new Vector3(float.MinValue);
            this.boundingBox.Min = new Vector3(float.MaxValue);
        }

        /// <summary>
        /// Regenerate line mesh.
        /// </summary>
        private void GenerateInternalModel()
        {
            if (this.Model != null)
            {
                this.Model.Dispose();
                this.Model = null;
            }

            this.Model = new Model(this.modelMeshName, this.mesh);
            this.ThrowRefreshEvent();
        }

        /// <summary>
        /// Strip lines.
        /// </summary>
        private unsafe void FillStripLines()
        {
            var forward = Vector3.Forward;
            var up = Vector3.Up;
            LinePointInfo currentPoint;
            this.bblist.Clear();

            int nPoints = Math.Min(int.MaxValue, this.LinePoints.Count);

            Vector3 direction;
            Vector3 currentPosition;
            Vector3 prevPosition = this.LinePoints[0].Position;

            // Set vertex buffer
            int vertexCount = nPoints * this.VerticesPerPoint;
            int vertexBufferSize = vertexCount * (int)LineVertexFormat.VertexFormat.Stride;
            var verticesHG = Marshal.AllocHGlobal(vertexBufferSize);
            var verticesPtr = (LineVertexFormat*)verticesHG.ToPointer();
            var currentVertex = verticesPtr;
            Vector3 nextPosition;
            int lastIndex = nPoints - 1;

            this.debugLineBatch?.Reset();

            for (int p = 0; p <= lastIndex; p++)
            {
                currentPoint = this.LinePoints[p];
                currentPosition = currentPoint.Position;

                if (p == 0)
                {
                    prevPosition = currentPosition;
                }

                if (p < lastIndex)
                {
                    nextPosition = this.LinePoints[p + 1].Position;
                    direction = ((currentPosition - prevPosition) + (nextPosition - currentPosition)) * 0.5f;

                    var halfExtends = new Vector3(currentPoint.Thickness * 0.5f);
                    var middlePoint = (currentPosition + nextPosition) * 0.5f;
                    var lookAt = middlePoint - currentPosition;
                    halfExtends.Z = lookAt.Length();
                    Quaternion.CreateFromLookAt(ref lookAt, ref up, out var orientation);
                    var pointBB = new BoundingOrientedBox(middlePoint, halfExtends, orientation);
                    this.bblist.Add(pointBB);
                    this.debugLineBatch?.DrawBoundingOrientedBox(pointBB, Color.Orange);
                }
                else
                {
                    direction = prevPosition - currentPosition;
                }

                this.debugLineBatch?.DrawLine(prevPosition, currentPosition, Color.Red);

                direction.Normalize();
                this.AddVertex(ref currentPoint, ref direction, ref currentVertex);

                prevPosition = currentPosition;
            }

            var bufferDescription = new BufferDescription()
            {
                SizeInBytes = (uint)vertexBufferSize,
                Flags = BufferFlags.VertexBuffer,
                Usage = ResourceUsage.Default,
                CpuAccess = ResourceCpuAccess.None,
            };

            var buffer = this.graphicsContext.Factory.CreateBuffer(verticesHG, ref bufferDescription);
            var vertexBuffer = new VertexBuffer(buffer, LineVertexFormat.VertexFormat);

            Marshal.FreeHGlobal(verticesHG);

            // Set index buffer
            bool is32Bits = nPoints >= ushort.MaxValue;
            IndexFormat indexFormat = is32Bits ? IndexFormat.UInt32 : IndexFormat.UInt16;
            int indexElementSize = is32Bits ? 4 : 2;
            var trianglesCount = ((vertexCount - this.VerticesPerPoint) * 2) + ((this.VerticesPerPoint - 2) * 2);
            var indexCount = trianglesCount * 3;
            int bufferSize = indexCount * indexElementSize;

            var indicesHG = Marshal.AllocHGlobal(bufferSize);

            if (!is32Bits)
            {
                var indicePtr = (ushort*)indicesHG.ToPointer();
                for (int i = 0; i < this.VerticesPerPoint - 2; i++)
                {
                    *indicePtr++ = 0;
                    *indicePtr++ = (ushort)(i + 2);
                    *indicePtr++ = (ushort)(i + 1);
                }

                for (int i = this.VerticesPerPoint; i > 2; i--)
                {
                    *indicePtr++ = (ushort)(vertexCount - this.VerticesPerPoint);
                    *indicePtr++ = (ushort)(vertexCount - i + 2);
                    *indicePtr++ = (ushort)(vertexCount - i + 1);
                }

                for (ushort i = 0; i < vertexCount - this.VerticesPerPoint; i += (ushort)this.VerticesPerPoint)
                {
                    for (ushort j = 0; j < this.VerticesPerPoint; j++)
                    {
                        *indicePtr++ = (ushort)(i + j);
                        *indicePtr++ = (ushort)(i + ((j + 1) % this.VerticesPerPoint));
                        *indicePtr++ = (ushort)(i + j + this.VerticesPerPoint);

                        *indicePtr++ = (ushort)(i + j);
                        *indicePtr++ = (ushort)(i + j + this.VerticesPerPoint);
                        *indicePtr++ = (ushort)(i + this.VerticesPerPoint + ((j + this.VerticesPerPoint - 1) % this.VerticesPerPoint));
                    }
                }
            }
            else
            {
                var indicePtr = (uint*)indicesHG.ToPointer();
                for (int i = 0; i < this.VerticesPerPoint - 2; i++)
                {
                    *indicePtr++ = 0;
                    *indicePtr++ = (uint)(i + 2);
                    *indicePtr++ = (uint)(i + 1);
                }

                for (int i = this.VerticesPerPoint; i > 2; i--)
                {
                    *indicePtr++ = (uint)(vertexCount - this.VerticesPerPoint);
                    *indicePtr++ = (uint)(vertexCount - i + 2);
                    *indicePtr++ = (uint)(vertexCount - i + 1);
                }

                for (uint i = 0; i < vertexCount - this.VerticesPerPoint; i += (uint)this.VerticesPerPoint)
                {
                    for (uint j = 0; j < this.VerticesPerPoint; j++)
                    {
                        *indicePtr++ = (uint)(i + j);
                        *indicePtr++ = (uint)(i + ((j + 1) % this.VerticesPerPoint));
                        *indicePtr++ = (uint)(i + j + this.VerticesPerPoint);

                        *indicePtr++ = (uint)(i + j);
                        *indicePtr++ = (uint)(i + j + this.VerticesPerPoint);
                        *indicePtr++ = (uint)(i + this.VerticesPerPoint + ((j + this.VerticesPerPoint - 1) % this.VerticesPerPoint));
                    }
                }
            }

            bufferDescription = new BufferDescription()
            {
                SizeInBytes = (uint)bufferSize,
                Flags = BufferFlags.IndexBuffer,
                Usage = ResourceUsage.Default,
                CpuAccess = ResourceCpuAccess.None,
            };

            buffer = this.graphicsContext.Factory.CreateBuffer(indicesHG, ref bufferDescription);
            var indexBuffer = new IndexBuffer(buffer, indexFormat);
            Marshal.FreeHGlobal(indicesHG);

            // Creates the mesh
            this.mesh = new Mesh(new VertexBuffer[] { vertexBuffer }, indexBuffer, PrimitiveTopology.TriangleList, indexCount / 3, 0, 0)
            {
                BoundingBox = this.boundingBox,
            };

            this.debugLineBatch?.DrawBoundingBox(this.boundingBox, Color.Orange);
        }

        private unsafe void AddVertex(ref LinePointInfo info, ref Vector3 forward, ref LineVertexFormat* vertices)
        {
            float halfThickness = info.Thickness * 0.5f;
            var pointCenter = info.Position;
            var rotation = Quaternion.CreateFromTwoVectors(Vector3.Forward, forward);
            var transform = Matrix4x4.CreateFromQuaternion(rotation);

            float angle = 0;
            var max = new Vector3(float.MinValue);
            var min = new Vector3(float.MaxValue);
            for (int i = 0; i < this.VerticesPerPoint; i++)
            {
                var vertex = vertices++;

                var p = new Vector3(
                    (float)Math.Cos(angle) * halfThickness,
                    (float)Math.Sin(angle) * halfThickness,
                    0);
                vertex->Position = pointCenter + Vector3.Transform(p, transform);

                angle += MathHelper.TwoPi / this.VerticesPerPoint;
                max = Vector3.Max(vertex->Position, max);
                min = Vector3.Min(vertex->Position, min);
            }

            this.boundingBox.Max = Vector3.Max(max, this.boundingBox.Max);
            this.boundingBox.Min = Vector3.Min(min, this.boundingBox.Min);

            this.debugLineBatch?.DrawPoint(pointCenter, info.Thickness, Color.Yellow);
        }

        private unsafe void AddVertex(Vector3 position, ref LineVertexFormat* vertices)
        {
            var vertex = vertices++;
            vertex->Position = position;
        }

        private void DisposeMeshes()
        {
            if (this.mesh != null)
            {
                this.mesh?.IndexBuffer?.Buffer?.Dispose();
                for (int i = 0; i < this.mesh?.VertexBuffers?.Length; i++)
                {
                    this.mesh?.VertexBuffers[i]?.Buffer?.Dispose();
                }

                this.mesh = null;
            }
        }
    }
}
