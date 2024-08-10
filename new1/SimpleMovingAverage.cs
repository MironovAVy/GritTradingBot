using System;
using System.Collections.Generic;
using QuikSharp;
using QuikSharp.DataStructures;

namespace new1
{
    public class SimpleMovingAverage
    {
        private readonly int period;
        private readonly Queue<decimal> values;
        private decimal sum;
        private Quik _quik;
        private string secCode = "SBER";
        private string classCode = "";
        private Tool tool;
        private List<double> SMAValues;
        private double currentSMA;

        public SimpleMovingAverage(int period)
        {
            // Инициализация Quik
            _quik = new Quik();

            classCode = _quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,QJSIM", secCode).Result;
            tool = new Tool(_quik, secCode, classCode);

            _quik.OrderBook.Subscribe(tool.ClassCode, tool.SecurityCode).Wait();

            _quik.Candles.Subscribe(classCode, secCode, CandleInterval.M1);

            // Подписка на событие NewCandle
            _quik.Candles.NewCandle += Candles_NewCandle;

            if (period <= 0)
            {
                throw new ArgumentException("Period must be greater than zero.", nameof(period));
            }

            this.period = period;
            this.values = new Queue<decimal>();
            this.sum = 0;
            this.SMAValues = new List<double>();

            var barList = _quik.Candles.GetLastCandles(classCode, secCode, CandleInterval.M1, period).Result;
            Console.WriteLine("SMA is started");

            // Инициализация значений SMA на основе предыдущих данных
            foreach (var bar in barList)
            {
                AddPrice(bar.Close);
            }
        }

        private void Candles_NewCandle(Candle candle)
        {
            // Добавляем новую цену и пересчитываем SMA
            AddPrice(candle.Close);
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

            // Вычисляем текущее значение SMA
            currentSMA = (double)(sum / values.Count);
            SMAValues.Add(currentSMA);
        }

        public double GetCurrentSMA()
        {
            if (values.Count == 0)
            {
                throw new InvalidOperationException("Not enough data to calculate SMA.");
            }

            return currentSMA;
        }

        public int Count => values.Count;

        // Возвращаем true, если достаточно элементов для расчета SMA
        public bool IsReady => values.Count == period;
    }
}
