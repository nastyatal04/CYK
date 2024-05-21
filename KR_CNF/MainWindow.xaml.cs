using Microsoft.VisualBasic;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KR_CNF
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Нетерминалами считаются только заглавные буквы латинского и русского алфавитов.
        /// </summary>
        static string possible_nonterminals = "ABCDEFGHIJKLMNOPQRSTUVWXYZАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";

        /// <summary>
        /// Правила грамматики.
        /// </summary>
        static Dictionary<string, List<List<string>>> R = new Dictionary<string, List<List<string>>>();
        /// <summary>
        /// Слово.
        /// </summary>
        public static string w = "";
        /// <summary>
        /// Имеется ли eps-правило.
        /// </summary>
        public static bool isEps = false;
        /// <summary>
        /// Есть ли ошибка в правилах.
        /// </summary>
        public static bool isErrorRules = false;

        /// <summary>
        /// Вывод общих ошибок.
        /// </summary>
        /// <param name="message">Выводимое сообщение.</param>
        public void ErrorAll(string message = "")
        {
            if (message == "")
            {
                errorAll.Text = message;
                errorAll.Visibility = Visibility.Collapsed;
            }
            else
            {
                errorAll.Text = message;
                errorAll.Visibility = Visibility.Visible;
                ShowResult();
            }
        }

        /// <summary>
        /// Вывод ошибок по вводу правил.
        /// </summary>
        /// <param name="message">Выводимое сообщение.</param>
        public void ErrorRules(string message = "")
        {
            if (message == "")
            {
                isErrorRules = true;
                errorRules.Text = message;
                errorRules.Visibility = Visibility.Collapsed;
            }
            else
            {
                isErrorRules = false;
                errorRules.Text = message;
                errorRules.Visibility = Visibility.Visible;
                ShowResult();
            }
        }

        /// <summary>
        /// Отображение результата работы CYK-алгоритма. 
        /// </summary>
        /// <param name="message">Выводимое сообщение.</param>
        public void ShowResult(string message = "")
        {
            if (message == "")
            {
                result.Content = message;
                result.Visibility = Visibility.Collapsed;
            }
            else
            {
                result.Content = message;
                result.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Разделение правил.
        /// </summary>
        /// <param name="rules">Список правил.</param>
        public void RulesParse(string rules)
        {
            rules = rules.Trim();
            if (rules == "")
            {
                ErrorRules("Введите правила.");
                return;
            }
            else
            {
                ErrorRules();
                rules = rules.Trim();
                string[] rules_str_array = rules.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);//Разделяем каждую строку из правила (может сюда проверку на то, что всё в одну строку зафигачено)
                foreach (string rules_str in rules_str_array) //Получили отдельные строки с правилами
                {
                    if (!rules_str.Contains("->"))
                    {
                        ErrorRules("Неверный формат записи правил. (Нет \"->\")");
                        return;
                    }
                    else
                    {
                        ErrorRules();
                        string[] rule = rules_str.Trim().Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);//Разделяем кадое правило на левую и правую часть по ->
                        if (rule.Length > 2)//Если ввели S -> A A -> AB
                        {
                            ErrorRules("Неверный формат записи правил. В одной строке одна стрелка.");
                            return;
                        }
                        else if (rule.Length == 1)//Если нет какой-то части у правила
                        {
                            ErrorRules("У правила отсутствует левая или правая часть.");
                            return;
                        }
                        else if (rule[0].Trim().Length != 1) //если нетерминала из левой части нет среди списка нетерминалов
                        {
                            ErrorRules("Не КС-граматика, слева должен содержаться только один нетерминал.");
                            return;
                        }
                        else//Парсим правую часть правила
                        {
                            ErrorRules();                            
                            R[rule[0]] = RightSideParsing(rule[0], rule[1]);
                            if (R[rule[0]] == null)
                            {
                                return;
                            }
                            if(!R.ContainsKey("S"))
                            {
                                ErrorRules("Отсутствует стартовый нетерминал S.");
                                return;
                            }
                            if(isEps)
                            {
                                IsEps();
                            }
                            
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Проверка отсутствия eps переходов.
        /// </summary>
        /// <returns></returns>
        public bool IsEps()
        {
            foreach (var rule in R)//Рассматриваем все правила
            {
                foreach (var rul in rule.Value) // Всё множество правил для конкретного нетерминала
                {
                    if (rul.Count == 2) //Берём одно конкретное правило с двумя нетерминалами
                    {
                        if (rul[0].ToString() == "S" || rul[1].ToString() == "S")
                        {
                            ErrorRules("Не КС-граматика, имеется eps-правило и нетерминалы, включающие в правила стартовый нетерминал.");
                            return false;
                        }
                    }
                }
            }
            ErrorRules();
            return true;
        }

        /// <summary>
        /// Создаёт переменную таблицы разбора и инициализирует её пустыми списками строк.
        /// </summary>
        /// <param name="size">Длина проверяемого слова.</param>
        /// <returns>Переменная таблицы рабора.</returns>
        public List<string>[,] InitialParseTable(int size)
        {
            List<string>[,] pt = new List<string>[size, size];
            for (int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    pt[i, j] = new List<string>();
                }
            }
            return pt;
        }

        /// <summary>
        /// Функция, реализующая алгоритм Кока-Янгера-Касами.
        /// Заполняет таблицу разбора и выводит сообщение о выводимости слова.
        /// </summary>
        public void CYK()
        {
            int sizeW = w.Length;
            List<string>[,] parse_table = InitialParseTable(sizeW);
            //Заполняем первый уровень таблицы разбора
            for (int i = 0; i < sizeW; ++i)
            {
                foreach (var rule in R)
                {
                    foreach (var rul in rule.Value) // Всё множество правил для нетерминала
                    {
                        if (rul.Count == 1)
                        {
                            foreach (var r in rul)//Одно конкретное правило
                            {
                                if (r == w[i].ToString())
                                {
                                    parse_table[i, i].Add(rule.Key);
                                }
                            }
                        }
                    }
                }
            }
            //Другие уровни
            int j0 = 1;
            while (j0 != sizeW)
            {
                int j = j0;
                for (int i = 0; i < sizeW - j0; ++i)
                {
                    for (int k = i; k < j; ++k)
                    {
                        //Проходжение по списку нетерминалов в pt[i, j]
                        foreach (string ptA in parse_table[i, k])
                        {
                            foreach (string ptB in parse_table[k + 1, j])
                            {
                                List<string> nt = SearchRule(ptA, ptB);
                                if (nt.Count != 0)
                                {
                                    foreach (string ntA in nt)
                                    {
                                        if (!parse_table[i, j].Contains(ntA))
                                        {
                                            parse_table[i, j].Add(ntA);
                                        }
                                    }

                                }
                            }
                        }
                    }
                    j += 1;
                }
                j0 += 1;
            }

            if (parse_table[0, sizeW - 1].Contains("S"))
            {
                ShowResult("Слово выводится:)");
            }
            else
            {
                ShowResult("Слово не выводится:(");
            }
        }

        /// <summary>
        /// Поиск правила вида S->AB.
        /// </summary>
        /// <param name="A">Первый нетерминал правила.</param>
        /// <param name="B">Второй нетерминал правила.</param>
        /// <returns>Список нетерминалов, имеющих правило соответствующего вида.</returns>
        public List<string> SearchRule(string A, string B)
        {
            List<string> nt = new List<string>();
            foreach (var rule in R)//Рассматриваем все правила
            {
                foreach (var rul in rule.Value) // Всё множество правил для конкретного нетерминала
                {
                    if (rul.Count == 2) //Берём одно конкретное правило с двумя нетерминалами
                    {
                        if (rul[0].ToString() == A && rul[1].ToString() == B)
                        {
                            nt.Add(rule.Key);
                        }
                    }
                }
            }
            return nt;
        }

        /// <summary>
        /// Разбор правой части правила.
        /// </summary>
        /// <param name="lside"></param>
        /// <param name="rside"></param>
        /// <returns></returns>
        public List<List<string>> RightSideParsing(string lside, string rside)
        {
            //Нам прилетает что-то вроде "AA|BB|eps"
            List<List<string>> ride_side = new List<List<string>>();
            rside = rside.Trim();
            if (!rside.Contains("|")) //то это либо "AA", "a", "eps"
            {
                if (rside.Length == 2) // "AA"
                {
                    if (possible_nonterminals.Contains(rside[0]) && possible_nonterminals.Contains(rside[1]))
                    {
                        ErrorRules();
                        ride_side.Add(new List<string>() { rside[0].ToString(), rside[1].ToString() });
                    }
                    else
                    {
                        ErrorRules("Грамматика не соответствует НФХ. В правой части либо два нетерминала, либо один терминал, либо, если стартовый нетерминал, то eps.");
                        return null;
                    }
                }
                else if (rside.Length == 1 && !possible_nonterminals.Contains(rside))//то это "a" -- терминал
                {
                    ErrorRules();
                    ride_side.Add(new List<string>() { rside });
                }
                else if (rside == "eps")//то это eps  
                {
                    if (lside == "S")
                    {
                        ErrorRules();
                        ride_side.Add(new List<string>() { rside });
                        isEps = true;
                    }
                    else
                    {
                        ErrorRules("Грамматика не соответствует НФХ. Eps-правило может быть только в стартовом нетерминале.");
                        return null;
                    }
                }
                else
                {
                    ErrorRules("Грамматика не соответствует НФХ. В правой части либо два нетерминала, либо один терминал, либо, если стартовый нетерминал, то eps.");
                    return null;
                }
            }
            else
            {
                string[] rule = rside.Trim().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                if (rule.Length == 0) //Если, например, изнчально было подано првило "S->||||"
                {
                    ErrorRules("Неверный формат записи правой части правил.");
                    return null;
                }
                else
                {
                    foreach (string sep_rule in rule)//Каждую штучку "AA", "a", "eps"
                    {
                        if (sep_rule.Length == 2) // "AA"
                        {
                            if (possible_nonterminals.Contains(sep_rule[0]) && possible_nonterminals.Contains(sep_rule[1]))
                            {
                                ErrorRules();
                                ride_side.Add(new List<string>() { sep_rule[0].ToString(), sep_rule[1].ToString() });
                            }
                            else
                            {
                                ErrorRules("Грамматика не соответствует НФХ. В правой части либо два нетерминала, либо один терминал, либо, если стартовый нетерминал, то eps.");
                                return null;
                            }
                        }
                        else if (sep_rule.Length == 1 && !possible_nonterminals.Contains(sep_rule))//то это "a" -- терминал
                        {
                            ErrorRules();
                            ride_side.Add(new List<string>() { sep_rule });
                        }
                        else if (sep_rule == "eps")//то это eps  
                        {
                            if (lside == "S")
                            {
                                ErrorRules();
                                ride_side.Add(new List<string>() { sep_rule });
                                isEps = true;
                            }
                            else
                            {
                                ErrorRules("Грамматика не соответствует НФХ. Eps-правило может быть только в стартовом нетерминале.");
                                return null;
                            }
                        }
                        else
                        {
                            ErrorRules("Грамматика не соответствует НФХ. В правой части либо два нетерминала, либо один терминал, либо, если стартовый нетерминал, то eps.");
                            return null;
                        }
                    }
                }
            }
            return ride_side;
        }

        /// <summary>
        /// Проверка того, выводимо ли в данной грамматике пустое слово.
        /// </summary>
        public void CYKfromEps()
        {
            bool flag = false;
            foreach (List<string> rule in R["S"])
            {
                if(rule.Contains("eps"))
                {
                    flag = true;
                }
            }
            if(flag)
            {
                ShowResult("Слово выводится:)");
            }
            else
            {
                ShowResult("Слово не выводится:(");
            }
            
        }

        /// <summary>
        /// Проверка слова на правильность ввода.
        /// Обрабатывает ввод eps.
        /// </summary>
        /// <param name="w">Строка со словом из поля формы.</param>
        public bool IsWord(string str)
        {
            str = str.Trim();
            if(str == "")
            {
                errorWord.Text = "Введите слово.";
                errorWord.Visibility = Visibility.Visible;
                ShowResult();
                return false;
            }
            else if(str.Trim() == "eps") {
                w = str.Trim();
                CYKfromEps();
                return false;
            }
            else
            {
                w = str.Trim();
                errorWord.Text = "";
                errorWord.Visibility = Visibility.Collapsed;
                return true;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            rules.Text = "S->AB|BC\nA->BA|a\nB->CC|b\nC->AB|a";
            word.Text = "baaba";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ErrorAll();
                RulesParse(rules.Text);
                if(IsWord(word.Text) && isErrorRules)
                {
                    CYK();
                }
            }
            catch {
                ErrorAll("Ошибка ввода.");
            }
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string messageBoxText = 
                "Программа предназначена для проверки выводимости слова из данной грамматики в нормальной форме Хомского (НФХ).\n\n" +
                    "На вход подаётся грамматика в НФХ и слово. Для проверки необходимо после заполнения полей нажать кнопку \"Проверить\". " +
                    "Кнопка \"Очистить\" удаляет все записи в полях и результат работы программы." + 
                    "\n\nПример ввода грамматики:\n" +
                    "S->AA|0\nA->SS|1\n\n" + 
                    "Пример ввода слова:\n"+
                    "0111\n\n" + 
                    "Стартовый нетерминал обозначается буквой S.\n" + "Для ввода пустого слова используйте слово \"eps\", например S->AA|eps.\n" +
                    "Нетерминалами могут быть только заглавные буквы латинского и русского алфавитов.";
            string caption = "Справка";
            MessageBox.Show(messageBoxText, caption);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            rules.Text = "";
            word.Text = "";

            result.Content = "";
            result.Visibility = Visibility.Collapsed;

            errorAll.Text = "";
            errorAll.Visibility = Visibility.Collapsed;

            errorRules.Text = "";
            errorRules.Visibility = Visibility.Collapsed;

            errorWord.Text = "";
            errorWord.Visibility = Visibility.Collapsed;
        }
    }
}