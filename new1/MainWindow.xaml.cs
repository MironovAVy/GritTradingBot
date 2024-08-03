using QuikSharp;
using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace new1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Quik _quik;
        string secCode = "SBER";
        string classCode = "";
        string clientCode = "";
        private bool isServerConnected = false;
        private Tool tool;
        private bool isSubscribedToolOrderBook = false;
        OrderBook toolOrderBook;
        private bool runRobot = false;
        List<Candle> _candleList = new List<Candle>();
        public MainWindow()
        {
            InitializeComponent();
            LogTextBox.Text = "";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("Подключаемся к терминалу Quik...");
                _quik = new Quik(Quik.DefaultPort, new InMemoryStorage());    // инициализируем объект Quik
                                                                              //_quik = new Quik(34136, new InMemoryStorage());    // отладочный вариант

            }
            catch
            {
                Log("Ошибка инициализации объекта Quik.");
            }
            if (_quik != null)
            {
                Log("Экземпляр Quik создан.");
                try
                {
                    Log("Получаем статус соединения с сервером...");
                    isServerConnected = _quik.Service.IsConnected().Result;
                    if (isServerConnected)
                    {
                        Log("Соединение с сервером установлено.");
                        Connect.Content = "Ok";
                        Connect.Background = Brushes.Aqua;
                    }
                    else
                    {
                        Log("Соединение с сервером НЕ установлено.");
                    }
                }
                catch
                {
                    Log("Неудачная попытка получить статус соединения с сервером.");
                }

            }
        }

        public void Log(string str)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText(str + Environment.NewLine);
                    LogTextBox.ScrollToLine(LogTextBox.LineCount - 1);
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            Run();
        }
        void Run()
        {
            try
            {
                Log("Определяем код класса инструмента " + secCode + ", по списку классов" + "...");
                classCode = _quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,QJSIM", secCode).Result;
            }
            catch
            {
                Log("Ошибка определения класса инструмента. Убедитесь, что тикер указан правильно");
            }
            if (classCode != null && classCode != "")
            {
                Log("Создаем экземпляр инструмента " + secCode + "|" + classCode + "...");
                tool = new Tool(_quik, secCode, classCode);
                if (tool != null && tool.Name != null && tool.Name != "")
                {
                    Log("Инструмент " + tool.Name + " создан.");
                    Log("Подписываемся на стакан котировок по бумаге " + tool.Name);
                    _quik.OrderBook.Subscribe(tool.ClassCode, tool.SecurityCode).Wait();
                    isSubscribedToolOrderBook = _quik.OrderBook.IsSubscribed(tool.ClassCode, tool.SecurityCode).Result;
                    if (isSubscribedToolOrderBook)
                    {
                        toolOrderBook = new OrderBook();
                        Log("Подписка на стакан прошла успешно.");
                        Log("Подписываемся на колбэк 'OnQuote'...");
                       

                    }
                    else
                    {
                        Log("Подписка на стакан не удалась.");

                    }
                    Log("Подписываемся на колбэк 'OnFuturesClientHolding'...");

                    Log("Подписываемся на колбэк 'OnDepoLimit'...");
                    
                    Log("Получаем свечи по инструменту '...");
                    _quik.Candles.Subscribe(classCode, secCode, CandleInterval.M1);
                    Log("Получаем свечи по инструменту за последние 10 дней'...");


       


                }
            }
            else
            {
                Log("Не удалось создать экземпляр инструмента " + secCode + "|" + classCode + "...");
            }
        }

        private void Robot_Click(object sender, RoutedEventArgs e)
        {
            GridBot grid = new GridBot(5.0m,3.0m);
            Log("Robot is started");
        }
    }
}

