#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace GemstoneDOS
{
    class Program
    {
        static void Main(string[] args)
        {
            var kernel = new GstdosKernel();
            kernel.Boot();
        }
    }

    public class VNode
    {
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public string Content { get; set; } = string.Empty;
        public VNode Parent { get; set; }
        public Dictionary<string, VNode> Children { get; set; } = new Dictionary<string, VNode>(StringComparer.OrdinalIgnoreCase);
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public int Size => IsDirectory ? 0 : (Content ?? string.Empty).Length;

        public VNode() { }

        public VNode(string name, bool isDirectory, VNode parent = null)
        {
            Name = name.ToUpper();
            IsDirectory = isDirectory;
            Parent = parent;
        }
    }

    // Flat file model representing a disk sector record
    public class FlatFileEntry
    {
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Flat data transfer object for saving state safely
    public class DiskStateDto
    {
        public List<FlatFileEntry> FileRecords { get; set; } = new List<FlatFileEntry>();
        public string RegisteredOwner { get; set; }
        public bool SetupCompleted { get; set; }
        public bool EnableHma { get; set; }
    }

    public class GstdosKernel
    {
        private const string DiskFileName = "gstdos_disk.json";
        private VNode _rootDir;
        private VNode _currentDir;
        private List<string> _pathStack = new List<string> { "C:" };
        private bool _running = true;
        private string _ownerName = "SYSTEM";
        private bool _hmaEnabled = false;

        public void Boot()
        {
            if (!CheckSetupCompleted())
            {
                RunSetupWizard();
            }
            else
            {
                LoadDiskState();
            }

            RunCommandShell();
        }

        private bool CheckSetupCompleted()
        {
            return File.Exists(DiskFileName);
        }

        private void RunSetupWizard()
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            // Draw header
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(CenterText("Gemstone Disk Operating System (GST-DOS) v0.10 Setup", Console.WindowWidth));
            Console.WriteLine(new string('═', Console.WindowWidth));

            Console.WriteLine("\n\n  Welcome to the GST-DOS Setup program.");
            Console.WriteLine("  This utility will prepare your virtual hard drive partition");
            Console.WriteLine("  and configure your system properties.\n");
            Console.WriteLine("  Press [ENTER] to continue...");
            Console.ReadLine();

            // Step 1: Owner Registration
            string owner = "";
            while (string.IsNullOrWhiteSpace(owner))
            {
                Console.Clear();
                DrawSetupHeader("Step 1 of 3: User Registration");
                Console.WriteLine("\n\n  Please enter your name to register your copy of GST-DOS:\n");
                Console.Write("  Owner Name: ");
                owner = Console.ReadLine();
            }
            _ownerName = owner;

            // Step 2: System Features Selection
            Console.Clear();
            DrawSetupHeader("Step 2 of 3: System Options Configuration");
            Console.WriteLine("\n\n  GST-DOS can load part of its kernel logic into the High Memory Area (HMA).");
            Console.WriteLine("  This frees conventional memory space for older program executables.");
            Console.WriteLine("\n  Do you wish to enable High Memory Area (HMA)? (Y/N): ");
            Console.Write("  Selection: ");
            string hmaChoice = Console.ReadLine()?.Trim().ToUpper();
            _hmaEnabled = (hmaChoice == "Y" || hmaChoice == "YES");

            // Step 3: File System Allocation
            Console.Clear();
            DrawSetupHeader("Step 3 of 3: System Files Copying Process");
            Console.WriteLine("\n\n  Ready to install the command shell binaries and boot scripts.");
            Console.WriteLine("  This will write virtual disk sectors to the local directory.\n");
            Console.WriteLine("  Press [ENTER] to begin copying files...");
            Console.ReadLine();

            Console.WriteLine("  Creating C:\\GSTDOS system directory...");
            Thread.Sleep(600);
            Console.WriteLine("  Copying GSTDOS.SYS kernel driver...");
            Thread.Sleep(500);
            Console.WriteLine("  Copying GSTBIOS.SYS input/output layer...");
            Thread.Sleep(500);
            Console.WriteLine("  Copying COMMAND.COM shell...");
            Thread.Sleep(700);
            Console.WriteLine("  Generating CONFIG.SYS and AUTOEXEC.BAT scripts...");
            Thread.Sleep(600);

            // Initialize virtual file system structure
            _rootDir = new VNode("C:", true);
            var gstdosDir = new VNode("GSTDOS", true, _rootDir);
            _rootDir.Children.Add("GSTDOS", gstdosDir);

            gstdosDir.Children.Add("GSTDOS.SYS", new VNode("GSTDOS.SYS", false, gstdosDir) { Content = "GST-DOS SYSTEM CORE REGION" });
            gstdosDir.Children.Add("GSTBIOS.SYS", new VNode("GSTBIOS.SYS", false, gstdosDir) { Content = "GST-BIOS SYSTEM HARDWARE BRIDGE" });
            gstdosDir.Children.Add("COMMAND.COM", new VNode("COMMAND.COM", false, gstdosDir) { Content = "GST-DOS COMMAND LINE INTERPRETER" });

            string configSysContent = _hmaEnabled ? "DOS=HIGH\nBUFFERS=20\nFILES=30" : "DOS=LOW\nBUFFERS=15\nFILES=20";
            _rootDir.Children.Add("CONFIG.SYS", new VNode("CONFIG.SYS", false, _rootDir) { Content = configSysContent });
            _rootDir.Children.Add("AUTOEXEC.BAT", new VNode("AUTOEXEC.BAT", false, _rootDir) { Content = $"@ECHO OFF\nPATH C:\\GSTDOS\nECHO Welcome, {_ownerName}" });

            // Save state to disk using flat structures
            SaveDiskState();

            Console.WriteLine("\n  Setup is complete. Press [ENTER] to reboot into GST-DOS.");
            Console.ReadLine();

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
        }

        private void DrawSetupHeader(string subtitle)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(CenterText("Gemstone Disk Operating System (GST-DOS) v0.10 Setup", Console.WindowWidth));
            Console.WriteLine(CenterText(subtitle, Console.WindowWidth));
            Console.WriteLine(new string('═', Console.WindowWidth));
        }

        private string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;
            int totalSpaces = width - text.Length;
            int padLeft = totalSpaces / 2;
            return new string(' ', padLeft) + text;
        }

        private void SaveDiskState()
        {
            var flatRecords = new List<FlatFileEntry>();
            FlattenDirectoryTree(_rootDir, string.Empty, flatRecords);

            var stateDto = new DiskStateDto
            {
                FileRecords = flatRecords,
                RegisteredOwner = _ownerName,
                SetupCompleted = true,
                EnableHma = _hmaEnabled
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(stateDto, options);
            File.WriteAllText(DiskFileName, jsonString);
        }

        // Depth-first search traversal to turn tree into flat paths
        private void FlattenDirectoryTree(VNode node, string parentPath, List<FlatFileEntry> records)
        {
            string currentPath = string.IsNullOrEmpty(parentPath) ? node.Name : $"{parentPath}\\{node.Name}";

            records.Add(new FlatFileEntry
            {
                Path = currentPath,
                IsDirectory = node.IsDirectory,
                Content = node.Content,
                Timestamp = node.Timestamp
            });

            foreach (var child in node.Children.Values)
            {
                FlattenDirectoryTree(child, currentPath, records);
            }
        }

        private void LoadDiskState()
        {
            try
            {
                string jsonString = File.ReadAllText(DiskFileName);
                var stateDto = JsonSerializer.Deserialize<DiskStateDto>(jsonString);

                _ownerName = stateDto.RegisteredOwner;
                _hmaEnabled = stateDto.EnableHma;

                // Reconstruct tree graph from database file records safely
                _rootDir = ReconstructTree(stateDto.FileRecords);
                _currentDir = _rootDir;
            }
            catch (Exception)
            {
                Console.WriteLine("Warning: Root storage file corrupted. Re-running setup script...");
                RunSetupWizard();
            }
        }

        private VNode ReconstructTree(List<FlatFileEntry> records)
        {
            var rootRecord = records.FirstOrDefault(r => r.Path == "C:");
            var root = new VNode("C:", true) { Timestamp = rootRecord?.Timestamp ?? DateTime.Now };

            // Process records ordered by hierarchy depth (parent folders must exist before files)
            var sortedRecords = records
                .Where(r => r.Path != "C:")
                .OrderBy(r => r.Path.Split('\\').Length)
                .ToList();

            foreach (var record in sortedRecords)
            {
                var parts = record.Path.Split('\\');
                VNode current = root;

                // Traverse path down to find the immediate parent node
                for (int i = 1; i < parts.Length - 1; i++)
                {
                    if (current.Children.TryGetValue(parts[i], out VNode next))
                    {
                        current = next;
                    }
                }

                string nodeName = parts.Last();
                var newNode = new VNode(nodeName, record.IsDirectory, current)
                {
                    Content = record.Content,
                    Timestamp = record.Timestamp
                };

                current.Children[nodeName] = newNode;
            }

            return root;
        }

        private void RunCommandShell()
        {
            Console.Clear();
            Console.Beep();
            Thread.Sleep(1000);
            Console.WriteLine("Gemstone Disk Operating System [Version 0.10]");
            Console.WriteLine($"(C) Copyright 1981-2026 Gemstone Corp. Registered to: {_ownerName}");
            Console.WriteLine();

            if (_hmaEnabled)
            {
                Console.WriteLine("HMA (High Memory Area) driver successfully initialized.");
            }
            Console.WriteLine("C:\\> PATH C:\\GSTDOS");
            Console.WriteLine($"C:\\> ECHO Welcome, {_ownerName}");
            Console.WriteLine($"Welcome, {_ownerName}\n");

            while (_running)
            {
                Console.Write(GetPathPrompt() + ">");
                string input = Console.ReadLine();
                if (input != null)
                {
                    ExecuteCommand(input);
                }
            }
        }

        private string GetPathPrompt()
        {
            if (_pathStack.Count == 1)
                return "C:\\";
            return string.Join("\\", _pathStack).Replace("C:", "C:\\");
        }

        private void ExecuteCommand(string cmdLine)
        {
            var tokens = cmdLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            string command = tokens[0].ToUpper();
            string[] args = tokens.Skip(1).ToArray();

            switch (command)
            {
                case "HELP":
                    ShowHelp();
                    break;
                case "VER":
                    Console.WriteLine("\nGST-DOS Version 0.10");
                    Console.WriteLine($"License Registered to: {_ownerName}\n");
                    break;
                case "CLS":
                    Console.Clear();
                    break;
                case "DIR":
                    ListDirectory();
                    break;
                case "CD":
                    ChangeDirectory(args);
                    break;
                case "MD":
                case "MKDIR":
                    MakeDirectory(args);
                    SaveDiskState();
                    break;
                case "RD":
                case "RMDIR":
                    RemoveDirectory(args);
                    SaveDiskState();
                    break;
                case "CREATE":
                    CreateFile(args);
                    SaveDiskState();
                    break;
                case "TYPE":
                    TypeFile(args);
                    break;
                case "DEL":
                    DeleteFile(args);
                    SaveDiskState();
                    break;
                case "COPY":
                    CopyFile(args);
                    SaveDiskState();
                    break;
                case "MEM":
                    ShowMemory();
                    break;
                case "UNINS0000":
                    Uninstall();
                    break;
                case "SETUP":
                    RunSetupWizard();
                    break;
                case "EXIT":
                    _running = false;
                    break;
                default:
                    Console.WriteLine($"Bad command or file name: '{command}'");
                    break;
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("\nAvailable commands:");
            Console.WriteLine("  HELP             - Show reference menu");
            Console.WriteLine("  VER              - View OS version / license registration information");
            Console.WriteLine("  CLS              - Clear console monitor");
            Console.WriteLine("  DIR              - List directories and contents");
            Console.WriteLine("  CD [path]        - Change directory path");
            Console.WriteLine("  MD [name]        - Create a folder directory");
            Console.WriteLine("  RD [name]        - Delete a folder directory");
            Console.WriteLine("  CREATE [name]    - Create a text file sequence");
            Console.WriteLine("  TYPE [name]      - Read text contents of target file");
            Console.WriteLine("  DEL [name]       - Remove target file");
            Console.WriteLine("  COPY [src] [dst] - Duplicate file target");
            Console.WriteLine("  MEM              - Show system memory mapping info");
            Console.WriteLine("  SETUP            - Force re-run of GST-DOS installation wizard");
            Console.WriteLine("  EXIT             - Close application execution context\n");
        }

        private void ListDirectory()
        {
            Console.WriteLine($"\n Directory of {GetPathPrompt()}\n");
            int filesCount = 0;
            int dirsCount = 0;
            int totalBytes = 0;

            if (_currentDir != _rootDir)
            {
                Console.WriteLine($".              <DIR>      {_currentDir.Timestamp:MM-dd-yyyy  hh:mm tt}");
                Console.WriteLine($"..             <DIR>      {_currentDir.Parent.Timestamp:MM-dd-yyyy  hh:mm tt}");
                dirsCount += 2;
            }

            foreach (var child in _currentDir.Children.Values)
            {
                if (child.IsDirectory)
                {
                    Console.WriteLine($"{child.Name,-14} <DIR>      {child.Timestamp:MM-dd-yyyy  hh:mm tt}");
                    dirsCount++;
                }
                else
                {
                    Console.WriteLine($"{child.Name,-14} {child.Size,10} {child.Timestamp:MM-dd-yyyy  hh:mm tt}");
                    filesCount++;
                    totalBytes += child.Size;
                }
            }

            Console.WriteLine($"\n{filesCount,16} File(s) {totalBytes,14} bytes");
            Console.WriteLine($"{dirsCount,16} Dir(s)   {655360 - totalBytes,14} bytes free\n");
        }

        private void ChangeDirectory(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(GetPathPrompt());
                return;
            }

            string target = args[0].ToUpper();

            if (target == "..")
            {
                if (_currentDir.Parent != null)
                {
                    _currentDir = _currentDir.Parent;
                    _pathStack.RemoveAt(_pathStack.Count - 1);
                }
                return;
            }

            if (target == "\\")
            {
                _currentDir = _rootDir;
                _pathStack = new List<string> { "C:" };
                return;
            }

            if (_currentDir.Children.TryGetValue(target, out VNode node))
            {
                if (node.IsDirectory)
                {
                    _currentDir = node;
                    _pathStack.Add(target);
                }
                else
                {
                    Console.WriteLine("Not a directory.");
                }
            }
            else
            {
                Console.WriteLine("Invalid directory path.");
            }
        }

        private void MakeDirectory(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Required parameter missing.");
                return;
            }

            string name = args[0].ToUpper();
            if (_currentDir.Children.ContainsKey(name))
            {
                Console.WriteLine("Directory or file already exists.");
                return;
            }

            var newDir = new VNode(name, true, _currentDir);
            _currentDir.Children.Add(name, newDir);
        }

        private void RemoveDirectory(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Required parameter missing.");
                return;
            }

            string name = args[0].ToUpper();
            if (_currentDir.Children.TryGetValue(name, out VNode node))
            {
                if (!node.IsDirectory)
                {
                    Console.WriteLine("Not a directory.");
                    return;
                }
                if (node.Children.Count > 0)
                {
                    Console.WriteLine("Directory is not empty.");
                    return;
                }
                _currentDir.Children.Remove(name);
            }
            else
            {
                Console.WriteLine("Directory not found.");
            }
        }

        private void CreateFile(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Required parameter missing. Usage: CREATE [filename]");
                return;
            }

            string name = args[0].ToUpper();
            Console.WriteLine("Enter file contents. Type EOF on a newline to save:");

            var lines = new List<string>();
            while (true)
            {
                string line = Console.ReadLine();
                if (line?.Trim().ToUpper() == "EOF") break;
                lines.Add(line);
            }

            string content = string.Join("\n", lines);
            var fileNode = new VNode(name, false, _currentDir) { Content = content };

            if (_currentDir.Children.ContainsKey(name))
            {
                _currentDir.Children[name] = fileNode;
            }
            else
            {
                _currentDir.Children.Add(name, fileNode);
            }
            Console.WriteLine("File created.");
        }

        private void TypeFile(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Required parameter missing.");
                return;
            }

            string name = args[0].ToUpper();
            if (_currentDir.Children.TryGetValue(name, out VNode node))
            {
                if (node.IsDirectory)
                    Console.WriteLine("Access denied - File is a directory.");
                else
                    Console.WriteLine(node.Content);
            }
            else
            {
                Console.WriteLine("File not found.");
            }
        }

        private void DeleteFile(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Required parameter missing.");
                return;
            }

            string name = args[0].ToUpper();
            if (_currentDir.Children.TryGetValue(name, out VNode node))
            {
                if (node.IsDirectory)
                    Console.WriteLine("Cannot delete directories using DEL. Use RMDIR.");
                else
                    _currentDir.Children.Remove(name);
            }
            else
            {
                Console.WriteLine("File not found.");
            }
        }

        private void CopyFile(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Required parameter missing. Usage: COPY [source] [destination]");
                return;
            }

            string src = args[0].ToUpper();
            string dst = args[1].ToUpper();

            if (_currentDir.Children.TryGetValue(src, out VNode srcNode))
            {
                if (srcNode.IsDirectory)
                {
                    Console.WriteLine("Cannot copy directories.");
                    return;
                }

                var dstNode = new VNode(dst, false, _currentDir) { Content = srcNode.Content };
                if (_currentDir.Children.ContainsKey(dst))
                    _currentDir.Children[dst] = dstNode;
                else
                    _currentDir.Children.Add(dst, dstNode);

                Console.WriteLine("        1 file(s) copied.");
            }
            else
            {
                Console.WriteLine("Source file not found.");
            }
        }

        private void ShowMemory()
        {
            int baseTotal = 640;
            int hmaSize = _hmaEnabled ? 64 : 0;
            int baseUsed = _hmaEnabled ? 48 : 94;

            Console.WriteLine("\nMemory Standard Base Area Map:");
            Console.WriteLine($"  Base/Conventional Memory:    {baseTotal} KB");
            Console.WriteLine($"  HMA Segment Allocated:        {hmaSize} KB");
            Console.WriteLine($"  Used (GST-DOS Resident):      {baseUsed} KB");
            Console.WriteLine($"  Available Conventional:      {baseTotal - baseUsed} KB\n");
        }

        // New uninstall implementation
        private void Uninstall()
        {
            Console.WriteLine("\nUNINSTALL GST-DOS");
            Console.Write("This will remove the GST-DOS disk state file and unregister your installation. Continue? (Y/N): ");
            var response = Console.ReadLine()?.Trim().ToUpper();
            if (response == "Y" || response == "YES")
            {
                try
                {
                    if (File.Exists(DiskFileName))
                    {
                        File.Delete(DiskFileName);
                        Console.WriteLine("GST-DOS installation removed (disk state deleted).");
                    }
                    else
                    {
                        Console.WriteLine("No installation found to remove.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while uninstalling: {ex.Message}");
                }

                // Reset in-memory state so session reflects uninstall
                _rootDir = new VNode("C:", true);
                _currentDir = _rootDir;
                _pathStack = new List<string> { "C:" };
                _ownerName = "SYSTEM";
                _hmaEnabled = false;

                Console.WriteLine("Press [ENTER] to exit.");
                Console.ReadLine();

                // Exit shell so user returns to host environment
                _running = false;
            }
            else
            {
                Console.WriteLine("Uninstall cancelled.");
            }
        }
    }
}
