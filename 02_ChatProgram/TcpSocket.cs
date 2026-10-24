using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatProgram
{
    /// <summary>
    /// TCP 소켓 (SocketBase 상속)
    /// - 연결형 : 서버(Listen/Accept) ↔ 클라이언트(Connect) 연결 후 통신
    /// - 신뢰성 보장 : 순서 보장, 손실 시 재전송
    /// - 스트림 방식 : 메시지 경계가 없음 → '\n'을 구분자로 붙여서 메시지를 나눔
    /// </summary>
    public class TcpSocket : SocketBase
    {
        private readonly bool isServer;   // true : 서버(접속 대기), false : 클라이언트(접속 요청)
        private Socket listenSocket;      // 서버 전용 : 클라이언트 접속을 기다리는 대기 소켓

        public TcpSocket(bool isServer)
        {
            this.isServer = isServer;
        }

        /// <summary>서버 : Bind → Listen → Accept / 클라이언트 : Connect</summary>
        public override void Open(string remoteIp, int remotePort, int localPort)
        {
            isRunning = true;

            if (isServer)
            {
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(new IPEndPoint(IPAddress.Any, localPort));  // 내 PC의 모든 IP, 지정 포트
                listenSocket.Listen(1);                                       // 대기 큐 크기 1 (1:1 채팅)
                OnStatusChanged($"TCP 서버 대기 중... (포트 {localPort})");
                StartThread(AcceptAndReceive);   // Accept도 블로킹 → 별도 스레드
            }
            else
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(IPAddress.Parse(remoteIp), remotePort);        // 3-way handshake 수행
                OnStatusChanged($"서버 접속 성공 ({remoteIp}:{remotePort})");
                StartThread(ReceiveLoop);
            }
        }

        /// <summary>서버 전용 : 클라이언트 접속을 기다린 뒤 수신 시작</summary>
        private void AcceptAndReceive()
        {
            try
            {
                socket = listenSocket.Accept();   // 접속이 오면 통신 전용 소켓이 새로 생성됨
                OnStatusChanged($"클라이언트 접속 : {socket.RemoteEndPoint}");
                ReceiveLoop();
            }
            catch (SocketException) { }          // 대기 중 Close() 호출 시 발생 → 정상 종료
            catch (ObjectDisposedException) { }
        }

        /// <summary>문자열 → UTF-8 바이트 변환 후 송신 (끝에 '\n' 구분자 추가)</summary>
        public override void Send(string message)
        {
            byte[] sendData = Encoding.UTF8.GetBytes(message + "\n");
            socket.Send(sendData);
        }

        /// <summary>
        /// 수신 반복문
        /// - TCP는 "안녕\n반가워\n"이 한 번에 오거나, 반으로 잘려서 올 수 있음
        /// - 받은 데이터를 pendingText에 쌓아두고 '\n'이 나올 때마다 한 메시지로 잘라 전달
        /// </summary>
        protected override void ReceiveLoop()
        {
            byte[] receiveBuffer = new byte[bufferSize];
            Decoder utf8Decoder = Encoding.UTF8.GetDecoder();   // 한글(3바이트)이 잘려 와도 이어서 해석
            string pendingText = string.Empty;

            try
            {
                while (isRunning)
                {
                    int receivedLength = socket.Receive(receiveBuffer);   // 데이터가 올 때까지 대기
                    if (receivedLength == 0)                              // 0바이트 = 상대가 연결 종료
                    {
                        OnStatusChanged("상대방이 연결을 종료했습니다.");
                        break;
                    }

                    char[] receivedChars = new char[utf8Decoder.GetCharCount(receiveBuffer, 0, receivedLength)];
                    utf8Decoder.GetChars(receiveBuffer, 0, receivedLength, receivedChars, 0);
                    pendingText += new string(receivedChars);

                    int newLineIndex;
                    while ((newLineIndex = pendingText.IndexOf('\n')) >= 0)
                    {
                        OnMessageReceived(pendingText.Substring(0, newLineIndex));
                        pendingText = pendingText.Substring(newLineIndex + 1);
                    }
                }
            }
            catch (SocketException) { if (isRunning) OnStatusChanged("연결이 끊어졌습니다."); }
            catch (ObjectDisposedException) { }   // Close()로 소켓을 닫은 경우
        }

        /// <summary>통신 소켓 + 서버 대기 소켓까지 모두 종료</summary>
        public override void Close()
        {
            base.Close();
            listenSocket?.Close();
            listenSocket = null;
        }
    }
}
