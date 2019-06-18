using System;

namespace Samples.SLMC
{
#if WINDOWS || XBOX || LINUX
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (Game1 game = new Game1())
            {
                game.Run();
            }
        }
    }
#endif
}

