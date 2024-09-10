using System;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string projectPath = Path.Combine(Directory.GetCurrentDirectory(), "Release");

        string repositoryUrl = "https://github.com/MaskUp-Org/Embedded.git";


        //string platformioScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "");


        string platformioScriptPath = "-m pip install -U platformio";

        string pythonCommand = "python --version";
        string gitCommand = "git --version";
        string platformioCommand = "platformio --version";


        InstallIfMissing("python", pythonCommand, "Python.Python.3.12");
        InstallIfMissing("git", gitCommand, "Git.Git");
        InstallPlatformIOIfMissing(platformioCommand, platformioScriptPath);

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

    static void InstallPlatformIOIfMissing(string checkCommand, string scriptPath)
    {
        if (!IsToolInstalled(checkCommand))
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string pythonPath = Path.Combine(userFolder, @"AppData\Local\Programs\Python\Python312\python.exe");

            Console.WriteLine("PlatformIO n'est pas installé. Installation en cours...");
            var installCmd = $"{pythonPath} {scriptPath}";
            RunCommand(installCmd);
        }
        else
        {
            Console.WriteLine("PlatformIO est déjà installé.");
        }
    }

    static bool IsToolInstalled(string checkCommand)
    {
        try
        {
            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {checkCommand}")
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

    static bool CompileProject(string projectPath)
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string platformioPath = Path.Combine(userFolder, @"AppData\Local\Programs\Python\Python312\Scripts\platformio.exe");

        Console.WriteLine("Compiling project...");

        var compileCmd = $"{platformioPath} run";
        return RunCommand(compileCmd, projectPath);
    }

    static bool UploadToESP32(string projectPath)
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string platformioPath = Path.Combine(userFolder, @"AppData\Local\Programs\Python\Python312\Scripts\platformio.exe");

        Console.WriteLine("Uploading to ESP32...");
        var uploadCmd = $"{platformioPath} run --target upload";
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

