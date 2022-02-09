using Assimp;
using Ege;
using Ege.Model;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Diagnostics;
using System.Drawing;
using Camera = Ege.Camera;

namespace Demo
{
    public sealed class Window : GameWindow
    {
        Shader sponzaShader;
        Shader depthShader;
        StaticModel sponza;

        Shader bobShader;
        DynamicModel bob;
        DynamicModel boneman;

        Shader skyboxShader;
        Skybox skybox;

        Camera camera;

        Texture shadow;

        PointLight[] pointLights = new PointLight[]
        {
            new PointLight()
            {
                position = new Vector3(0.0f, 60.0f, 0.0f),
                linear = 0.027f,
                quadratic = 0.0028f
            },
            new PointLight()
            {
                position = new Vector3(60.0f, 60.0f, 0.0f),
                linear = 0.027f,
                quadratic = 0.0028f
            },
            new PointLight()
            {
                position = new Vector3(-60.0f, 60.0f, 0.0f),
                linear = 0.027f,
                quadratic = 0.0028f
            },

            new PointLight()
            {
                position = new Vector3(112, 24, -40),
                ambient = new Vector3(0.7f, 0.7f, 0.7f),
                diffuse = new Vector3(0.8f, 0.2f, 0.2f),
                specular = new Vector3(2.0f, 2.0f, 2.0f)
            },
            new PointLight()
            {
                position = new Vector3(112, 24, 40),
                ambient = new Vector3(0.7f, 0.7f, 0.7f),
                diffuse = new Vector3(0.8f, 0.2f, 0.2f),
                specular = new Vector3(2.0f, 2.0f, 2.0f)
            },

            new PointLight()
            {
                position = new Vector3(-120, 24, 40),
                ambient = new Vector3(0.7f, 0.7f, 0.7f),
                diffuse = new Vector3(0.8f, 0.2f, 0.8f),
                specular = new Vector3(2.0f, 2.0f, 2.0f)
            },
            new PointLight()
            {
                position = new Vector3(-120, 24, -40),
                ambient = new Vector3(0.7f, 0.7f, 0.7f),
                diffuse = new Vector3(0.8f, 0.2f, 0.8f),
                specular = new Vector3(2.0f, 2.0f, 2.0f)
            },

            new PointLight()
            {
                position = new Vector3(112, 64, -40),
				ambient = new Vector3(0.3f, 0.3f, 0.3f),
				diffuse = new Vector3(0.2f, 0.2f, 1.0f),
				specular = new Vector3(2.0f, 2.0f, 2.0f)
			},
            new PointLight()
            {
                position = new Vector3(112, 64, 40),
                ambient = new Vector3(0.3f, 0.3f, 0.3f),
				diffuse = new Vector3(1.0f, 0.2f, 0.2f),
				specular = new Vector3(2.0f, 2.0f, 2.0f)
            },
            new PointLight()
            {
                position = new Vector3(-120, 64, 40),
                ambient = new Vector3(0.3f, 0.3f, 0.3f),
				diffuse = new Vector3(0.2f, 1.0f, 0.2f),
				specular = new Vector3(2.0f, 2.0f, 2.0f)
            },
            new PointLight()
            {
                position = new Vector3(-120, 64, -40),
                ambient = new Vector3(0.3f, 0.3f, 0.3f),
				diffuse = new Vector3(1.0f, 0.2f, 1.0f),
				specular = new Vector3(2.0f, 2.0f, 2.0f)
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

            // camera
            camera = new Camera(Vector3.UnitY * 17)
            {
                AspectRatio = Width / (float)Height,
                Speed = 1.2f,
                Sensitivity = 0.07f,
                Far = 3000f,
                Fov = 45f,
                Yaw = 0f,
                Pitch = 0f
            };

            skyboxShader = new Shader(@"Shader/skyboxV.glsl", @"Shader/skyboxF.glsl");
            sponzaShader = new Shader(@"Shader/staticV.glsl", @"Shader/staticF.glsl");
            depthShader = new Shader(@"Shader/depthV.glsl", @"Shader/depthF.glsl", @"Shader/depthG.glsl");                        
            bobShader = new Shader(@"Shader/skeletalV2.glsl", @"Shader/skeletalF2.glsl");
            
            // skybox
            skybox = new Skybox();

            //// sponza & shadow
            shadow = new Texture(pointLights);
            PostProcessSteps postProcessSteps = PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace;
            sponza = new StaticModel(@"Entities/sponza/sponza.obj", postProcessSteps);
            sponza.Scale = new Vector3(0.1f);            
            SetShadowMapsS(pointLights, depthShader, sponzaShader,sponza);

            // bob			
            postProcessSteps |= PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.GenerateUVCoords;
            bob = new DynamicModel(@"Entities/bob/bob_lamp_update_export.md5mesh", postProcessSteps);
            bob.Position = new Vector3(70.0f, -0.5f, -25.0f);
            bob.Scale = new Vector3(3f);
            SetShadowMapsD(pointLights, depthShader, bobShader,bob);

            // boneman
            boneman = new DynamicModel(@"Entities/boneman/boneman_running.md5mesh", postProcessSteps);
            boneman.Scale = new Vector3(1.5f);
            boneman.Position = new Vector3(120f, -0.2f, 00f);
            boneman.Rotation.Y = MathHelper.DegreesToRadians(90f);
            SetShadowMapsD(pointLights, depthShader, bobShader, boneman);

            WindowState = WindowState.Maximized;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Title = $"(Vsync: {VSync}) FPS: {1f / e.Time:0}";
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            skyboxShader.Use();
            skyboxShader.SetMat4("viewMatrix", new Matrix4(new Matrix3(camera.ViewMatrix())));
            skyboxShader.SetMat4("projectionMatrix", camera.ProjectionMatrix());
            skyboxShader.SetInt("cubeTexture", 0);
            skybox.Draw();
            //CheckLastError();

            sponzaShader.Use();
            sponzaShader.SetMat4("transformationMatrix", sponza.TransformationMatrix());
            sponzaShader.SetMat4("viewMatrix", camera.ViewMatrix());
            sponzaShader.SetMat4("projectionMatrix", camera.ProjectionMatrix());
            sponzaShader.SetVec3("cameraPos", camera.Position);
            sponzaShader.SetVec3("lightPos", camera.Position);
            for (int i = 0; i < pointLights.Length; i++)
                pointLights[i].Set(sponzaShader, i);
            sponza.DrawAll(sponzaShader);
            //CheckLastError();

            bobShader.Use();
            bobShader.SetMat4("transformationMatrix", bob.TransformationMatrix());
            bobShader.SetMat4("viewMatrix", camera.ViewMatrix());
            bobShader.SetMat4("projectionMatrix", camera.ProjectionMatrix());
            bobShader.SetVec3("cameraPos", camera.Position);
            bobShader.SetVec3("lightPos", camera.Position);
            for (int i = 0; i < pointLights.Length; i++)
                pointLights[i].Set(bobShader, i);
            bob.DrawAll(bobShader);
            //CheckLastError();

            bobShader.Use();
            boneman.Position.Z -= 0.35f;
            if (boneman.Position.Z < -30f) boneman.Position.Z = 30f;
            bobShader.SetMat4("transformationMatrix", boneman.TransformationMatrix());
            bobShader.SetMat4("viewMatrix", camera.ViewMatrix());
            bobShader.SetMat4("projectionMatrix", camera.ProjectionMatrix());
            bobShader.SetVec3("cameraPos", camera.Position);
            bobShader.SetVec3("lightPos", camera.Position);
            for (int i = 0; i < pointLights.Length; i++)
                pointLights[i].Set(bobShader, i);
            boneman.DrawAll(bobShader);
            //CheckLastError();

            Context.SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            if (!Focused) return;

            //if (Focused)
            //{
            //	if (input.IsKeyDown(Key.ControlLeft))
            //		CursorVisible = !CursorVisible;
            //}

            if (Focused && !CursorVisible)
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
            sponzaShader.Dispose();
            depthShader.Dispose();
            base.OnUnload(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Escape)
                Exit();
            if (e.Key == Key.F11)
                WindowState = WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
        }

        private void SetShadowMapsS(PointLight[] lights, Shader depthShader, Shader targetShader, StaticModel staticModel)
        {

            for (int i = 0; i < lights.Length; i++)
            {
                GL.ClearColor(Color.Black);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.Viewport(0, 0, shadow.shadowWidth, shadow.shadowHeight);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadow.FBO[i]);
                GL.Clear(ClearBufferMask.DepthBufferBit);

                float far_plane = 300f;

                Vector3 lightPos = pointLights[i].position;
                Matrix4 shadowProj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f),
                    shadow.shadowWidth / (float)shadow.shadowHeight, 0.1f, 300.0f);
                Matrix4[] shadowTransforms = new Matrix4[]
                {
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj
                };

                depthShader.Use();
                for (int z = 0; z < 6; ++z)
                    depthShader.SetMat4("shadowMatrices[" + z + "]", shadowTransforms[z]);
                depthShader.SetVec3("lightPos", lightPos);
                depthShader.SetFloat("far_plane", far_plane);
                depthShader.SetMat4("transformationMatrix", sponza.TransformationMatrix());

                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(1.1f, 1.5f);
                staticModel.DrawAll(depthShader);
                GL.Disable(EnableCap.PolygonOffsetFill);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                targetShader.Use();
                targetShader.SetMat4("cubeProjection", shadowProj);
                GL.ActiveTexture(TextureUnit.Texture10 + i);
                GL.BindTexture(TextureTarget.TextureCubeMap, shadow.shadowCubemaps[i]);
                targetShader.SetInt("depthMaps[" + i + "]", 10 + i);
                CheckLastError();
                GL.DeleteFramebuffer(shadow.FBO[i]);
                //GL.DeleteTexture(shadowCubemaps[i]);
            }
        }

        private void SetShadowMapsD(PointLight[] lights, Shader depthShader, Shader targetShader, DynamicModel dynamicModel)
        {

            for (int i = 0; i < lights.Length; i++)
            {
                GL.ClearColor(Color.Black);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.Viewport(0, 0, shadow.shadowWidth, shadow.shadowHeight);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadow.FBO[i]);
                GL.Clear(ClearBufferMask.DepthBufferBit);

                float far_plane = 300f;

                Vector3 lightPos = pointLights[i].position;
                Matrix4 shadowProj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f),
                    shadow.shadowWidth / (float)shadow.shadowHeight, 0.1f, 300.0f);
                Matrix4[] shadowTransforms = new Matrix4[]
                {
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj
                };

                depthShader.Use();
                for (int z = 0; z < 6; ++z)
                    depthShader.SetMat4("shadowMatrices[" + z + "]", shadowTransforms[z]);
                depthShader.SetVec3("lightPos", lightPos);
                depthShader.SetFloat("far_plane", far_plane);
                depthShader.SetMat4("transformationMatrix", sponza.TransformationMatrix());

                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(1.1f, 1.5f);
                dynamicModel.DrawAll(depthShader);
                GL.Disable(EnableCap.PolygonOffsetFill);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                targetShader.Use();
                targetShader.SetMat4("cubeProjection", shadowProj);
                GL.ActiveTexture(TextureUnit.Texture10 + i);
                GL.BindTexture(TextureTarget.TextureCubeMap, shadow.shadowCubemaps[i]);
                targetShader.SetInt("depthMaps[" + i + "]", 10 + i);
                //CheckLastError();
                GL.DeleteFramebuffer(shadow.FBO[i]);
            }
        }


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

    }
}
