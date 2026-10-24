using System;

namespace SerialGps
{
    /// <summary>UTM 좌표 결과 (예: 52S 321,000 E / 4,160,000 N)</summary>
    public class UtmCoordinate
    {
        public int ZoneNumber { get; set; }   // 경도 6도 간격 구역 번호 (1~60, 한국은 51~52)
        public char ZoneLetter { get; set; }  // 위도 8도 간격 밴드 문자 (C~X, 한국은 S)
        public double Easting { get; set; }   // 동향 거리 (m) : 구역 중앙 경선 = 500,000m
        public double Northing { get; set; }  // 북향 거리 (m) : 적도 = 0m (남반구는 10,000,000m 더함)
    }

    /// <summary>
    /// 좌표 변환 클래스
    /// - WGS84 : GPS가 사용하는 전 세계 표준 타원체/좌표계 (위도·경도, 단위 = 도)
    /// - UTM   : 지구를 경도 6도씩 60개 구역으로 나누고 각 구역을 평면으로 펼친 좌표계 (단위 = m)
    /// </summary>
    public static class CoordinateConverter
    {
        // WGS84 타원체 상수
        private const double semiMajorAxis = 6378137.0;           // 장반경 a (적도 반지름, m)
        private const double flattening = 1 / 298.257223563;      // 편평률 f
        private const double scaleFactor = 0.9996;                // UTM 축척 계수 k0 (중앙 경선)
        private const double falseEasting = 500000.0;             // 동향 가산값 (음수 방지)
        private const double falseNorthingSouth = 10000000.0;     // 남반구 북향 가산값
        private const string zoneLetters = "CDEFGHJKLMNPQRSTUVWX"; // 위도 밴드 문자 (I, O 제외)

        /// <summary>
        /// 십진수 도 → 도분초(DMS) 문자열   예) 37.387458 → 37° 23' 14.85" N
        /// </summary>
        public static string ToDms(double decimalDegree, bool isLatitude)
        {
            string hemisphere = isLatitude ? (decimalDegree >= 0 ? "N" : "S")
                                           : (decimalDegree >= 0 ? "E" : "W");
            double absDegree = Math.Abs(decimalDegree);
            int degree = (int)absDegree;                  // 정수 부분 = 도
            double totalMinute = (absDegree - degree) * 60;
            int minute = (int)totalMinute;                // 소수 부분 × 60 = 분
            double second = (totalMinute - minute) * 60;  // 남은 소수 × 60 = 초

            return $"{degree}° {minute}' {second:F2}\" {hemisphere}";
        }

        /// <summary>
        /// WGS84 위경도 → UTM 좌표 (횡메르카토르 투영 공식, USGS Snyder 방식)
        /// </summary>
        public static UtmCoordinate ToUtm(double latitude, double longitude)
        {
            // 1) 구역 번호 : 경도 -180°부터 6도 간격 / 중앙 경선 = 구역의 가운데 경도
            int zoneNumber = (int)Math.Floor((longitude + 180) / 6) + 1;
            double centralMeridian = (zoneNumber - 1) * 6 - 180 + 3;

            // 2) 타원체 계수 : e² = 제1 이심률 제곱, e'² = 제2 이심률 제곱
            double eccSquared = flattening * (2 - flattening);
            double eccPrimeSquared = eccSquared / (1 - eccSquared);

            double latRad = latitude * Math.PI / 180;   // 삼각함수 계산을 위해 라디안 변환
            double lonDiffRad = (longitude - centralMeridian) * Math.PI / 180;

            // 3) 투영 보조값
            double sinLat = Math.Sin(latRad), cosLat = Math.Cos(latRad), tanLat = Math.Tan(latRad);
            double radiusN = semiMajorAxis / Math.Sqrt(1 - eccSquared * sinLat * sinLat); // 묘유선 곡률 반경
            double tanSquared = tanLat * tanLat;                     // 공식의 T
            double cosTerm = eccPrimeSquared * cosLat * cosLat;      // 공식의 C
            double lonTerm = cosLat * lonDiffRad;                    // 공식의 A

            // 4) 적도에서 해당 위도까지의 자오선 호 길이 M
            double eccFourth = eccSquared * eccSquared, eccSixth = eccFourth * eccSquared;
            double meridianArc = semiMajorAxis * (
                (1 - eccSquared / 4 - 3 * eccFourth / 64 - 5 * eccSixth / 256) * latRad
                - (3 * eccSquared / 8 + 3 * eccFourth / 32 + 45 * eccSixth / 1024) * Math.Sin(2 * latRad)
                + (15 * eccFourth / 256 + 45 * eccSixth / 1024) * Math.Sin(4 * latRad)
                - (35 * eccSixth / 3072) * Math.Sin(6 * latRad));

            // 5) Easting / Northing 계산
            double easting = scaleFactor * radiusN * (lonTerm
                + (1 - tanSquared + cosTerm) * Math.Pow(lonTerm, 3) / 6
                + (5 - 18 * tanSquared + tanSquared * tanSquared + 72 * cosTerm - 58 * eccPrimeSquared)
                  * Math.Pow(lonTerm, 5) / 120) + falseEasting;

            double northing = scaleFactor * (meridianArc + radiusN * tanLat * (lonTerm * lonTerm / 2
                + (5 - tanSquared + 9 * cosTerm + 4 * cosTerm * cosTerm) * Math.Pow(lonTerm, 4) / 24
                + (61 - 58 * tanSquared + tanSquared * tanSquared + 600 * cosTerm - 330 * eccPrimeSquared)
                  * Math.Pow(lonTerm, 6) / 720));

            if (latitude < 0) northing += falseNorthingSouth;

            // 6) 위도 밴드 문자 : -80°부터 8도 간격 (84°까지는 X)
            int letterIndex = Math.Clamp((int)Math.Floor((latitude + 80) / 8), 0, zoneLetters.Length - 1);

            return new UtmCoordinate
            {
                ZoneNumber = zoneNumber,
                ZoneLetter = zoneLetters[letterIndex],
                Easting = easting,
                Northing = northing
            };
        }
    }
}
