using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BmpMaker
{
    /// <summary>
    /// 프로그램 실행(창 Loaded) 시 Red / Green / Blue 단색 BMP 3개를 자동 생성하고 미리보기 전시
    /// </summary>
    public partial class MainWindow : Window
    {
        // 결과 저장 폴더 : 실행 파일(.exe) 위치\BmpOutput
        private readonly string outputFolder = Path.Combine(AppContext.BaseDirectory, "BmpOutput");

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>창이 화면에 뜨면 기본 크기(300 x 200)로 바로 생성</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateAllBmp(300, 200);
        }

        /// <summary>[다시 생성] 버튼 : 입력한 크기로 재생성</summary>
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(widthTextBox.Text, out int width) || !int.TryParse(heightTextBox.Text, out int height) ||
                width <= 0 || height <= 0)
            {
                MessageBox.Show("가로/세로는 1 이상의 숫자로 입력하세요.", "입력 오류");
                return;
            }
            CreateAllBmp(width, height);
        }

        /// <summary>[폴더 열기] 버튼 : 탐색기로 결과 폴더 열기</summary>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(outputFolder);
            Process.Start("explorer.exe", outputFolder);
        }

        /// <summary>R, G, B 3개 파일 생성 → 미리보기 갱신</summary>
        private void CreateAllBmp(int width, int height)
        {
            Directory.CreateDirectory(outputFolder);   // 폴더가 없으면 생성 (있으면 무시)

            string redPath = Path.Combine(outputFolder, "Red.bmp");
            string greenPath = Path.Combine(outputFolder, "Green.bmp");
            string bluePath = Path.Combine(outputFolder, "Blue.bmp");

            //                         경로       가로   세로    R    G    B
            BmpWriter.Save(redPath,   width, height, 255,   0,   0);
            BmpWriter.Save(greenPath, width, height,   0, 255,   0);
            BmpWriter.Save(bluePath,  width, height,   0,   0, 255);

            redImage.Source = LoadImage(redPath);
            greenImage.Source = LoadImage(greenPath);
            blueImage.Source = LoadImage(bluePath);

            long fileLength = new FileInfo(redPath).Length;
            statusTextBlock.Text = $"생성 완료 ({width} x {height}, 파일당 {fileLength:N0} byte)\n저장 위치 : {outputFolder}";
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
