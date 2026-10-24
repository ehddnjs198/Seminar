using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatProgram
{
    /// <summary>
    /// UDP 소켓 (SocketBase 상속)
    /// - 비연결형 : 연결 과정 없이 상대 주소(IP:포트)로 바로 전송 (SendTo)
    /// - 신뢰성 없음 : 순서/도착 보장 X, 대신 빠르고 가벼움
    /// - 데이터그램 방식 : 보낸 단위 그대로 1건씩 도착 → 별도 구분자 불필요
    /// </summary>
    public class UdpSocket : SocketBase
    {
        private IPEndPoint remoteEndPoint;   // 메시지를 보낼 상대방 주소 (IP + 포트)

        /// <summary>내 포트에 Bind(수신용) + 상대 주소 저장(송신용)</summary>
        public override void Open(string remoteIp, int remotePort, int localPort)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);

            isRunning = true;
            OnStatusChanged($"UDP 시작 (내 포트 {localPort} → 상대 {remoteIp}:{remotePort})");
            StartThread(ReceiveLoop);
        }

        /// <summary>상대 주소로 데이터그램 1개 전송</summary>
        public override void Send(string message)
        {
            byte[] sendData = Encoding.UTF8.GetBytes(message);
            socket.SendTo(sendData, remoteEndPoint);
        }

        /// <summary>수신 반복문 : ReceiveFrom 1회 = 메시지 1건</summary>
        protected override void ReceiveLoop()
        {
            byte[] receiveBuffer = new byte[bufferSize];
            EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);   // 보낸 쪽 주소가 여기에 채워짐

            while (isRunning)
            {
                try
                {
                    int receivedLength = socket.ReceiveFrom(receiveBuffer, ref senderEndPoint);
                    OnMessageReceived(Encoding.UTF8.GetString(receiveBuffer, 0, receivedLength));
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // 상대 프로그램이 아직 안 켜져 있으면 Windows가 ICMP 'Port Unreachable'을 이 예외로 알려줌
                    // UDP는 연결 개념이 없으므로 무시하고 계속 수신
                }
                catch (SocketException) { break; }        // Close() 호출 시
                catch (ObjectDisposedException) { break; }
            }
        }
    }
}
