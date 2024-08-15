using QuikSharp.DataStructures;
using QuikSharp;
using System;
using System.Collections.Generic;
using System.Windows;
using QuikSharp.DataStructures.Transaction;
using System.Threading.Tasks;

namespace new1
{
    public partial class SMA : Window
    {
        private readonly int _period;
        private readonly Queue<decimal> _values;
        private decimal _sum;
        private readonly Quik _quik;
        private readonly string _secCode = "SBER";
        private string _classCode;
        private Tool _tool;
        private readonly List<double> _sma1Values;
        private double _currentSMA1;
        private double _currentSMA2;
        private double _previousSMA1;
        private double _previousSMA2;
        private readonly OrderManager _orderManager;

        public SMA()
        {
            InitializeComponent();
            LogTextBox.Text = "";
            _period = 10;
            _values = new Queue<decimal>();
            _sum = 0;
            _sma1Values = new List<double>();

            _quik = new Quik();
            _orderManager = new OrderManager(_quik, _secCode, _classCode, 0.5m);
        }

        public void Log(string message)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText(message + Environment.NewLine);
                    LogTextBox.ScrollToLine(LogTextBox.LineCount - 1);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log Error: {ex.Message}");
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Log("Начинаем расчет SMA и отслеживание пересечений");

            try
            {
                _classCode = await _quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,QJSIM", _secCode);
                _tool = new Tool(_quik, _secCode, _classCode);

                await _quik.OrderBook.Subscribe(_tool.ClassCode, _tool.SecurityCode);
                await _quik.Candles.Subscribe(_classCode, _secCode, CandleInterval.M1);
                _quik.Candles.NewCandle += Candles_NewCandle;
                _quik.Events.OnOrder += Events_OnOrder;

                var barList = await _quik.Candles.GetLastCandles(_classCode, _secCode, CandleInterval.M1, _period);
            }
            catch (Exception ex)
            {
                Log($"Ошибка при инициализации: {ex.Message}");
            }

            Log("SMA calculation started");
        }

        private async void Candles_NewCandle(Candle candle)
        {
            AddPrice(candle.Close);
            CalculateSMA2();

            if (CheckForCross())
            {
                Log($"Произошло пересечение - активируем сетку ордеров c началом построения {candle.Close}");
                await _orderManager.PlaceGridOrders(candle.Close);
            }

            Log($"Цена закрытия: {candle.Close}");
            Log($"Новое значение SMA1: {_currentSMA1}, SMA2 (со сдвигом на 3): {_currentSMA2}");
        }

        private void AddPrice(decimal price)
        {
            _previousSMA1 = _currentSMA1;

            _values.Enqueue(price);
            _sum += price;

            if (_values.Count > _period)
            {
                _sum -= _values.Dequeue();
            }

            _currentSMA1 = (double)(_sum / _values.Count);
            _sma1Values.Add(_currentSMA1);
        }

        private void CalculateSMA2()
        {
            _previousSMA2 = _currentSMA2;

            if (_sma1Values != null && _sma1Values.Count >= 3)
            {
                // Используем обычный индекс для получения третьего элемента с конца списка
                _currentSMA2 = _sma1Values[_sma1Values.Count - 3];
            }
            else
            {
                _currentSMA2 = double.NaN; // Если недостаточно данных, возвращаем NaN
            }
        }
        private bool CheckForCross()
        {
            return (_previousSMA1 < _previousSMA2 && _currentSMA1 > _currentSMA2) ||
                   (_previousSMA1 > _previousSMA2 && _currentSMA1 < _currentSMA2);
        }

        private void Events_OnOrder(Order order)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _orderManager.HandleOrderExecution(order);
                    Log($"Исполнен ордер: {order.Operation} по цене {order.Price}. Выставлен лимитный ордер на закрытие позиции.");
                }
                catch (Exception ex)
                {
                    Log($"Ошибка при обработке ордера: {ex.Message}");
                }
            });
        }
    }

    public class OrderManager
    {
        private readonly Quik _quik;
        private readonly string _secCode;
        private readonly string _classCode;
        private readonly decimal _gridStep;
        private Tool _tool;


        public OrderManager(Quik quik, string secCode, string classCode, decimal gridStep)
        {
            _quik = quik;
            _secCode = secCode;
            _classCode = classCode;
            _gridStep = gridStep;
        }

        public async Task PlaceGridOrders(decimal currentPrice)
        {
            for (int i = 1; i <= 3; i++)
            {
                decimal buyPrice = currentPrice - _gridStep * i;
                await PlaceOrder(Operation.Buy, buyPrice);

                decimal sellPrice = currentPrice + _gridStep * i;
                await PlaceOrder(Operation.Sell, sellPrice);
            }
        }

        private async Task PlaceOrder(Operation operation, decimal price)
        {
            try
            {
                var order = new Order
                {
                    ClassCode = _classCode,
                    SecCode = _secCode,
                    Price = price,
                    Quantity = 1,
                    Operation = operation,
                    Account = _tool.AccountID,
                };

                await _quik.Orders.CreateOrder(order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выставлении ордера: {ex.Message}");
            }
        }

        public async Task HandleOrderExecution(Order order)
        {
            decimal closePrice = order.Operation == Operation.Buy
                ? order.Price + _gridStep
                : order.Price - _gridStep;

            await PlaceOrder(order.Operation == Operation.Buy ? Operation.Sell : Operation.Buy, closePrice);
        }
    }
}
