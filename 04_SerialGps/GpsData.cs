namespace SerialGps
{
    /// <summary>
    /// 파싱된 GPS 정보를 보관하는 데이터 클래스 (NMEA 문장이 들어올 때마다 값이 갱신됨)
    /// </summary>
    public class GpsData
    {
        public string UtcTime { get; set; } = "-";   // 수신 시각 (UTC, 한국시간 = UTC + 9)
        public double Latitude { get; set; }         // 위도 (십진수 도, 북위 +, 남위 -)
        public double Longitude { get; set; }        // 경도 (십진수 도, 동경 +, 서경 -)
        public double Altitude { get; set; }         // 해발 고도 (m)
        public int SatelliteCount { get; set; }      // 사용 중인 위성 수
        public int FixQuality { get; set; }          // 측위 품질 (0 = 측위 안됨, 1 = GPS, 2 = DGPS ...)
        public double SpeedKmh { get; set; }         // 속도 (km/h)

        /// <summary>위치가 유효한지 (FixQuality가 0보다 크면 측위 성공)</summary>
        public bool HasFix => FixQuality > 0;
    }
}
