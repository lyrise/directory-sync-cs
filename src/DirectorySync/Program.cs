using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.IO;
using Nett;
using System.Text.RegularExpressions;

namespace DirectorySync
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = Toml.ReadFile<Config>("config.toml");

            var regexList = config.IgnorePatternList.Select(n => new Regex(n)).ToList();
            bool isIgnoreFilePath(string text) => regexList.Any(n => n.IsMatch(text.Replace('\\', '/')));

            Run(config.Source, config.Destination, isIgnoreFilePath);

            Console.WriteLine("Completed.");
        }

        private static void Run(string source, string destination, Func<string, bool> isIgnoreFilePath)
        {
            Sync.Run(source, destination, true, isIgnoreFilePath);
        }
    }
}
