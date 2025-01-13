using System;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Reflection;
using System.Media;
using System.Security.Principal;

internal class CNU_Student_Pack
{
    private static string? language = null; //Ініціалізація глобальної змінної "language" (для вибору мови)
    private static Dictionary<string, Dictionary<string, string>> localizations = new(); //Ініціалізація глобальної змінної "localizations" (як словник)

    [STAThread] //Single-Threaded Apartment, необхідний для коректної роботи Process.Start в режимі адміністратора

    //Метод Main
    private static void Main(string[] args)
    {
        if (!IsAdministrator())
        {
            RunAsAdministrator();
            return;
        }
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "CNU Student Pack";
        Console.WriteLine("CNU Student Pack (by Vlas Maltsev)");
        LoadLocalizations();
        SelectLanguage(); 
        while (true)
        {
            WriteColoredLine(Localize("Warning"), ConsoleColor.Red);
            Console.WriteLine(Localize("WarningMessage"));
            Console.Write(Localize("EnterChoice"));
            if (!int.TryParse(Console.ReadLine(), out var choice))
            {
                Console.Clear();
                PlaySound(1);
                WriteColoredLine(Localize("InvalidInput"), ConsoleColor.Red);
                continue;
            }

            if ((choice < 0 || choice > 6) && choice != 9)
            {
                Console.Clear();
                PlaySound(1);
                WriteColoredLine(Localize("OutOfRange"), ConsoleColor.Red);   
                continue;
            }

            ProcessChoice(choice);
        }
    }

    //Метод обробки вибору користувача
    private static void ProcessChoice(int choice)
    {
        switch (choice)
        {
            case 0:
                WriteColoredLine(Localize("ExitMessage"), ConsoleColor.Yellow);
                Thread.Sleep(1500);
                Environment.Exit(0);
                break;

            case 1:
                WriteColoredLine(Localize("Option1"), ConsoleColor.Yellow);
                RunInstaller(1);
                PressAnyKeyToContinue();
                break;

            case 2:
                WriteColoredLine(Localize("Option2"), ConsoleColor.Yellow);
                RunInstaller(2);
                PressAnyKeyToContinue();
                break;

            case 3:
                WriteColoredLine(Localize("Option3"), ConsoleColor.Yellow);
                WriteColoredLine(Localize("LongDownloading"), ConsoleColor.DarkYellow);
                ExecuteCommand(1);
                PlaySound(2);
                PressAnyKeyToContinue();
                break;

            case 4:
                WriteColoredLine(Localize("Option4"), ConsoleColor.Yellow);
                WriteColoredLine(Localize("LongDownloading"), ConsoleColor.DarkYellow);
                ExecuteCommand(2);
                PlaySound(2);
                PressAnyKeyToContinue();
                break;

            case 5:
                WriteColoredLine(Localize("Option5"), ConsoleColor.Yellow);
                WriteColoredLine(Localize("LongDownloading"), ConsoleColor.DarkYellow);
                ExecuteCommand(3);
                PlaySound(2);
                PressAnyKeyToContinue();
                break;

            case 6:
               WriteColoredLine(Localize("Option6"), ConsoleColor.Yellow);
               WriteColoredLine(Localize("LongDownloading"), ConsoleColor.DarkYellow);

                var tasks = new List<Action>
    {
        () => RunInstaller(2),
        () => ExecuteCommand(1),
        () => ExecuteCommand(2),
        () => ExecuteCommand(3)
    };

                foreach (var task in tasks)
                {
                    task();
                }

                Console.WriteLine(Localize("AllTasksCompleted"));
                PlaySound(2);
                PressAnyKeyToContinue();
                break;

            case 9:
                WriteColoredLine(Localize("Option9"), ConsoleColor.Yellow);
                UseReadmeFile();
                PlaySound(2);
                Thread.Sleep(1000);
                PressAnyKeyToContinue();
                break;

            default:
                Console.WriteLine(Localize("UnsupportedChoice"));
                break;
        }
    }

    //Метод використання файлу README.txt
    private static void UseReadmeFile()
    {
        try
        {
            string resourcePath = "CNU_Student_Pack.README.txt";
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    Console.WriteLine(Localize("ReadMeNotFound"));
                    return;
                }

                string tempFilePath = Path.Combine(Path.GetTempPath(), "README.txt");
                using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }

                RegisterFileDeletionOnExit(tempFilePath);
                OpenFile(tempFilePath);
            }
        }
        catch (Exception ex)
        {
            PlaySound(1);
            WriteColoredLine(Localize("Error", ex.Message), ConsoleColor.Red);
        }
    }

    //Метод відкриття файлу (в нашому випадку README.txt)
    private static void OpenFile(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            PlaySound(1);
            WriteColoredLine(Localize("Error", ex.Message), ConsoleColor.Red);
        }
    }

    //Метод видалення файлу з тимчасової пам'яті при виході з програми
    private static void RegisterFileDeletionOnExit(string filePath)
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch (Exception ex)
            {
                PlaySound(1);
                WriteColoredLine(Localize("Error", ex.Message), ConsoleColor.Red);
            }
        };
    }

    //Метод очікування натискання клавіші для продовження
    private static void PressAnyKeyToContinue()
    {
        WriteColoredLine(Localize("PressAnyKey"), ConsoleColor.Green);
        Console.ReadKey(intercept: true);
        Console.Clear();
    }

    //Метод виконання інсталятора Visual Studio Community 2022 Bootstrapper (з різними умовами)
    private static void RunInstaller(int installerNumber)
    {
        (string? resourceName, string? arguments) installerConfig = installerNumber switch
        {
            1 => ("CNU_Student_Pack.Resources.vs_community.exe", "--passive --wait --norestart"),
            2 => ("CNU_Student_Pack.Resources.vs_community.exe", "--passive --wait --norestart --add Microsoft.VisualStudio.Workload.ManagedDesktop --add Microsoft.VisualStudio.Workload.NetWeb --add Microsoft.Net.Component.SDK --add Component.GitHub.VisualStudio"),
            _ => (null, null)
        };

        string? resourceName = installerConfig.resourceName;
        string? arguments = installerConfig.arguments;
        string tempFilePath = Path.GetTempFileName() + ".exe";


        if (string.IsNullOrWhiteSpace(resourceName))
        {
            WriteColoredLine(Localize("UnknownNumber"), ConsoleColor.Red);
            return;
        }

        try
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    PlaySound(1);
                    WriteColoredLine(Localize("ResourceNotFound", resourceName), ConsoleColor.Red);
                    return;
                }
                using (var fileStream = File.Create(tempFilePath))
                {
                    stream.CopyTo(fileStream);
                }
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = tempFilePath,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                process.WaitForExit();
                Console.WriteLine(Localize("ExitCode", resourceName, process.ExitCode));
                if (process.ExitCode == 1602)
                {
                    PlaySound(1); //Після додавання потреби у запускі "від адміністратора" ця помилка майже неможлива, рудимент
                    WriteColoredLine(Localize("AdminRightsRequired"), ConsoleColor.Red); 
                }
                else
                {
                    PlaySound(2);
                }
            }
        }
        catch (Exception ex)
        {
            PlaySound(1);
            WriteColoredLine(Localize("Error", ex.Message), ConsoleColor.Red);
        }
        finally
        {
            try
            {
                File.Delete(tempFilePath);
            }
            catch (Exception e)
            {
                PlaySound(1);
                WriteColoredLine(Localize("Error", e.Message), ConsoleColor.Red);
            }
        }
    }

    //Метод виконання команди через cmd
    private static void ExecuteCommand(int commandNumber)
    {
        string? command = commandNumber switch
        {
            1 => "winget install --id Microsoft.VisualStudioCode",
            2 => "winget install --id Git.Git -e --source winget",
            3 => "winget install --id=MaximaTeam.Maxima  -e",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(command))
        {
            try
            {
                var animationTokenSource = new CancellationTokenSource();
                var animationToken = animationTokenSource.Token;

                Task.Run(() => ShowLoadingAnimation(animationToken), animationToken);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                using (Process process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        animationTokenSource.Cancel();

                        string output = process.StandardOutput.ReadToEnd();
                        string errorOutput = process.StandardError.ReadToEnd();

                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            Console.WriteLine(Localize("CommandExecutedSuccessfully", output));
                        }

                        if (!string.IsNullOrWhiteSpace(errorOutput))
                        {
                            
                                Console.WriteLine(Localize("Error", errorOutput));
                        }
                    }
                    else
                    {
                        animationTokenSource.Cancel();
                        Console.WriteLine(Localize("ProcessNotStarted"));
                    }
                }
            }
            catch (Exception ex)
            {
                PlaySound(1);
                WriteColoredLine(Localize("Error", ex.Message), ConsoleColor.Red);
            }
        }
        else
        {
            PlaySound(1);
            WriteColoredLine(Localize("UnknownNumber"), ConsoleColor.Red);
        }
    }

    //Метод виведення анімації виконання (муляж)
    private static void ShowLoadingAnimation(CancellationToken token) 
    {
        string[] animationFrames = { "|", "/", "-", "\\" };
        int frameIndex = 0;

        while (!token.IsCancellationRequested)
        {
            Console.Write(Localize("Executing", animationFrames[frameIndex++ % animationFrames.Length]));
            Thread.Sleep(100); 
        }

        Console.Write("\r");
    }

    //Метод завантаження json-файлів локалізації
    private static void LoadLocalizations()
    {
        string[] langFiles = { "en.json", "uk.json" };
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var file in langFiles)
        {
            string resourcePath = $"CNU_Student_Pack.Localizations.{file}";
            try
            {
                using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string jsonString = reader.ReadToEnd();
                            string langCode = Path.GetFileNameWithoutExtension(file);
                            var localization = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                            if (localization != null)
                            {
                                localizations[langCode] = localization;
                            }
                        }
                    }
                    else
                    {
                        PlaySound(1);
                        WriteColoredLine($"Embedded resource not found: {resourcePath}", ConsoleColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                PlaySound(1);
                WriteColoredLine($"Error loading localization file {file}: {ex.Message}", ConsoleColor.Red);
            }
        }
    }

    //Метод вибору мови
    private static void SelectLanguage()
    {
        while (language == null)
        {
            WriteColored("\nSelect your language / Виберіть мову:\n1 - English\n2 - Українська\n\nChoice / Вибір: ", ConsoleColor.Green);

            if (int.TryParse(Console.ReadLine(), out var langChoice))
            {
                if (langChoice == 1)
                {
                    Console.Clear();
                    language = "en";
                }
                else if (langChoice == 2)
                {
                    Console.Clear();
                    language = "uk";
                }
            }

            if (language == null)
            {
                PlaySound(1);
                Console.Clear();
                WriteColoredLine("Invalid choice. Please try again. / Невірний вибір. Спробуйте ще раз.", ConsoleColor.Red);
            }
        }
    }

    //Метод локалізації
    public static string Localize(string key, params object[] args)
    {
        if (language != null && localizations.ContainsKey(language) &&
            localizations[language].ContainsKey(key))
        {
            string localizedString = localizations[language][key];

            int placeholderCount = CountPlaceholders(localizedString);

            if (args.Length < placeholderCount)
            {
                PlaySound(1);
                return $"[Error: Insufficient arguments for key '{key}'. Expected {placeholderCount}, got {args.Length}] {localizedString}";
            }

            try
            {
                return string.Format(localizedString, args);
            }
            catch (FormatException ex)
            {
                PlaySound(1);
                return $"[FormatError: {ex.Message}] {localizedString}";
            }
        }
        PlaySound(1);
        return $"Missing localization for key: {key}";
    }

    //Метод підрахунку плейсхолдерів (для виводу помилок при локалізації, дебаг)
    private static int CountPlaceholders(string input)
    {
        return System.Text.RegularExpressions.Regex.Matches(input, @"\{\d+\}").Count;
    }

    //Метод відтворення звуку
    private static void PlaySound(int audioNum)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();

            string? resourceName = audioNum switch
            {
                1 => "CNU_Student_Pack.Resources.ErrorSound.wav",
                2 => "CNU_Student_Pack.Resources.CompleteSound.wav",
                _ => null
            };

            if (resourceName == null)
            {
                PlaySound(1); 
                WriteColoredLine(Localize("UnknownNumber"), ConsoleColor.Red);
                return;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                WriteColoredLine(Localize("ResourceNotFound", resourceName), ConsoleColor.Red);
                return;
            }

            using var player = new SoundPlayer(stream);
            player.Play();

        }
        catch (Exception ex)
        {
            WriteColoredLine(Localize("Error", ex.Message), ConsoleColor.Red);
        }
    }

    //Метод перевірки прав адміністратора
    private static bool IsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    //Метод запуску програми в режимі адміністратора
    private static void RunAsAdministrator()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = Assembly.GetEntryAssembly()?.Location,
            Verb = "runas",
            UseShellExecute = true
        };

        try
        {
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            WriteColoredLine(Localize("Error", ex.Message), ConsoleColor.Red);
            Environment.Exit(1);
        }
    }

    //Метод виведення тексту певного кольору (без переведення на новий рядок, WriteLine)
    private static void WriteColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = ConsoleColor.Cyan;
    }

    //Метод виведення тексту певного кольору (з переведенням на новий рядок, Write)
    private static void WriteColoredLine(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = ConsoleColor.Cyan;
    }

}
