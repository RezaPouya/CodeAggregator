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
                ".razor", ".css", ".scss", ".html", ".xml", ".yml", ".yaml",
                ".nuxt", ".env", ".md", ".sln", ".config", ".ps1", ".sh",
                ".dockerfile", ".yml", ".yaml", ".tf", ".tfvars"
            };

            // Directories to exclude
            var excludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "data-transfer",

                // دات نت و ASP.NET Core
                "bin",
                "obj",
                "publish",
                "packages",
                "ref",
                "runtimes",
                
                // Node.js و Nuxt.js
                "node_modules",
                ".nuxt",
                ".output",
                ".cache",
                "coverage",
                ".nyc_output",
                "dist",
                
                // کنترل نسخه و CI/CD
                ".git",
                ".github",
                ".vs",
                ".idea",
                ".vscode",
                ".azuredevops",
                ".gitlab",

                ".cr",
                ".idea",
                
                // داکیومنت و تست
                "docs",
                "README",
                "tests",
                "test",
                "unittest",
                "integrationtest",
                "e2e",
                
                // Migration
                "Migration",
                "Migrations",
                "commands_migrations",
                "Data\\Migrations",
                
                // سیستم‌عامل و IDE
                ".vsconfig",
                ".vspscc",
                ".vssscc",
                
                // وب‌سایت
                "wwwroot\\lib",
                "wwwroot\\dist",
                "wwwroot\\fonts",
                "wwwroot\\css",
                "wwwroot\\js",
                "wwwroot\\images",
                "wwwroot\\webfonts",
                
                // ابزارها
                ".editorconfig",
                ".gitignore",
                ".gitattributes",
                "fonts",
                "build",
                "deploy",
                "scripts",
                "docker",
                "helm",
                "k8s",
                "Content"
                //"infrastructure"
            };

            // File name patterns to exclude
            var excludedFilePatterns = new List<string>
            {
                // جاوااسکریپت و CSS
                ".min.js",
                ".min.css",
                ".map",
                ".bundle.js",
                ".chunk.js",
                ".chunk.css",
                ".d.ts",
                
                // دات نت
                ".Designer.cs",
                ".g.cs",
                ".generated.cs",
                ".AssemblyInfo.cs",
                ".GlobalUsings.g.cs",
                "SourceLink.",
                ".pdb",
                
                // فایل‌های قفل
                "package-lock.json",
                "yarn.lock",
                "pnpm-lock.yaml"
            };

            // فایل‌های خاص برای حذف
            var excludedFiles = new List<string>
            {
                // فایل‌های قفل Node.js
                "package-lock.json",
                "yarn.lock",
                "pnpm-lock.yaml",
                ".npmrc",
                ".nvmrc",
                
                // فایل‌های محیطی
                ".env.local",
                ".env.development",
                ".env.production",
                ".env.test",
                ".env.staging",
                ".env.dev",
                ".env.prod",
                "appsettings.Development.json",
                "appsettings.Production.json",
                "appsettings.Staging.json",
                "appsettings.Local.json",
                "secrets.json",
                "user-secrets.json",
                
                // فایل‌های پیکربندی TypeScript
                "tsconfig.json",
                "tsconfig.node.json",
                "tsconfig.build.json",
                "tsconfig.esm.json",
                "tsconfig.cjs.json",
                
                // فایل‌های پیکربندی ابزارها
                ".eslintrc.json",
                ".eslintrc.js",
                ".prettierrc.json",
                ".prettierrc.js",
                "vite.config.ts",
                "vite.config.js",
                "vitest.config.ts",
                "vitest.config.js",
                "playwright.config.ts",
                "jest.config.js",
                "babel.config.js",
                "webpack.config.js",
                "rollup.config.js",
                "gulpfile.js",
                "gruntfile.js",
                
                // فایل‌های دات نت
                "Program.cs", // اگر می‌خواهید برنامه اصلی را هم حذف کنید
                "Startup.cs", // در پروژه‌های قدیمی
                "Dockerfile",
                "docker-compose.yml",
                "docker-compose.override.yml",
                ".dockerignore",
                
                // فایل‌های NuGet
                "packages.config",
                "nuget.config",
                ".nuspec",
                
                // فایل‌های CI/CD
                ".gitlab-ci.yml",
                ".github/workflows/*.yml",
                "azure-pipelines.yml",
                "Jenkinsfile",
                ".travis.yml",
                "appveyor.yml",
                
                // فایل‌های لاگ و خطا
                "*.log",
                "*.tmp",
                "*.temp",
                "error.log",
                "access.log",
                
                // فایل‌های تولید شده
                "components.d.ts",
                "auto-imports.d.ts",
                "vite-env.d.ts",
                "shims-vue.d.ts",
                "*.generated.cs",
                "*.designer.cs",
                "*.g.cs",
                ".version",
                "version.txt"
            };

            Console.WriteLine("Scanning files...");

            // Collect all files recursively, excluding unwanted folders
            var allFiles = GetFilesRecursive(rootPath, allowedExtensions, excludedDirectories, excludedFilePatterns, excludedFiles)
                           .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
                           .ToList();

            if (!allFiles.Any())
            {
                Console.WriteLine("No matching files found.");
                Console.ReadLine();
                return;
            }

            var fileName = rootPath.Replace(":", "_").Replace("\\", "_").Replace("/", "_");
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

                    if ((i + 1) % 50 == 0 || i == allFiles.Count - 1)
                        Console.Write($"\rProgress: {i + 1}/{allFiles.Count} files");

                    writer.WriteLine($"=== FILE: {file.RelativePath} ===");
                    writer.WriteLine(file.Content);
                    writer.WriteLine();
                    filesWritten++;
                }
            }

            Console.WriteLine($"\n✅ Operation completed successfully.");
            Console.WriteLine($"📁 Output file: {outputFile}");
            Console.WriteLine($"📄 Files written: {filesWritten}");
            Console.WriteLine($"💾 Total content size: {FormatBytes(totalSize)}");
            Console.WriteLine($"📏 AllText.txt file size: {FormatBytes(new FileInfo(outputFile).Length)}");

            // نمایش آمار
            ShowStatistics(allFiles);

            Console.WriteLine("\n⚠️  Note: If the file is very large (more than a few MB), DeepSeek may not process it entirely.");
            Console.WriteLine("💡 To reduce size, remove less important extensions or add more excluded folders.");
            Console.ReadLine();
        }

        private static void ShowStatistics(List<(string RelativePath, string Content, long Size)> files)
        {
            Console.WriteLine("\n📊 File type statistics:");
            var extensions = files
                .GroupBy(f => Path.GetExtension(f.RelativePath).ToLower() ?? "no-extension")
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToList();

            foreach (var group in extensions)
            {
                var totalSize = group.Sum(f => f.Size);
                Console.WriteLine($"   {group.Key,-12} {group.Count(),4} files  {FormatBytes(totalSize)}");
            }
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
            List<string> excludedFilePatterns,
            List<string> excludedFiles)
        {
            var results = new List<(string, string, long)>();

            var directoriesToProcess = new Stack<string>();
            directoriesToProcess.Push(rootPath);

            while (directoriesToProcess.Count > 0)
            {
                string currentDir = directoriesToProcess.Pop();

                // Skip excluded directories
                string dirName = Path.GetFileName(currentDir);

                // بررسی پوشه‌های مستثنی
                bool shouldSkipDir = false;
                foreach (var excludedDir in excludedDirectories)
                {
                    string currentDirName = Path.GetFileName(currentDir);
                    if (excludedDirectories.Contains(currentDirName, StringComparer.OrdinalIgnoreCase))
                    {
                        shouldSkipDir = true;
                        break;
                    }
                }

                if (shouldSkipDir)
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

                        // بررسی فایل‌های مستثنی
                        bool shouldExclude = false;

                        // بررسی نام فایل
                        if (excludedFiles.Any(f => fileName.Contains(f, StringComparison.OrdinalIgnoreCase)))
                            shouldExclude = true;

                        // بررسی الگوهای فایل
                        if (!shouldExclude && excludedFilePatterns.Any(pattern =>
                            fileName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0))
                            shouldExclude = true;

                        if (shouldExclude)
                            continue;

                        // Read file content
                        string content;
                        long fileSize;
                        try
                        {
                            fileSize = new FileInfo(filePath).Length;
                            if (fileSize > 5 * 1024 * 1024) // 5 MB
                            {
                                content = $"// File too large (>5 MB) - skipped: {fileName} (Size: {FormatBytes(fileSize)})";
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
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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