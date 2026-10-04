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
        World world;

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

            GL.ClearColor(Color.Black);

            string EntitiesFolder = "../../../Entities";

            world = new World(pointLights);
            world.SetSkybox(new Skybox(EntitiesFolder));
            world.Add(new StaticModel(EntitiesFolder + "/sponza/sponza.obj"));
            world.Add(new DynamicModel(EntitiesFolder + "/bob/bob_lamp_update_export.md5mesh")
            {
                Scale = new Vector3(30f),
                Position = new Vector3(600f, 0f, -200f)
            });
            world.EnableShadows();

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

            WindowState = WindowState.Maximized;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Title = $"(Vsync: {VSync}) FPS: {1f / e.Time:0}";

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            world.Draw(camera);

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
            world.Dispose();
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
    }
}
