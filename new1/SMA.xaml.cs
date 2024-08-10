using QuikSharp.DataStructures;
using QuikSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace new1
{
    public partial class SMA : Window
    {
        private int period;
        private Queue<decimal> values;
        private decimal sum;
        private Quik _quik;
        private string secCode = "SBER";
        private string classCode = "";
        private Tool tool;
        private List<double> SMA1Values; // Список для первой SMA
        private double currentSMA1;
        private double currentSMA2; // Для второй SMA

        public SMA()
        {
            InitializeComponent();
            LogTextBox.Text = "";
            this.period = 10;
            this.
            this.values = new Queue<decimal>();
            this.sum = 0;
            this.SMA1Values = new List<double>();

            _quik = new Quik();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Log("Начинаем расчет SMA");

            classCode = _quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,QJSIM", secCode).Result;
            tool = new Tool(_quik, secCode, classCode);

            _quik.OrderBook.Subscribe(tool.ClassCode, tool.SecurityCode).Wait();

            _quik.Candles.Subscribe(classCode, secCode, CandleInterval.M1);
            _quik.Candles.NewCandle += Candles_NewCandle;

            var barList = _quik.Candles.GetLastCandles(classCode, secCode, CandleInterval.M1, period).Result;

            if (period <= 0)
            {
                throw new ArgumentException("Period must be greater than zero.", nameof(period));
            }

            Log("SMA is started");
        }

        private void Candles_NewCandle(Candle candle)
        {
            // Добавляем новую цену и пересчитываем SMA
            AddPrice(candle.Close);

            // Вычисляем значение второй SMA со сдвигом на 3
            CalculateSMA2();
            Log($"Закрытие свечи - {candle.Close.ToString()}"); ;

            // Логируем новое значение SMA1 и SMA2
            Log($"Новое значение SMA1: {currentSMA1}, SMA2 (со сдвигом на 3): {currentSMA2}");
        }

        private void AddPrice(decimal price)
        {
            // Добавляем новую цену в очередь
            values.Enqueue(price);
            sum += price;

            // Если количество элементов превышает период, удаляем старый элемент
            if (values.Count > period)
            {
                sum -= values.Dequeue();
            }

            // Вычисляем текущее значение SMA1
            currentSMA1 = (double)(sum / values.Count);
            SMA1Values.Add(currentSMA1);
        }

        private void CalculateSMA2()
        {
            // Проверяем, достаточно ли значений для смещения на 3
            if (SMA1Values.Count >= 3)
            {
                // Устанавливаем значение SMA2 как SMA1 со сдвигом на 3
                currentSMA2 = SMA1Values[SMA1Values.Count - 3];
            }
            else
            {
                currentSMA2 = double.NaN; // Если недостаточно данных, возвращаем NaN
            }
        }

        private double GetCurrentSMA1()
        {
            if (values.Count == 0)
            {
                throw new InvalidOperationException("Not enough data to calculate SMA.");
            }

            return currentSMA1;
        }

        public int Count => values.Count;

        public bool IsReady => values.Count == period;
    }
}
