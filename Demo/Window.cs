using Ege;
using Ege.Model;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using Camera = Ege.Camera;

namespace Demo
{
    public sealed class Window : GameWindow
    {
        Shader skyboxShader;
        Skybox skybox;

        Shader sponzaShader;
        StaticModel sponza;

        Shader bobShader;
        DynamicModel bob;

        Shader depthShader;
        Texture shadow;

        Camera camera;
        List<PointLight> pointLights = new List<PointLight>
        {
            new PointLight()
            {
                position = new Vector3(0.0f, 600.0f, 0.0f),
                ambient = new Vector3(7f, 7f, 7f),
                diffuse = new Vector3(5f, 5f, 5f),
                //linear = 10.0027f,
                //quadratic = 0.0028f
            },
            new PointLight()
            {
                position = new Vector3(600.0f, 600.0f, 0.0f),
                ambient = new Vector3(7f, 7f, 7f),
                diffuse = new Vector3(5f, 5f, 5f),
                //linear = 10.0027f,
                //quadratic = 0.0028f
            },
            new PointLight()
            {
                position = new Vector3(-600.0f, 600.0f, 0.0f),
                ambient = new Vector3(7f, 7f, 7f),
                diffuse = new Vector3(5f, 5f, 5f),
                //linear = 10.0027f,
                //quadratic = 0.0028f
            },

            new PointLight()
            {
                position = new Vector3(1120, 240, -400),
                ambient = new Vector3(7f, 7f, 7f),
                diffuse = new Vector3(8f, 2f, 2f),
                specular = new Vector3(20f, 20f, 20f)
            },
            new PointLight()
            {
                position = new Vector3(1120, 240, 400),
                ambient = new Vector3(7f, 7f, 7f),
                diffuse = new Vector3(8f, 2f, 2f),
                specular = new Vector3(20f, 20f, 20f)
            },
            new PointLight()
            {
                position = new Vector3(-1200, 240, 400),
                ambient = new Vector3(7f, 7f, 7f),
                diffuse = new Vector3(8f, 2f, 8f),
                specular = new Vector3(20f, 20f, 20f)
            },
            new PointLight()
            {
                position = new Vector3(-1200, 240, -400),
                ambient = new Vector3(7f, 7f, 7f),
                diffuse = new Vector3(8f, 2f, 8f),
                specular = new Vector3(20f, 20f, 20f)
            },

            new PointLight()
            {
                position = new Vector3(1120, 640, -400),
                ambient = new Vector3(3f, 3f, 3f),
                diffuse = new Vector3(2f, 2f, 10f),
                specular = new Vector3(20f, 20f, 20f)
            },
            new PointLight()
            {
                position = new Vector3(1120, 640, 400),
                ambient = new Vector3(3f, 3f, 3f),
                diffuse = new Vector3(10f, 2f, 2f),
                specular = new Vector3(20f, 20f, 20f)
            },
            new PointLight()
            {
                position = new Vector3(-1200, 640, 400),
                ambient = new Vector3(3f, 3f, 3f),
                diffuse = new Vector3(2f, 10f, 2f),
                specular = new Vector3(20f, 20f, 20f)
            },
            new PointLight()
            {
                position = new Vector3(-1200, 640, -400),
                ambient = new Vector3(3f, 3f, 3f),
                diffuse = new Vector3(10f, 2f, 10f),
                specular = new Vector3(20f, 20f, 20f)
            },
        };

        bool firstMove = true;
        Vector2 lastPos;

        public Window(int width, int height, GraphicsMode mode, string title) : base(width, height, mode, title)
        {
            WindowState = WindowState.Minimized;
            Mouse.SetPosition(X + Width / 2f, Y + Height / 2f);
            VSync = VSyncMode.Off;
            CursorVisible = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            string EntitiesFolder = "../../../Entities";

            // shaders
            skyboxShader = new Shader("Shaders/skyboxV.glsl", "Shaders/skyboxF.glsl");
            bobShader = new Shader("Shaders/skeletalV.glsl", "Shaders/staticF.glsl");
            sponzaShader = new Shader("Shaders/staticV.glsl", "Shaders/staticF.glsl");
            depthShader = new Shader("Shaders/depthV.glsl", "Shaders/depthF.glsl", "Shaders/depthG.glsl");

            shadow = new Texture(pointLights.ToArray());

            // skybox
            skybox = new Skybox(EntitiesFolder);

            // models
            sponza = new StaticModel(EntitiesFolder + "/sponza/sponza.obj");
            bob = new DynamicModel(EntitiesFolder + "/bob/bob_lamp_update_export.md5mesh")
            {
                Scale = new Vector3(30f),
                Position = new Vector3(600f, 0f, -200f)
            };

            // camera
            camera = new Camera(new Vector3(0, 170, 0))
            {
                AspectRatio = Width / (float)Height,
                Speed = 5f,
                Sensitivity = 0.1f,
                Far = 3000f,
                Fov = 45f,
                Yaw = 0f,
                Pitch = 0f
            };

            SetShadowMaps();

            WindowState = WindowState.Maximized;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Title = $"(Vsync: {VSync}) FPS: {1f / e.Time:0}";
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            skyboxShader.SetMat4("viewMatrix", new Matrix4(new Matrix3(camera.ViewMatrix())));
            skyboxShader.SetMat4("projectionMatrix", camera.ProjectionMatrix());
            skyboxShader.SetInt("cubeTexture", 0);
            skyboxShader.Use();
            skybox.Draw();

            sponzaShader.SetMat4("transformationMatrix", sponza.TransformationMatrix());
            sponzaShader.SetMat4("viewMatrix", camera.ViewMatrix());
            sponzaShader.SetMat4("projectionMatrix", camera.ProjectionMatrix());
            sponzaShader.SetVec3("cameraPos", camera.Position);
            for (int i = 0; i < pointLights.Count; i++)
                pointLights[i].Set(sponzaShader, i);
            sponzaShader.Use();
            sponza.DrawAll(sponzaShader);

            bobShader.SetMat4("transformationMatrix", bob.TransformationMatrix());
            bobShader.SetMat4("viewMatrix", camera.ViewMatrix());
            bobShader.SetMat4("projectionMatrix", camera.ProjectionMatrix());
            bobShader.SetVec3("cameraPos", camera.Position);
            for (int i = 0; i < pointLights.Count; i++)
                pointLights[i].Set(bobShader, i);
            bobShader.Use();
            bob.DrawAll(bobShader);

            Context.SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            if (!Focused) return;

            if (!CursorVisible)
            {
                KeyboardState input = Keyboard.GetState();
                if (input.IsKeyDown(Key.W))
                    camera.Position += camera.Front * camera.Speed; //Forward 
                if (input.IsKeyDown(Key.S))
                    camera.Position -= camera.Front * camera.Speed; //Backwards
                if (input.IsKeyDown(Key.A))
                    camera.Position -= camera.Right * camera.Speed; //Left
                if (input.IsKeyDown(Key.D))
                    camera.Position += camera.Right * camera.Speed; //Right
                if (input.IsKeyDown(Key.Space))
                    camera.Position += camera.Up * camera.Speed; //Up 
                if (input.IsKeyDown(Key.LShift))
                    camera.Position -= camera.Up * camera.Speed; //Down				

                MouseState mouse = Mouse.GetState();
                if (firstMove)
                {
                    lastPos = new Vector2(mouse.X, mouse.Y);
                    firstMove = false;
                }
                else
                {
                    float deltaX = mouse.X - lastPos.X;
                    float deltaY = mouse.Y - lastPos.Y;
                    lastPos = new Vector2(mouse.X, mouse.Y);
                    camera.Yaw += deltaX * camera.Sensitivity;
                    camera.Pitch -= deltaY * camera.Sensitivity;
                }
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            if (Focused && !CursorVisible)
                Mouse.SetPosition(X + Width / 2f, Y + Height / 2f);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            camera.Fov -= e.DeltaPrecise;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Width, Height);
            camera.AspectRatio = Width / (float)Height;
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            skyboxShader.Dispose();
            base.OnUnload(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Escape)
                Exit();
            if (e.Key == Key.F11)
                WindowState = WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
            if (e.Key == Key.ControlLeft)
                CursorVisible = !CursorVisible;
        }

        private void SetShadowMaps()
        {

            float far_plane = 3000f;

            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0, 0, shadow.shadowWidth, shadow.shadowHeight);

            Matrix4 shadowProj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f),
                shadow.shadowWidth / (float)shadow.shadowHeight, 0.1f, far_plane);

            depthShader.SetFloat("far_plane", far_plane);

            for (int i = 0; i < pointLights.Count; i++)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadow.FBO[i]);
                GL.Clear(ClearBufferMask.DepthBufferBit);
                Vector3 lightPos = pointLights[i].position;
                Matrix4[] shadowTransforms = new Matrix4[]
                {
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj
                };

                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(1.1f, 1.1f);

                for (int z = 0; z < 6; ++z)
                    depthShader.SetMat4("shadowMatrices[" + z + "]", shadowTransforms[z]);
                depthShader.SetVec3("lightPos", lightPos);

                depthShader.Use();
                depthShader.SetMat4("transformationMatrix", sponza.TransformationMatrix());
                sponza.DrawAll(depthShader);

                depthShader.Use();
                depthShader.SetMat4("transformationMatrix", bob.TransformationMatrix());
                bob.DrawAll(depthShader);

                GL.Disable(EnableCap.PolygonOffsetFill);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                sponzaShader.Use();
                sponzaShader.SetMat4("cubeProjection", shadowProj);
                GL.ActiveTexture(TextureUnit.Texture10 + i);
                GL.BindTexture(TextureTarget.TextureCubeMap, shadow.shadowCubemaps[i]);
                sponzaShader.SetInt("depthMaps[" + i + "]", 10 + i);
                GL.DeleteFramebuffer(shadow.FBO[i]);

                bobShader.Use();
                bobShader.SetMat4("cubeProjection", shadowProj);
                GL.ActiveTexture(TextureUnit.Texture10 + i);
                GL.BindTexture(TextureTarget.TextureCubeMap, shadow.shadowCubemaps[i]);
                bobShader.SetInt("depthMaps[" + i + "]", 10 + i);
                GL.DeleteFramebuffer(shadow.FBO[i]);

            }
        }
    }
}
