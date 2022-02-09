using OpenTK.Graphics;

namespace Demo
{
	static class Program
	{
		static void Main()
		{
			using( Window window =
				new Window(800, 600, 
				new GraphicsMode(new ColorFormat(8, 8, 8, 0), 24, 8, 4),
				"Ege Game"))
            {
				window.Run(30, 30);
            }
		}
	}
}
