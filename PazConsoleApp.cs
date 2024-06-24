using System.Net;
using System.Text;

namespace PazUnlocker.Console
{
    internal class PazConsoleApp
    {
        private int _take;
        private string _name;
        private string _group;
        private int _attempts = 10;
        private bool _consoleQuestionsLog = true;
        private bool _waitBeforeSubmit = false;
        private bool _waitBeforeStartNewAttempt;
        private bool _examMode;
        private bool _studyMode;
        private OpenXMLParser openXMLParser = new OpenXMLParser();
        private string _testLink;
        private string _answersUrl;
        private Dictionary<string, string> _questionComments = new Dictionary<string, string>();
        private Dictionary<string, string> _normalizedQuestions = new Dictionary<string, string>();
        private CaptchaSolver _captchaSolver;

        public PazConsoleApp()
        {
            var json = File.ReadAllText("appConfig.json");
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            _name = jsonObj["Name"];
            _group = jsonObj["Group"];
            _attempts = jsonObj["Attempts"];
            _consoleQuestionsLog = jsonObj["ConsoleQuestionsLog"];
            _waitBeforeSubmit = jsonObj["WaitBeforeSubmit"];
            _testLink = jsonObj["TestUrl"];
            _answersUrl = jsonObj["AnswersUrl"];
            _waitBeforeStartNewAttempt = jsonObj["WaitBeforeStartNewAttempt"];
            _examMode = jsonObj["ExamMode"];
            _studyMode = jsonObj["StudyMode"];
            int? take = jsonObj["Take"];
            _take = take.HasValue ? take.Value : 0;
            _captchaSolver = new CaptchaSolver();
        }

        private List<string> GetNumbersOfCorrectAnswers(string answerStr)
        {
            var answerNumberList = new List<string>();
            if (string.IsNullOrWhiteSpace(answerStr))
            {
                return answerNumberList;
            }

            var answers = new List<string>();
            if (answerStr.Contains(';'))
            {
                answers = answerStr.Split(";").ToList();
            }
            else if (answerStr.Contains('\n'))
            {
                answers = answerStr.Split("\n").ToList();
            }
            else
            {
                answers.Add(answerStr);
            }

            answers = answers.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            
            return answers;
        }


        public string RemoveSymbols(string input)
        {
            char[] symbolsToRemove = new[] { '.','?','/',';','\n',' ',':','-','(',')' };
            StringBuilder result = new StringBuilder();

            foreach (char c in input)
            {
                bool toRemove = false;
                foreach (char symbol in symbolsToRemove)
                {
                    if (c == symbol)
                    {
                        toRemove = true;
                        break;
                    }
                }
                if (!toRemove)
                {
                    result.Append(c);
                }
            }

            return result.ToString().ToUpper();
        }

        async Task DownloadGoogleSheetAsXlsx(string url, string destinationPath)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                await using (FileStream fs = new FileStream(destinationPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
        }

        private async Task<Dictionary<string, List<string>>> InitData()
        {
            
            string destinationPath = @"PAZ.xlsx";
        
            await DownloadGoogleSheetAsXlsx(_answersUrl, destinationPath);

            var data = openXMLParser.ParseDocument("PAZ.xlsx");
       
            var questionAnswers = new Dictionary<string, List<string>>();
            foreach (var rowData in data)
            {
                var list = rowData.Value;
                var answerStr = list.Count > 1 ? list[1] : string.Empty;
                var answerNumberList = GetNumbersOfCorrectAnswers(answerStr);
                var questionText = RemoveSymbols(list[0]);
                questionAnswers.Add(questionText, answerNumberList);
                _normalizedQuestions.Add(questionText, list[0]);

                var comment = list.Count > 3 ? list[3] : string.Empty;

                _questionComments.Add(questionText, answerStr + "\n" + comment);
            }
            return questionAnswers;
        }

        public async Task Start()
        {
            try
            {
                var questionNumberAnswers = await InitData();

                var pazUnlocker = new PazUnlocker(_consoleQuestionsLog, _waitBeforeSubmit, RemoveSymbols);
                pazUnlocker._testLink = _testLink;
                pazUnlocker._questionAnswers = questionNumberAnswers;
                pazUnlocker._questionComments = _questionComments;
                pazUnlocker._normalizedQuestions = _normalizedQuestions;
                pazUnlocker._waitBeforeStartNewAttempt = _waitBeforeStartNewAttempt;

                System.Console.Clear();
                if (_studyMode)
                {
                    System.Console.WriteLine("START STUDY MODE");
                    System.Console.WriteLine($"Loaded {questionNumberAnswers.Count} rows");
                    pazUnlocker.Study(_name, _group);
                }
                else if (_examMode)
                {
                    pazUnlocker.OpenPage();
                    pazUnlocker.ExamMode();
                }
                else
                {
                    pazUnlocker.PazUnlock(_name, _group, _attempts);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
