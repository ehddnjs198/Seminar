using System;
using System.Globalization;

namespace SerialGps
{
    /// <summary>
    /// NMEA-0183 문장 파서
    ///
    /// [NMEA 문장 형식]  $GPGGA,123519.00,3723.2475,N,12658.6344,E,1,08,0.9,45.0,M,...*47
    ///  - '$' 시작, ','로 필드 구분, '*' 뒤 2자리 = 체크섬(16진수)
    ///  - 앞 2글자 = Talker ID (GP = GPS, GN = 복합 GNSS, GL = GLONASS), 뒤 3글자 = 문장 종류
    ///  - GGA : 시간, 위도, 경도, 측위 품질, 위성 수, 고도
    ///  - RMC : 시간, 유효 여부(A/V), 위도, 경도, 속도(knot)
    /// </summary>
    public static class NmeaParser
    {
        private const double knotToKmh = 1.852;   // 1 knot = 1.852 km/h

        /// <summary>문장 1개를 해석해 gpsData를 갱신. 처리한 문장이면 true</summary>
        public static bool Parse(string sentence, GpsData gpsData)
        {
            if (!sentence.StartsWith("$") || !IsChecksumValid(sentence)) return false;

            int starIndex = sentence.IndexOf('*');
            string[] fields = sentence.Substring(1, starIndex - 1).Split(',');   // '$'와 '*' 사이만 분리
            if (fields[0].Length < 5) return false;

            string sentenceType = fields[0].Substring(2);   // "GPGGA" → "GGA" (Talker ID 무시)
            switch (sentenceType)
            {
                case "GGA": return ParseGga(fields, gpsData);
                case "RMC": return ParseRmc(fields, gpsData);
                default: return false;
            }
        }

        /// <summary>
        /// 체크섬 검사 : '$'와 '*' 사이 모든 문자를 XOR 한 값 == '*' 뒤 16진수 값
        /// - 전송 중 데이터가 깨졌는지 확인하는 용도
        /// </summary>
        private static bool IsChecksumValid(string sentence)
        {
            int starIndex = sentence.IndexOf('*');
            if (starIndex < 0 || starIndex + 3 > sentence.Length) return false;

            byte calculatedChecksum = 0;
            for (int i = 1; i < starIndex; i++)
            {
                calculatedChecksum ^= (byte)sentence[i];
            }

            string checksumText = sentence.Substring(starIndex + 1, 2);
            return byte.TryParse(checksumText, NumberStyles.HexNumber, null, out byte receivedChecksum)
                   && calculatedChecksum == receivedChecksum;
        }

        /// <summary>
        /// GGA : [1]시간 [2]위도 [3]N/S [4]경도 [5]E/W [6]측위품질 [7]위성수 [8]HDOP [9]고도
        /// </summary>
        private static bool ParseGga(string[] fields, GpsData gpsData)
        {
            if (fields.Length < 10) return false;

            gpsData.UtcTime = FormatTime(fields[1]);
            gpsData.FixQuality = ParseInt(fields[6]);
            gpsData.SatelliteCount = ParseInt(fields[7]);

            if (gpsData.HasFix)   // 측위가 안 된 상태에서는 위치 필드가 비어 있음
            {
                gpsData.Latitude = ToDecimalDegree(fields[2], fields[3]);
                gpsData.Longitude = ToDecimalDegree(fields[4], fields[5]);
                gpsData.Altitude = ParseDouble(fields[9]);
            }
            return true;
        }

        /// <summary>
        /// RMC : [1]시간 [2]상태(A=유효, V=무효) [3~6]위치 [7]속도(knot)
        /// </summary>
        private static bool ParseRmc(string[] fields, GpsData gpsData)
        {
            if (fields.Length < 8) return false;

            if (fields[2] == "A")
            {
                gpsData.SpeedKmh = ParseDouble(fields[7]) * knotToKmh;
            }
            return true;
        }

        /// <summary>
        /// NMEA 위경도(도분 형식) → 십진수 도
        /// - 위도 ddmm.mmmm, 경도 dddmm.mmmm   예) 3723.2475 = 37도 23.2475분
        /// - 십진수 도 = 도 + (분 / 60)        예) 37 + 23.2475 / 60 = 37.387458°
        /// - 남위(S), 서경(W)은 음수
        /// </summary>
        public static double ToDecimalDegree(string value, string hemisphere)
        {
            if (string.IsNullOrEmpty(value)) return 0;

            double rawValue = ParseDouble(value);
            int degree = (int)(rawValue / 100);        // 앞자리 = 도
            double minute = rawValue - degree * 100;   // 나머지 = 분
            double decimalDegree = degree + minute / 60.0;

            return (hemisphere == "S" || hemisphere == "W") ? -decimalDegree : decimalDegree;
        }

        /// <summary>"123519.00" → "12:35:19"</summary>
        private static string FormatTime(string value)
        {
            if (value.Length < 6) return "-";
            return $"{value.Substring(0, 2)}:{value.Substring(2, 2)}:{value.Substring(4, 2)}";
        }

        // InvariantCulture : PC 지역 설정과 상관없이 소수점을 '.'으로 해석
        private static double ParseDouble(string value)
        {
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result);
            return result;
        }

        private static int ParseInt(string value)
        {
            int.TryParse(value, out int result);
            return result;
        }
    }
}
