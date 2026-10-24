using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;

namespace SerialGps
{
    /// <summary>
    /// GPS 수신 화면
    /// [데이터 흐름] 시리얼 수신(GpsSerialReader) → 문장 파싱(NmeaParser) → 좌표 변환(CoordinateConverter) → 전시
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int maxRawLineCount = 200;   // 원본 로그 최대 줄 수 (메모리 무한 증가 방지)

        private readonly GpsSerialReader gpsReader = new GpsSerialReader();   // 시리얼 수신 담당
        private readonly GpsData gpsData = new GpsData();                     // 최신 GPS 정보 보관

        public MainWindow()
        {
            InitializeComponent();

            // 자주 쓰는 통신 속도 목록 (USB GPS 기본값은 대부분 4800 또는 9600)
            baudRateComboBox.ItemsSource = new[] { 4800, 9600, 19200, 38400, 57600, 115200 };
            baudRateComboBox.SelectedItem = 9600;

            gpsReader.LineReceived += OnLineReceived;
            LoadPortNames();
        }

        /// <summary>PC에 연결된 COM 포트 목록 불러오기</summary>
        private void LoadPortNames()
        {
            string[] portNames = SerialPort.GetPortNames();
            Array.Sort(portNames);
            portComboBox.ItemsSource = portNames;
            if (portNames.Length > 0) portComboBox.SelectedIndex = 0;
        }

        /// <summary>[새로고침] 버튼 : GPS를 나중에 꽂은 경우 포트 목록 갱신</summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPortNames();
        }

        /// <summary>[연결] 버튼</summary>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (portComboBox.SelectedItem == null)
            {
                MessageBox.Show("COM 포트를 선택하세요.", "알림");
                return;
            }

            string portName = (string)portComboBox.SelectedItem;
            int baudRate = (int)baudRateComboBox.SelectedItem;

            try
            {
                gpsReader.Open(portName, baudRate);
                statusTextBlock.Text = $"{portName} 연결됨";
                SetConnectedUi(true);
            }
            catch (Exception ex)   // 다른 프로그램이 포트 사용 중, 장치 분리 등
            {
                MessageBox.Show(ex.Message, "연결 실패");
            }
        }

        /// <summary>[해제] 버튼</summary>
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            gpsReader.Close();
            statusTextBlock.Text = "연결 안 됨";
            SetConnectedUi(false);
        }

        /// <summary>
        /// NMEA 문장 1개 수신 (시리얼 스레드에서 호출됨)
        /// - UI 컨트롤은 UI 스레드에서만 접근 가능 → Dispatcher.BeginInvoke로 넘겨서 처리
        /// </summary>
        private void OnLineReceived(string line)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddRawLine(line);
                if (NmeaParser.Parse(line, gpsData)) UpdateDisplay();
            }));
        }

        /// <summary>원본 로그 추가 (오래된 줄은 삭제) 후 맨 아래로 스크롤</summary>
        private void AddRawLine(string line)
        {
            rawDataListBox.Items.Add(line);
            if (rawDataListBox.Items.Count > maxRawLineCount) rawDataListBox.Items.RemoveAt(0);
            rawDataListBox.ScrollIntoView(line);
        }

        /// <summary>gpsData 값을 화면 텍스트박스에 실시간 반영</summary>
        private void UpdateDisplay()
        {
            utcTimeTextBox.Text = gpsData.UtcTime;
            string fixText = gpsData.HasFix ? $"Fix({gpsData.FixQuality})" : "No Fix";
            fixInfoTextBox.Text = $"{fixText} / {gpsData.SatelliteCount}개 / {gpsData.SpeedKmh:F1} km/h";

            if (!gpsData.HasFix) return;   // 측위 전에는 좌표 갱신하지 않음

            // WGS84 : 십진수 도 (소수점 6자리 ≒ 약 0.1m 정밀도)
            latitudeDecimalTextBox.Text = $"{gpsData.Latitude:F6}°";
            longitudeDecimalTextBox.Text = $"{gpsData.Longitude:F6}°";

            // WGS84 : 도분초
            latitudeDmsTextBox.Text = CoordinateConverter.ToDms(gpsData.Latitude, true);
            longitudeDmsTextBox.Text = CoordinateConverter.ToDms(gpsData.Longitude, false);
            altitudeTextBox.Text = $"{gpsData.Altitude:F1} m";

            // UTM
            UtmCoordinate utmCoordinate = CoordinateConverter.ToUtm(gpsData.Latitude, gpsData.Longitude);
            utmZoneTextBox.Text = $"{utmCoordinate.ZoneNumber}{utmCoordinate.ZoneLetter}";
            eastingTextBox.Text = $"{utmCoordinate.Easting:N2} m E";
            northingTextBox.Text = $"{utmCoordinate.Northing:N2} m N";
        }

        /// <summary>연결 상태에 따라 버튼 활성화 전환</summary>
        private void SetConnectedUi(bool isConnected)
        {
            connectButton.IsEnabled = !isConnected;
            disconnectButton.IsEnabled = isConnected;
            portComboBox.IsEnabled = !isConnected;
            baudRateComboBox.IsEnabled = !isConnected;
        }

        /// <summary>창 닫을 때 포트 정리 (포트를 열어둔 채 종료하면 다음 실행 시 접근 거부될 수 있음)</summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            gpsReader.Close();
        }
    }
}
