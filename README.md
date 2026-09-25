# 📋 ConsultationHub v2.0 - 스마트 고객 상담 관리 장부

> C# .NET 8.0 WPF 및 SQLite 기반의 **차세대 모던 데스크톱 고객 상담 관리 솔루션**입니다.  
> 세련된 Slate & Indigo UI, 마스터-디테일(Master-Detail) 인터페이스, 원클릭 퀵 필터 칩, 클립보드 복사 툴, 실시간 고객 감지 및 엑셀(CSV) 내보내기 기능을 제공합니다.

---

## ✨ 핵심 업그레이드 기능 (Key Features in v2.0)

- **🖥️ 2단 분할 마스터-디테일 (Master-Detail) 레이아웃**
  - 기존 3단의 답답했던 필터 패널 공간을 최적화하고, 좌측 **스마트 검색 & 목록(550px)** 과 우측 **상담 작성 캔버스(가변)** 의 현대적인 CRM 워크스페이스를 제공합니다.

- **🏷️ 원클릭 퀵 필터 칩 (Quick Filter Chips)**
  - 콤보박스를 열 필요 없이 `[전체]`, `[오늘 상담]`, `[진행중]`, `[재상담 예정]`, `[완료]`, `[긴급]` 칩 클릭 한 번으로 목록이 즉시 필터링됩니다.

- **⌨️ 완벽한 업무 단축키 지원**
  - `Ctrl + S`: 상담 내용 즉시 저장
  - `Ctrl + N`: 신규 상담 작성
  - `Ctrl + F`: 검색창 포커스 및 검색어 전체 선택
  - `F5`: 최신 데이터 새로고침
  - `Ctrl + 마우스 휠`: 상담 상세 입력란 실시간 줌 확대/축소

- **📋 원클릭 클립보드 복사 툴바**
  - 연락처 옆 `[복사]`, 주소 옆 `[복사]` 버튼으로 1초 만에 클립보드 복사
  - `📋 내용 복사` 버튼 클릭 시 메신저(카카오톡/잔디/슬랙)나 사내 ERP에 바로 공유할 수 있는 정돈된 요약 텍스트로 자동 복사

- **📞 전화번호 자동 하이픈 포맷팅**
  - `01012345678`처럼 숫자만 연속 입력해도 포커스 이동 시 `010-1234-5678`로 자동 변환

- **⏰ 빠른 재상담일 프리셋 지정**
  - 달력을 찾을 필요 없이 `[오늘]`, `[+1일]`, `[+3일]`, `[+7일(1주)]`, `[지우기]` 버튼으로 즉시 일정 설정

- **🎨 세련된 알약형 상태 뱃지 (Pill Badges)**
  - 완료(그린), 진행중(블루), 대기중(슬레이트), 보류(앰버)의 현대적인 컬러 뱃지 적용

---

## 🛠 기술 스택 (Tech Stack)

| 구분 | 기술 / 프레임워크 |
| :--- | :--- |
| **Framework** | .NET 8.0 WPF (Windows Presentation Foundation) |
| **Architecture** | MVVM (Model-View-ViewModel) Pattern |
| **Database** | SQLite (`Microsoft.Data.Sqlite` 8.0.8) |
| **Design** | Modern Slate & Indigo Design System, Custom Pill Badges |
| **Packaging** | Win-X64 Single-File Self-Contained Desktop Application |

---

## 🚀 실행 및 빌드 방법

### 1. 단독 실행형 (.exe) 빌드 및 배포
```powershell
& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" publish ConsultationLedger.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "C:\Users\grayc\OneDrive\Desktop\상담장부"
```

### 2. 바로 실행
- 바탕화면의 **`상담장부`** 또는 **`ConsultationHub`** 바로가기를 더블 클릭하여 실행합니다.

---

## 📖 사용 설명서
상세한 기능 가이드라인은 [MANUAL.md](MANUAL.md) 문서를 확인하세요.
