﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.Platform;
using glTFLoader;
using glTFLoader.Schema;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xrv.LoadModel.Effects;
using Xrv.LoadModel.Importers.Images;
using static glTFLoader.Schema.Material;
using Buffer = Evergine.Common.Graphics.Buffer;
using Material = Evergine.Framework.Graphics.Material;
using Mesh = Evergine.Framework.Graphics.Mesh;
using Texture = Evergine.Common.Graphics.Texture;

namespace Xrv.LoadModel.Importers.GLB
{
    /// <summary>
    /// GLB files loader in runtime.
    /// </summary>
    public class GLBRuntime
    {
        /// <summary>
        /// Single instance (Singleton).
        /// </summary>
        public readonly static GLBRuntime Instance = new GLBRuntime();

        private GraphicsContext graphicsContext;
        private AssetsService assetsService;
        private AssetsDirectory assetsDirectory;

        private RenderLayerDescription opaqueLayer;
        private RenderLayerDescription alphaLayer;
        private SamplerState linearClampSampler;
        private SamplerState linearWrapSampler;
        private List<char> invalidNameCharacters;

        private Dictionary<int, (string name, Material material)> materials = new Dictionary<int, (string, Material)>();
        private Dictionary<int, Texture> images = new Dictionary<int, Texture>();
        private Dictionary<int, List<Mesh>> meshes = new Dictionary<int, List<Mesh>>();
        private List<MeshContainer> meshContainers = new List<MeshContainer>();
        private List<NodeContent> allNodes = new List<NodeContent>();
        private List<int> rootIndices = new List<int>();

        private Gltf glbModel;
        private byte[] binaryChunk;
        private BufferInfo[] bufferInfos;

        private GLBRuntime()
        {
        }

        /// <summary>
        /// Read a glb file and return a model asset.
        /// </summary>
        /// <param name="filePath">Glb filepath.</param>
        /// <returns>Model asset.</returns>
        public Model Read(string filePath)
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
                    throw new ArgumentException("Invalid parameter. Stream must be readable", "imageStream");
                }

                model = this.Read(stream);
            }

            return model;
        }

        /// <summary>
        /// Read a glb file from stream and return a model asset.
        /// </summary>
        /// <param name="stream">Seeked stream.</param>
        /// <returns>Model asset.</returns>
        public Model Read(Stream stream)
        {
            this.LoadStaticResources();

            var model = this.ReadGLB(stream);

            this.FreeResources();

            return model;
        }

        private void LoadStaticResources()
        {
            if (this.graphicsContext == null)
            {
                // Get Services
                this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
                this.assetsService = Application.Current.Container.Resolve<AssetsService>();

                // Get invalid character used in node names
                this.invalidNameCharacters = Path.GetInvalidFileNameChars().ToList();
                this.invalidNameCharacters.Add('.');
                this.invalidNameCharacters.Add('[');
                this.invalidNameCharacters.Add(']');

                // Get static resources
                this.opaqueLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
                this.alphaLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
                this.linearClampSampler = this.assetsService.Load<SamplerState>(DefaultResourcesIDs.LinearClampSamplerID);
                this.linearWrapSampler = this.assetsService.Load<SamplerState>(DefaultResourcesIDs.LinearWrapSamplerID);
            }
        }

        private void FreeResources()
        {
            for (int i = 0; i < this.bufferInfos.Length; i++)
            {
                this.bufferInfos[i].Dispose();
            }

            this.glbModel = null;
            this.materials.Clear();
            this.images.Clear();
            this.meshes.Clear();
            this.meshContainers.Clear();
            this.allNodes.Clear();
            this.rootIndices.Clear();
            this.binaryChunk = null;
        }

        private Model ReadGLB(Stream stream)
        {
            Model model = null;

            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("Invalid parameter. Stream must be readable", "imageStream");
            }

            var result = GLBHelpers.LoadModel(stream);
            this.glbModel = result.Gltf;
            this.binaryChunk = result.Data;

            this.ReadBuffers();
            this.ReadDefaultScene();

            var materialCollection = new List<(string, Guid)>();
            foreach (var materialInfo in this.materials.Values)
            {
                this.assetsService.RegisterInstance<Material>(materialInfo.material);
                materialCollection.Add((materialInfo.name, materialInfo.material.Id));
            }

            model = new Model()
            {
                MeshContainers = this.meshContainers.ToArray(),
                AllNodes = this.allNodes.ToArray(),
                Materials = materialCollection,
                RootNodes = this.rootIndices.ToArray(),
            };

            // Compute global bounding box
            model.RefreshBoundingBox();

            return model;
        }

        private void ReadBuffers()
        {
            // read all buffers
            int numBuffers = this.glbModel.Buffers.Length;
            this.bufferInfos = new BufferInfo[numBuffers];
            for (int i = 0; i < numBuffers; ++i)
            {
                this.bufferInfos[i] = new BufferInfo(this.glbModel.LoadBinaryBuffer(i, this.GetExternalFileSolver));
            }
        }

        private void ReadDefaultScene()
        {
            if (this.glbModel.Scene.HasValue)
            {
                var defaultSceneId = this.glbModel.Scene.Value;
                var scene = this.glbModel.Scenes[defaultSceneId];
                var nodeCount = scene.Nodes.Length;

                for (int n = 0; n < nodeCount; n++)
                {
                    int nodeId = scene.Nodes[n];
                    var rootNode = this.ReadNode(nodeId);
                    this.rootIndices.Add(rootNode.index);
                }
            }
            else
            {
                throw new Exception("GLB not defines any scene");
            }
        }

        private (NodeContent node, int index) ReadNode(int nodeId)
        {
            var node = this.glbModel.Nodes[nodeId];

            // Process the children
            NodeContent[] children = null;
            int[] childIndices = null;
            if (node.Children != null)
            {
                children = new NodeContent[node.Children.Length];
                childIndices = new int[node.Children.Length];
                for (int c = 0; c < node.Children.Length; c++)
                {
                    int childNodeId = node.Children[c];
                    var childNode = this.ReadNode(childNodeId);

                    children[c] = childNode.node;
                    childIndices[c] = childNode.index;
                }
            }

            // Get Matrices
            Vector3 position;
            Quaternion orientation;
            Vector3 scale;

            Matrix4x4 transform = node.Matrix.ToEvergineMatrix();
            if (transform != Matrix4x4.Identity)
            {
                position = transform.Translation;
                orientation = transform.Orientation;
                scale = transform.Scale;
            }
            else
            {
                position = node.Translation.ToEvergineVector3();
                orientation = node.Rotation.ToEvergineQuaternion();
                scale = node.Scale.ToEvergineVector3();
            }

            // Read mesh
            List<Mesh> nodePrimitives = null;
            MeshContainer meshContainer = null;
            if (node.Mesh.HasValue)
            {
                int meshId = node.Mesh.Value;
                var glbMesh = this.glbModel.Meshes[meshId];

                if (!this.meshes.TryGetValue(meshId, out nodePrimitives))
                {
                    nodePrimitives = new List<Mesh>(glbMesh.Primitives.Length);
                    for (int p = 0; p < glbMesh.Primitives.Length; p++)
                    {
                        var primitive = glbMesh.Primitives[p];
                        nodePrimitives.Add(this.ReadPrimitive(primitive));
                    }

                    this.meshes[meshId] = nodePrimitives;
                }

                meshContainer = new MeshContainer()
                {
                    Name = string.IsNullOrEmpty(glbMesh.Name) ? $"_Mesh_{meshId}" : this.MakeSafeName(glbMesh.Name),
                    Meshes = nodePrimitives,
                };
                meshContainer.RefreshBoundingBox();

                this.meshContainers.Add(meshContainer);
            }

            // Create node content
            var nodeContent = new NodeContent()
            {
                Name = string.IsNullOrEmpty(node.Name) ? $"_Node_{nodeId}" : this.MakeSafeName(node.Name),
                Translation = position,
                Orientation = orientation,
                Scale = scale,
                Children = children,
                ChildIndices = childIndices,
                Mesh = meshContainer,
            };

            this.allNodes.Add(nodeContent);

            // Set parent to the children
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    children[i].Parent = nodeContent;
                }
            }

            return (nodeContent, this.allNodes.Count - 1);
        }

        private Mesh ReadPrimitive(MeshPrimitive primitive)
        {
            // Create Vertex Buffers
            var attributes = primitive.Attributes.ToArray();

            List<VertexBuffer> vertexBuffersList = new List<VertexBuffer>();

            BoundingBox meshBounding = default;
            bool vertexColorEnabled = false;
            for (int i = 0; i < attributes.Length; i++)
            {
                var attributeName = attributes[i].Key;

                if (attributeName.Contains("JOINTS") ||
                    attributeName.Contains("WEIGHTS"))
                {
                    // Discard JOINTS and WEIGHTS
                    continue;
                }

                if (attributeName.Contains("COLOR"))
                {
                    vertexColorEnabled = true;
                }

                var accessor = this.glbModel.Accessors[attributes[i].Value];
                int bufferViewId = accessor.BufferView.Value;
                var bufferView = this.glbModel.BufferViews[bufferViewId];

                IntPtr attributePointer = this.bufferInfos[bufferView.Buffer].bufferPointer + bufferView.ByteOffset + accessor.ByteOffset;
                ElementDescription elementDesc = this.GetElementFromAttribute(attributeName, accessor);
                uint attributeSizeInBytes = (uint)(this.SizeInBytes(accessor) * accessor.Count);
                int strideInBytes = bufferView.ByteStride.HasValue ? bufferView.ByteStride.Value : 0;

                BufferDescription bufferDesc = new BufferDescription(
                                                                    attributeSizeInBytes,
                                                                    BufferFlags.ShaderResource | BufferFlags.VertexBuffer,
                                                                    ResourceUsage.Default,
                                                                    ResourceCpuAccess.None,
                                                                    strideInBytes);

                Buffer buffer = this.graphicsContext.Factory.CreateBuffer(attributePointer, ref bufferDesc);
                var layoutDescription = new LayoutDescription()
                                                .Add(elementDesc);
                vertexBuffersList.Add(new VertexBuffer(buffer, layoutDescription));

                // Create bounding box
                if (elementDesc.Semantic == ElementSemanticType.Position && elementDesc.SemanticIndex == 0)
                {
                    meshBounding = new BoundingBox(
                        new Vector3(accessor.Min[0], accessor.Min[1], accessor.Min[2]),
                        new Vector3(accessor.Max[0], accessor.Max[1], accessor.Max[2]));
                }
            }

            VertexBuffer[] meshVertexBuffers = vertexBuffersList.ToArray();

            // Create Index buffer
            var indicesAccessor = this.glbModel.Accessors[primitive.Indices.Value];
            var indicesbufferView = this.glbModel.BufferViews[indicesAccessor.BufferView.Value];

            IntPtr indicesPointer = this.bufferInfos[indicesbufferView.Buffer].bufferPointer + indicesbufferView.ByteOffset + indicesAccessor.ByteOffset;
            var indexFormatInfo = this.GetIndexFormat(indicesAccessor.ComponentType);
            uint indexSizeInBytes = (uint)(indexFormatInfo.size * indicesAccessor.Count);
            int indexStrideInBytes = indicesbufferView.ByteStride.HasValue ? indicesbufferView.ByteStride.Value : 0;

            var iBufferDesc = new BufferDescription(indexSizeInBytes, BufferFlags.IndexBuffer, ResourceUsage.Default, ResourceCpuAccess.None, indexStrideInBytes);
            Buffer iBuffer = this.graphicsContext.Factory.CreateBuffer(indicesPointer, ref iBufferDesc);
            var indexBuffer = new IndexBuffer(iBuffer, indexFormatInfo.format, flipWinding: true);

            // Get Topology
            primitive.Mode.ToEverginePrimitive(out var primitiveTopology);

            // Get material
            int materialIndex = 0;
            if (primitive.Material.HasValue)
            {
                int materialId = primitive.Material.Value;
                materialIndex = this.ReadMaterial(materialId, vertexColorEnabled);
            }

            // Create Mesh
            return new Mesh(meshVertexBuffers, indexBuffer, primitiveTopology)
            {
                BoundingBox = meshBounding,
                MaterialIndex = materialIndex,
            };
        }

        private ElementDescription GetElementFromAttribute(string name, Accessor accessor)
        {
            var semanticSplit = name.Split('_');
            var usageStr = semanticSplit[0];
            ElementSemanticType semantic;
            uint semanticIndex;

            switch (usageStr)
            {
                default:
                case "POSITION":
                    semantic = ElementSemanticType.Position;
                    break;
                case "NORMAL":
                    semantic = ElementSemanticType.Normal;
                    break;
                case "TANGENT":
                    semantic = ElementSemanticType.Tangent;
                    break;
                case "TEXCOORD":
                    semantic = ElementSemanticType.TexCoord;
                    break;
                case "COLOR":
                    semantic = ElementSemanticType.Color;
                    break;
                case "JOINTS":
                    semantic = ElementSemanticType.BlendIndices;
                    break;
                case "WEIGHTS":
                    semantic = ElementSemanticType.BlendWeight;
                    break;
            }

            // Usage index
            if (semanticSplit.Length > 1)
            {
                semanticIndex = uint.Parse(semanticSplit[1]);
            }
            else
            {
                semanticIndex = 0;
            }

            // Element Format
            ElementFormat format;
            switch (accessor.ComponentType)
            {
                case Accessor.ComponentTypeEnum.BYTE:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = accessor.Normalized ? ElementFormat.ByteNormalized : ElementFormat.Byte;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = accessor.Normalized ? ElementFormat.Byte2Normalized : ElementFormat.Byte2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = accessor.Normalized ? ElementFormat.Byte3Normalized : ElementFormat.Byte3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = accessor.Normalized ? ElementFormat.Byte4Normalized : ElementFormat.Byte4;
                            break;
                    }

                    break;
                case Accessor.ComponentTypeEnum.UNSIGNED_BYTE:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = accessor.Normalized ? ElementFormat.UByteNormalized : ElementFormat.UByte;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = accessor.Normalized ? ElementFormat.UByte2Normalized : ElementFormat.UByte2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = accessor.Normalized ? ElementFormat.UByte3Normalized : ElementFormat.UByte3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = accessor.Normalized ? ElementFormat.UByte4Normalized : ElementFormat.UByte4;
                            break;
                    }

                    break;

                case Accessor.ComponentTypeEnum.SHORT:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = accessor.Normalized ? ElementFormat.ShortNormalized : ElementFormat.Short;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = accessor.Normalized ? ElementFormat.Short2Normalized : ElementFormat.Short2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = accessor.Normalized ? ElementFormat.Short3Normalized : ElementFormat.Short3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = accessor.Normalized ? ElementFormat.Short4Normalized : ElementFormat.Short4;
                            break;
                    }

                    break;

                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = accessor.Normalized ? ElementFormat.UShortNormalized : ElementFormat.UShort;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = accessor.Normalized ? ElementFormat.UShort2Normalized : ElementFormat.UShort2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = accessor.Normalized ? ElementFormat.UShort3Normalized : ElementFormat.UShort3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = accessor.Normalized ? ElementFormat.UShort4Normalized : ElementFormat.UShort4;
                            break;
                    }

                    break;

                case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = ElementFormat.UInt;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = ElementFormat.UInt2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = ElementFormat.UInt3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = ElementFormat.UInt4;
                            break;
                    }

                    break;

                default:
                case Accessor.ComponentTypeEnum.FLOAT:

                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            format = ElementFormat.Float;
                            break;
                        case Accessor.TypeEnum.VEC2:
                            format = ElementFormat.Float2;
                            break;
                        case Accessor.TypeEnum.VEC3:
                            format = ElementFormat.Float3;
                            break;
                        case Accessor.TypeEnum.VEC4:
                            format = ElementFormat.Float4;
                            break;
                    }

                    break;
            }

            return new ElementDescription(format, semantic, semanticIndex);
        }

        private int SizeInBytes(Accessor accessor)
        {
            switch (accessor.ComponentType)
            {
                case Accessor.ComponentTypeEnum.FLOAT:

                    switch (accessor.Type)
                    {
                        default:
                        case Accessor.TypeEnum.SCALAR:
                            return 4;

                        case Accessor.TypeEnum.VEC2:
                            return 8;

                        case Accessor.TypeEnum.VEC3:
                            return 12;

                        case Accessor.TypeEnum.VEC4:
                            return 16;
                    }
            }

            return 0;
        }

        private (IndexFormat format, int size) GetIndexFormat(Accessor.ComponentTypeEnum componentType)
        {
            switch (componentType)
            {
                default:
                case Accessor.ComponentTypeEnum.SHORT:
                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                    return (IndexFormat.UInt16, 2);
                case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                    return (IndexFormat.UInt32, 4);
            }
        }

        private int ReadMaterial(int materialId, bool vertexColorEnabled)
        {
            var glbMaterial = this.glbModel.Materials[materialId];
            if (!this.materials.ContainsKey(materialId))
            {
                // Get the base color
                LinearColor baseColor = new LinearColor(1, 1, 1, 1);
                Texture baseColorTexture = null;
                SamplerState baseColorSampler = null;
                if (glbMaterial.PbrMetallicRoughness != null)
                {
                    baseColor = glbMaterial.PbrMetallicRoughness.BaseColorFactor.ToLinearColor();

                    // Get the baseColor texture
                    if (this.glbModel.Images != null && glbMaterial.PbrMetallicRoughness.BaseColorTexture != null)
                    {
                        var textureId = glbMaterial.PbrMetallicRoughness.BaseColorTexture.Index;
                        var result = this.ReadTexture(textureId);

                        baseColorTexture = result.texture;
                        baseColorSampler = result.sampler;
                    }
                }
                else if (glbMaterial.Extensions != null && glbMaterial.Extensions.TryGetValue("KHR_materials_pbrSpecularGlossiness", out var pbrSpecularGlossiness))
                {
                    var jobject = pbrSpecularGlossiness as JObject;
                    var diffuseFactorToken = jobject.SelectToken("diffuseFactor");
                    if (diffuseFactorToken != null && diffuseFactorToken.HasValues)
                    {
                        baseColor = new LinearColor(
                                                    (float)diffuseFactorToken[0],
                                                    (float)diffuseFactorToken[1],
                                                    (float)diffuseFactorToken[2],
                                                    (float)diffuseFactorToken[3]);
                    }

                    var diffuseTextureToken = jobject.SelectToken("diffuseTexture");
                    if (diffuseTextureToken != null && diffuseTextureToken.HasValues)
                    {
                        int textureId = (int)diffuseTextureToken["index"];
                        var result = this.ReadTexture(textureId);

                        baseColorTexture = result.texture;
                        baseColorSampler = result.sampler;
                    }
                }

                var material = this.CreateEngineMaterial(baseColor.ToColor(), baseColorTexture, baseColorSampler, glbMaterial.AlphaMode, baseColor.A, glbMaterial.AlphaCutoff, vertexColorEnabled);
                this.materials.Add(materialId, (glbMaterial.Name, material));

                return this.materials.Count - 1;
            }

            return this.materials.Keys.ToList().IndexOf(materialId);
        }

        private Material CreateEngineMaterial(Color baseColor, Texture baseColorTexture, SamplerState baseColorSampler, AlphaModeEnum alphaMode, float alpha, float alphaCutOff, bool vertexColorEnabled)
        {
            RenderLayerDescription layer;
            switch (alphaMode)
            {
                default:
                case AlphaModeEnum.MASK:
                case AlphaModeEnum.OPAQUE:
                    layer = this.opaqueLayer;
                    break;
                case AlphaModeEnum.BLEND:
                    layer = alpha < 1.0f ? this.alphaLayer : this.opaqueLayer;
                    break;
            }

            var effect = this.assetsService.Load<Effect>(LoadModelResourceIDs.Effects.SolidEffect);
            SolidEffect material = new SolidEffect(effect)
            {
                Parameters_Color = baseColor.ToVector3(),
                Parameters_Alpha = alpha,
                BaseColorTexture = baseColorTexture,
                BaseColorSampler = baseColorSampler,
                LayerDescription = layer,
                Parameters_AlphaCutOff = alphaCutOff,
            };

            if (vertexColorEnabled)
            {
                material.ActiveDirectivesNames = new string[] { "VERTEXCOLOR" };
            }

            if (baseColorTexture != null)
            {
                material.ActiveDirectivesNames = new string[] { "TEXTURECOLOR" };
            }

            return material.Material;
        }

        private (Texture texture, SamplerState sampler) ReadTexture(int textureId)
        {
            // Get texture info
            if (textureId > this.glbModel.Textures.Length)
            {
                return (null, null);
            }

            var glbTexture = this.glbModel.Textures[textureId];
            Texture texture = null;
            SamplerState sampler = null;

            // Get image info
            int imageId = -1;
            if (glbTexture.Source.HasValue)
            {
                imageId = glbTexture.Source.Value;
            }

            if (imageId >= 0 && imageId < this.glbModel.Images.Length)
            {
                var glbImage = this.glbModel.Images[imageId];
                if (glbImage.BufferView.HasValue)
                {
                    if (this.images.TryGetValue(imageId, out var textureCached))
                    {
                        texture = textureCached;
                    }
                    else
                    {
                        texture = this.ReadImage(imageId);
                    }
                }
            }

            // Get sampler info
            int samplerId = -1;
            if (glbTexture.Sampler.HasValue)
            {
                samplerId = glbTexture.Sampler.Value;
            }

            if (samplerId >= 0 && samplerId < this.glbModel.Samplers.Length)
            {
                var glbSampler = this.glbModel.Samplers[samplerId];
                sampler = glbSampler.WrapS == Sampler.WrapSEnum.CLAMP_TO_EDGE ? this.linearClampSampler : this.linearWrapSampler;
            }

            return (texture, sampler);
        }

        private Texture ReadImage(int imageId)
        {
            Texture result = null;

            using (Stream fileStream = this.glbModel.OpenImageFile(imageId, this.GetExternalFileSolver))
            {
                var imageInfo = SixLabors.ImageSharp.Image.Identify(fileStream);

                TextureDescription desc = new TextureDescription()
                {
                    Type = TextureType.Texture2D,
                    Width = (uint)imageInfo.Width,
                    Height = (uint)imageInfo.Height,
                    Depth = 1,
                    ArraySize = 1,
                    Faces = 1,
                    Usage = ResourceUsage.Default,
                    CpuAccess = ResourceCpuAccess.None,
                    Flags = TextureFlags.ShaderResource,
                    Format = PixelFormat.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    SampleCount = TextureSampleCount.None,
                };
                result = this.graphicsContext.Factory.CreateTexture(ref desc);

                fileStream.Seek(0, SeekOrigin.Begin);

                using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fileStream))
                {
                    RawImageLoader.CopyImageToArrayPool(image, false, out _, out byte[] data);
                    this.graphicsContext.UpdateTextureData(result, data);
                }

                // Read
                fileStream.Flush();
            }

            return result;
        }

        private byte[] GetExternalFileSolver(string empty)
        {
            return this.binaryChunk;
        }

        private string MakeSafeName(string name)
        {
            foreach (char c in this.invalidNameCharacters)
            {
                name = name.Replace(c, '_');
            }

            return name;
        }
    }
}