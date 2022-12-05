// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Common.Graphics.VertexFormats;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Buffer = Evergine.Common.Graphics.Buffer;

namespace Xrv.LoadModel.Importers.STL
{
    /// <summary>
    /// STL files loader in runtime.
    /// Source: https://github.com/karl-/pb_Stl/blob/master/Runtime/Importer.cs.
    /// </summary>
    public class STLRuntime : ModelRuntime
    {
        /// <summary>
        /// Get the unique instance of the class (Singleton).
        /// </summary>
        public readonly static STLRuntime Instance = new STLRuntime();

        private const int EMPTY = 0;
        private const int SOLID = 1;
        private const int FACET = 2;
        private const int OUTER = 3;
        private const int VERTEX = 4;
        private const int ENDLOOP = 5;
        private const int ENDFACET = 6;
        private const int ENDSOLID = 7;

        private GraphicsContext graphicsContext;
        private AssetsDirectory assetsDirectory;

        private STLRuntime()
        {
        }

        /// <inheritdoc/>
        public override string Extentsion => ".stl";

        /// <summary>
        /// Read a STL from file Path.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        /// <returns>Model.</returns>
        public async Task<Model> Read(string filePath)
        {
            Model model = null;

            if (this.assetsDirectory == null)
            {
                this.assetsDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            }

            using (var stream = this.assetsDirectory.Open(filePath))
            {
                if (stream == null || !stream.CanRead)
                {
                    throw new ArgumentException("Invalid parameter. Stream must be readable");
                }

                model = await this.Read(stream);
            }

            return model;
        }

        /// <summary>
        /// Read a STL from stream.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <returns>Model.</returns>
        public override async Task<Model> Read(Stream stream, Func<Color, Texture, SamplerState, glTFLoader.Schema.Material.AlphaModeEnum, float, float, bool, Material> materialAssigner = null)
        {
            if (this.graphicsContext == null)
            {
                this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
            }

            Facet[] facets = null;

            // Read STL data
            ////if (this.IsBinary(stream))
            ////{
            ////Debug.WriteLine("Binary STL file");
            facets = this.ImportBinary(stream);
            ////}
            ////else
            ////{
            ////Debug.WriteLine("ASCII STL file");
            ////facets = this.ImportAscii(stream);
            ////}

            // Generate meshes
            var meshContainer = await this.ImportHardNormals(facets);

            // Generate model
            var rootNode = new NodeContent()
            {
                Name = "STL file",
                Mesh = meshContainer,
                Children = new NodeContent[0],
                ChildIndices = new int[0],
            };

            var materialCollection = new List<(string, Guid)>()
            {
                ("Default", LoadModelResourceIDs.Effects.SolidEffect),
            };

            var model = new Model()
            {
                MeshContainers = new[] { meshContainer },
                Materials = materialCollection,
                AllNodes = new[] { rootNode },
                RootNodes = new[] { 0 },
            };

            model.RefreshBoundingBox();

            return model;
        }

        private bool IsBinary(Stream stream)
        {
            bool isBinary = false;

            using (BufferedStream bs0 = new BufferedStream(stream))
            {
                for (long i = 0; i < 80; i++)
                {
                    var readByte = bs0.ReadByte();
                    if (readByte == 0x0)
                    {
                        isBinary = true;
                        break;
                    }
                }

                if (!isBinary)
                {
                    bs0.Position = 0;

                    var byteArray = new byte[6];

                    for (var i = 0; i < 6; i++)
                    {
                        byteArray[i] = (byte)bs0.ReadByte();
                    }

                    var text = Encoding.UTF8.GetString(byteArray);
                    isBinary = text != "solid ";
                }
            }

            return isBinary;
        }

        private Facet[] ImportBinary(Stream stream)
        {
            Facet[] facets;

            using (BinaryReader br = new BinaryReader(stream, new ASCIIEncoding()))
            {
                // read header
                br.ReadBytes(80);

                uint facetCount = br.ReadUInt32();
                facets = new Facet[facetCount];

                for (uint i = 0; i < facetCount; i++)
                {
                    facets[i] = this.GetFacet(br);
                }
            }

            return facets;
        }

        private Facet GetFacet(BinaryReader binaryReader)
        {
            Facet facet = new Facet(
                this.GetVector3(binaryReader),  // Normal
                this.GetVector3(binaryReader),  // A
                this.GetVector3(binaryReader),  // B
                this.GetVector3(binaryReader)); // C

            binaryReader.ReadUInt16(); // padding

            return facet;
        }

        private Vector3 GetVector3(BinaryReader binaryReader)
        {
            float x = binaryReader.ReadSingle();
            float y = binaryReader.ReadSingle();
            float z = binaryReader.ReadSingle();

            // Coordinate Space left transformation
            return new Vector3(-y, z, x);
        }

        private Facet[] ImportAscii(Stream stream)
        {
            List<Facet> facets = new List<Facet>();

            using (StreamReader sr = new StreamReader(stream))
            {
                string line;
                int state = EMPTY;
                int vertex = 0;
                Vector3 normal = Vector3.Zero;
                Vector3 a = Vector3.Zero;
                Vector3 b = Vector3.Zero;
                Vector3 c = Vector3.Zero;
                bool exit = false;

                while (sr.Peek() > 0 && !exit)
                {
                    line = sr.ReadLine().Trim();
                    state = this.ReadState(line);

                    switch (state)
                    {
                        case SOLID:
                            continue;

                        case FACET:
                            normal = this.StringToVec3(line.Replace("facet normal ", string.Empty));
                            break;

                        case OUTER:
                            vertex = 0;
                            break;

                        case VERTEX:
                            // maintain counter-clockwise orientation of vertices:
                            if (vertex == 0)
                            {
                                a = this.StringToVec3(line.Replace("vertex ", string.Empty));
                            }
                            else if (vertex == 2)
                            {
                                c = this.StringToVec3(line.Replace("vertex ", string.Empty));
                            }
                            else if (vertex == 1)
                            {
                                b = this.StringToVec3(line.Replace("vertex ", string.Empty));
                            }

                            vertex++;
                            break;

                        case ENDLOOP:
                            break;

                        case ENDFACET:
                            facets.Add(new Facet(normal, a, b, c));
                            break;

                        case ENDSOLID:
                            exit = true;
                            break;

                        case EMPTY:
                        default:
                            break;
                    }
                }
            }

            return facets.ToArray();
        }

        private int ReadState(string line)
        {
            if (line.StartsWith("solid"))
            {
                return SOLID;
            }
            else if (line.StartsWith("facet"))
            {
                return FACET;
            }
            else if (line.StartsWith("outer"))
            {
                return OUTER;
            }
            else if (line.StartsWith("vertex"))
            {
                return VERTEX;
            }
            else if (line.StartsWith("endloop"))
            {
                return ENDLOOP;
            }
            else if (line.StartsWith("endfacet"))
            {
                return ENDFACET;
            }
            else if (line.StartsWith("endsolid"))
            {
                return ENDSOLID;
            }
            else
            {
                return EMPTY;
            }
        }

        private Vector3 StringToVec3(string str)
        {
            string[] split = str.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            Vector3 v = new Vector3();

            float.TryParse(split[0], out float x);
            float.TryParse(split[1], out float y);
            float.TryParse(split[2], out float z);

            // Coordinate Space Left trasnform
            v.X = -y;
            v.Y = z;
            v.Z = x;

            return v;
        }

        private async Task<MeshContainer> ImportHardNormals(Facet[] faces)
        {
            VertexPositionNormal[] vertexData = new VertexPositionNormal[faces.Length * 3];
            int index = 0;
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < faces.Length; i++)
            {
                var face = faces[i];

                // CoodrinateSpace Left
                vertexData[index] = new VertexPositionNormal(face.a, face.normal);
                Vector3.Max(ref vertexData[index].Position, ref max, out max);
                Vector3.Min(ref vertexData[index].Position, ref min, out min);
                index++;

                vertexData[index] = new VertexPositionNormal(face.b, face.normal);
                Vector3.Max(ref vertexData[index].Position, ref max, out max);
                Vector3.Min(ref vertexData[index].Position, ref min, out min);
                index++;

                vertexData[index] = new VertexPositionNormal(face.c, face.normal);
                Vector3.Max(ref vertexData[index].Position, ref max, out max);
                Vector3.Min(ref vertexData[index].Position, ref min, out min);
                index++;
            }

            Mesh mesh = null;
            await EvergineForegroundTask.Run(() =>
            {
                // Vertex Buffer
                var vBufferDescription = new BufferDescription(
                                                                    (uint)Unsafe.SizeOf<VertexPositionNormal>() * (uint)vertexData.Length,
                                                                    BufferFlags.ShaderResource | BufferFlags.VertexBuffer,
                                                                    ResourceUsage.Default);

                Buffer vBuffer = this.graphicsContext.Factory.CreateBuffer(vertexData, ref vBufferDescription);
                VertexBuffer vertexBuffer = new VertexBuffer(vBuffer, VertexPositionNormal.VertexFormat);

                mesh = new Mesh(new VertexBuffer[] { vertexBuffer }, PrimitiveTopology.TriangleList, vertexData.Length / 3, 0)
                {
                    BoundingBox = new BoundingBox(min, max),
                };
            });

            return new MeshContainer()
            {
                Name = "STL MeshContainer",
                Meshes = new List<Mesh> { mesh },
                BoundingBox = new BoundingBox(min, max),
            };
        }
    }
}
