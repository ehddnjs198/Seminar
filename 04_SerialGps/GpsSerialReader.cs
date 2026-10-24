using System;
using System.IO.Ports;
using System.Text;

namespace SerialGps
{
    /// <summary>
    /// 시리얼 포트로 GPS 데이터를 받아 "한 줄(NMEA 문장 1개)" 단위로 넘겨주는 클래스
    /// - GPS는 1초마다 여러 NMEA 문장을 텍스트로 보냄 (각 문장은 \r\n으로 끝남)
    /// - 시리얼은 바이트가 끊겨서 들어오므로 버퍼에 모았다가 '\n' 기준으로 잘라냄
    /// </summary>
    public class GpsSerialReader
    {
        private SerialPort serialPort;                 // .NET 시리얼 포트 객체
        private string receiveBuffer = string.Empty;   // 아직 '\n'이 오지 않은 미완성 문자열 보관

        public event Action<string> LineReceived;      // 완성된 NMEA 문장 1개를 UI로 전달

        public bool IsOpen => serialPort != null && serialPort.IsOpen;

        /// <summary>
        /// 포트 열기 (통신 설정 : 8N1 = 데이터 8bit, 패리티 없음, 정지 비트 1)
        /// </summary>
        public void Open(string portName, int baudRate)
        {
            serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serialPort.Encoding = Encoding.ASCII;   // NMEA는 ASCII 텍스트
            serialPort.DtrEnable = true;            // 일부 USB GPS는 DTR 신호가 있어야 데이터 송신 시작
            serialPort.DataReceived += SerialPort_DataReceived;
            serialPort.Open();
        }

        /// <summary>
        /// 데이터 수신 이벤트 (UI 스레드가 아닌 별도 스레드에서 호출됨)
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                receiveBuffer += serialPort.ReadExisting();   // 현재 도착한 데이터 전부 읽기
            }
            catch (Exception)
            {
                return;   // 포트를 닫는 도중에 이벤트가 들어온 경우
            }

            int newLineIndex;
            while ((newLineIndex = receiveBuffer.IndexOf('\n')) >= 0)
            {
                string line = receiveBuffer.Substring(0, newLineIndex).Trim();   // \r 제거
                receiveBuffer = receiveBuffer.Substring(newLineIndex + 1);
                if (line.Length > 0) LineReceived?.Invoke(line);
            }
        }

        /// <summary>포트 닫기 및 자원 해제</summary>
        public void Close()
        {
            if (serialPort == null) return;
            serialPort.DataReceived -= SerialPort_DataReceived;
            if (serialPort.IsOpen) serialPort.Close();
            serialPort.Dispose();
            serialPort = null;
            receiveBuffer = string.Empty;
        }
    }
}
