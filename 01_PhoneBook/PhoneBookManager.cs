using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace PhoneBook
{
    /// <summary>
    /// 전화번호부 핵심 로직(추가 / 삭제 / 검색) 담당 클래스
    /// - 화면(MainWindow)과 데이터 처리 로직을 분리 → UI가 바뀌어도 로직은 그대로 재사용 가능
    /// </summary>
    public class PhoneBookManager
    {
        // 전화번호 형식 검사용 정규식 : 010-1234-5678, 01012345678, 02-123-4567 등 허용
        private static readonly Regex phonePattern = new Regex(@"^\d{2,3}-?\d{3,4}-?\d{4}$");

        // 전체 연락처 목록
        // ObservableCollection : 항목 추가/삭제 시 바인딩된 UI(ListView)에 자동으로 변경을 알려줌
        private readonly ObservableCollection<Contact> contactList = new ObservableCollection<Contact>();

        /// <summary>외부(UI)에서 목록을 읽기 위한 속성 (전시 기능)</summary>
        public ObservableCollection<Contact> ContactList => contactList;

        /// <summary>
        /// 연락처 추가. 성공 시 true, 실패 시 false + 실패 사유(errorMessage) 반환
        /// </summary>
        public bool Add(string name, string phoneNumber, out string errorMessage)
        {
            // 1) 빈 값 검사
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                errorMessage = "이름과 전화번호를 모두 입력하세요.";
                return false;
            }

            // 2) 전화번호 형식 검사
            if (!phonePattern.IsMatch(phoneNumber))
            {
                errorMessage = "전화번호 형식이 올바르지 않습니다. (예: 010-1234-5678)";
                return false;
            }

            // 3) 중복 검사 : 같은 전화번호는 한 번만 등록 (LINQ Any = 하나라도 조건을 만족하면 true)
            if (contactList.Any(contact => contact.PhoneNumber == phoneNumber))
            {
                errorMessage = "이미 등록된 전화번호입니다.";
                return false;
            }

            contactList.Add(new Contact { Name = name, PhoneNumber = phoneNumber });
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>연락처 삭제</summary>
        public void Remove(Contact contact)
        {
            contactList.Remove(contact);
        }

        /// <summary>
        /// 검색 : 이름 또는 전화번호에 keyword가 포함된 연락처 목록 반환 (부분 일치)
        /// </summary>
        public List<Contact> Search(string keyword)
        {
            return contactList
                .Where(contact => contact.Name.Contains(keyword) || contact.PhoneNumber.Contains(keyword))
                .ToList();
        }
    }
}
