using Assimp;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ege.Model
{
    public class DynamicModel : Mesh
    {
        internal Scene scene;
        internal List<Mesh> meshes = new List<Mesh>();        
        internal List<Animation> animations = new List<Animation>();
        internal Node rootNode = new Node();        
        internal int time;
        
        string extention;

        public DynamicModel(string file) : base(false)
        {
            PostProcessSteps postProcessSteps = 
                PostProcessSteps.Triangulate | 
                PostProcessSteps.FlipUVs |
                PostProcessSteps.CalculateTangentSpace | 
                PostProcessSteps.GenerateSmoothNormals | 
                PostProcessSteps.GenerateUVCoords;
            
            LoadModel(file, postProcessSteps);
        }

        private void LoadModel(string file, PostProcessSteps postProcessSteps)
        {
            AssimpContext importer = new AssimpContext();
            scene = importer.ImportFile(file, postProcessSteps);

            if (scene == null || scene.RootNode == null ||
                (scene.SceneFlags & SceneFlags.Incomplete) == SceneFlags.Incomplete)
            {
                Console.WriteLine("ERROR::ASSIMP (DynamicModel)");
                return;
            }

            extention = Path.GetExtension(file);
            Materials.directory = Path.GetDirectoryName(file);

            rootNode = scene.RootNode;
            ProcessNode();
            ProcessAnimations();
        }

        private void ProcessNode()
        {
            for (int i = 0; i < scene.RootNode.ChildCount; i++)
            {
                for (int j = 0; j < scene.RootNode.Children[i].MeshCount; j++)
                {
                    Assimp.Mesh mesh = scene.Meshes[scene.RootNode.Children[i].MeshIndices[j]];
                    meshes.Add(ProcessMesh(mesh));
                }
            }
        }

        private Mesh ProcessMesh(Assimp.Mesh mesh)
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
                    indices.Add((uint)face.Indices[j]);
            }

            // bones
            List<BoneTransform> boneTransforms = new List<BoneTransform>();
            for (int b = 0; b < mesh.BoneCount; b++)
            {
                // Bone.BoneTransform
                Bone bone = mesh.Bones[b];
                boneTransforms.Add(new BoneTransform(bone.Name, Maths.ConvertMatrix(bone.OffsetMatrix)));

                // Vertices.VertexWeight
                for (int w = 0; w < bone.VertexWeightCount; w++)
                {
                    VertexWeight vw = bone.VertexWeights[w];
                    int access = vw.VertexID;
                    Vertex vertex = vertices[access];

                    if (vertices[access].BoneID.X == 0 && vertices[access].BoneWeight.X == 0)
                    {
                        vertex.BoneID.X = b;
                        vertex.BoneWeight.X = vw.Weight;
                        vertices[access] = vertex;
                    }
                    else if (vertices[access].BoneID.Y == 0 && vertices[access].BoneWeight.Y == 0)
                    {
                        vertex.BoneID.Y = b;
                        vertex.BoneWeight.Y = vw.Weight;
                        vertices[access] = vertex;
                    }
                    else if (vertices[access].BoneID.Z == 0 && vertices[access].BoneWeight.Z == 0)
                    {
                        vertex.BoneID.Z = b;
                        vertex.BoneWeight.Z = vw.Weight;
                        vertices[access] = vertex;
                    }
                    else
                    {
                        vertex.BoneID.W = b;
                        vertex.BoneWeight.W = vw.Weight;
                        vertices[access] = vertex;
                    }
                }
            }


            Materials material1 = new Materials();

            // textures                    
            List<TextureInfo> textureInfos = new List<TextureInfo>();
            Material material = scene.Materials[mesh.MaterialIndex];
            
            List<TextureInfo> diffuseMaps = material1.LoadMaterialTextures(material, TextureType.Diffuse);
            textureInfos.AddRange(diffuseMaps);
            
            List<TextureInfo> specularMaps = material1.LoadMaterialTextures(material, TextureType.Specular);
            textureInfos.AddRange(specularMaps);

            List<TextureInfo> normalMaps = material1.LoadMaterialTextures(material, TextureType.Normals);
            textureInfos.AddRange(normalMaps);
            
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

        public void DrawAll(Shader shader)
        {
            time++;
            foreach (Mesh mesh in meshes)
            {
                UpdateAnimation(time / 30f, 0);
                boneTransforms = mesh.boneTransforms;
                mesh.Draw(shader);
            }
        }

        // Animation
        private void ProcessAnimations()
        {
            for (int i = 0; i < scene.AnimationCount; i++)
                animations.Add(scene.Animations[i]);
        }

        private void UpdateAnimation(float time, int animationIndex)
        {
            if (animationIndex >= 0 && animationIndex < animations.Count)
            {      
                Animation target = animations[animationIndex];
                float tickPerSecond = target.TicksPerSecond != 0 ? (float)target.TicksPerSecond : 60.0f;
                float ticks = time * tickPerSecond;
                float animationTime = ticks % (float)target.DurationInTicks;
                ProcessNode(target, animationTime, rootNode, Matrix4.Identity);
            }
        }

        private void ProcessNode(Animation target, float animationTime, Node node, Matrix4 parentTransform)
        {
            string nodeName = node.Name;
            Matrix4 nodeTransform = Maths.ConvertMatrix(node.Transform);            
            
            NodeAnimationChannel boneAnimation = FindBoneAnimation(target, nodeName);
            
            if (boneAnimation != null)
            {
                Vector3 interpolatedScale = CalcInterpolateScale(animationTime, boneAnimation);
                Matrix4 scaleMatrix = Matrix4.CreateScale(interpolatedScale);

                OpenTK.Quaternion interpolatedRotation = CalcInterpolatedRotation(animationTime, boneAnimation);
                Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(interpolatedRotation);

                Vector3 interpolatedPosition = CalcInterpolatedPosition(animationTime, boneAnimation);
                Matrix4 translationMatrix = Matrix4.CreateTranslation(interpolatedPosition);

                nodeTransform = Matrix4.Mult(rotationMatrix, translationMatrix);
                nodeTransform = Matrix4.Mult(scaleMatrix, nodeTransform);
            }

            Matrix4 toGlobalSpace = Matrix4.Mult(nodeTransform, parentTransform);

            BoneTransform bone = FindBone(nodeName);
            if (bone != null)
                bone.SetTransformation(Matrix4.Mult(bone.GetOffsetMatrix(), toGlobalSpace));

            for (int i = 0; i < node.ChildCount; i++)
            {
                Node childNode = node.Children[i];
                ProcessNode(target, animationTime, childNode, toGlobalSpace);
            }
        }

        private NodeAnimationChannel FindBoneAnimation(Animation target, string nodeName)
        {
            for (int i = 0; i < target.NodeAnimationChannelCount; i++)
            {
                NodeAnimationChannel nodeAnim = target.NodeAnimationChannels[i];
                if (nodeAnim.NodeName.Equals(nodeName)) return nodeAnim;
            }
            return null;
        }

        private Vector3 CalcInterpolateScale(float timeAt, NodeAnimationChannel boneAnimation)
        {
            if (boneAnimation.ScalingKeyCount == 1)
                return Maths.ConvertVector3(boneAnimation.ScalingKeys[0].Value);

            int index0 = FindScaleIndex(timeAt, boneAnimation);
            int index1 = index0 + 1;
            float time0 = (float)boneAnimation.ScalingKeys[index0].Time;
            float time1 = (float)boneAnimation.ScalingKeys[index1].Time;
            float deltaTime = time1 - time0;
            float percentage = (timeAt - time0) / deltaTime;

            Vector3 start = Maths.ConvertVector3(boneAnimation.ScalingKeys[index0].Value);
            Vector3 end = Maths.ConvertVector3(boneAnimation.ScalingKeys[index1].Value);
            Vector3 delta = Vector3.Subtract(end, start);
            delta = Vector3.Multiply(delta, percentage);

            return Vector3.Add(start, delta);
        }

        private int FindScaleIndex(float timeAt, NodeAnimationChannel boneAnimation)
        {
            if (boneAnimation.ScalingKeyCount > 0)
            {
                for (int i = 0; i < boneAnimation.ScalingKeyCount - 1; i++)
                {
                    if (timeAt < boneAnimation.ScalingKeys[i + 1].Time) return i;
                }
            }
            return 0;
        }

        private OpenTK.Quaternion CalcInterpolatedRotation(float timeAt, NodeAnimationChannel boneAnimation)
        {
            if (boneAnimation.RotationKeyCount == 1)
                return Maths.ConvertQuaternion(boneAnimation.RotationKeys[0].Value);

            int index0 = FindRotationIndex(timeAt, boneAnimation);
            int index1 = index0 + 1;
            float time0 = (float)boneAnimation.RotationKeys[index0].Time;
            float time1 = (float)boneAnimation.RotationKeys[index1].Time;
            float deltaTime = time1 - time0;
            float percentage = (timeAt - time0) / deltaTime;

            OpenTK.Quaternion start = Maths.ConvertQuaternion(boneAnimation.RotationKeys[index0].Value);
            OpenTK.Quaternion end = Maths.ConvertQuaternion(boneAnimation.RotationKeys[index1].Value);

            return OpenTK.Quaternion.Slerp(start, end, percentage);
        }

        private int FindRotationIndex(float timeAt, NodeAnimationChannel boneAnimation)
        {
            if (boneAnimation.RotationKeyCount > 0)
            {
                for (int i = 0; i < boneAnimation.RotationKeyCount - 1; i++)
                    if (timeAt < boneAnimation.RotationKeys[i + 1].Time) return i;
            }
            return 0;
        }

        private Vector3 CalcInterpolatedPosition(float timeAt, NodeAnimationChannel boneAnimation)
        {
            if (boneAnimation.PositionKeyCount == 1)
                return Maths.ConvertVector3(boneAnimation.PositionKeys[0].Value);

            int index0 = FindPositionIndex(timeAt, boneAnimation);
            int index1 = index0 + 1;
            float time0 = (float)boneAnimation.PositionKeys[index0].Time;
            float time1 = (float)boneAnimation.PositionKeys[index1].Time;
            float deltaTime = time1 - time0;
            float percentage = (timeAt - time0) / deltaTime;

            Vector3 start = Maths.ConvertVector3(boneAnimation.PositionKeys[index0].Value);
            Vector3 end = Maths.ConvertVector3(boneAnimation.PositionKeys[index1].Value);
            Vector3 delta = Vector3.Subtract(end, start);

            return Vector3.Add(start, Vector3.Multiply(delta, percentage));
        }

        private int FindPositionIndex(float timeAt, NodeAnimationChannel boneAnimation)
        {
            if (boneAnimation.PositionKeyCount > 0)
            {
                for (int i = 0; i < boneAnimation.PositionKeyCount - 1; i++)
                    if (timeAt < boneAnimation.PositionKeys[i + 1].Time) return i;
            }
            return 0;
        }

        private BoneTransform FindBone(string nodeName)
        {
            foreach (BoneTransform b in boneTransforms)
                if (b.GetName().Equals(nodeName)) return b;
            return null;
        }
    }
}
