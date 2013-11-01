using System;
using System.IO;
using System.Collections.Generic;

namespace Kraken.Unique
{
    /// <summary>
    /// Main class.
    /// 
    /// Unique helps us to determine the duplications within files.
    /// 
    /// Basically - this is simply to iterate through a given directory, and then 
    /// calculate the checksum of the files.
    /// 
    /// Then we should store the checksum somewhere.
    /// 
    /// </summary>
    class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: unique <directory>");
                return;
            }
            string sourceDir = args [0];
            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine("Directory {0} does not exist.", sourceDir);
                return;
            }
            Unique unique = new Unique();
            unique.Process(sourceDir);
            Console.WriteLine("\nResults\n");
            unique.ShowDupes();
        }
    }
}
