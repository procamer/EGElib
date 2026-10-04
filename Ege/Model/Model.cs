using Assimp;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ege.Model
{
    public abstract class Model : Transform, IDisposable
    {
        internal Scene scene;
        internal readonly List<Mesh> meshes = new List<Mesh>();

        protected bool IsLoaded { get; }

        // true when the model carries skeletal animation (drawn with skinning shaders)
        public bool IsAnimated => IsLoaded && scene.HasAnimations;

        private readonly TextureType normalMapType;
        private bool disposed;

        protected Model(string file, PostProcessSteps postProcessSteps, TextureType normalMapType)
        {
            this.normalMapType = normalMapType;

            AssimpContext importer = new AssimpContext();
            scene = importer.ImportFile(file, postProcessSteps);

            if (scene == null || scene.RootNode == null ||
                (scene.SceneFlags & SceneFlags.Incomplete) == SceneFlags.Incomplete)
            {
                Console.WriteLine($"ERROR::ASSIMP ({GetType().Name})");
                return;
            }

            ProcessNode(scene.RootNode, Matrix4.Identity, Path.GetDirectoryName(file));
            IsLoaded = true;
        }

        private void ProcessNode(Node node, Matrix4 parentTransform, string directory)
        {
            Matrix4 transform = Maths.ConvertMatrix(node.Transform) * parentTransform;

            for (int i = 0; i < node.MeshCount; i++)
                meshes.Add(ProcessMesh(scene.Meshes[node.MeshIndices[i]], transform, directory));

            for (int i = 0; i < node.ChildCount; i++)
                ProcessNode(node.Children[i], transform, directory);
        }

        private Mesh ProcessMesh(Assimp.Mesh mesh, Matrix4 nodeTransform, string directory)
        {
            // vertices
            List<Vertex> vertices = new List<Vertex>();
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                Vertex vertex = new Vertex
                {
                    Position = Maths.ConvertVector3(mesh.Vertices[i]),
                    Normal = Maths.ConvertVector3(mesh.Normals[i]),
                    Tangent = Maths.ConvertVector3(mesh.Tangents[i]),
                    Bitangent = Maths.ConvertVector3(mesh.BiTangents[i]),
                    TexCoords = mesh.HasTextureCoords(0)
                        ? Maths.ConvertVector2(mesh.TextureCoordinateChannels[0][i])
                        : Vector2.Zero
                };
                vertices.Add(vertex);
            }

            // Skinned meshes get their placement from the bone hierarchy at draw time,
            // everything else is baked into model space here.
            if (mesh.BoneCount == 0 && nodeTransform != Matrix4.Identity)
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vertex vertex = vertices[i];
                    vertex.Position = Vector3.TransformPosition(vertex.Position, nodeTransform);
                    vertex.Normal = Vector3.Normalize(Vector3.TransformNormal(vertex.Normal, nodeTransform));
                    vertex.Tangent = Vector3.Normalize(Vector3.TransformVector(vertex.Tangent, nodeTransform));
                    vertex.Bitangent = Vector3.Normalize(Vector3.TransformVector(vertex.Bitangent, nodeTransform));
                    vertices[i] = vertex;
                }
            }

            // indices
            List<uint> indices = new List<uint>();
            for (int i = 0; i < mesh.FaceCount; i++)
            {
                Face face = mesh.Faces[i];
                for (int j = 0; j < face.IndexCount; j++)
                    indices.Add((uint)face.Indices[j]);
            }

            // bones (static meshes have none)
            List<BoneTransform> boneTransforms = new List<BoneTransform>();
            for (int b = 0; b < mesh.BoneCount; b++)
            {
                Bone bone = mesh.Bones[b];
                boneTransforms.Add(new BoneTransform(bone.Name, Maths.ConvertMatrix(bone.OffsetMatrix)));

                for (int w = 0; w < bone.VertexWeightCount; w++)
                {
                    VertexWeight vw = bone.VertexWeights[w];
                    Vertex vertex = vertices[vw.VertexID];

                    if (vertex.BoneID.X == 0 && vertex.BoneWeight.X == 0)
                    {
                        vertex.BoneID.X = b;
                        vertex.BoneWeight.X = vw.Weight;
                    }
                    else if (vertex.BoneID.Y == 0 && vertex.BoneWeight.Y == 0)
                    {
                        vertex.BoneID.Y = b;
                        vertex.BoneWeight.Y = vw.Weight;
                    }
                    else if (vertex.BoneID.Z == 0 && vertex.BoneWeight.Z == 0)
                    {
                        vertex.BoneID.Z = b;
                        vertex.BoneWeight.Z = vw.Weight;
                    }
                    else
                    {
                        vertex.BoneID.W = b;
                        vertex.BoneWeight.W = vw.Weight;
                    }
                    vertices[vw.VertexID] = vertex;
                }
            }

            // textures
            Material material = scene.Materials[mesh.MaterialIndex];
            List<TextureInfo> textureInfos = new List<TextureInfo>();
            textureInfos.AddRange(Materials.LoadMaterialTextures(material, TextureType.Diffuse, directory));
            textureInfos.AddRange(Materials.LoadMaterialTextures(material, TextureType.Specular, directory));
            textureInfos.AddRange(Materials.LoadMaterialTextures(material, normalMapType, directory));

            Mesh returnMesh = new Mesh(scene.HasAnimations)
            {
                vertices = vertices,
                indices = indices,
                textures = textureInfos,
                boneTransforms = boneTransforms
            };
            returnMesh.InitGL();
            return returnMesh;
        }

        public virtual void DrawAll(Shader shader)
        {
            foreach (Mesh mesh in meshes)
                mesh.Draw(shader);
        }

        // Textures are shared between models through the Materials cache,
        // so they are released with Materials.DeleteLoadedTextures(), not here.
        public void Dispose()
        {
            if (disposed) return;
            foreach (Mesh mesh in meshes)
                mesh.Dispose();
            meshes.Clear();
            disposed = true;
        }
    }
}
