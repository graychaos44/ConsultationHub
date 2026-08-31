# 📋 ConsultationHub - 일일 상담 관리 장부

> C# .NET 8.0 WPF 및 SQLite 기반 **윈도우 데스크톱 상담 관리 응용 프로그램**입니다.  
> 고객 인적사항(이름, 연락처, 주소)과 상담 메모를 손쉽게 기록하고, 실시간 자동 중복 감지 힌트 및 엑셀(CSV) 내보내기 기능을 제공합니다.

---

## ✨ 핵심 기능 (Key Features)

- **👤 핵심 고객 인적사항 카드 (중앙 배치)**
  - 가장 중요한 **고객명(성함)**, **연락처(전화번호)**, **주소(소재지/배송지)** 입력란을 중앙 최상단에 직관적인 강조 카드로 배치하여 편안하게 기록할 수 있습니다.

- **⚡ 실시간 자동 힌트 감지 (Auto Duplicate Hint)**
  - 타자를 치는 순간, 기존 DB에 저장된 동일/유사 고객 정보가 있는 경우 파란색 **자동 힌트 메모 배너**(`💡 힌트: '김철수' 님과 일치하는 이전 상담 N건 보관 중`)가 실시간으로 나타납니다.
  - `기존정보 채우기` 및 `이력 보기` 버튼을 통해 기존 고객 정보를 즉시 자동 완성하거나 과거 상담 이력을 바로 대조할 수 있습니다.

- **🔢 깔끔한 순번(No.) 및 총 건수 표시**
  - 삭제 시 번호가 건너뛰던 DB ID를 숨기고, 목록 기준 깔끔한 **1, 2, 3... 순번**과 **전체 데이터 총 건수 뱃지**를 직관적으로 보여줍니다.

- **🔍 다중 필터 & 스마트 실시간 검색**
  - 고객명, 연락처, 주소, 상담 요약, 상세 메모, 태그 항목을 0.1초 내 즉시 검색할 수 있으며, 카테고리/진행 상태/날짜 범위별 필터 조회가 가능합니다.

- **💾 안전한 로컬 SQLite DB & 엑셀(CSV) 내보내기**
  - 사용자 컴퓨터 내 로컬 데이터베이스(`consultations.db`)에 오프라인으로 100% 안전하게 자동 보관됩니다.
  - `📊 CSV 파일 내보내기` 버튼 클릭 시 현재 검색된 기록이 바탕화면에 엑셀 호환 CSV 파일로 즉시 출력됩니다.

---

## 🛠 기술 스택 (Tech Stack)

| 구분 | 기술 / 프레임워크 |
| :--- | :--- |
| **Framework** | .NET 8.0 WPF (Windows Presentation Foundation) |
| **Architecture** | MVVM (Model-View-ViewModel) Pattern |
| **Database** | SQLite (`Microsoft.Data.Sqlite` 8.0.8) |
| **Design** | Custom Modern XAML ResourceDictionary (Slate/Indigo UI, Rounded Cards, Custom Badges) |
| **Packaging** | Win-X64 Single-File Self-Contained Desktop Application |

---

## 📁 프로젝트 구조 (Project Structure)

```text
ConsultationLedger/
├── Models/
│   └── ConsultationRecord.cs       # 상담 데이터 모델 (순번, 고객명, 연락처, 주소, 일시 등)
├── Services/
│   ├── DatabaseService.cs          # SQLite DB CRUD, 자동 마이그레이션, 실시간 중복 검색
│   └── ExportService.cs            # CSV 엑셀 파일 내보내기
├── ViewModels/
│   ├── ViewModelBase.cs            # INotifyPropertyChanged 기본 클래스
│   ├── RelayCommand.cs             # MVVM Command 바인딩 구현체
│   └── MainViewModel.cs            # 메인 앱 상태, 실시간 힌트 감지, 필터링 로직
├── Styles/
│   └── ModernTheme.xaml            # 슬레이트/인디고풍 UI 컨트롤 테마
├── Views/
│   ├── MainWindow.xaml             # 3단 컬럼 메인 대시보드 레이아웃 XAML
│   └── MainWindow.xaml.cs          # 실시간 TextChanged 이벤트 처리
├── App.xaml / App.xaml.cs          # 앱 진입점 및 리소스 머지
├── MANUAL.md                       # 상세 사용자 매뉴얼
└── ConsultationLedger.csproj       # WPF 프로젝트 구성 파일
```

---

## 🚀 실행 및 빌드 방법 (Getting Started)

### 1. 소스 코드 빌드 및 실행
```powershell
# 프로젝트 빌드
dotnet build ConsultationLedger.csproj

# 실행
dotnet run --project ConsultationLedger.csproj
```

### 2. 단독 실행형 (Self-Contained .exe) 파일 단일 빌드
```powershell
# .NET 미설치 PC에서도 즉시 실행되는 단일 파일 빌드
dotnet publish ConsultationLedger.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./Publish
```

---

## 📖 사용 설명서 (Manual)
자세한 화면 구성 및 기능 사용 방법은 [MANUAL.md](MANUAL.md) 문서를 참고하세요.

---

## 📄 License
This project is licensed under the MIT License.
