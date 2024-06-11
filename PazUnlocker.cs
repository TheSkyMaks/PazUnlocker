using System.Text;
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


        private const string _templateForQuestion = "Візьми на себе роль спеціаліста з 30 років досвіду у низькорівневому програмуванні вбудованих систем на базі мікроконтролерів та мікропроцесорів. Перерахуй усі можливі правильні відповіді, відповідь повинна бути у короткому вигляді: номер відповіді (якщо кілька, то через кому) - відповідь - опис відповіді. Дай відповідь на запитання: {0}";

        private readonly bool _consoleQuestionsLog;

        private readonly bool _waitBeforeSubmit;

        private int _numberOfAttempt;

        private int _maxMark;

        private List<(string, string, int)> _studies = new List<(string, string, int)>();
        private (string, string) SELECTED;
        #endregion

        #region Fields: Public

        public Dictionary<string, List<string>> _questionAnswers = new Dictionary<string, List<string>>();

        public Dictionary<string, string> _questionComments = new Dictionary<string, string>();
        public Dictionary<string, string> _normalizedQuestions = new Dictionary<string, string>();
        public List<string> _unknownQuestions = new List<string>();

        public string _testLink;

        public bool _waitBeforeStartNewAttempt;
        private Func<string, string> _removeSymbol; 
        #endregion

        #region Constructors: Public
        
        public PazUnlocker(bool consoleQuestionsLog, bool waitBeforeSubmit, Func<string, string> removeSymbol)
        {
            _consoleQuestionsLog = consoleQuestionsLog;
            _removeSymbol = removeSymbol;
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
                return inputString + ";" + codeText;
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
                return inputString + ";" + src;
            }
            catch (Exception)
            {
                return inputString;
            }
        }
        private bool ClickByAnswer(System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> answers, string questionText)
        {
            var isClicked = false;
            if (!_questionAnswers.ContainsKey(questionText))
            {
                _unknownQuestions.Add(questionText);
                return isClicked;
            }

            var correctAnswers = _questionAnswers[questionText];
            if (!isClicked)
            {
                foreach (var correctAnswer in correctAnswers)
                {
                    foreach (var answerElement in answers)
                    {
                        var answerText = $"{answerElement.Text.Split(";").First()}";
                        if (_removeSymbol(correctAnswer) == _removeSymbol(answerText))
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
        
        private bool FindAnswer()
        {
            Thread.Sleep(500);
            var answers = _driver.FindElements(By.CssSelector("label.form-check-label"));
            var question = _driver.FindElement(By.CssSelector("h6"));
            var questionText = TryGetCode(question.Text);
            questionText = TryGetPicture(questionText);
            questionText = _removeSymbol(questionText);
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

        private bool ClickByAllAnswer(System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> answers)
        {
            foreach (var answer in answers)
            {
                answer.Click();
            }

            return true;
        }
        private bool ClickByAllAnswer(System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> answers, string questionText)
        {
            if (!_questionAnswers.ContainsKey(questionText))
            {
                return ClickByAllAnswer(answers);
            }

            var correctAnswers = _questionAnswers[questionText];
            if (correctAnswers.Count == answers.Count)
            {
                answers.FirstOrDefault().Click();
                return true;
            }
            ClickByAllAnswer(answers);
            return true;
        }

        private bool FindTrue()
        {
            Thread.Sleep(500);
            var answers = _driver.FindElements(By.CssSelector("label.form-check-label"));
            var question = _driver.FindElement(By.CssSelector("h6"));
            var questionText = TryGetCode(question.Text);
            questionText = TryGetPicture(questionText);
            questionText = _removeSymbol(questionText);
            var index = 0;
            StringBuilder sbAnswers = new StringBuilder();
            foreach (var answerElement in answers)
            {
                index++;
                sbAnswers.AppendLine($"{index}) {answerElement.Text};");
            }
            var answersText = sbAnswers.ToString();
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(questionText);
            sb.AppendLine(answersText);
            var questionWithAnswers = sb.ToString();

            questionText = questionText.Replace("\r\n", "\n").Replace(" ", "");
            var isClicked = false;
            var isInDatabase = _questionAnswers.ContainsKey(questionText);

            if (isInDatabase || (!string.IsNullOrWhiteSpace(SELECTED.Item1) && !string.IsNullOrWhiteSpace(SELECTED.Item2)))
            {
                isClicked = ClickByAllAnswer(answers, questionText);
            }
            
            if (isClicked)
            {
                return isClicked;
            }
            
            if (!isClicked)
            {
                var s = question.Text;
                System.Console.WriteLine($"Q: {s} ({questionText})");
                var a = System.Console.ReadLine();
                SELECTED = (questionText,a);
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
            sb.AppendLine($"Unknown Questions: {_unknownQuestions.Count}");
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
            
        }

        public void Study(string name, string group)
        {
            string attempt;
            int mark;
            do
            {
                
                
                SELECTED = (null,null);
                _unknownQuestions = new List<string>();
                _numberOfAttempt++;
                _driver.Navigate().GoToUrl(_testLink);

                IWebElement unameInputElement = _driver.FindElement(By.Id("username"));
                unameInputElement.SendKeys(name);

                IWebElement ugroupInputElement = _driver.FindElement(By.Id("usergroup"));
                ugroupInputElement.SendKeys(group);

                IWebElement submitButton = _driver.FindElement(By.Id("submit"));

                try
                {
                    IWebElement? captha = _driver.FindElement(By.ClassName("g-recaptcha"));
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"Confirm the captcha and press ENTER: ");
                    System.Console.ForegroundColor = ConsoleColor.Gray;

                }
                catch (Exception e)
                {
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("INIT SUCCESS");
                    System.Console.WriteLine("PRESS ENTER FOR START TEST: ");
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                }
                
                System.Console.ReadLine();
                submitButton.Click();
                int i = 1;
                while (i <= 20)
                {
                    Thread.Sleep(3000);
                    try
                    {
                        IWebElement? captha = _driver.FindElement(By.ClassName("g-recaptcha"));
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine($"Confirm the captcha and press ENTER: ");
                        System.Console.ForegroundColor = ConsoleColor.Gray;
                        System.Console.ReadLine();
                        System.Console.Clear();
                    }
                    catch (Exception e)
                    {
                    }

                    FindTrue();

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

                mark = int.Parse(resultArr.First().Split('\n')[4].Replace("Вірних відповідей ", ""));

                if (mark >= 1)
                {
                }
                else
                {
                    
                }
                System.Console.WriteLine($"Q: {SELECTED.Item1}");
                System.Console.WriteLine($"A: {SELECTED.Item2}");
                System.Console.WriteLine($"M: {mark}");
                System.Console.WriteLine($"Waiting...");
                System.Console.ReadLine();
            } while (mark < 19);
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
                _unknownQuestions = new List<string>();
                _numberOfAttempt++;
                _driver.Navigate().GoToUrl(_testLink);

                IWebElement unameInputElement = _driver.FindElement(By.Id("username"));
                unameInputElement.SendKeys(name);

                IWebElement ugroupInputElement = _driver.FindElement(By.Id("usergroup"));
                ugroupInputElement.SendKeys(group);

                IWebElement submitButton = _driver.FindElement(By.Id("submit"));

                try
                {
                    IWebElement? captha = _driver.FindElement(By.ClassName("g-recaptcha"));
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"Confirm the captcha and press ENTER: ");
                    System.Console.ForegroundColor = ConsoleColor.Gray;

                }
                catch (Exception e)
                {
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("INIT SUCCESS");
                    System.Console.WriteLine("PRESS ENTER FOR START TEST: ");
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                }
                
                System.Console.ReadLine();
                submitButton.Click();
                int i = 1;
                while (i <= 20)
                {
                    Thread.Sleep(3000);
                    System.Console.Clear();
                    try
                    {
                        IWebElement? captha = _driver.FindElement(By.ClassName("g-recaptcha"));
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine($"Confirm the captcha and press ENTER: ");
                        System.Console.ForegroundColor = ConsoleColor.Gray;
                        System.Console.ReadLine();
                        System.Console.Clear();
                    }
                    catch (Exception e)
                    {
                    }

                    FindAnswer();

                    if (_waitBeforeSubmit)
                    {
                        System.Console.WriteLine($"Q {i} | Press Enter to submit...");
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

                mark = int.Parse(resultArr.First().Split('\n')[4].Replace("Вірних відповідей ", ""));

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
