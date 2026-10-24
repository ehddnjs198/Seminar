using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ChatProgram
{
    /// <summary>
    /// 채팅 화면 이벤트 처리
    /// - chatSocket을 부모 타입(SocketBase)으로 선언 → TCP/UDP 구분 없이 같은 코드로 송수신 (다형성)
    /// </summary>
    public partial class MainWindow : Window
    {
        private SocketBase chatSocket;   // 현재 사용 중인 소켓 (TcpSocket 또는 UdpSocket)

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>[연결] 버튼 : 선택한 방식으로 소켓 생성 후 Open</summary>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(localPortTextBox.Text, out int localPort) ||
                !int.TryParse(remotePortTextBox.Text, out int remotePort))
            {
                MessageBox.Show("포트는 숫자로 입력하세요.", "입력 오류");
                return;
            }
            string remoteIp = remoteIpTextBox.Text.Trim();

            chatSocket = CreateSocket(protocolComboBox.SelectedIndex);
            chatSocket.MessageReceived += OnMessageReceived;
            chatSocket.StatusChanged += OnStatusChanged;

            try
            {
                chatSocket.Open(remoteIp, remotePort, localPort);
                SetConnectedUi(true);
            }
            catch (Exception ex)   // 포트 사용 중, 서버 미실행, 잘못된 IP 등
            {
                AddChatLine($"[오류] {ex.Message}");
                chatSocket.Close();
                chatSocket = null;
            }
        }

        /// <summary>ComboBox 선택값에 따라 알맞은 자식 클래스 생성 (반환 타입은 부모)</summary>
        private SocketBase CreateSocket(int protocolIndex)
        {
            switch (protocolIndex)
            {
                case 0: return new TcpSocket(true);    // TCP 서버
                case 1: return new TcpSocket(false);   // TCP 클라이언트
                default: return new UdpSocket();       // UDP
            }
        }

        /// <summary>[해제] 버튼</summary>
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            CloseSocket();
            AddChatLine("[시스템] 연결 해제");
        }

        /// <summary>[전송] 버튼</summary>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        /// <summary>입력창에서 Enter 키로 전송</summary>
        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        /// <summary>메시지 송신 + 내 화면에 표시</summary>
        private void SendMessage()
        {
            string message = messageTextBox.Text;
            if (message.Length == 0) return;

            if (chatSocket == null || !chatSocket.IsReady)
            {
                AddChatLine("[시스템] 아직 상대와 연결되지 않았습니다.");
                return;
            }

            try
            {
                chatSocket.Send(message);
                AddChatLine($"[나] {message}");
                messageTextBox.Clear();
            }
            catch (Exception ex)
            {
                AddChatLine($"[오류] 전송 실패 : {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지 수신 이벤트 (수신 스레드에서 호출됨)
        /// - WPF 컨트롤은 만든 스레드(UI 스레드)에서만 접근 가능 → Dispatcher로 UI 스레드에 작업 전달
        /// </summary>
        private void OnMessageReceived(string message)
        {
            Dispatcher.BeginInvoke(new Action(() => AddChatLine($"[상대] {message}")));
        }

        /// <summary>상태 변경 이벤트 (수신 스레드에서 호출될 수 있음)</summary>
        private void OnStatusChanged(string status)
        {
            Dispatcher.BeginInvoke(new Action(() => AddChatLine($"[시스템] {status}")));
        }

        /// <summary>대화 목록에 한 줄 추가 후 맨 아래로 스크롤</summary>
        private void AddChatLine(string text)
        {
            chatListBox.Items.Add($"{DateTime.Now:HH:mm:ss}  {text}");
            chatListBox.ScrollIntoView(chatListBox.Items[chatListBox.Items.Count - 1]);
        }

        /// <summary>연결 상태에 따라 버튼/설정 입력 활성화 전환</summary>
        private void SetConnectedUi(bool isConnected)
        {
            connectButton.IsEnabled = !isConnected;
            disconnectButton.IsEnabled = isConnected;
            protocolComboBox.IsEnabled = !isConnected;
        }

        /// <summary>소켓 정리 (이벤트 구독 해제 → Close)</summary>
        private void CloseSocket()
        {
            if (chatSocket == null) return;
            chatSocket.MessageReceived -= OnMessageReceived;
            chatSocket.StatusChanged -= OnStatusChanged;
            chatSocket.Close();
            chatSocket = null;
            SetConnectedUi(false);
        }

        /// <summary>창 닫을 때 소켓 정리</summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseSocket();
        }
    }
}
