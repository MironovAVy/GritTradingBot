using QuikSharp;
using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace new1
{
    public class GridBot
    {
        private decimal gridSpacing; // Расстояние между уровнями сетки
        private decimal gridSize; // Количество уровней сетки
        private decimal basePrice; // Базовая цена, от которой строится сетка
        private List<decimal> buyOrders;
        private List<decimal> sellOrders;
        private List<decimal> Candels;

        private Quik _quik;
        string secCode = "SBER";
        string classCode = "";
        private Tool tool;
        private bool isSubscribedToolOrderBook = false;
        OrderBook toolOrderBook;

        public GridBot( decimal gridSpacing, decimal gridSize)
        {
            _quik = new Quik();
            this.gridSpacing = gridSpacing;
            this.gridSize = gridSize;
            buyOrders = new List<decimal>();
            sellOrders = new List<decimal>();
            classCode = _quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,QJSIM", secCode).Result;

            tool = new Tool(_quik, secCode, classCode);

            _quik.Candles.Subscribe(classCode, secCode, CandleInterval.M1);

            var barList =       _quik.Candles.GetLastCandles(classCode, secCode, CandleInterval.M1, 10).Result;

            // Выводим данные для проверки.
            //foreach (var bar in barList)
            //{
            //    Console.WriteLine($" Open: {bar.Open}, High: {bar.High}, Low: {bar.Low}, Close: {bar.Close}, Volume: {bar.Volume}");
            //}


            var SMA = new SimpleMovingAverage(barList.Count);
            Console.WriteLine($"{SMA.GetCurrentSMA()}");
            

        }
    }
}

    