using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BmpMaker
{
    /// <summary>
    /// 사용자가 입력한 R, G, B 값과 가로/세로 크기로 단색 BMP 파일을 생성
    /// - 프로그램 실행(창 Loaded) 시 기본값(빨강 255,0,0 / 300 x 200)으로 바로 1개 생성
    /// - 가로/세로를 바꾸면 예상 파일 크기가 실시간으로 바뀜 (파일 크기 = 54 + 한 줄 크기 x 세로)
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int maxPixelSize = 5000;   // 가로/세로 최대값 (너무 큰 파일 방지 : 5000x5000 ≒ 75MB)

        // 결과 저장 폴더 : 실행 파일(.exe) 위치\BmpOutput
        private readonly string outputFolder = Path.Combine(AppContext.BaseDirectory, "BmpOutput");

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>창이 화면에 뜨면 기본 입력값으로 바로 생성</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
            CreateBmp();
        }

        /// <summary>[BMP 생성] 버튼</summary>
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            CreateBmp();
        }

        /// <summary>[폴더 열기] 버튼 : 탐색기로 결과 폴더 열기</summary>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(outputFolder);
            Process.Start("explorer.exe", outputFolder);
        }

        /// <summary>입력값이 바뀔 때마다 색상 미리보기와 예상 파일 크기 갱신</summary>
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // InitializeComponent 중에도 호출되므로, 창이 다 만들어진 뒤에만 처리
            if (!IsLoaded) return;
            UpdatePreview();
        }

        /// <summary>색상 박스 색 변경 + 예상 파일 크기 표시</summary>
        private void UpdatePreview()
        {
            if (!TryReadInputs(out byte red, out byte green, out byte blue, out int width, out int height))
            {
                fileSizeTextBlock.Text = "입력값을 확인하세요";
                return;
            }

            colorPreviewBorder.Background = new SolidColorBrush(Color.FromRgb(red, green, blue));
            fileSizeTextBlock.Text = $"예상 파일 크기 : {BmpWriter.CalculateFileSize(width, height):N0} byte";
        }

        /// <summary>입력값 검사 후 BMP 1개 생성 → 미리보기 갱신</summary>
        private void CreateBmp()
        {
            if (!TryReadInputs(out byte red, out byte green, out byte blue, out int width, out int height))
            {
                MessageBox.Show($"R, G, B는 0~255, 가로/세로는 1~{maxPixelSize} 숫자로 입력하세요.", "입력 오류");
                return;
            }

            Directory.CreateDirectory(outputFolder);   // 폴더가 없으면 생성 (있으면 무시)

            // 파일 이름에 색상값과 크기를 넣어 서로 덮어쓰지 않게 함   예) R255_G0_B0_300x200.bmp
            string fileName = $"R{red}_G{green}_B{blue}_{width}x{height}.bmp";
            string filePath = Path.Combine(outputFolder, fileName);

            BmpWriter.Save(filePath, width, height, red, green, blue);
            resultImage.Source = LoadImage(filePath);

            long fileLength = new FileInfo(filePath).Length;
            statusTextBlock.Text = $"생성 완료 : {fileName} ({fileLength:N0} byte)\n저장 위치 : {outputFolder}";
        }

        /// <summary>
        /// 5개 입력칸 읽기. 하나라도 잘못되면 false
        /// - byte.TryParse : 0~255 범위를 벗어나면 자동으로 실패 → 색상값 범위 검사가 따로 필요 없음
        /// - '&&' 대신 '&' : &&는 앞이 false면 뒤를 건너뛰어 out 변수가 비게 됨 → &로 모두 실행
        /// </summary>
        private bool TryReadInputs(out byte red, out byte green, out byte blue, out int width, out int height)
        {
            bool isColorValid = byte.TryParse(redTextBox.Text, out red)
                              & byte.TryParse(greenTextBox.Text, out green)
                              & byte.TryParse(blueTextBox.Text, out blue);

            bool isSizeValid = int.TryParse(widthTextBox.Text, out width)
                             & int.TryParse(heightTextBox.Text, out height)
                             && width >= 1 && width <= maxPixelSize
                             && height >= 1 && height <= maxPixelSize;

            return isColorValid && isSizeValid;
        }

        /// <summary>
        /// 파일에서 이미지 읽기
        /// - CacheOption.OnLoad : 메모리에 다 읽고 파일을 바로 닫음 → 재생성 시 "파일 사용 중" 오류 방지
        /// </summary>
        private BitmapImage LoadImage(string filePath)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.UriSource = new Uri(filePath);
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }
}
