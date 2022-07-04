using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using Ege.Model;

namespace Ege
{
	public class Skybox
	{

		[Conditional("DEBUG")]
		[DebuggerStepThrough]
		public static void CheckLastError()
		{
			ErrorCode errorCode = GL.GetError();
			if (errorCode != ErrorCode.NoError)
			{
				throw new Exception(errorCode.ToString());
			}
		}

		readonly Texture _texture;

		private int VBO, VAO;

		private readonly float[] vertices = {
			// positions          
			-1.0f,  1.0f, -1.0f,
			-1.0f, -1.0f, -1.0f,
			 1.0f, -1.0f, -1.0f,
			 1.0f, -1.0f, -1.0f,
			 1.0f,  1.0f, -1.0f,
			-1.0f,  1.0f, -1.0f,

			-1.0f, -1.0f,  1.0f,
			-1.0f, -1.0f, -1.0f,
			-1.0f,  1.0f, -1.0f,
			-1.0f,  1.0f, -1.0f,
			-1.0f,  1.0f,  1.0f,
			-1.0f, -1.0f,  1.0f,

			 1.0f, -1.0f, -1.0f,
			 1.0f, -1.0f,  1.0f,
			 1.0f,  1.0f,  1.0f,
			 1.0f,  1.0f,  1.0f,
			 1.0f,  1.0f, -1.0f,
			 1.0f, -1.0f, -1.0f,

			-1.0f, -1.0f,  1.0f,
			-1.0f,  1.0f,  1.0f,
			 1.0f,  1.0f,  1.0f,
			 1.0f,  1.0f,  1.0f,
			 1.0f, -1.0f,  1.0f,
			-1.0f, -1.0f,  1.0f,

			-1.0f,  1.0f, -1.0f,
			 1.0f,  1.0f, -1.0f,
			 1.0f,  1.0f,  1.0f,
			 1.0f,  1.0f,  1.0f,
			-1.0f,  1.0f,  1.0f,
			-1.0f,  1.0f, -1.0f,

			-1.0f, -1.0f, -1.0f,
			-1.0f, -1.0f,  1.0f,
			 1.0f, -1.0f, -1.0f,
			 1.0f, -1.0f, -1.0f,
			-1.0f, -1.0f,  1.0f,
			 1.0f, -1.0f,  1.0f
		};

		public Skybox(string skyboxFolder)
		{
			skyboxFolder = skyboxFolder + "/Skybox";
			string[] faces = new string[]
			{
				skyboxFolder + "/right.jpg",
				skyboxFolder + "/left.jpg",
				skyboxFolder + "/top.jpg",
				skyboxFolder + "/bottom.jpg",
				skyboxFolder + "/front.jpg",
				skyboxFolder + "/back.jpg"
			};
			_texture = new Texture(faces);
			Init();
		}

		private void Init()
		{
			GL.GenVertexArrays(1, out VAO);
			GL.GenBuffers(1, out VBO);
			GL.BindVertexArray(VAO);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
		}

		public void Draw()
		{
			GL.DepthFunc(DepthFunction.Lequal);
			GL.BindVertexArray(VAO);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.TextureCubeMap, _texture.Handle);
			GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Length);
			GL.BindVertexArray(0);
			GL.DepthFunc(DepthFunction.Less);
		}
	}
}
