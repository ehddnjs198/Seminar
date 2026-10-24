using System.Windows;
using System.Windows.Input;

namespace PhoneBook
{
    /// <summary>
    /// 화면 이벤트 처리 (버튼 클릭 → PhoneBookManager 호출 → 결과 전시)
    /// </summary>
    public partial class MainWindow : Window
    {
        // 실제 데이터 처리를 담당하는 객체 (UI와 로직 분리)
        private readonly PhoneBookManager phoneBookManager = new PhoneBookManager();

        public MainWindow()
        {
            InitializeComponent();

            // 시연용 샘플 데이터
            phoneBookManager.Add("홍길동", "010-1234-5678", out _);
            phoneBookManager.Add("김철수", "010-9876-5432", out _);

            ShowAll();
        }

        /// <summary>[추가] 버튼</summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = nameTextBox.Text.Trim();          // 앞뒤 공백 제거
            string phoneNumber = phoneTextBox.Text.Trim();

            if (!phoneBookManager.Add(name, phoneNumber, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "추가 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 입력창 비우고 커서를 이름 칸으로 이동
            nameTextBox.Clear();
            phoneTextBox.Clear();
            nameTextBox.Focus();
            ShowAll();
        }

        /// <summary>[선택 삭제] 버튼 : ListView에서 선택된 항목 삭제</summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // SelectedItem은 object 타입 → as로 Contact 변환 (실패 시 null)
            Contact selectedContact = contactListView.SelectedItem as Contact;
            if (selectedContact == null)
            {
                MessageBox.Show("삭제할 연락처를 선택하세요.", "알림");
                return;
            }

            MessageBoxResult answer = MessageBox.Show($"'{selectedContact.Name}'을(를) 삭제할까요?",
                "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            phoneBookManager.Remove(selectedContact);
            ShowAll();
        }

        /// <summary>[검색] 버튼</summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        /// <summary>검색창에서 Enter 키를 눌러도 검색</summary>
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Search();
        }

        /// <summary>[전체보기] 버튼</summary>
        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            searchTextBox.Clear();
            ShowAll();
        }

        /// <summary>검색 결과만 ListView에 전시</summary>
        private void Search()
        {
            string keyword = searchTextBox.Text.Trim();
            if (keyword.Length == 0)
            {
                ShowAll();
                return;
            }

            contactListView.ItemsSource = phoneBookManager.Search(keyword);
            statusTextBlock.Text = $"'{keyword}' 검색 결과 : {contactListView.Items.Count}건";
        }

        /// <summary>전체 목록 전시 (ObservableCollection을 연결 → 이후 추가/삭제 자동 반영)</summary>
        private void ShowAll()
        {
            contactListView.ItemsSource = phoneBookManager.ContactList;
            statusTextBlock.Text = $"전체 {phoneBookManager.ContactList.Count}명";
        }
    }
}
