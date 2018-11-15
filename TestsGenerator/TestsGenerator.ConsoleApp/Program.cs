using System;
using System.Collections.Generic;
using System.IO;

namespace TestsGenerator.ConsoleApp
{
    class Program
    {
        private const int MinCountParams = 5;
        private const string InvalidParametersMessage = "Expected. List of path to c# class files, "
                                                        + "output directory, read number of files at a time, "
                                                        + "number of max tasks at pool thread, "
                                                        + "write number of files at a time.";

        static void Main(string[] args)
        {
            if (args.Length < MinCountParams)
            {
                throw new ArgumentException(InvalidParametersMessage);
            }

            try
            {
                int writeCountFiles = Convert.ToInt32(args[args.Length - 1]);
                int maxTasks = Convert.ToInt32(args[args.Length - 2]);
                int readCountFiles = Convert.ToInt32(args[args.Length - 3]);
                string outputDirectory = args[args.Length - 4];
                if (!Directory.Exists(outputDirectory))
                {
                    Console.WriteLine("Output directory not found: " + outputDirectory);
                    outputDirectory = Directory.GetCurrentDirectory();
                }

                List<string> paths = new List<string>();
                for (int i = 0; i < args.Length - 4; i++)
                {
                    if (File.Exists(args[i]))
                        paths.Add(args[i]);
                    else
                        Console.WriteLine("File not found: " + args[i]);
                }

                if (paths.Count == 0)
                {
                    Console.WriteLine("Not found correct path to c# class file.");
                }
                else
                {
                    Generator generator = new Generator(outputDirectory, readCountFiles, maxTasks, writeCountFiles);
                    var task = generator.Generate(paths);
                    task.Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("The end.");
            Console.ReadLine();
        }
    }
}
