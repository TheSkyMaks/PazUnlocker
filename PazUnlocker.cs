using System.Text;
using PazUnlocker.ChatGpt;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PazUnlocker
{
    #region PazUnlocker

    internal class PazUnlocker
    {
        #region Fields: Private

        private readonly IWebDriver _driver = new ChromeDriver();

        private List<string> _listResults = new List<string>();

        private Dictionary<string, string> _questionGptResponse = new Dictionary<string, string>();
        private Dictionary<string, string> _questionNumberAnswers = new Dictionary<string, string>();

        private readonly ChatGptApiClient _chatGpt;

        private const string _templateForQuestion = "Візьми на себе роль спеціаліста з 30 років досвіду у низькорівневому програмуванні вбудованих систем на базі мікроконтролерів та мікропроцесорів. Перерахуй усі можливі правильні відповіді, відповідь повинна бути у короткому вигляді: номер відповіді (якщо кілька, то через кому) - відповідь - опис відповіді. Дай відповідь на запитання: {0}";

        private readonly bool _consoleQuestionsLog;

        private readonly bool _waitBeforeSubmit;

        private int _numberOfAttempt;

        private int _maxMark;

        #endregion

        #region Fields: Public

        public Dictionary<string, List<string>> _questionAnswers = new Dictionary<string, List<string>>();

        public Dictionary<string, string> _questionComments = new Dictionary<string, string>();

        public string _testLink;

        public bool _waitBeforeStartNewAttempt;

        #endregion

        #region Constructors: Public

        public PazUnlocker(string chatGptApiKey)
        {
            _chatGpt = new ChatGptApiClient(chatGptApiKey);
            _consoleQuestionsLog = false;
        }

        public PazUnlocker(string chatGptApiKey, bool consoleQuestionsLog, bool waitBeforeSubmit)
        {
            _chatGpt = new ChatGptApiClient(chatGptApiKey);
            _consoleQuestionsLog = consoleQuestionsLog;
            _waitBeforeSubmit = waitBeforeSubmit;
        }

        #endregion

        #region Methods: Private

        private string TryGetCode(string inputString)
        {
            try
            {
                var code = _driver.FindElement(By.CssSelector("code"));
                var codeText = code.Text;
                return inputString + "; code: " + codeText;
            }
            catch (Exception)
            {
                return inputString;
            }
        }

        private string TryGetPicture(string inputString)
        {
            try
            {
                var img = _driver.FindElement(By.ClassName("img-fluid"));
                var src = img.GetAttribute("src");
                src = src.Replace(_testLink, "http://lysenko.in:5000/");
                return inputString + "; link: " + src;
            }
            catch (Exception)
            {
                return inputString;
            }
        }

        private string GetAnswerFromChatGpt(string content, int tryNumber = 0)
        {
            var response = _chatGpt.GetAnswerFromChatGpt(content);
            if (string.IsNullOrEmpty(response))
            {
                response = "Empty response";
                tryNumber++;

                if (tryNumber < 3)
                {
                    return GetAnswerFromChatGpt(content, tryNumber);
                }
            }

            return response;
        }

        private string GetAnswer(string questionWithAnswers, string questionText)
        {
            string response;

            if (!_questionGptResponse.ContainsKey(questionText))
            {
                var content = string.Format(_templateForQuestion, questionWithAnswers);
                response = GetAnswerFromChatGpt(content);
                if (response.StartsWith("#Error#"))
                {
                    var redColor = ConsoleColor.Red;
                    System.Console.ForegroundColor = redColor;
                    System.Console.WriteLine("-----response error-----", redColor);
                    System.Console.WriteLine(response, redColor);
                    System.Console.WriteLine("------------------", redColor);
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                    return string.Empty;
                }
                _questionGptResponse.Add(questionText, response);
            }
            else
            {
                response = _questionGptResponse[questionText];
            }

            if (_consoleQuestionsLog || _questionComments.ContainsKey(questionText) && !string.IsNullOrWhiteSpace(_questionComments[questionText]))
            {
                System.Console.WriteLine("-----response-----");
                System.Console.WriteLine(response);
                System.Console.WriteLine("------------------");
            }

            return response;
        }

        private void AddOrUpdateQuestionAnswer(string questionText, List<string> correctAnswers)
        {
            if (!_questionAnswers.ContainsKey(questionText))
            {
                _questionAnswers.Add(questionText, correctAnswers);
            }
            else
            {
                _questionAnswers[questionText] = correctAnswers;
            }
        }

        private void AddOrUpdateQuestionComment(string questionText, string comment)
        {
            if (!_questionComments.ContainsKey(questionText))
            {
                _questionComments.Add(questionText, comment);
            }
            else
            {
                _questionComments[questionText] = comment;
            }
        }

        private bool ClickByAnswer(System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> answers, string questionText)
        {
            var isClicked = false;
            if (!_questionAnswers.ContainsKey(questionText))
            {
                return isClicked;
            }

            var correctAnswers = _questionAnswers[questionText];
            if (!isClicked)
            {
                foreach (var correctAnswer in correctAnswers)
                {
                    foreach (var answerElement in answers)
                    {
                        var answerText = $"){answerElement.Text.Split(";").First()}";
                        if (correctAnswer == answerText)
                        {
                            answerElement.Click();
                            isClicked = true;
                            break;
                        }
                    }
                }
            }

            return isClicked;
        }

        private bool ClickByResponse(System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> answers, string response, string questionText)
        {
            var isClicked = false;
            var correctAnswers = new List<string>();
            var answersList = response.Split("\n").ToList();
            foreach (var answer in answersList)
            {
                var answersResponses = answer.Split("-").First().Split(")").First().Split(", ");
                foreach (var answersResponse in answersResponses)
                {
                    if (int.TryParse(answersResponse, out int answerIndex))
                    {
                        answerIndex--;
                        var answerElement = answers.ElementAtOrDefault(answerIndex);
                        if (answerElement != null)
                        {
                            var answerText = $"){answerElement.Text.Split(";").First()}";
                            correctAnswers.Add(answerText);
                            answerElement.Click();
                            isClicked = true;
                        }
                    }
                }
            }

            AddOrUpdateQuestionAnswer(questionText, correctAnswers);
            AddOrUpdateQuestionComment(questionText, response);

            return isClicked;
        }

        private bool FindAnswer()
        {
            Thread.Sleep(100);
            var answers = _driver.FindElements(By.CssSelector("label.form-check-label"));
            var question = _driver.FindElement(By.CssSelector("h6"));
            var questionText = TryGetCode(question.Text);
            questionText = TryGetPicture(questionText);

            var index = 0;
            StringBuilder sbAnswers = new StringBuilder();
            foreach (var answerElement in answers)
            {
                index++;
                sbAnswers.AppendLine($"{index}) {answerElement.Text};");
            }
            var answersText = sbAnswers.ToString();

            if (!_questionNumberAnswers.ContainsKey(questionText))
            {
                _questionNumberAnswers.Add(questionText, answersText);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(questionText);
            sb.AppendLine(answersText);
            var questionWithAnswers = sb.ToString();

            questionText = questionText.Replace("\r\n", "\n").Replace(" ", "");
            var isClicked = ClickByAnswer(answers, questionText);

            if (_consoleQuestionsLog)
            {
                System.Console.WriteLine("-----question-----");
                System.Console.WriteLine(questionWithAnswers);
            }

            if (!isClicked && _questionComments.ContainsKey(questionText) && !string.IsNullOrWhiteSpace(_questionComments[questionText]))
            {
                if (!_consoleQuestionsLog)
                {
                    System.Console.WriteLine("-----question-----");
                    System.Console.WriteLine(questionWithAnswers);
                }
                System.Console.WriteLine("-----Answer from file-----");
                System.Console.WriteLine(_questionComments[questionText]);
                System.Console.WriteLine("------------------");
            }

            if (isClicked)
            {
                return isClicked;
            }

            string response = GetAnswer(questionWithAnswers, questionText);
            isClicked = ClickByResponse(answers, response, questionText);

            if (isClicked)
            {
                return isClicked;
            }

            if (!isClicked)
            {
                var firstAnswer = answers.FirstOrDefault();
                if (firstAnswer != null)
                {
                    firstAnswer.Click();
                }
            }

            return isClicked;
        }

        private string GetQuestionGptResponseContent()
        {
            StringBuilder sb = new StringBuilder();
            var i = 1;
            foreach (var item in _questionGptResponse)
            {
                sb.AppendLine("Question #:" + i++);
                sb.AppendLine(item.Key);
                sb.AppendLine("Chat GPT answer:");
                sb.AppendLine(item.Value);
            }
            return sb.ToString();
        }

        private void WriteFile(string message)
        {
            var fileName = "QuestionGptResponse.txt";
            using (var file = File.Open(fileName, FileMode.OpenOrCreate))
            {
                using (var stream = new StreamWriter(file))
                {
                    stream.WriteLine(message);
                    var str = GetQuestionGptResponseContent();
                    stream.WriteLine(str);
                }
            }
        }

        private string GetAttemptsListString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var result in _listResults)
            {
                sb.AppendLine(GetAttemptMessage(result));
            }
            return sb.ToString();
        }

        private void WriteAttemptsFile(string message)
        {
            var fileName = "AttemptsResults.txt";
            using (var file = File.Open(fileName, FileMode.OpenOrCreate))
            {
                using (var stream = new StreamWriter(file))
                {
                    stream.WriteLine(message);
                    var str = GetAttemptsListString();
                    stream.WriteLine(str);
                }
            }
        }

        private string GetAttemptMessage(string attempt)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("---------------------------");
            sb.AppendLine($"End of Attempt: {DateTime.Now}");
            sb.AppendLine($"Number Of Attempt: {_numberOfAttempt}");
            sb.AppendLine($"Current max mark: {_maxMark}");
            sb.AppendLine($"Attempt: {attempt}");
            sb.AppendLine("---------------------------");
            return sb.ToString();
        }

        private void WriteAttemptMessage(string attempt)
        {
            var color = ConsoleColor.Yellow;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(GetAttemptMessage(attempt), color);
            System.Console.ForegroundColor = ConsoleColor.Gray;
        }

        private IWebElement? GetElementByCssSelector(string cssSelector, int tryNumber = 0)
        {
            try
            {
                return _driver.FindElement(By.CssSelector(cssSelector));
            }
            catch (Exception ex)
            {
                tryNumber++;
                var color = ConsoleColor.Red;
                System.Console.ForegroundColor = color;
                System.Console.WriteLine(ex.Message);
                System.Console.ForegroundColor = ConsoleColor.Gray;
                if (tryNumber > 3)
                {
                    return null;
                }
                return GetElementByCssSelector(cssSelector, tryNumber);
            }
        }

        private string GetTextFromElement(string cssSelector)
        {
            return GetTextFromElement(GetElementByCssSelector(cssSelector));
        }

        private string GetTextFromElement(IWebElement? element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            return element.Text;
        }

        #endregion

        #region Methods: Public

        public void OpenPage()
        {
            _driver.Navigate().GoToUrl(_testLink);
        }

        public void ExamMode()
        {
            try
            {
                int i = 0;
                int indexValue = -1;
                while (true)
                {
                    Thread.Sleep(5000);
                    var indexText = GetTextFromElement("h1").Replace("Питання", "").Split("з").FirstOrDefault();
                    if (int.TryParse(indexText, out int tempIndexValue) && indexValue != tempIndexValue)
                    {
                        UnlockExam();
                        indexValue = tempIndexValue;
                    }
                }
            }
            catch (Exception ex)
            {
                var color = ConsoleColor.Red;
                System.Console.ForegroundColor = color;
                System.Console.WriteLine(ex.Message);
                System.Console.ForegroundColor = ConsoleColor.Gray;
                ExamMode();
            }
        }

        public void UnlockExam()
        {
            var answers = _driver.FindElements(By.CssSelector("label.form-check-label"));
            var questionText = TryGetCode(GetTextFromElement("h6"));
            questionText = TryGetPicture(questionText);

            string answersText;
            if (_questionNumberAnswers.ContainsKey(questionText))
            {
                answersText = _questionNumberAnswers[questionText];
            }
            else
            {
                var index = 0;
                StringBuilder sbAnswers = new StringBuilder();
                foreach (var answerElement in answers)
                {
                    index++;
                    sbAnswers.AppendLine($"{index}) {answerElement.Text};");
                }
                answersText = sbAnswers.ToString();
                _questionNumberAnswers.Add(questionText, answersText);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(questionText);
            sb.AppendLine(answersText);
            var questionWithAnswers = sb.ToString();

            questionText = questionText.Replace("\r\n", "\n").Replace(" ", "");
            if (string.IsNullOrWhiteSpace(questionText))
            {
                return;
            }

            System.Console.WriteLine("-----question-----");
            System.Console.WriteLine(questionWithAnswers);

            if (_questionComments.ContainsKey(questionText) && !string.IsNullOrWhiteSpace(_questionComments[questionText]))
            {
                System.Console.WriteLine("-----Answer from file-----");
                System.Console.WriteLine(_questionComments[questionText]);
                System.Console.WriteLine("------------------");
            }

            var response = GetAnswer(questionWithAnswers, questionText);
            if (!_consoleQuestionsLog && (!_questionComments.ContainsKey(questionText) || string.IsNullOrWhiteSpace(_questionComments[questionText])))
            {
                System.Console.WriteLine("-----response-----");
                System.Console.WriteLine(response);
                System.Console.WriteLine("------------------");
            }

        }

        public void PazUnlock(string name, string group, int maxNumberOfAttempts = 10)
        {
            _numberOfAttempt = 0;
            try
            {
                UnlockPaz(name, group, maxNumberOfAttempts);
            }
            catch (Exception ex)
            {
                var color = ConsoleColor.Red;
                System.Console.ForegroundColor = color;
                System.Console.WriteLine(ex.Message);
                System.Console.WriteLine("Restart");
                System.Console.ForegroundColor = ConsoleColor.Gray;
                UnlockPaz(name, group, maxNumberOfAttempts);
            }
        }

        public void UnlockPaz(string name, string group, int maxNumberOfAttempts)
        {
            string attempt;
            int mark;

            do
            {
                _numberOfAttempt++;
                _driver.Navigate().GoToUrl(_testLink);

                IWebElement unameInputElement = _driver.FindElement(By.Id("uname"));
                unameInputElement.SendKeys(name);

                IWebElement ugroupInputElement = _driver.FindElement(By.Id("ugroup"));
                ugroupInputElement.SendKeys(group);

                IWebElement submitButton = _driver.FindElement(By.CssSelector("button.btn.btn-outline-info"));
                submitButton.Click();
                int i = 1;
                while (i <= 20)
                {
                    FindAnswer();

                    if (_waitBeforeSubmit)
                    {
                        System.Console.WriteLine("Press Enter to submit...");
                        System.Console.ReadLine();
                    }
                    IWebElement submitAnswerButton = _driver.FindElement(By.CssSelector("input#submit.btn.btn-outline-info"));
                    submitAnswerButton.Click();

                    var indexText = GetTextFromElement("h1").Replace("Питання", "").Split("з").FirstOrDefault();
                    if (int.TryParse(indexText, out int indexValue))
                    {
                        i = indexValue;
                    }
                    else
                    {
                        i++;
                    }
                }

                Thread.Sleep(60);

                attempt = GetTextFromElement("div.media-body");

                _listResults.Add(attempt);

                var resultArr = attempt.Split("\r\n");

                mark = int.Parse(resultArr[4].Replace("Вірних відповідей ", ""));

                if (mark > _maxMark)
                {
                    _maxMark = mark;
                }

                WriteAttemptMessage(attempt);

                WriteFile("number Of Attempt: " + _numberOfAttempt + " max Mark: " + _maxMark);
                WriteAttemptsFile($"Current max mark: {_maxMark}");

                if (_waitBeforeStartNewAttempt)
                {
                    System.Console.WriteLine("Press Enter to start new attempt...");
                    System.Console.ReadLine();
                }

                if (_numberOfAttempt == maxNumberOfAttempts)
                {
                    break;
                }
            } while (mark < 19);

            WriteAttemptMessage(attempt);
            System.Console.ReadLine();

            _driver.Quit();
        }

        #endregion
    }

    #endregion
}
