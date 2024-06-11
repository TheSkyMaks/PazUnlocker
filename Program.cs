using PazUnlocker.Console;
using System.Text;
using System.Text.Json;

static void EnsureFileExists(string filePath)
{
    if (!File.Exists(filePath))
    {
        object defaultContent = new
        {
            Name = "test",
            Group = "1",
            Attempts = 5,
            TestUrl = "https://quiz.itmpaz2024.online/h",
            AnswersUrl = "https://docs.google.com/spreadsheets/d/1jLs0nQccE8CZdkv64pFkrEnfQobnewu2-JLtTeR6Wxg/export?format=xlsx",
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
await console.Start();
