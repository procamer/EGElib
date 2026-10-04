using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ege
{
    // A renderable scene: skybox, models with their shaders, point lights and optional shadows.
    // The world takes ownership of everything handed to it and releases it in Dispose.
    public class World : IDisposable
    {
        // lighting shaders must be compiled with this define set to PointLights.Count
        public const string LightCountDefine = "NR_POINT_LIGHTS";

        public IReadOnlyList<PointLight> PointLights { get; }
        public PointShadowMaps Shadows { get; private set; }

        private readonly List<Entity> entities = new List<Entity>();
        private readonly HashSet<Shader> ownedShaders = new HashSet<Shader>();
        private Skybox skybox;
        private Shader skyboxShader;
        private Shader staticShader;   // built-in, created on first use
        private Shader skinnedShader;  // built-in, created on first use
        private bool staticShadowsDirty = true;
        private bool disposed;

        private struct Entity
        {
            public Model.Model Model;
            public Shader Shader;
        }

        public World(IReadOnlyList<PointLight> pointLights)
        {
            PointLights = pointLights;
        }

        // defines to compile lighting shaders with
        public Dictionary<string, object> ShaderDefines()
        {
            return new Dictionary<string, object> { { LightCountDefine, PointLights.Count } };
        }

        public void SetSkybox(Skybox skybox)
        {
            SetSkybox(skybox, Shader.FromResources("skyboxV.glsl", "skyboxF.glsl"));
        }

        public void SetSkybox(Skybox skybox, Shader shader)
        {
            this.skybox = skybox;
            skyboxShader = shader;
            ownedShaders.Add(shader);
        }

        // draws the model with the built-in lighting shader (skinned when animated)
        public void Add(Model.Model model)
        {
            Shader shader;
            if (model.IsAnimated)
                shader = skinnedShader ?? (skinnedShader = Shader.FromResources("skeletalV.glsl", "staticF.glsl", defines: ShaderDefines()));
            else
                shader = staticShader ?? (staticShader = Shader.FromResources("staticV.glsl", "staticF.glsl", defines: ShaderDefines()));
            Add(model, shader);
        }

        public void Add(Model.Model model, Shader shader)
        {
            if (shader.Defines.TryGetValue(LightCountDefine, out object count) &&
                Convert.ToInt32(count) != PointLights.Count)
            {
                throw new ArgumentException(
                    $"Shader was compiled for {count} point lights, the world has {PointLights.Count}.", nameof(shader));
            }

            entities.Add(new Entity { Model = model, Shader = shader });
            ownedShaders.Add(shader);
            if (!model.IsAnimated)
                staticShadowsDirty = true;
        }

        // shadows with the built-in depth shaders
        public void EnableShadows(int size = 512, float farPlane = 3000f, int firstTextureUnit = 10)
        {
            EnableShadows(
                Shader.FromResources("depthV.glsl", "depthF.glsl", "depthG.glsl"),
                Shader.FromResources("depthSkeletalV.glsl", "depthF.glsl", "depthG.glsl"),
                size, farPlane, firstTextureUnit);
        }

        public void EnableShadows(Shader depthShader, Shader depthSkinnedShader,
            int size = 512, float farPlane = 3000f, int firstTextureUnit = 10)
        {
            Shadows?.Dispose();
            Shadows = new PointShadowMaps(PointLights, depthShader, depthSkinnedShader, size, farPlane, firstTextureUnit);
            ownedShaders.Add(depthShader);
            if (depthSkinnedShader != null)
                ownedShaders.Add(depthSkinnedShader);
            staticShadowsDirty = true;
        }

        public void Draw(Camera camera)
        {
            if (Shadows != null)
            {
                if (staticShadowsDirty)
                {
                    Shadows.RenderStatic(entities.Where(e => !e.Model.IsAnimated).Select(e => e.Model));
                    staticShadowsDirty = false;
                }
                Shadows.Update(entities.Where(e => e.Model.IsAnimated).Select(e => e.Model));
                Shadows.BindTextures();
            }

            Matrix4 view = camera.ViewMatrix();
            Matrix4 projection = camera.ProjectionMatrix();

            if (skybox != null)
            {
                // rotation only, so the sky stays at infinity
                skyboxShader.SetMat4("viewMatrix", new Matrix4(new Matrix3(view)));
                skyboxShader.SetMat4("projectionMatrix", projection);
                skyboxShader.SetInt("cubeTexture", 0);
                skyboxShader.Use();
                skybox.Draw();
            }

            foreach (Entity entity in entities)
            {
                Shader shader = entity.Shader;
                shader.SetMat4("transformationMatrix", entity.Model.TransformationMatrix());
                shader.SetMat4("viewMatrix", view);
                shader.SetMat4("projectionMatrix", projection);
                shader.SetVec3("cameraPos", camera.Position);
                for (int i = 0; i < PointLights.Count; i++)
                    PointLights[i].Set(shader, i);
                Shadows?.SetUniforms(shader);

                shader.Use();
                entity.Model.DrawAll(shader);
            }
        }

        public void Dispose()
        {
            if (disposed) return;

            foreach (Shader shader in ownedShaders)
                shader.Dispose();
            foreach (Entity entity in entities)
                entity.Model.Dispose();
            skybox?.Dispose();
            Shadows?.Dispose();
            Model.Materials.DeleteLoadedTextures();

            disposed = true;
        }
    }
}
