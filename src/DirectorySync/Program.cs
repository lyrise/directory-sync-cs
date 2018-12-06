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
            var config = Toml.ReadFile<Config>(args[0]);

            var regexList = config.IgnorePatternList.Select(n => new Regex(n)).ToList();
            bool isIgnoreFilePath(string text) => regexList.Any(n => n.IsMatch(text.Replace('\\', '/')));

            Run(config.DirectoryPathList, isIgnoreFilePath);

            Console.WriteLine("Completed.");
        }

        private static void Run(IEnumerable<string> directoryPathList, Func<string, bool> isIgnoreFilePath)
        {
            // 基準とするディレクトリのパス
            var baseDirectoryPath = directoryPathList.ElementAt(0);

            // 最後に更新されたフォルダに基づき、削除を行い同期する
            foreach (var destDirectoryPath in directoryPathList)
            {
                if (baseDirectoryPath == destDirectoryPath) continue;

                Sync.Run(baseDirectoryPath, destDirectoryPath, true, isIgnoreFilePath);
            }

            // 削除を行わずに同期する
            foreach (var sourceDirectoryPath in directoryPathList)
            {
                if (baseDirectoryPath == sourceDirectoryPath) continue;

                Sync.Run(sourceDirectoryPath, baseDirectoryPath, false, isIgnoreFilePath);
            }
        }
    }
}
