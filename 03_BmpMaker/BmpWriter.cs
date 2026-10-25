using System.IO;

namespace BmpMaker
{
    /// <summary>
    /// 24비트 BMP 파일을 직접 바이트 단위로 작성하는 클래스 (라이브러리 사용 X)
    ///
    /// [BMP 파일 구조]
    ///  ① 파일 헤더 (BITMAPFILEHEADER, 14 byte) : "BM", 파일 크기, 픽셀 데이터 시작 위치
    ///  ② 정보 헤더 (BITMAPINFOHEADER, 40 byte) : 가로/세로, 색 깊이(24bit), 압축 방식 등
    ///  ③ 픽셀 데이터 : 한 픽셀 = B, G, R 순서 3 byte (RGB가 아니라 BGR!)
    ///     - 맨 아래 줄부터 위로 저장 (Bottom-Up)
    ///     - 한 줄(Row)의 크기는 반드시 4의 배수 → 부족하면 0으로 패딩
    /// </summary>
    public static class BmpWriter
    {
        private const int fileHeaderSize = 14;    // BITMAPFILEHEADER 크기
        private const int infoHeaderSize = 40;    // BITMAPINFOHEADER 크기
        private const short bitsPerPixel = 24;    // 픽셀당 비트 수 (R,G,B 각 8bit)
        private const int pixelsPerMeter = 3780;  // 해상도 96 DPI ≒ 3780 픽셀/m (인쇄용 정보, 화면 표시엔 영향 X)

        /// <summary>한 줄(Row) 바이트 수 : 픽셀당 3byte, 4의 배수로 올림</summary>
        public static int GetRowSize(int width)
        {
            return (width * 3 + 3) / 4 * 4;
        }

        /// <summary>파일 크기 미리 계산 = 헤더 54 + 한 줄 크기 x 세로 (무압축이라 정확히 예측 가능)</summary>
        public static int CalculateFileSize(int width, int height)
        {
            return fileHeaderSize + infoHeaderSize + GetRowSize(width) * height;
        }

        /// <summary>
        /// 지정한 단색(red, green, blue)으로 채운 BMP 파일 생성
        /// </summary>
        public static void Save(string filePath, int width, int height, byte red, byte green, byte blue)
        {
            int rowSize = GetRowSize(width);                     // 한 줄 바이트 수 (4의 배수)
            int imageSize = rowSize * height;                    // 픽셀 데이터 전체 크기
            int pixelOffset = fileHeaderSize + infoHeaderSize;   // 픽셀 데이터 시작 위치 = 54
            int fileSize = pixelOffset + imageSize;              // 전체 파일 크기

            // BinaryWriter : int는 4byte, short는 2byte로 Little-Endian(하위 바이트 먼저) 기록 → BMP 규격과 일치
            using (BinaryWriter writer = new BinaryWriter(File.Create(filePath)))
            {
                // ① 파일 헤더 (14 byte)
                writer.Write((byte)'B');        // 파일 식별자 "BM"
                writer.Write((byte)'M');
                writer.Write(fileSize);         // 파일 전체 크기 (4)
                writer.Write((short)0);         // 예약 영역 (2)
                writer.Write((short)0);         // 예약 영역 (2)
                writer.Write(pixelOffset);      // 픽셀 데이터 시작 위치 (4)

                // ② 정보 헤더 (40 byte)
                writer.Write(infoHeaderSize);   // 이 헤더의 크기 (4)
                writer.Write(width);            // 가로 픽셀 수 (4)
                writer.Write(height);           // 세로 픽셀 수 (4) : 양수 = Bottom-Up
                writer.Write((short)1);         // 색상 평면 수, 항상 1 (2)
                writer.Write(bitsPerPixel);     // 픽셀당 비트 수 = 24 (2)
                writer.Write(0);                // 압축 방식 0 = BI_RGB (무압축) (4)
                writer.Write(imageSize);        // 픽셀 데이터 크기 (4)
                writer.Write(pixelsPerMeter);   // 가로 해상도 (4)
                writer.Write(pixelsPerMeter);   // 세로 해상도 (4)
                writer.Write(0);                // 사용 색상 수 (24bit는 팔레트 없음 → 0) (4)
                writer.Write(0);                // 중요 색상 수 (0 = 전부) (4)

                // ③ 픽셀 데이터 : 단색이므로 한 줄을 만들어 height번 반복 기록
                byte[] rowData = new byte[rowSize];   // 패딩 부분은 기본값 0 유지
                for (int x = 0; x < width; x++)
                {
                    rowData[x * 3] = blue;            // B
                    rowData[x * 3 + 1] = green;       // G
                    rowData[x * 3 + 2] = red;         // R
                }
                for (int y = 0; y < height; y++)
                {
                    writer.Write(rowData);
                }
            }
        }
    }
}
