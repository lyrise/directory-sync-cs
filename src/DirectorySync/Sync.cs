using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;

namespace DirectorySync
{
    public static class Sync
    {
        /// <summary>
        /// 最後に更新されたディレクトリを算出する。
        /// </summary>
        public static string GetLastUpdatedDirectory(IEnumerable<string> pathList, Func<string, bool> isIgnoreFilePath)
        {
            var map = new ConcurrentDictionary<string, DateTime>();

            foreach (var targetDirectoryPath in pathList)
            {
                foreach (var filePath in Directory.EnumerateFiles(targetDirectoryPath, "*", SearchOption.AllDirectories))
                {
                    string relativePath = filePath.Remove(0, targetDirectoryPath.Length);
                    if (isIgnoreFilePath(relativePath)) continue;

                    // 一番古い書き込み時間を抽出する。
                    var lastWriteTime = (new FileInfo(filePath)).LastWriteTimeUtc;
                    map.AddOrUpdate(targetDirectoryPath, (_) => lastWriteTime, (_, current) => current > lastWriteTime ? current : lastWriteTime);
                }
            }

            // 一番古い書き込み時間のファイルを持つディレクトリを返す。
            return map.OrderByDescending(n => n.Value).Select(n => n.Key).First();
        }

        /// <summary>
        /// ディレクトリの同期を行う。
        /// </summary>
        public static void Run(string sourceDirectoryPath, string destDirectoryPath, bool deletable, Func<string, bool> isIgnoreFilePath)
        {
            var now = DateTime.Now;
            var baseBackupDirectoryPath = Path.Combine(Path.GetDirectoryName(destDirectoryPath), ".backup", $"{now.ToString("yyyy-MM-dd_HH-mm-ss")}", Path.GetFileName(destDirectoryPath));

            var hashSet = new HashSet<string>();

            foreach (var sourceFilePath in Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = sourceFilePath.Remove(0, sourceDirectoryPath.Length);
                string destFilePath = Path.Combine(destDirectoryPath, relativePath.Trim('\\'));

                if (isIgnoreFilePath(relativePath)) continue;

                // コピー対象ファイルとしてマーク。
                hashSet.Add(destFilePath);

                var sourceLastWriteTime = new FileInfo(sourceFilePath).LastWriteTimeUtc;
                sourceLastWriteTime = sourceLastWriteTime.AddTicks(-(sourceLastWriteTime.Ticks % TimeSpan.TicksPerSecond));

                var destLastWriteTime = new FileInfo(destFilePath).LastWriteTimeUtc;
                destLastWriteTime = destLastWriteTime.AddTicks(-(destLastWriteTime.Ticks % TimeSpan.TicksPerSecond));

                // コピー先にファイルが存在し、最終書き込み日時が大きい場合、コピーしない。
                if (File.Exists(destFilePath) && destLastWriteTime >= (sourceLastWriteTime))
                {
                    continue;
                }

                // コピー先ディレクトリを作成する。
                {
                    string parentDirectoryPath = Path.GetDirectoryName(destFilePath);
                    if (!Directory.Exists(parentDirectoryPath)) Directory.CreateDirectory(parentDirectoryPath);
                }

                try
                {
                    // コピー先にファイルが存在する場合、バックアップディレクトリへ移動する。
                    if (File.Exists(destFilePath))
                    {
                        var backupFilePath = Path.Combine(baseBackupDirectoryPath, relativePath.Trim('\\'));
                        var backupParentDirectoryPath = Path.GetDirectoryName(backupFilePath);

                        if (!Directory.Exists(backupParentDirectoryPath)) Directory.CreateDirectory(backupParentDirectoryPath);
                        File.Move(destFilePath, backupFilePath);
                    }

                    File.Copy(sourceFilePath, destFilePath, true);
                    Console.WriteLine($"{now.ToString("yyyy/MM/dd HH:mm:ss")} Copy: {sourceFilePath} -> {destFilePath}");
                }
                catch (Exception)
                {

                }
            }

            // 削除しない。
            if (!deletable) return;

            // コピー対象でないファイルがコピー先フォルダに存在する場合、コピー先から該当ファイルを削除する。
            foreach (var destFilePath in Directory.GetFiles(destDirectoryPath, "*", SearchOption.AllDirectories))
            {
                if (hashSet.Contains(destFilePath)) continue;

                string relativePath = destFilePath.Remove(0, destDirectoryPath.Length);
                if (isIgnoreFilePath(relativePath)) continue;

                try
                {
                    // コピー先にファイルが存在する場合、バックアップディレクトリへ移動する。
                    if (File.Exists(destFilePath))
                    {
                        var backupFilePath = Path.Combine(baseBackupDirectoryPath, relativePath.Trim('\\'));
                        var backupParentDirectoryPath = Path.GetDirectoryName(backupFilePath);

                        if (!Directory.Exists(backupParentDirectoryPath)) Directory.CreateDirectory(backupParentDirectoryPath);
                        File.Move(destFilePath, backupFilePath);
                    }

                    Console.WriteLine($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} Delete: {destFilePath}");
                }
                catch (Exception)
                {

                }
            }

            // コピー先フォルダ内の空のフォルダを削除する。
            foreach (var path in Directory.GetDirectories(destDirectoryPath, "*", SearchOption.AllDirectories).Reverse())
            {
                try
                {
                    Directory.Delete(path, false);
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
