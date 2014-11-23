#region open Statements
open System;
open System.Collections.Generic;
open System.Linq;
#endregion

namespace opengl01
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            open (var game = new Game1())
                game.Run();
        }
    }
#endif
}
