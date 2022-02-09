﻿using Assimp;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ege.Model
{
    public class StaticModel : StaticMesh
    {
        internal Scene scene;
        internal readonly List<StaticMesh> meshes = new List<StaticMesh>();

        public StaticModel(string file, PostProcessSteps postProcessSteps)
        {
            LoadModel(file, postProcessSteps);
        }

        private void LoadModel(string file, PostProcessSteps postProcessSteps)
        {
            AssimpContext context = new AssimpContext();
            scene = context.ImportFile(file, postProcessSteps);
            if (scene == null ||
                scene.RootNode == null ||
                (scene.SceneFlags & SceneFlags.Incomplete) == SceneFlags.Incomplete)
            {
                Console.WriteLine("ERROR::ASSIMP (StaticModel)");
                return;
            }
            Materials.directory = Path.GetDirectoryName(file);
            ProcessNode();
        }

        private void ProcessNode()
        {
            for (int i = 0; i < scene.RootNode.ChildCount; i++)
            {
                for (int j = 0; j < scene.RootNode.Children[i].MeshCount; j++)
                {
                    Mesh mesh = scene.Meshes[scene.RootNode.Children[i].MeshIndices[j]];
                    meshes.Add(ProcessMesh(mesh));
                }
            }
        }

        private StaticMesh ProcessMesh(Mesh mesh)
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
                    Bitangent = Maths.ConvertVector3(mesh.BiTangents[i])
                };

                if (mesh.HasTextureCoords(0))
                {
                    vertex.TexCoords = Maths.ConvertVector2(mesh.TextureCoordinateChannels[0][i]);
                }
                else
                {
                    vertex.TexCoords = Vector2.Zero;
                }
                vertices.Add(vertex);
            }

            // indices
            List<uint> indices = new List<uint>();
            for (int i = 0; i < mesh.FaceCount; i++)
            {
                Face face = mesh.Faces[i];
                for (int j = 0; j < face.IndexCount; j++)
                {
                    indices.Add((uint)face.Indices[j]);
                }
            }

            Materials material1 = new Materials( );

            // textures                    
            List<TextureInfo> textureInfos = new List<TextureInfo>();
            Material material = scene.Materials[mesh.MaterialIndex];
            List<TextureInfo> diffuseMaps = material1.LoadMaterialTextures(material, TextureType.Diffuse);
            textureInfos.AddRange(diffuseMaps);
            List<TextureInfo> specularMaps = material1.LoadMaterialTextures(material, TextureType.Specular);
            textureInfos.AddRange(specularMaps);
            List<TextureInfo> normalMaps = material1.LoadMaterialTextures(material, TextureType.Height);
            textureInfos.AddRange(normalMaps);
            //List<TextureInfo> heightMaps = returnMesh.materials.LoadMaterialTextures(material, TextureType.Ambient);
            //textureInfos.AddRange(heightMaps);
            
            return new StaticMesh(vertices,indices,textureInfos);
        }

        public void DrawAll(Shader shader)
        {
            foreach (StaticMesh mesh in meshes)
            {
                mesh.Draw(shader); 
            }
        }

    }

}
