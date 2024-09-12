using System;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string projectPath = Path.Combine(Directory.GetCurrentDirectory(), "Release");
        string repositoryUrl = "https://github.com/MaskUp-Org/Embedded.git";

        string platformioScriptPath = "-m pip install -U platformio";
        string pythonCommand = "python --version";
        string gitCommand = "git --version";
        string platformioPath = FindPlatformioPath();

        if (string.IsNullOrEmpty(platformioPath))
        {
            Console.WriteLine("PlatformIO n'est pas installé ou non trouvé.");
        }
        else
        {
            Console.WriteLine($"PlatformIO trouvé à : {platformioPath}");
        }

        InstallIfMissing("python", pythonCommand, "Python.Python.3.12");
        InstallIfMissing("git", gitCommand, "Git.Git");
        InstallPlatformIOIfMissing(platformioPath, platformioScriptPath);

        if (!Directory.Exists(projectPath))
        {
            Directory.CreateDirectory(projectPath);
        }

        bool repoSuccess = CloneOrPullRepository(repositoryUrl, projectPath);

        if (repoSuccess)
        {
            bool compileSuccess = CompileProject(projectPath);

            if (compileSuccess)
            {
                bool uploadSuccess = UploadToESP32(projectPath);

                if (!uploadSuccess)
                {
                    Console.WriteLine("Échec du téléversement vers l'ESP32.");
                }
                else
                {
                    bool buildFileSystemSuccess = BuildFileSystemToESP32(projectPath);
                    if (!buildFileSystemSuccess)
                    {
                        Console.WriteLine("Échec de la construction du système de fichiers.");
                    }
                    else
                    {
                        bool uploadFileSystemSuccess = UploadFileSystemToESP32(projectPath);
                        if (!uploadFileSystemSuccess)
                        {
                            Console.WriteLine("Échec du téléversement du système de fichiers vers l'ESP32.");
                        }
                        else
                        {
                            Console.WriteLine("Upload Filesystem success !!");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Échec de la compilation.");
            }
        }
        else
        {
            Console.WriteLine("Échec du clonage ou de la mise à jour du dépôt.");
        }
    }

    static void InstallIfMissing(string toolName, string checkCommand, string wingetPackage)
    {
        if (!IsToolInstalled(checkCommand))
        {
            Console.WriteLine($"{toolName} n'est pas installé. Installation en cours...");
            var installCmd = $"winget install {wingetPackage}";
            RunCommand(installCmd);
        }
        else
        {
            Console.WriteLine($"{toolName} est déjà installé.");
        }
    }

    static void InstallPlatformIOIfMissing(string platformioPath, string scriptPath)
    {
        if (string.IsNullOrEmpty(platformioPath) || !IsCommandAvailable($"\"{platformioPath}\" --version"))
        {
            string pythonPath = FindPythonPath();

            if (!string.IsNullOrEmpty(pythonPath) && File.Exists(pythonPath))
            {
                Console.WriteLine("PlatformIO n'est pas installé. Installation en cours...");
                var installCmd = $"{pythonPath} {scriptPath}";
                RunCommand(installCmd);
            }
            else
            {
                Console.WriteLine("Python n'est pas trouvé. Veuillez installer Python avant de continuer.");
            }
        }
        else
        {
            Console.WriteLine("PlatformIO est déjà installé.");
        }
    }

    static bool IsToolInstalled(string checkCommand)
    {
        return IsCommandAvailable(checkCommand);
    }

    static bool IsCommandAvailable(string command)
    {
        try
        {
            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = processInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    static bool CloneOrPullRepository(string repositoryUrl, string projectPath)
    {
        string gitPath = "git";
        var gitPullCmd = $"{gitPath} pull origin main";
        var gitCloneCmd = $"{gitPath} clone {repositoryUrl} {projectPath}";

        if (Directory.Exists(Path.Combine(projectPath, ".git")))
        {
            Console.WriteLine("Pulling latest changes...");
            return RunCommand(gitPullCmd, projectPath);
        }
        else
        {
            Console.WriteLine("Cloning repository...");
            return RunCommand(gitCloneCmd);
        }
    }

    static string FindPythonPath()
    {
        // Vérifie les chemins courants pour Python
        string[] possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Python\PythonXX\python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Python\PythonXX\python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python\PythonXX\python.exe")
        };

        // Remplace 'XX' par les versions spécifiques si nécessaire, ou utiliser des modèles génériques si vous avez besoin de prendre en compte plusieurs versions
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Vérifie la variable d'environnement PATH
        string pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            string[] paths = pathEnv.Split(Path.PathSeparator);

            foreach (var p in paths)
            {
                string potentialPath = Path.Combine(p, "python.exe");
                if (File.Exists(potentialPath))
                {
                    return potentialPath;
                }
            }
        }

        return null;
    }

    static string FindPlatformioPath()
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string pythonFolder = Path.Combine(userFolder, @"AppData\Local\Programs\Python");

        if (Directory.Exists(pythonFolder))
        {
            var pythonVersions = Directory.GetDirectories(pythonFolder);

            foreach (var versionFolder in pythonVersions)
            {
                string platformioPath = Path.Combine(versionFolder, "Scripts", "platformio.exe");

                if (File.Exists(platformioPath))
                {
                    return platformioPath;
                }
            }
        }

        Console.WriteLine("PlatformIO not found in the expected directory.");
        return null;
    }

    static bool CompileProject(string projectPath)
    {
        string platformioPath = FindPlatformioPath();

        if (platformioPath == null)
        {
            return false;
        }

        Console.WriteLine("Compiling project...");
        var compileCmd = $"\"{platformioPath}\" run";
        return RunCommand(compileCmd, projectPath);
    }

    static bool BuildFileSystemToESP32(string projectPath)
    {
        string platformioPath = FindPlatformioPath();

        if (platformioPath == null)
        {
            return false;
        }

        Console.WriteLine("Building Filesystem image...");
        var buildFsCmd = $"\"{platformioPath}\" run --target buildfs";
        return RunCommand(buildFsCmd, projectPath);
    }

    static bool UploadFileSystemToESP32(string projectPath)
    {
        string platformioPath = FindPlatformioPath();

        if (platformioPath == null)
        {
            return false;
        }

        Console.WriteLine("Uploading FileSystem to ESP32...");
        var uploadFsCmd = $"\"{platformioPath}\" run --target uploadfs";
        return RunCommand(uploadFsCmd, projectPath);
    }

    static bool UploadToESP32(string projectPath)
    {
        string platformioPath = FindPlatformioPath();

        if (platformioPath == null)
        {
            return false;
        }

        Console.WriteLine("Uploading to ESP32...");
        var uploadCmd = $"\"{platformioPath}\" run --target upload";
        return RunCommand(uploadCmd, projectPath);
    }

    static bool RunCommand(string command, string workingDirectory = null)
    {
        var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (workingDirectory != null)
        {
            processInfo.WorkingDirectory = workingDirectory;
        }

        try
        {
            var process = new Process { StartInfo = processInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Commande exécutée avec succès :");
                Console.WriteLine(output);
                return true;
            }
            else
            {
                Console.WriteLine("Erreur lors de l'exécution de la commande :");
                Console.WriteLine(error);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur s'est produite lors de l'exécution de la commande : {ex.Message}");
            return false;
        }
    }
}
