using Ege.Model;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Ege
{
    // Omnidirectional (cubemap) shadow maps for a fixed set of point lights.
    // Static geometry is rendered once into a cache; every frame the cache is copied
    // and only the animated models are drawn on top of it.
    // Lights are assumed not to move; call RenderStatic again if they do.
    public class PointShadowMaps : IDisposable
    {
        public int Size { get; }
        public float FarPlane { get; }
        public int FirstTextureUnit { get; }
        public Matrix4 Projection { get; }

        private readonly IReadOnlyList<PointLight> lights;
        private readonly Shader depthShader;
        private readonly Shader depthSkinnedShader;

        private readonly int[] staticCubemaps;
        private readonly int[] staticFbos;
        private readonly int[] cubemaps;
        private readonly int[] fbos;
        private bool disposed;

        public PointShadowMaps(IReadOnlyList<PointLight> lights, Shader depthShader, Shader depthSkinnedShader,
            int size = 512, float farPlane = 3000f, int firstTextureUnit = 10)
        {
            int maxUnits = GL.GetInteger(GetPName.MaxTextureImageUnits);
            if (firstTextureUnit + lights.Count > maxUnits)
                throw new NotSupportedException(
                    $"{lights.Count} shadow maps from texture unit {firstTextureUnit} need {firstTextureUnit + lights.Count} units, the GPU has {maxUnits}.");

            this.lights = lights;
            this.depthShader = depthShader;
            this.depthSkinnedShader = depthSkinnedShader;
            Size = size;
            FarPlane = farPlane;
            FirstTextureUnit = firstTextureUnit;
            Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f), 1f, 0.1f, farPlane);

            depthShader.SetFloat("far_plane", farPlane);
            depthSkinnedShader?.SetFloat("far_plane", farPlane);

            CreateCubemaps(out staticCubemaps, out staticFbos);
            CreateCubemaps(out cubemaps, out fbos);
        }

        // Renders models that never move into the cached maps.
        public void RenderStatic(IEnumerable<Model.Model> staticModels)
        {
            BeginPass(out int[] viewport);
            for (int i = 0; i < lights.Count; i++)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, staticFbos[i]);
                GL.Clear(ClearBufferMask.DepthBufferBit);
                foreach (Model.Model model in staticModels)
                    DrawDepth(model, lights[i].position);
            }
            EndPass(viewport);
        }

        // Rebuilds the live maps: cached static depth + the given (animated) models.
        public void Update(IEnumerable<Model.Model> dynamicModels)
        {
            BeginPass(out int[] viewport);
            for (int i = 0; i < lights.Count; i++)
            {
                GL.CopyImageSubData(
                    staticCubemaps[i], ImageTarget.TextureCubeMap, 0, 0, 0, 0,
                    cubemaps[i], ImageTarget.TextureCubeMap, 0, 0, 0, 0,
                    Size, Size, 6);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbos[i]);
                foreach (Model.Model model in dynamicModels)
                    DrawDepth(model, lights[i].position);
            }
            EndPass(viewport);
        }

        // Binds the live maps to their texture units (FirstTextureUnit + light index).
        public void BindTextures()
        {
            for (int i = 0; i < lights.Count; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + FirstTextureUnit + i);
                GL.BindTexture(TextureTarget.TextureCubeMap, cubemaps[i]);
            }
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        // Points a lighting shader at the maps (depthMaps[] samplers and cubeProjection).
        public void SetUniforms(Shader shader)
        {
            shader.SetMat4("cubeProjection", Projection);
            for (int i = 0; i < lights.Count; i++)
                shader.SetInt("depthMaps[" + i + "]", FirstTextureUnit + i);
        }

        private void DrawDepth(Model.Model model, Vector3 lightPos)
        {
            Shader shader = model.IsAnimated ? depthSkinnedShader : depthShader;
            if (shader == null)
                throw new InvalidOperationException("Animated models need a skinned depth shader.");

            SetShadowMatrices(shader, lightPos);
            shader.SetMat4("transformationMatrix", model.TransformationMatrix());
            model.DrawAll(shader);
        }

        private void SetShadowMatrices(Shader shader, Vector3 lightPos)
        {
            Matrix4[] shadowTransforms = new Matrix4[]
            {
                Matrix4.LookAt(lightPos, lightPos + new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * Projection,
                Matrix4.LookAt(lightPos, lightPos + new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * Projection,
                Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)) * Projection,
                Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)) * Projection,
                Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * Projection,
                Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * Projection
            };

            for (int z = 0; z < 6; ++z)
                shader.SetMat4("shadowMatrices[" + z + "]", shadowTransforms[z]);
            shader.SetVec3("lightPos", lightPos);
        }

        private void BeginPass(out int[] viewport)
        {
            viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            GL.Viewport(0, 0, Size, Size);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1.1f, 1.1f);
        }

        private static void EndPass(int[] viewport)
        {
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(viewport[0], viewport[1], viewport[2], viewport[3]);
        }

        private void CreateCubemaps(out int[] maps, out int[] framebuffers)
        {
            maps = new int[lights.Count];
            framebuffers = new int[lights.Count];
            for (int i = 0; i < lights.Count; i++)
            {
                GL.GenTextures(1, out maps[i]);
                GL.BindTexture(TextureTarget.TextureCubeMap, maps[i]);
                for (int face = 0; face < 6; face++)
                {
                    GL.TexImage2D(
                        TextureTarget.TextureCubeMapPositiveX + face,
                        0,
                        PixelInternalFormat.DepthComponent32f,
                        Size,
                        Size, 0,
                        PixelFormat.DepthComponent,
                        PixelType.Float,
                        IntPtr.Zero);
                }

                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

                framebuffers[i] = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffers[i]);
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, maps[i], 0);
                GL.DrawBuffer(DrawBufferMode.None);
                GL.ReadBuffer(ReadBufferMode.None);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            GL.DeleteTextures(staticCubemaps.Length, staticCubemaps);
            GL.DeleteTextures(cubemaps.Length, cubemaps);
            GL.DeleteFramebuffers(staticFbos.Length, staticFbos);
            GL.DeleteFramebuffers(fbos.Length, fbos);
            disposed = true;
        }
    }
}
