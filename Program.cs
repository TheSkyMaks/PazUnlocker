using PazUnlocker.Console;
using System.Text;
using System.Text.Json;

static void EnsureFileExists(string filePath)
{
    if (!File.Exists(filePath))
    {
        object defaultContent = new
        {
            Name = "[*Enter Name]",
            Group = "[*Enter Group]",
            Attempts = 1,
            TestUrl = "[*Enter test URL]",
            AnswersUrl = "[*Enter answers sheet URL]",
            ConsoleQuestionsLog = true,
            WaitBeforeSubmit = false,
            WaitBeforeStartNewAttempt = true,
            ExamMode = false,
            StudyMode = true
        };
        
        File.WriteAllText(filePath, JsonSerializer.Serialize(defaultContent));
    }
    else
    {
        Console.WriteLine("appconfig.json already exists.");
    }
}

EnsureFileExists("appconfig.json");
Console.OutputEncoding = Encoding.UTF8;
var console = new PazConsoleApp();
try
{
    await console.Start();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
    Console.Write("Press Enter for close app");
    Console.ReadLine();
}
