using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Microsoft.Win32;
using ArduinoDataLogger.Services;

namespace ArduinoDataLogger
{
    public partial class MainWindow : Window
    {
        private readonly MeasurementBuffer _measurements = new(MaxHistoryPoints);

        private SerialPort? _serialPort;
        private bool _isReading;
        private DispatcherTimer _chartTimer;
        private DateTime _startTime;
        private const int MaxHistoryPoints = 500;

        public SeriesCollection Series { get; set; }
        public Func<double, string> TimeFormatter { get; set; }
        public Func<double, string> ValueFormatter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Данные с Arduino",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = null,
                    LineSmoothness = 0
                }
            };

            TimeFormatter = value => TimeSpan.FromSeconds(value).ToString(@"mm\:ss");
            ValueFormatter = value => value.ToString("F2");

            DataContext = this;

            _chartTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _chartTimer.Tick += UpdateChart;

            LoadAvailablePorts();
        }

        private void LoadAvailablePorts()
        {
            try
            {
                cmbPorts.Items.Clear();
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    cmbPorts.Items.Add(port);
                }
                if (cmbPorts.Items.Count > 0)
                {
                    cmbPorts.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при загрузке портов: {ex.Message}");
            }
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPorts.SelectedItem == null)
            {
                MessageBox.Show("Выберите COM-порт!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Закрываем предыдущее подключение
            if (_serialPort != null)
            {
                _isReading = false;
                if (_serialPort.IsOpen)
                    _serialPort.Close();
                _serialPort.Dispose();
            }

            string portName = cmbPorts.SelectedItem.ToString()!;
            int baudRate = int.Parse(((ComboBoxItem)cmbBaudRate.SelectedItem!).Content.ToString()!);

            try
            {
                // Очищаем данные
                _measurements.Clear();

                Dispatcher.Invoke(() =>
                {
                    Series[0].Values.Clear();
                });

                _serialPort = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    NewLine = "\n"
                };

                _serialPort.Open();
                _isReading = true;
                _startTime = DateTime.Now;
                new Thread(ReadDataFromArduino) { IsBackground = true }.Start();

                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = true;
                _chartTimer.Start();

                Log($"Подключено к {portName} ({baudRate} бод)");
            }
            catch (Exception ex)
            {
                Log($"Ошибка подключения: {ex.Message}");
            }
        }

        private void ReadDataFromArduino()
        {
            while (_isReading && _serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    string line = _serialPort.ReadLine().Trim();

                    if (MeasurementParser.TryParse(line, out double value))
                    {
                        var now = DateTime.Now;

                        _measurements.Add(new Measurement(now, value));

                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            Log($"{now:HH:mm:ss.fff} → {value:F2}");
                        }));
                    }
                }
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    Log($"Ошибка чтения: {ex.Message}");
                    Disconnect();
                }
            }
        }

        private void UpdateChart(object? sender, EventArgs e)
        {
            try
            {
                var measurements = _measurements.Snapshot();
                if (measurements.Count == 0)
                    return;

                Series[0].Values.Clear();
                foreach (var measurement in measurements)
                {
                    Series[0].Values.Add(new ObservablePoint
                    {
                        X = (measurement.Timestamp - _startTime).TotalSeconds,
                        Y = measurement.Value
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка обновления графика: {ex.Message}");
            }
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        private void Disconnect()
        {
            try
            {
                _isReading = false;
                _chartTimer.Stop();

                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();

                    _serialPort.Dispose();
                    _serialPort = null;
                }

                Dispatcher.Invoke(() =>
                {
                    btnConnect.IsEnabled = true;
                    btnDisconnect.IsEnabled = false;
                    Log("Отключено");
                });
            }
            catch (Exception ex)
            {
                Log($"Ошибка отключения: {ex.Message}");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filename = $"arduino_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                File.WriteAllLines(filename, MeasurementCsvExporter.CreateLines(_measurements.Snapshot()));
                Log($"Данные сохранены в {filename}");
            }
            catch (Exception ex)
            {
                Log($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = $"arduino_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllLines(saveFileDialog.FileName, MeasurementCsvExporter.CreateLines(_measurements.Snapshot()));
                    Log($"Данные сохранены в {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke((Action)(() => Log(message)));
                return;
            }

            tbLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
            tbLog.ScrollToEnd();
        }

        protected override void OnClosed(EventArgs e)
        {
            _isReading = false;
            _chartTimer.Stop();
            Disconnect();
            base.OnClosed(e);
        }
    }
}
