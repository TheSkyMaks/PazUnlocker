namespace PazUnlocker.Console
{
    internal class PazConsoleApp
    {
        private string _name;
        private string _group;
        private string _apiKey;
        private int _attempts = 10;
        private bool _consoleQuestionsLog = true;
        private bool _waitBeforeSubmit = false;
        private bool _waitBeforeStartNewAttempt;
        private bool _examMode;
        private OpenXMLParser openXMLParser = new OpenXMLParser();
        private string _testLink;
        private Dictionary<string, string> _questionComments = new Dictionary<string, string>();


        public PazConsoleApp()
        {
            var json = File.ReadAllText("appConfig.json");
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            _name = jsonObj["Name"];
            _group = jsonObj["Group"];
            _group = jsonObj["Group"];
            _attempts = jsonObj["Attempts"];
            _apiKey = jsonObj["ApiKey"];
            _consoleQuestionsLog = jsonObj["ConsoleQuestionsLog"];
            _waitBeforeSubmit = jsonObj["WaitBeforeSubmit"];
            _testLink = jsonObj["TestUrl"];
            _waitBeforeStartNewAttempt = jsonObj["WaitBeforeStartNewAttempt"];
            _examMode = jsonObj["ExamMode"];
        }

        private List<int> GetNumbersOfCorrectAnswers(string answerStr)
        {
            var answerNumberList = new List<int>();
            if (string.IsNullOrWhiteSpace(answerStr))
            {
                return answerNumberList;
            }

            var answers = answerStr.Split("\n").ToList();
            foreach (var answer in answers)
            {
                var answerResponse = answer.Split("-").First().Split(")").First();
                if (int.TryParse(answerResponse, out int answerIndex))
                {
                    answerNumberList.Add(answerIndex);
                }
            }

            return answerNumberList;
        }

        private Dictionary<int, string> GetNumbersAnswerDictionary(string answerStr)
        {
            var numberAnswers = new Dictionary<int, string>();
            if (string.IsNullOrWhiteSpace(answerStr))
            {
                return numberAnswers;
            }

            var baseAnswers = answerStr.Split("\n").ToList();

            foreach (var answer in baseAnswers)
            {
                var tempAnswer = answer.Trim();
                var tempAnswers = tempAnswer.Split("-").First().Split(")");
                var tempAnswersFirst = tempAnswers[0];
                if (int.TryParse(tempAnswersFirst, out int answerIndex))
                {
                    var lengthOfFirst = tempAnswersFirst.Length + 1;
                    var ans = tempAnswer.Substring(lengthOfFirst).ToString();
                    var answerText = $"){ans.Trim().Split(";").First()}";
                    numberAnswers.Add(answerIndex, answerText);
                }
            }

            return numberAnswers;
        }

        private List<string> GetCorrectAnswers(List<int> answerNumberList, Dictionary<int, string> numberAnswers)
        {
            var correctAnswers = new List<string>();

            foreach (var answerNumber in answerNumberList)
            {
                if (numberAnswers.ContainsKey(answerNumber))
                {
                    correctAnswers.Add(numberAnswers[answerNumber]);
                }
            }

            return correctAnswers;
        }

        private Dictionary<string, List<string>> InitData()
        {
            var data = openXMLParser.ParseDocument("PAZ.xlsx");
            var questionAnswers = new Dictionary<string, List<string>>();
            foreach (var rowData in data)
            {
                var list = rowData.Value;
                var answerStr = list.Count > 2 ? list[2] : string.Empty;
                var answerNumberList = GetNumbersOfCorrectAnswers(answerStr);

                var correctAnswerStr = list.Count > 1 ? list[1] : string.Empty;
                var numberAnswers = GetNumbersAnswerDictionary(correctAnswerStr);

                var correctAnswers = GetCorrectAnswers(answerNumberList, numberAnswers);
                var questionText = list[0].Replace("\r\n", "\n").Replace(" ", "");
                questionAnswers.Add(questionText, correctAnswers);

                var comment = list.Count > 3 ? list[3] : string.Empty;

                _questionComments.Add(questionText, answerStr + "\n" + comment);
            }
            return questionAnswers;
        }

        public void Start()
        {
            try
            {
                var questionNumberAnswers = InitData();

                var pazUnlocker = new PazUnlocker(_apiKey, _consoleQuestionsLog, _waitBeforeSubmit);
                pazUnlocker._testLink = _testLink;
                pazUnlocker._questionAnswers = questionNumberAnswers;
                pazUnlocker._questionComments = _questionComments;
                pazUnlocker._waitBeforeStartNewAttempt = _waitBeforeStartNewAttempt;

                if (_examMode)
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

