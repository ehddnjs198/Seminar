namespace PhoneBook
{
    /// <summary>
    /// 연락처 1건(이름 + 전화번호)을 담는 데이터 클래스 (Model)
    /// - ListView의 각 열이 이 속성들과 바인딩된다. ({Binding Name}, {Binding PhoneNumber})
    /// - 바인딩은 '속성(Property)'만 가능하므로 필드가 아닌 { get; set; } 형태로 선언
    /// </summary>
    public class Contact
    {
        public string Name { get; set; }          // 이름
        public string PhoneNumber { get; set; }   // 전화번호
    }
}
