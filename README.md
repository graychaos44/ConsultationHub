# 📋 ConsultationHub v2.5 - 스마트 고객 상담 관리 장부

> C# .NET 8.0 WPF 및 SQLite 기반의 **차세대 모던 데스크톱 고객 상담 관리 솔루션**입니다.  
> 세련된 Slate & Indigo UI, 마스터-디테일(Master-Detail) 가변 레이아웃, 사진/도면 첨부 및 고화질 확대 뷰어, 드래그 앤 드롭, 원클릭 퀵 필터 칩, 클립보드 복사 툴, 실시간 기존 고객 감지 및 엑셀(CSV) 내보내기 기능을 제공합니다.

---

## ✨ 핵심 기능 (Key Features in v2.5)

- **📷 사진/도면 파일 첨부 & 크기 조절 (New!)**
  - **다양한 포맷 지원**: JPG, JPEG, PNG, BMP, GIF, WEBP 지원
  - **드래그 앤 드롭 (Drag & Drop)**: 파일 탐색기, 바탕화면, 웹 브라우저(크롬/엣지)에서 마우스로 사진을 끌어다 놓으면 즉시 자동 첨부 (입력창 어디에나 가능)
  - **클립보드 붙여넣기 (`Ctrl + V`)**: 윈도우 캡처 도구(`Win + Shift + S`)로 캡처한 이미지나 복사한 이미지를 즉시 붙여넣기
  - **썸네일 크기 실시간 조절**: 슬라이더(50px ~ 240px) 및 `[-]`, `[+]`, `[기본]` 버튼으로 썸네일 크기를 취향에 맞게 자유 조절 (설정값 자동 저장)
  - **전체화면 고화질 줌 뷰어**: 썸네일 더블클릭 또는 `🔍` 클릭 시 고화질 모달 뷰어 실행, **마우스 휠 줌(30%~400%)** 및 스크롤 패닝 지원

- **🖥️ 가변 분할 마스터-디테일 (Master-Detail) 레이아웃 & 전체화면 모드 (New!)**
  - **마우스 드래그 분할 조절**: 중앙 분할선(GridSplitter)을 마우스로 끌어 좌측 상담 목록과 우측 작성 공간의 비율을 자유롭게 조절 (더블클릭 시 기본 비율 복원, 크기 자동 기억)
  - **전체화면 집중 작성 모드 (`F11` / `[◀ 목록 접기]`)**: 왼쪽 목록을 접어 상담 상세 작성 공간을 넓게 장악하여 집중 작성 가능

- **🏷️ 원클릭 퀵 필터 칩 (Quick Filter Chips)**
  - 콤보박스를 열 필요 없이 `[전체]`, `[오늘 상담]`, `[진행중]`, `[재상담 예정]`, `[완료]`, `[긴급]` 칩 클릭 한 번으로 목록 즉시 필터링

- **⌨️ 완벽한 업무 단축키 지원**
  - `Ctrl + S`: 상담 내용 즉시 저장
  - `Ctrl + N`: 신규 상담 작성
  - `Ctrl + F`: 검색창 포커스 및 검색어 전체 선택
  - `F11`: 상담 목록 접기 / 펼치기 토글
  - `F5`: 최신 데이터 및 통계 새로고침
  - `Ctrl + V`: 클립보드 이미지/사진 즉시 붙여넣기
  - `Ctrl + 마우스 휠`: 상담 상세 입력창 글자 크기 실시간 확대/축소
  - `ESC`: 전체화면 사진 뷰어 닫기

- **📋 원클릭 클립보드 복사 툴바**
  - 연락처 옆 `[복사]`, 주소 옆 `[복사]` 버튼으로 1초 만에 클립보드 복사
  - `📋 내용 복사` 버튼 클릭 시 메신저(카카오톡/잔디/슬랙)나 사내 ERP에 바로 공유할 수 있는 정돈된 요약 텍스트로 자동 복사

- **📞 전화번호 자동 하이픈 포맷팅**
  - `01012345678`처럼 숫자만 연속 입력해도 포커스 이동 시 `010-1234-5678`로 자동 변환

- **⏰ 빠른 재상담일 프리셋 지정**
  - 달력을 찾을 필요 없이 `[오늘]`, `[+1일]`, `[+3일]`, `[+7일(1주)]`, `[지우기]` 버튼으로 즉시 일정 설정

- **🎨 세련된 알약형 상태 뱃지 & 사진 첨부 아이콘**
  - 완료(그린), 진행중(블루), 대기중(슬레이트), 보류(앰버) 컬러 뱃지
  - 사진이 첨부된 상담 건은 목록에 `📷` 아이콘과 함께 첨부 장수 툴팁 표시

---

## 🛠 기술 스택 (Tech Stack)

| 구분 | 기술 / 프레임워크 |
| :--- | :--- |
| **Framework** | .NET 8.0 WPF (Windows Presentation Foundation) |
| **Architecture** | MVVM (Model-View-ViewModel) Pattern |
| **Database** | SQLite (`Microsoft.Data.Sqlite` 8.0.8) |
| **Image Storage** | AppData 전용 격리 저장소 (`%APPDATA%\ConsultationLedger\Images\`) |
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
