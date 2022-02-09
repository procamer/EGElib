using Assimp;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Ege.Model
{
    public class DynamicMesh : Transform
    {
        public struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 TexCoords;
            public Vector3 Tangent;
            public Vector3 Bitangent;
            public Vector4 BoneID;
            public Vector4 BoneWeight;

            public static int SizeInBytes() => Vector3.SizeInBytes * 4 + Vector2.SizeInBytes + Vector4.SizeInBytes * 2;
        }

        internal List<Vertex> vertices = new List<Vertex>();
        internal List<uint> indices = new List<uint>();
        internal List<TextureInfo> textures = new List<TextureInfo>();
        internal List<BoneTransform> boneTransforms = new List<BoneTransform>();

        internal Materials materials = new Materials();


        private readonly int VAO;
        private readonly int VBO;
        private readonly int EBO;

        public DynamicMesh()
        {
        }

        public DynamicMesh(List<Vertex> vertices, List<uint> indices, List<TextureInfo> textures)
        {
            this.vertices = vertices;
            this.indices = indices;
            this.textures = textures;

            // VAO
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // VBO
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vertex.SizeInBytes(), vertices.ToArray(), BufferUsageHint.StaticDraw);

            // Attributes
            GL.EnableVertexAttribArray(0); // vertex.Positions
            GL.EnableVertexAttribArray(1); // vertex.Normals
            GL.EnableVertexAttribArray(2); // vertex.TexCoords
            GL.EnableVertexAttribArray(3); // vertex.Tangents
            GL.EnableVertexAttribArray(4); // vertex.Bitangents
            GL.EnableVertexAttribArray(5); // vertex.BoneID
            GL.EnableVertexAttribArray(6); // vertex.BoneWeight

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes(), 0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes(), sizeof(float) * 3);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vertex.SizeInBytes(), sizeof(float) * 6);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes(), sizeof(float) * 8);
            GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes(), sizeof(float) * 11);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, Vertex.SizeInBytes(), sizeof(float) * 14);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, Vertex.SizeInBytes(), sizeof(float) * 18);

            // EBO
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void Draw(Shader shader)
        {
            // Bind Textures
            uint diffuseNr = 1;
            uint specularNr = 1;
            uint normalNr = 1;
            //uint heightNr = 1;
            shader.Use();
            for (int i = 0; i < textures.Count; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                string number = "";
                TextureType name = textures[i].Type;
                if (name == TextureType.Diffuse) number = "texture_diffuse" + diffuseNr++.ToString();
                else if (name == TextureType.Specular) number = "texture_specular" + specularNr++.ToString();
                else if (name == TextureType.Normals) number = "texture_normal" + normalNr++.ToString();
                GL.Uniform1(GL.GetUniformLocation(shader.Handle, number), i);
                GL.BindTexture(TextureTarget.Texture2D, textures[i].Id);
            }

            for (int j = 0; j < boneTransforms.Count; j++)
                shader.SetMat4("boneTransform[" + j + "]", boneTransforms[j].GetTransformation());

            // Draw mesh
            GL.BindVertexArray(VAO);
            GL.DrawElements(BeginMode.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

    }

}
