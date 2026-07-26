# Avalonia UI Designer

Qt Designer 스타일의 Avalonia XAML 비주얼 디자이너.

## 스택

- .NET 8
- Avalonia 11.3.12
- CommunityToolkit.Mvvm 8.4.1
- bodong.Avalonia.PropertyGrid 11.3.11.1

## 실행

```bash
dotnet run --project src/AvaloniaUIDesigner.App/AvaloniaUIDesigner.App.csproj
```

## 현재 상태

- 4-Pane 레이아웃 (Toolbox / Canvas / Object Tree / Property Inspector)
- **Toolbox**: 내장 컨트롤, 복합 프리셋, JSON 컴포넌트 팩
- **배치**: 클릭-투-플레이스와 드래그 앤 드롭으로 실제 Avalonia 컨트롤 생성
- **컨테이너 편집**: Grid 셀, StackPanel 순서·주축 크기, DockPanel 순서·방향·크기·LastChildFill, WrapPanel 순서·방향·항목 크기·간격·정렬, UniformGrid 순서·행·열·첫 열·간격, 중첩 Canvas 로컬 좌표·직접 변형·z-order, TabControl 탭 정의·탭별 단일 자식·활성 페이지, Border·ScrollViewer·Expander의 단일 Content 자식을 편집하고 재귀 Object Tree·AXAML·미리보기에 보존
- **요소 선택**: 배치된 요소 클릭 시 파란 외곽선
- **Object Tree 자동 동기화**: 배치된 요소가 루트(Window) 아래에 추가
- **이동/리사이즈 기즈모**: 선택된 요소를 드래그로 이동, 8방향 핸들로 리사이즈 (최소 10px)
- **PropertyGrid 연동**: 선택된 컨트롤의 속성을 bodong PropertyGrid로 실시간 편집
- **Appearance 편집**: 배경·전경·테두리·두께·모서리를 편집하고 Undo/Redo, 미리보기, AXAML 왕복에 보존
- **색상 리소스**: 문서 단위 SolidColorBrush를 편집하고 DynamicResource로 컨트롤에 적용
- **클래스·상태 스타일**: `[Button.primary:pointerover]` 형식의 Setter, 선택 컨트롤별 상태 선택기와 캔버스 배지, 대화형 미리보기, 로컬 속성 우선순위 지원
- **이벤트 선언**: Button Click 핸들러 이름 편집 및 AXAML 내보내기
- **AXAML 워크플로**: Window 저장/열기, 유효성 확인, UserControl 내보내기, 복사, 미리보기
- 상태바 피드백

## 사용법

1. 좌측 Toolbox에서 컨트롤 또는 프리셋 선택
2. 중앙 Canvas 영역을 클릭 → 클릭 위치에 기본 크기로 생성
3. 생성된 요소를 클릭하여 선택 → 8개 핸들로 이동/리사이즈
4. 우측 하단 Properties 패널에서 속성 편집

## 로드맵

- ~~v0.3: 이동/리사이즈 기즈모~~ ✅
- ~~v0.4: bodong PropertyGrid 실제 연동~~ ✅
- ~~v0.5: .axaml 저장/로드~~ ✅
- ~~v0.6: 실제 드래그&드롭, 삭제, 언두~~ ✅

## 컴포넌트 팩

`File > Load Component Pack...`에서 JSON 팩을 불러오면 현재 세션의 Toolbox에 별칭 컨트롤을 추가할 수 있습니다. 각 항목은 이미 지원되는 Avalonia 타입을 기반으로 하며, 표시 이름, 기본 크기, 기본 속성을 지정합니다. 예시는 [component-pack.example.json](docs/component-pack.example.json)을 참고하세요. 캔버스에서 컨트롤 하나를 선택한 뒤 `File > Export Selected as Component Pack...`을 사용하면 해당 크기와 시각 속성을 재사용 가능한 JSON 팩으로 저장할 수 있습니다.

`File > Export UserControl AXAML...`은 현재 캔버스를 재사용 가능한 `UserControl` 레이아웃으로 내보냅니다. 코드비하인드를 추가할 때는 생성된 루트에 프로젝트의 `x:Class`를 지정하면 됩니다.
