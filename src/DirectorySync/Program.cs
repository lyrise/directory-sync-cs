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

            Run(config.BaseDirectoryPathList, isIgnoreFilePath);

            Console.WriteLine("Completed.");
        }

        private static void Run(IEnumerable<string> baseDirectoryPathList, Func<string, bool> isIgnoreFilePath)
        {
            // ルートディレクトリ下の対象ディレクトリ名
            var projectNameList = new List<string>();
            projectNameList.AddRange(Directory.GetDirectories(baseDirectoryPathList.ElementAt(0)).Select(n => Path.GetFileName(n)));

            foreach (var projectName in projectNameList)
            {
                // 対象のプロジェクトのパスリストを算出。
                var directoryPathList = baseDirectoryPathList.Select(n => Path.Combine(n, projectName)).ToList();

                // 対象のプロジェクトのパスにディレクトリが存在しない場合、作成する。
                foreach (var path in directoryPathList)
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                }

                // 最も更新日時の古いディレクトリの算出。
                var lastUpdatedDirectoryPath = Sync.GetLastUpdatedDirectory(directoryPathList, isIgnoreFilePath);

                // 最後に更新されたフォルダに基づき、削除を行い同期する。
                foreach (var destDirectoryPath in directoryPathList)
                {
                    if (lastUpdatedDirectoryPath == destDirectoryPath) continue;

                    Sync.Run(lastUpdatedDirectoryPath, destDirectoryPath, true, isIgnoreFilePath);
                }

                // 削除を行わずに同期する。
                foreach (var sourceDirectoryPath in directoryPathList)
                {
                    if (lastUpdatedDirectoryPath == sourceDirectoryPath) continue;

                    Sync.Run(sourceDirectoryPath, lastUpdatedDirectoryPath, false, isIgnoreFilePath);
                }
            }
        }
    }
}
