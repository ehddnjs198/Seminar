using System;
using System.Net.Sockets;
using System.Threading;

namespace ChatProgram
{
    /// <summary>
    /// 소켓 공통 부모 클래스 (추상 클래스)
    /// - TCP / UDP 모두 필요한 공통 기능 : 소켓 보관, 수신 스레드, 이벤트 알림, 종료
    /// - 프로토콜마다 다른 기능 : Open / Send / ReceiveLoop → abstract로 선언하여 자식이 구현
    /// - UI는 SocketBase 타입 하나로 TCP/UDP를 동일하게 다룬다 (다형성)
    /// </summary>
    public abstract class SocketBase
    {
        protected const int bufferSize = 1024;   // 한 번에 받을 최대 바이트 수

        protected Socket socket;                 // 실제 데이터를 주고받는 소켓
        protected volatile bool isRunning;       // 수신 스레드 동작 여부 (volatile : 여러 스레드에서 최신 값 보장)

        public event Action<string> MessageReceived;   // 메시지 수신 시 UI에 알림
        public event Action<string> StatusChanged;     // 접속/종료 등 상태 변화 시 UI에 알림

        /// <summary>송신 가능 상태인지 (TCP 서버는 클라이언트가 접속해야 true)</summary>
        public bool IsReady => socket != null && isRunning;

        /// <summary>소켓 생성 및 연결/바인딩 (프로토콜별 구현)</summary>
        public abstract void Open(string remoteIp, int remotePort, int localPort);

        /// <summary>메시지 송신 (프로토콜별 구현)</summary>
        public abstract void Send(string message);

        /// <summary>메시지 수신 반복문 (별도 스레드에서 실행)</summary>
        protected abstract void ReceiveLoop();

        /// <summary>
        /// 백그라운드 스레드 시작
        /// - Receive/Accept는 데이터가 올 때까지 멈추는 블로킹 함수 → UI 스레드에서 호출하면 화면이 멈춤
        /// - IsBackground = true : 창을 닫으면 스레드도 같이 종료
        /// </summary>
        protected void StartThread(ThreadStart work)
        {
            Thread workThread = new Thread(work) { IsBackground = true };
            workThread.Start();
        }

        // 자식 클래스에서 이벤트를 발생시키기 위한 함수 (이벤트는 선언한 클래스에서만 Invoke 가능)
        protected void OnMessageReceived(string message) => MessageReceived?.Invoke(message);
        protected void OnStatusChanged(string status) => StatusChanged?.Invoke(status);

        /// <summary>소켓 종료 (virtual : 자식이 추가 정리 작업을 덧붙일 수 있음)</summary>
        public virtual void Close()
        {
            isRunning = false;
            socket?.Close();   // 블로킹 중인 Receive가 예외를 내며 빠져나옴 → 수신 스레드 종료
            socket = null;
        }
    }
}
