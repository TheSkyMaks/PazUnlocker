namespace PazUnlocker.Console;

using System;
using System.IO;
using System.Text;

public class FileManager
{
    private string _path;
    public FileManager(string fileName, string dir = null)
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string fullPath;
        if (string.IsNullOrWhiteSpace(dir))
        {
            fullPath=Path.Combine(basePath, fileName);
        }
        else
        {
            fullPath=Path.Combine(basePath, dir, fileName);

        }
        
        _path = fullPath;
        CreateFile();
    }
    public void CreateFile()
    {
        if (File.Exists(_path))
        {
            return;
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllText(_path, "");
            Console.WriteLine($"File created: {_path}");
        }
    }

    public void EditFile(string content)
    {
        if (File.Exists(_path))
        {
            File.WriteAllText(_path, content);
            Console.WriteLine($"File edited: {_path}");
        }
        else
        {
            Console.WriteLine($"File not found: {_path}");
        }
    }

    public void AppendToFile(string content)
    {
        if (File.Exists(_path))
        {
            File.AppendAllText(_path, $"{content}{Environment.NewLine}");
        }
        else
        {
            Console.WriteLine($"File not found: {_path}");
        }
    }

    public static void CreateFolder(string folderPath)
    {
        if (FolderExists(folderPath))
        {
            return;
        }
        Directory.CreateDirectory(folderPath);
        Console.WriteLine($"Folder created: {folderPath}");
    }
    private static bool FolderExists(string folderPath)
    {
        return Directory.Exists(folderPath);
    }

}
