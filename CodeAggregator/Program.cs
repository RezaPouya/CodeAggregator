using System.Text;

namespace CodeAggregator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== Project File Aggregator for DeepSeek ===");
            Console.WriteLine("This tool collects all allowed source files into a single AllText.txt file.");
            Console.WriteLine("Folders like bin, obj, node_modules, .git, .vs are ignored.\n");

            // Get root path from user
            string rootPath = GetRootPath();
            if (rootPath == null) return;

            // Allowed file extensions
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".cs", ".cshtml", ".js", ".ts", ".json", ".vue", ".csproj",
                ".razor", ".css", ".scss", ".html", ".xml", ".yml", ".yaml"
            };

            // Directories to exclude
            var excludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin", 
                "obj", 
                "node_modules", 
                "Migration" , 
                "commands_migrations" ,
                "docs" , 
                "README", 
                ".idea",  
                "Migrations" , 
                ".github" , 
                "tests" , 
                ".editorconfig" , 
                ".gitignore" , 
                ".gitattributes" , 
                ".git", 
                ".vs", 
                "dist", 
                ".nuxt",
                "build", 
                "wwwroot/lib", 
                "wwwroot/dist" ,
                "wwwroot",
                "appsettings.json",
                "appsettings.Development.json",
                "migration-commands.txt",
                "RefahBank.BETA.sln.DotSettings" , 
            };

            // File name patterns to exclude (e.g., minified files)
            var excludedFilePatterns = new List<string>
            {
                ".min.js", ".min.css", ".map", ".bundle.js"
            };

            Console.WriteLine("Scanning files...");

            // Collect all files recursively, excluding unwanted folders
            var allFiles = GetFilesRecursive(rootPath, allowedExtensions, excludedDirectories, excludedFilePatterns)
                           .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
                           .ToList();

            if (!allFiles.Any())
            {
                Console.WriteLine("No matching files found.");
                return;
            }

            var fileName = rootPath.Replace(":","_").Replace("\\" , "_").Replace("/", "_");

            string outputFile = Path.Combine(rootPath, $"{fileName}.txt");


            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
                Console.WriteLine("Previous AllText.txt deleted.");
            }

            Console.WriteLine($"Total files found: {allFiles.Count}");
            Console.WriteLine("Writing output file...");

            long totalSize = 0;
            int filesWritten = 0;

            using (var writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                for (int i = 0; i < allFiles.Count; i++)
                {
                    var file = allFiles[i];
                    totalSize += file.Size;

                    // Show progress every 50 files or at the end
                    if ((i + 1) % 50 == 0 || i == allFiles.Count - 1)
                        Console.Write($"\rProgress: {i + 1}/{allFiles.Count} files");

                    writer.WriteLine($"=== FILE: {file.RelativePath} ===");
                    writer.WriteLine(file.Content);
                    writer.WriteLine(); // blank line between files
                    filesWritten++;
                }
            }

            Console.WriteLine($"\n✅ Operation completed successfully.");
            Console.WriteLine($"📁 Output file: {outputFile}");
            Console.WriteLine($"📄 Files written: {filesWritten}");
            Console.WriteLine($"💾 Total content size: {FormatBytes(totalSize)}");
            Console.WriteLine($"📏 AllText.txt file size: {FormatBytes(new FileInfo(outputFile).Length)}");
            Console.WriteLine("\nNote: If the file is very large (more than a few MB), DeepSeek may not process it entirely.");
            Console.WriteLine("To reduce size, remove less important extensions or add more excluded folders.");
        }

        private static string GetRootPath()
        {
            Console.Write("Enter full project root path (or 'q' to quit): ");
            string input = Console.ReadLine()?.Trim();
            if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
                return null;

            if (string.IsNullOrEmpty(input) || !Directory.Exists(input))
            {
                Console.WriteLine("Error: Invalid path. Try again.");
                return GetRootPath();
            }
            return input;
        }

        private static List<(string RelativePath, string Content, long Size)> GetFilesRecursive(
            string rootPath,
            HashSet<string> allowedExtensions,
            HashSet<string> excludedDirectories,
            List<string> excludedFilePatterns)
        {
            var results = new List<(string, string, long)>();

            var directoriesToProcess = new Stack<string>();
            directoriesToProcess.Push(rootPath);

            while (directoriesToProcess.Count > 0)
            {
                string currentDir = directoriesToProcess.Pop();

                // Skip excluded directories
                string dirName = Path.GetFileName(currentDir);
                if (excludedDirectories.Contains(dirName))
                    continue;

                // Add subdirectories
                try
                {
                    foreach (var subDir in Directory.GetDirectories(currentDir))
                    {
                        directoriesToProcess.Push(subDir);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                // Process files in current directory
                try
                {
                    foreach (var filePath in Directory.GetFiles(currentDir))
                    {
                        string extension = Path.GetExtension(filePath);
                        if (!allowedExtensions.Contains(extension))
                            continue;

                        string fileName = Path.GetFileName(filePath);
                        bool shouldExclude = excludedFilePatterns.Any(pattern =>
                            fileName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (shouldExclude)
                            continue;

                        // Read file content (with error handling)
                        string content;
                        long fileSize;
                        try
                        {
                            fileSize = new FileInfo(filePath).Length;
                            // Skip files larger than 5 MB (optional)
                            if (fileSize > 5 * 1024 * 1024)
                            {
                                content = $"// File too large (>5 MB) - skipped: {fileName}";
                            }
                            else
                            {
                                content = File.ReadAllText(filePath, Encoding.UTF8);
                            }
                        }
                        catch (Exception ex)
                        {
                            content = $"// Error reading file: {ex.Message}";
                            fileSize = 0;
                        }

                        string relativePath = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
                        results.Add((relativePath, content, fileSize));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
            }

            return results;
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}