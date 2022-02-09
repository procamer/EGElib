using System;
using OpenTK;

namespace Ege
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public float AspectRatio { get; set; }
        public float Speed { get; set; }
        public float Sensitivity { get; set; }
        public float Near { get; set; }
        public float Far { get; set; }

        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;

        private Vector3 _front = -Vector3.UnitZ;
        private Vector3 _up = Vector3.UnitY;
        private Vector3 _right = Vector3.UnitX;

        private float _pitch;   // Rotation around the X axis (radians)
        private float _yaw;     // Rotation around the Y axis (radians) Without this you would be started rotated 90 degrees right
        private float _fov;      // The field of view of the camera (radians)

        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set => _fov = MathHelper.DegreesToRadians(MathHelper.Clamp(value, 1f, 60f));
        }

        public float Pitch
        {
            get => MathHelper.RadiansToDegrees(_pitch);
            set
            {
                _pitch = MathHelper.DegreesToRadians(MathHelper.Clamp(value, -89f, 89f));
                UpdateVectors();
            }
        }

        public float Yaw
        {
            get => MathHelper.RadiansToDegrees(_yaw);
            set
            {
                _yaw = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        public Camera(Vector3 position)
        {
            Position = position;
            AspectRatio = 4 / 3;
            Sensitivity = 1.0f;
            Speed = 1.0f;
            Fov = 45.0f;
            Near = 0.1f;
            Far = 100.0f;
        }

        public Matrix4 ViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        public Matrix4 ProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, Near, Far);
        }

        private void UpdateVectors()
        {
            _front.X = (float)Math.Cos(_pitch) * (float)Math.Cos(_yaw);
            _front.Y = (float)Math.Sin(_pitch);
            _front.Z = (float)Math.Cos(_pitch) * (float)Math.Sin(_yaw);

            _front = Vector3.Normalize(_front);
            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        }

        public Vector3 GetRotation()
        {
            Vector3 Rotation = new Vector3(_pitch, _yaw, 0);
            return Rotation;
        }





    }
}
