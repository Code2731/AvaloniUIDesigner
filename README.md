# Avalonia UI Designer

Qt Designer 스타일의 Avalonia XAML 비주얼 디자이너.

## 스택

- .NET 8
- Avalonia 11.3.12
- Avalonia.Controls.DataGrid 11.3.12
- CommunityToolkit.Mvvm 8.4.1
- bodong.Avalonia.PropertyGrid 11.3.11.1

## 실행

```bash
dotnet run --project src/AvaloniaUIDesigner.App/AvaloniaUIDesigner.App.csproj
```

## 현재 상태

- 4-Pane 레이아웃 (Toolbox / Canvas / Object Tree / Property Inspector)
- **Toolbox**: 내장 컨트롤, 복합 프리셋, JSON 컴포넌트 팩
- **선택 영역 Toolbox 프리셋**: 여러 root 컨트롤을 상대 좌표·현재 속성과 함께 Toolbox에 등록하고 JSON 팩으로 저장·불러오기
- **배치**: 클릭-투-플레이스와 드래그 앤 드롭으로 실제 Avalonia 컨트롤 생성
- **캔버스 뷰포트**: 큰 아트보드와 확대 상태를 양축 자동 스크롤로 탐색하고, Desktop·Tablet·Mobile·사용자 지정 아트보드 크기와 회전, Zoom In/Out·Actual Size·Fit to View와 스크롤 콘텐츠 크기를 동기화하며 중간 마우스 드래그로 뷰포트를 팬하고 Ctrl+휠로 포인터 중심 줌
- **아트보드 배경**: White·Soft Gray·Ink 프리셋과 사용자 지정 `#RRGGBB`/`#AARRGGBB` 색상을 편집하고 Undo·Preview·AXAML 왕복에 보존
- **디자인 룰러**: 가로·세로 눈금을 ScrollViewer 오프셋과 렌더 줌에 동기화하고 포인터 기준선을 표시해 현재 화면의 아트보드 좌표를 확인
- **디자인 가이드**: 가로·세로 룰러에서 드래그해 가이드라인을 만들고 캔버스 이동·리사이즈 Smart Snap 후보로 사용하며, 캔버스 밖으로 드래그하면 제거합니다. View 메뉴에서 표시·가이드 스냅을 각각 끄거나 전체 가이드를 지울 수 있습니다.
- **리사이즈 Smart Snap**: 8방향 핸들로 크기를 조정할 때 아트보드 경계·중앙선, 디자인 가이드, 다른 컨트롤의 모서리·중앙선에 맞추고 스냅 기준선을 표시하며 최소 10px 크기를 보호합니다. 크기 변경은 이동과 같은 Undo·AXAML 왕복 흐름을 사용합니다.
- **다중 선택 리사이즈**: 같은 root 또는 같은 Canvas의 여러 컨트롤을 선택하면 bounding box 핸들로 위치·크기를 비율 조정하고 Canvas 자식의 로컬 좌표도 동기화합니다. Grid·StackPanel·Content 자식이나 서로 다른 부모를 섞은 선택은 좌표 손실을 막기 위해 리사이즈를 차단합니다.
- **리사이즈 비율 잠금**: 코너 핸들을 `Shift`와 함께 드래그하면 단일 컨트롤과 다중 선택 bounding box의 원래 가로·세로 비율을 유지하며, 잠금 중에는 Smart Snap보다 비율을 우선합니다.
- **디자인 그리드**: 그리드 표시·Snap to Grid를 전환하고 4·8·16px 프리셋 또는 4~32px 사용자 지정 간격을 편집하며 문서 설정·Undo/Redo·Preview·AXAML 메타데이터에 보존
- **계층 클립보드**: 컨테이너를 선택해 복사·잘라내기·붙여넣기·복제하면 내부 자식 계층과 부모별 배치 메타데이터를 함께 보존하고, 붙여넣은 부모 이름을 새 이름으로 재매핑
- **컨테이너 편집**: Grid 셀, StackPanel 순서·주축 크기, DockPanel 순서·방향·크기·LastChildFill, WrapPanel 순서·방향·항목 크기·간격·정렬, UniformGrid 순서·행·열·첫 열·간격, 중첩 Canvas 로컬 좌표·직접 변형·z-order, TabControl 탭 정의·탭별 단일 자식·활성 페이지·TabStripPlacement·콘텐츠 정렬, SplitView Pane·Content 슬롯·Inline/Overlay·배치 방향, Border·ContentControl·UserControl·ScrollViewer·Expander의 단일 Content 자식을 편집하고 재귀 Object Tree·AXAML·미리보기에 보존
- **계층 항목 편집**: TreeView 항목을 `[-]`(펼침), `[+]`(접힘), 두 칸 들여쓰기 문법으로 편집하고 Undo/Redo, 복제, 미리보기, AXAML 왕복에 보존
- **메뉴 구조 편집**: Menu 항목을 두 칸 들여쓰기로 중첩하고 `---` 구분선, `[x]/[ ]` 체크, `(x)/( )` 라디오와 `{Group}`, `| Ctrl+N` 표시·실행 단축키를 편집해 Undo/Redo, 복제, 미리보기, AXAML 왕복에 보존
- **DataGrid 열 설계**: Text·CheckBox 열의 Header·Binding·Width·ReadOnly를 편집하고 샘플 행, Undo/Redo, 복제, 미리보기, AXAML 왕복에 보존
- **DataGrid Behavior 속성**: 헤더·그리드선·선택·클립보드, 열 조작, 읽기 전용, 고정 열, 행·열 크기, 스크롤 정책을 전용 편집기로 검증하고 열 정의·Undo/Redo·Preview·Binding·AXAML 왕복에 보존
- **GridSplitter Behavior 속성**: GridSplitter를 Toolbox에서 배치하고 행/열 방향·인접 track 동작·미리보기·키보드/드래그 증분을 편집해 Grid 셀 배치·Undo/Redo·Preview·Binding·AXAML 왕복에 보존
- **Canvas 그룹화**: 다중 선택한 같은 root 또는 같은 Canvas의 형제 컨트롤을 실제 Canvas 그룹으로 묶고 해제하며, bounding box·로컬 좌표·z-order·Object Tree·Undo/Redo·Preview·AXAML 왕복을 보존
- **다중 선택 StackPanel 레이아웃**: 같은 root 또는 같은 Canvas의 형제 컨트롤을 가로·세로 StackPanel로 한 번에 감싸고, bounding box·Canvas 상대 좌표·기존 순서·Object Tree·Undo/Redo·Preview·AXAML 왕복을 보존
- **다중 선택 Grid 레이아웃**: 같은 root 또는 같은 Canvas의 형제 컨트롤을 자동 행·열 Grid로 한 번에 감싸고, 선택 순서에 따른 셀 배치·Canvas 상대 좌표·Object Tree·Undo/Redo·Preview·AXAML 왕복을 보존
- **다중 선택 UniformGrid 레이아웃**: 같은 root 또는 같은 Canvas의 형제 컨트롤을 자동 행·열 UniformGrid로 한 번에 감싸고, 동일 셀 크기·간격·선택 순서·Object Tree·Undo/Redo·Preview·AXAML 왕복을 보존
- **다중 선택 DockPanel/WrapPanel 레이아웃**: 같은 root 또는 같은 Canvas의 형제 컨트롤을 가로·세로 DockPanel 또는 WrapPanel로 한 번에 감싸고, 도킹 방향·LastChildFill·항목 간격·자동 행/열·선택 순서·Object Tree·Undo/Redo·Preview·AXAML 왕복을 보존
- **레이아웃 해제**: 선택한 Canvas·Grid·StackPanel·DockPanel·WrapPanel·UniformGrid 컨테이너를 `Break Selected Layout`으로 제거하고 자식을 독립 컨트롤 또는 원래 Canvas 형제로 복원하며 좌표·순서·다중 선택을 보존
- **다중 선택 레이아웃 안전성**: Arrange와 Center on Artboard를 root 또는 같은 Canvas의 형제에 적용하고, 부모가 좌표를 관리하는 Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·TabControl·SplitView·Content 자식은 부모 전용 배치 명령으로 보호
- **데이터 바인딩**: 선택 컨트롤의 지원 속성에 Path·Mode·Fallback을 여러 개 선언하고, 디자인 샘플을 유지한 채 ReflectionBinding·Undo/Redo·복제·미리보기·AXAML 왕복에 보존
- **샘플 DataContext**: 문서 단위 JSON을 바인딩 Path에 연결해 Text·상태·숫자·선택·ItemsSource를 캔버스와 Preview에서 확인하고, 원래 디자인 값·Undo/Redo·AXAML 왕복에 보존
- **공통 레이아웃 속성**: Margin·Padding·수평/수직 정렬·Min/Max 크기를 편집하고 컨테이너 자식, Undo/Redo, 복제, Preview, AXAML 왕복에 보존
- **공통 Typography 속성**: FontFamily·Size·Style·Weight와 TextBlock/TextBox의 Alignment·Wrapping을 편집하고 Undo/Redo, 복제, Preview, AXAML 왕복에 보존
- **공통 Transform 속성**: 이동·회전·크기·기울기·변환 기준점을 레이아웃 슬롯과 독립적으로 편집하고 Undo/Redo, 복제, Preview, Draft·Full·UserControl AXAML 왕복에 보존
- **Accessibility & Navigation 속성**: Tooltip·접근 가능한 이름·Automation ID·HelpText·접근성 뷰·HeadingLevel·LiveSetting·필수 입력·TabIndex·TabStop·Focusable을 통합 편집하고 Undo/Redo, 복제, Preview, Draft·Full·UserControl AXAML 왕복에 보존
- **Interaction & Rendering 속성**: Opacity·Enabled·Visible·HitTest·ClipToBounds·LayoutRounding·FlowDirection·Cursor를 통합 편집하고 Undo/Redo, 복제, Preview, Draft·Full·UserControl AXAML 왕복에 보존
- **Visual Effects 속성**: None·Blur·Drop Shadow 모드와 반경·오프셋·색상·불투명도를 편집하고 Undo/Redo, 복제, Preview, Draft·Full·UserControl AXAML 왕복에 보존
- **Range & Value 속성**: Slider의 step·tick·방향, ProgressBar의 indeterminate·진행 텍스트, NumericUpDown의 increment·format·spinner 정책을 검증된 범위/값과 함께 편집하고 Undo/Redo, 복제, Preview, Draft·Full·UserControl AXAML 왕복에 보존
- **Text Input 속성**: TextBox의 디자인 텍스트·watermark·multiline/tab·wrapping/alignment·read-only·길이/줄 제한·password·floating watermark·undo/selection 정책을 통합 편집하고 Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **SelectableTextBlock 속성**: 선택 가능한 텍스트·선택 브러시·선택 전경색을 전용 편집기로 검증하고 Typography, Undo/Redo, Preview, Text Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **SplitView Pane Behavior 속성**: DisplayMode·pane 열림 상태·Open/Compact 길이·PanePlacement·light-dismiss 오버레이·solid PaneBackground를 전용 편집기로 검증하고 SplitView Pane/Content 계층, Undo/Redo, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **TabControl Behavior 속성**: TabStripPlacement와 선택 탭 콘텐츠의 가로·세로 정렬을 전용 편집기로 검증하고 탭 항목·탭별 자식·활성 페이지, Undo/Redo, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **ItemsControl 항목 편집**: 일반 ItemsControl의 정적 문자열 항목을 기존 항목 편집기로 관리하고 Undo/Redo, 복제, Preview, ItemsSource Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **MaskedTextBox 속성**: MaskedTextBox의 .NET mask·PromptChar·prompt 숨김 정책을 전용 편집기로 검증하고 Undo/Redo, 복제, Preview, Text Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **AutoCompleteBox 속성**: Text·watermark·자동 완성·최소 접두사·populate 지연·FilterMode·drop-down 높이/열림 상태와 정적 suggestion을 편집하고 Undo/Redo, 복제, Preview, Text Binding, Draft·Full·UserControl AXAML 왕복에 보존합니다. AsyncPopulator와 selector delegate는 코드 영역으로 남깁니다.
- **Selection Behavior 속성**: ComboBox·ListBox·TreeView의 선택·검색·자동 스크롤 정책과 ComboBox editable/placeholder/drop-down 표현, ListBox·TreeView 다중 선택 모드를 편집하고 Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **Date & Time Input 속성**: DatePicker의 날짜 범위·구성 요소·표시 형식, CalendarDatePicker의 표시 범위·선택 형식·watermark, Calendar의 선택·표시 모드·표시 범위·첫 요일·탭 범위 선택, TimePicker의 시간·증분·12/24시간·초 표시를 원자적으로 편집하고 Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **ColorPicker 속성**: ColorPicker의 색상·색상 모델·스펙트럼·알파·팔레트·입력 표시 정책과 팔레트 열 수를 원자적으로 편집하고 Undo/Redo, 복제, Preview, Color Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **Toggle & Choice Behavior 속성**: CheckBox·RadioButton·ToggleSwitch·ToggleButton의 checked/unchecked/indeterminate 상태, three-state, ClickMode, 콘텐츠 정렬과 Radio 그룹·Switch On/Off 문구를 편집하고 Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **Disclosure & Scrolling 속성**: Expander의 header·expanded 상태·전개 방향·콘텐츠 정렬과 ScrollViewer의 양축 scrollbar·auto-hide·chaining·deferred/focus scrolling·snap 정책을 편집하고 실제 중첩 Content, Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **ContentControl·UserControl 계층**: ContentControl과 UserControl을 Toolbox에서 배치하고 단일 디자이너 자식 또는 fallback TextBlock 콘텐츠를 편집하며, Content Binding·Undo/Redo·Preview·Draft·Full·UserControl AXAML 왕복에 보존
- **Image Source & Rendering 속성**: Image의 로컬 Source 선택·해제, Stretch·StretchDirection, bitmap interpolation·edge·blending 모드를 원자적으로 편집하고 Undo/Redo, 복제, Preview, Source Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **Button Actions & Commands 속성**: Button의 Content·ClickMode·HotKey·Window 기본/취소 동작·CommandParameter·Click 핸들러를 통합 편집하고 Command Binding, Undo/Redo, 복제, Preview, Draft·Full·UserControl AXAML 왕복에 보존
- **선택 요소 AXAML 재사용**: 선택한 컨트롤을 하위 계층·리소스·스타일·바인딩·컨트롤 전용 선언과 함께 독립 UserControl AXAML로 클립보드 복사하거나 파일로 내보냄
- **문서 루트 속성**: Window/UserControl 루트 종류와 Window 제목·리사이즈·시작 위치, 루트 Min/Max 크기를 편집하고 Undo/Redo, Preview, Draft·Full AXAML 왕복에 보존
- **벡터 Shape 편집**: Rectangle, Ellipse, Line, Path의 Fill·Stroke·대시·끝점·결합 스타일과 반지름·점 좌표를 편집하고, 검증된 Path geometry를 리소스·Undo/Redo·복제·미리보기·AXAML 왕복에 보존
- **요소 선택**: 배치된 요소 클릭 시 파란 외곽선
- **키보드 선택 순환**: 캔버스에 포커스가 있을 때 `Tab`/`Shift+Tab`으로 보이는 컨트롤을 순환 선택
- **Object Tree 자동 동기화**: 배치된 요소가 루트(Window) 아래에 추가
- **Object Tree 다중 선택 표시**: 다중 선택 항목에는 `SEL`, 잠긴 항목에는 `LOCK` 마커를 표시하고 헤더에 선택 개수를 표시
- **Object Tree 다중 선택 입력**: 트리에서 일반 클릭은 단일 선택, `Ctrl+클릭`은 선택 항목 추가/해제로 동작
- **Object Tree 컨텍스트 편집**: 트리 행을 우클릭해 Rename·Lock/Unlock·Copy·Cut·Duplicate·Delete 실행
- **Object Tree 계층 편집**: 트리 행의 컨텍스트 메뉴에서 지원 컨테이너 할당과 부모 컨테이너 해제를 바로 실행
- **Object Tree 순서 편집**: StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas 자식의 순서를 `Move Earlier/Later`로 바로 변경
- **Object Tree 탐색 상태**: 트리 노드의 펼침 상태를 계층 재빌드 후에도 보존하고, Canvas에서 자식을 선택하면 접힌 부모 경로를 자동으로 펼침
- **Object Tree 드래그 재배치**: 지원 컨테이너의 자식 행을 같은 부모의 다른 행 앞에 드래그해 순서를 변경하거나 Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas·TabControl·SplitView·Content 컨테이너 행으로 드래그해 부모를 변경하며, 잠긴 대상·순환 계층·가득 찬 슬롯은 거부
- **Object Tree 드롭 피드백**: 드래그 중 유효한 대상 행은 초록색, 거부되는 대상 행은 빨간색으로 강조하고 드롭·취소·트리 이탈 시 상태를 정리
- **Object Tree 삽입 위치 표시**: 같은 부모의 행 위쪽/아래쪽 절반에 드롭하면 앞/뒤 삽입선을 표시하고, 표시된 위치 그대로 StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas 순서를 변경
- **Object Tree 검색 순환**: 검색 결과를 `Enter`로 다음, `Shift+Enter`로 이전 항목으로 순환하고 현재 위치/전체 개수를 표시
- **이동/리사이즈 기즈모**: 선택된 요소를 드래그로 이동, 8방향 핸들로 리사이즈 (최소 10px)
- **PropertyGrid 연동**: 선택된 컨트롤의 속성을 bodong PropertyGrid로 실시간 편집
- **Property Inspector 검색**: 우측 PropertyGrid의 Quick Filter로 선택 컨트롤의 속성 이름을 즉시 필터링
- **Appearance 편집**: 배경·전경·테두리·두께·모서리를 편집하고 Undo/Redo, 미리보기, AXAML 왕복에 보존
- **색상 리소스**: 문서 단위 SolidColorBrush를 편집하고 DynamicResource로 컨트롤에 적용
- **클래스·상태 스타일**: `[Button.primary:pointerover]` 형식의 Setter, 선택 컨트롤별 상태 선택기와 캔버스 배지, 대화형 미리보기, 로컬 속성 우선순위 지원
- **이벤트 선언**: Button Click 핸들러 이름 편집 및 AXAML 내보내기
- **AXAML 워크플로**: Window 저장/열기, UserControl 내보내기, 복사, 현재 문서 유효성 확인과 런타임 미리보기
- **Live Preview**: Preview 창을 한 번 열어두면 컨트롤 배치·속성·Undo/Redo·AXAML 적용과 문서 로드 결과를 별도 창에 자동 반영하고, Preview 창은 하나만 유지
- **Crash-safe 저장**: 기존 AXAML을 `.bak`으로 원자적으로 보존하고 File > Recover Backup...에서 복구하며, 복구 결과를 dirty 상태와 Undo/Redo에 연결
- **디자인 ↔ 소스 왕복 편집**: 전체 Window/UserControl AXAML을 직접 편집하고, 캔버스를 바꾸지 않는 검증·미리보기 후 파일 경로를 유지한 채 단일 Undo/Redo 작업으로 적용
- **도움말**: Help 메뉴에서 실제 편집·저장·Preview·AXAML 작업에 연결된 키보드 단축키와 앱 정보를 확인
- 상태바 피드백

## 사용법

1. 좌측 Toolbox에서 컨트롤 또는 프리셋 선택
2. 중앙 Canvas 영역을 클릭 → 클릭 위치에 기본 크기로 생성
3. 생성된 요소를 클릭하여 선택 → 8개 핸들로 이동/리사이즈
4. 우측 하단 Properties 패널에서 속성 편집
5. ComboBox, AutoCompleteBox, ListBox, ItemsControl, TreeView, Menu, TabControl은 `Edit > Edit Items / Columns...`에서 항목 편집
6. Path는 `Edit > Edit Path Data...`에서 Avalonia geometry mini-language 편집
7. DataGrid는 `Edit > Edit Items / Columns...`에서 `Type | Header | Binding | Width | ReadOnly` 형식으로 열을 편집하고, `Edit > Edit DataGrid Behavior...`에서 표 동작과 크기 정책을 편집
8. 선택 컨트롤은 `Edit > Edit Bindings...`에서 `Property | Path | Mode | Fallback` 형식으로 바인딩 편집
9. `Edit > Edit AXAML Source...`에서 전체 AXAML을 검증·미리보기하고 캔버스에 적용
10. `Edit > Edit Root Properties...`에서 Window/UserControl 루트와 Window 동작·루트 크기 제약 편집
11. `Edit > Edit Sample Data...`에서 JSON 샘플 DataContext를 검증하고 바인딩된 컨트롤에 적용
12. `Edit > Edit Layout Properties...`에서 선택 컨트롤의 Margin·Padding·Alignment·Min/Max 크기 편집
13. `Edit > Edit Typography Properties...`에서 글꼴과 지원 컨트롤의 텍스트 정렬·줄바꿈 편집
14. `Edit > Edit Transform Properties...`에서 선택 컨트롤의 이동·회전·크기·기울기와 변환 기준점 편집
15. `Edit > Edit Accessibility & Navigation...`에서 스크린리더 메타데이터와 키보드 포커스 순서 편집
16. `Edit > Edit Interaction & Rendering...`에서 표시·입력 참여·클리핑·RTL·포인터 커서 편집
17. `Edit > Edit Visual Effects...`에서 선택 컨트롤의 Blur 또는 Drop Shadow 편집
18. `Edit > Edit Range & Value...`에서 Slider·ProgressBar·NumericUpDown의 범위와 타입별 동작 편집
19. `Edit > Edit Text Input...`에서 TextBox와 MaskedTextBox의 공통 입력·multiline·password·undo 정책 편집
20. `Edit > Edit SelectableTextBlock...`에서 텍스트와 선택 브러시·전경색 편집
21. `Edit > Edit SplitView Pane Behavior...`에서 SplitView의 pane 표시 모드·열림 상태·길이·위치·배경 편집
22. `Edit > Edit MaskedTextBox...`에서 Mask·PromptChar·prompt 숨김 정책 편집
23. `Edit > Edit AutoCompleteBox...`에서 입력·자동 완성·filter·지연·drop-down 정책 편집
24. `Edit > Edit Selection Behavior...`에서 ComboBox·ListBox·TreeView의 선택·검색·다중 선택 정책 편집
25. `Edit > Edit Date & Time Input...`에서 DatePicker·CalendarDatePicker·Calendar·TimePicker의 값·범위·선택 모드·표시 형식 편집
26. `Edit > Edit ColorPicker...`에서 색상, 색상 모델, 스펙트럼, 알파, 팔레트와 입력 표시 정책 편집
27. `Edit > Edit Toggle & Choice Behavior...`에서 CheckBox·RadioButton·ToggleSwitch·ToggleButton의 상태·클릭·타입별 콘텐츠 편집
28. `Edit > Edit Disclosure & Scrolling...`에서 Expander의 전개 동작과 ScrollViewer의 scrollbar·snap 정책 편집
29. `Edit > Edit TabControl Behavior...`에서 TabControl 탭 스트립 위치와 선택 콘텐츠 정렬 편집
30. `Edit > Edit Image Source & Rendering...`에서 Image의 파일, 배율, 보간, edge, blending 동작 편집
31. `Edit > Edit Button Actions & Commands...`에서 Button의 포인터·키보드 활성화, Window 기본/취소 역할, command data와 Click 이벤트 편집
32. `File > Copy Selected AXAML`로 선택 컨트롤을 클립보드에 복사하거나 `File > Export Selected AXAML...`로 독립 UserControl AXAML 파일로 내보냄
33. ContentControl과 UserControl은 `Edit > Edit Content...`에서 fallback 텍스트를 편집하거나 `Assign as Container Content...`에서 단일 디자이너 자식을 할당
34. GridSplitter는 `Edit > Edit GridSplitter Behavior...`에서 방향·resize behavior·preview·keyboard/drag 증분을 편집하고 `Assign to Grid Cell...`로 Grid에 배치
35. 같은 root 또는 같은 Canvas 안의 형제 컨트롤을 여러 개 선택한 뒤 `Edit > Group Selected into Canvas`로 묶고, Canvas 그룹을 선택해 `Edit > Ungroup Selected Canvas`로 해제
36. UserControl은 기존 Window/UserControl 문서 안에 중첩 배치할 수 있으며, 단일 Content 자식·fallback 텍스트·Content Binding을 각각 Preview와 생성 AXAML에서 같은 `<UserControl>` 계층으로 확인
37. 여러 root 컨트롤 또는 같은 Canvas의 형제 컨트롤을 선택해 `Edit > Arrange`에서 정렬·분배·크기 맞춤을 실행하고, `Center on Artboard`로 root 컨트롤을 아트보드 중앙에 배치
38. 여러 root 컨트롤 또는 같은 Canvas의 형제 컨트롤을 선택해 `File > Add Selection to Toolbox...`에서 이름을 지정하면 상대 배치와 계층을 Toolbox 프리셋으로 등록하고, 이후 Toolbox에서 반복 배치
39. Toolbox에서 프리셋을 선택해 `File > Export Selected Toolbox Preset...`으로 JSON 팩을 저장하거나 `File > Load Toolbox Preset Pack...`으로 다른 세션에 불러옴
40. View 메뉴에서 `Show Design Guides`, `Snap to Guides`, `Clear Design Guides`로 가이드 보조 기능을 관리하고 `Ctrl+Shift+G`로 전체 가이드를 지움
41. 우측 Properties 패널의 Quick Filter에 `width`, `background`, `font` 같은 속성명을 입력해 편집할 속성만 표시
42. View > Artboard Background > `Custom...`에서 `#RRGGBB` 또는 `#AARRGGBB`를 입력하고 미리보기 후 적용
43. View > Artboard Size > `Custom...`에서 320-3840 x 240-2160 px 크기를 입력하고 미리보기 후 적용
44. Help > `Keyboard Shortcuts...`에서 New·Open·Save·Preview·Undo/Redo·선택·가이드 단축키를 확인하고 Help > `About AvaloniaUIDesigner...`에서 앱 정보를 확인
45. View > Grid Size > `Custom...`에서 4-32 px 간격을 입력하고 미리보기 후 적용
46. AXAML을 두 번 이상 저장한 뒤 File > `Recover Backup...`에서 직전 저장본을 복구하고 필요하면 Undo/Redo로 확인
47. 선택한 컨트롤의 8방향 핸들을 드래그하면 이동과 동일한 Smart Snap 후보에 크기의 해당 모서리가 맞고, 최소 크기 10px 아래로 줄어들지 않음
48. 같은 root 또는 같은 Canvas의 컨트롤을 여러 개 선택한 뒤 bounding box 핸들을 드래그하면 상대 배치와 크기를 함께 조정하며, 부모가 좌표를 관리하는 컨트롤은 기존 전용 편집기를 사용
49. 코너 핸들을 `Shift`와 함께 드래그하면 원래 가로·세로 비율을 유지하며, 잠금 중에는 Smart Snap 기준선보다 비율 보존을 우선함
50. 캔버스에서 텍스트·버튼·토글·라벨·fallback 컨트롤을 더블클릭하면 해당 요소의 visible `Text`/`Content`를 빠르게 편집하고, 적용 시 Undo·AXAML·Preview에 함께 반영
51. View > `Live Preview`를 열어두면 배치·속성·Undo/Redo·AXAML 적용·문서 로드가 같은 Preview 창에 자동 반영되며, 창을 다시 열면 현재 문서로 즉시 갱신
52. Object Tree 검색에서 `Enter`를 누르면 다음 일치 컨트롤, `Shift+Enter`를 누르면 이전 일치 컨트롤을 선택하고 결과 위치를 확인
53. 캔버스를 클릭한 뒤 `Tab`/`Shift+Tab`을 누르면 보이는 컨트롤을 다음/이전 순서로 선택하고, 숨겨진 TabControl 페이지는 자동으로 건너뜀
54. 여러 컨트롤을 선택하면 Object Tree의 각 항목에 `SEL` 마커와 헤더의 선택 개수가 표시되고, 잠긴 컨트롤에는 `LOCK` 마커가 표시됨
55. Object Tree에서 일반 클릭은 하나만 선택하고, `Ctrl+클릭`은 여러 컨트롤을 추가 선택하거나 선택 해제함
56. Object Tree 행을 우클릭하면 해당 컨트롤을 먼저 선택한 뒤 Rename·Lock/Unlock·Copy·Cut·Duplicate·Delete 명령을 실행할 수 있음
57. Object Tree 행의 `Assign to Container`에서 Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas·TabControl·SplitView·Content를 선택하거나 `Remove from Container`로 부모를 해제
58. 컨테이너 자식 행에서 `Move Earlier`/`Move Later`를 선택하면 지원되는 부모의 형제 순서가 바뀌고, Grid·TabControl·Content·SplitView는 전용 배치 방식을 안내함
59. Object Tree에서 컨테이너를 펼치거나 접으면 순서 변경·Undo/Redo·AXAML 적용 뒤에도 상태가 유지되고, Canvas에서 하위 컨트롤을 선택하면 부모가 자동으로 펼쳐짐
60. Object Tree의 잠기지 않은 컨테이너 자식 행을 같은 부모의 다른 자식 행 위로 드래그하면 해당 위치 앞으로 이동하고, 컨테이너 행으로 드래그하면 유효한 부모로 재배치됨
61. Toolbox 컨트롤을 Canvas 위로 드래그하면 포인터 아래 가장 안쪽의 수용 가능한 컨테이너를 강조하고, StackPanel·DockPanel·WrapPanel·UniformGrid의 삽입 위치와 Grid 셀을 포인터에 맞춰 한 번의 Undo 작업으로 배치함
62. 같은 root 또는 같은 Canvas의 형제 컨트롤을 여러 개 선택한 뒤 Edit > `Lay Out Selected`에서 `Horizontally (StackPanel)` 또는 `Vertically (StackPanel)`을 선택하면 새 레이아웃 컨테이너로 감싸고 Object Tree·Preview·Undo/Redo 결과를 확인
63. 같은 선택 상태에서 `Grid (Auto)`를 선택하면 자동 행·열 Grid를 만들고 선택 순서대로 컨트롤을 셀에 배치하며, AXAML 검증에서 `Grid.Row`·`Grid.Column`과 행/열 정의를 확인
64. 같은 선택 상태에서 `UniformGrid (Auto)`를 선택하면 동일 크기 셀과 8px 간격의 UniformGrid를 만들고 Rows·Columns·자식 순서를 AXAML과 Preview에서 확인
65. 레이아웃 컨테이너를 선택한 뒤 Edit > `Break Selected Layout`을 선택하면 컨테이너를 제거하고 자식 컨트롤을 원래 계층·좌표·순서로 복원
66. 같은 선택 상태에서 `DockPanel (Horizontal/Vertical)` 또는 `WrapPanel (Horizontal/Vertical)`을 선택하면 선택 순서 기반 자동 레이아웃을 만들고 도킹 방향·행/열·간격을 AXAML과 Preview에서 확인

DataGrid가 포함된 생성 AXAML을 다른 프로젝트에서 사용할 때는 같은 Avalonia 버전의 `Avalonia.Controls.DataGrid` 패키지와 `avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml` 스타일 include가 필요합니다.

DataGrid Behavior 편집기는 열 정의와 분리된 표 동작을 관리합니다. `HeadersVisibility`, `GridLinesVisibility`, `SelectionMode`, `ClipboardCopyMode`, 열 조작 허용 여부, `FrozenColumnCount`, 행·열 크기, `ColumnWidth`와 축별 scrollbar를 원자적으로 검증하며, `RowHeight=Auto` 같은 Avalonia의 `NaN` 기본값과 `MaxColumnWidth=Infinity`도 AXAML에서 안전하게 보존합니다.

선택 AXAML 내보내기는 선택 컨트롤을 독립 `UserControl` 루트로 감싸며, 선택 루트의 디자인 surface 좌표는 제거하고 크기·이름·스타일·바인딩은 유지합니다. 선택 컨트롤이 컨테이너이면 현재 하위 계층과 DataGrid 열, Menu/TreeView/ItemsControl 항목도 함께 출력하며, 동적 리소스와 문서 스타일을 사용하는 경우 필요한 선언도 포함합니다. 바인딩의 실제 DataContext와 이벤트 핸들러가 있는 코드 영역은 호스트 프로젝트에서 연결해야 합니다.

바인딩 편집기는 선택 타입에서 지원하는 속성을 대화상자에 표시합니다. 생성 AXAML은 ViewModel 타입을 알 수 없는 디자이너 문서가 compiled-bindings 설정과 독립적으로 컴파일되도록 `ReflectionBinding`을 사용합니다.

샘플 DataContext 편집기는 JSON 주석과 trailing comma를 허용하며 저장 시 정규화합니다. 샘플 값은 디자이너 전용 Base64 메타데이터로 보존되어 생성 AXAML의 실제 DataContext를 강제하지 않고, 캔버스·Preview에서만 바인딩 결과를 표시합니다. 샘플을 지우면 바인딩 전 디자인 값과 정적 항목이 복원됩니다.

공통 레이아웃 편집기의 Margin과 Padding은 `8`, `8,12`, `8,12,16,20`처럼 1·2·4개의 값을 사용할 수 있습니다. Padding은 지원 컨트롤에서만 활성화되며 음수 값을 허용하지 않습니다. 최대 너비·높이를 빈 칸으로 적용하면 해당 제한이 제거됩니다.

Typography 편집기는 TextBlock과 텍스트를 표시하는 TemplatedControl의 공통 글꼴을 편집합니다. TextAlignment와 TextWrapping은 해당 속성을 직접 지원하는 TextBlock과 TextBox에서만 활성화됩니다.

Transform 편집기는 이동 값을 픽셀, 회전과 기울기를 도, 기준점을 0~100%로 입력합니다. 변환은 컨트롤의 레이아웃 슬롯을 바꾸지 않으며 AXAML에는 `translate`, `rotate`, `scale`, `skew` 순서로 정규화됩니다. `Reset`은 이동·회전·기울기를 0, 크기를 1, 기준점을 중앙 50%로 되돌립니다. Matrix 변환과 같은 표현 불가능한 소스 속성은 가져오기 경고와 함께 안전하게 무시됩니다.

Accessibility & Navigation 편집기는 Avalonia의 `AutomationProperties`와 키보드 포커스 속성을 한 번에 편집합니다. Heading level `0`은 heading 의미를 사용하지 않으며, Live setting의 `Polite`와 `Assertive`는 동적 변경 알림 우선순위를 지정합니다. Tooltip과 HelpText는 멀티라인 문자열도 AXAML 왕복에서 보존됩니다. 기존 `Edit Tooltip`, `Edit Accessible Name`, `Edit Tab Order` 개별 명령과 값이 동기화됩니다.

Interaction & Rendering 편집기는 컨트롤의 레이아웃 크기를 바꾸지 않고 렌더링과 포인터 입력 동작을 조정합니다. `ClipToBounds`는 Button과 Panel처럼 타입마다 다른 기본값을 그대로 읽고 보존하며, FlowDirection은 좌우 언어 방향을 전환합니다. Cursor는 Avalonia의 표준 Cursor 목록과 `Default`를 지원합니다. 기존 Opacity·Enable·Visibility 개별 명령과 값이 동기화됩니다.

Visual Effects 편집기는 레이아웃 슬롯과 독립적인 Avalonia `Visual.Effect`를 설정합니다. Blur는 `blur(radius)`, Drop Shadow는 `drop-shadow(offsetX offsetY blurRadius color)` AXAML 문법으로 정규화됩니다. Drop Shadow 불투명도는 Avalonia의 단일 속성 문법과 호환되도록 색상 알파 채널에 결합되며, 가져올 때 다시 편집 가능한 불투명도로 복원됩니다. 지원하지 않는 사용자 정의 Effect는 기존 값을 손실하지 않도록 편집을 거부합니다.

Range & Value 편집기는 `Minimum < Maximum`과 범위 안의 `Value`를 원자적으로 검증해 부분 적용을 막습니다. Slider는 Small/LargeChange, 방향 반전, tick 배치와 snap을 지원하고 ProgressBar는 Orientation, IsIndeterminate, 진행 텍스트와 composite format을 지원합니다. NumericUpDown은 빈 Value를 `null`로 처리하며 Increment, .NET 숫자 format, 범위 clip, spin 허용, spinner 표시·위치를 편집합니다. `{`로 시작하는 진행 텍스트 format은 Avalonia XAML의 markup extension과 충돌하지 않도록 출력 시 `{}` 접두사로 이스케이프되고 가져올 때 원문으로 복원됩니다.

Text Input 편집기에서 MaxLength·MinLines·MaxLines의 `0`은 제한 없음 또는 자동 크기를 의미하며, MinLines와 MaxLines가 모두 양수이면 최소 줄 수가 최대 줄 수를 넘을 수 없습니다. `Toggle Multiline TextBox` 명령과 Typography의 TextWrapping·TextAlignment는 같은 TextBox 값에 동기화됩니다. PasswordChar를 설정하면 디자이너는 입력된 디자인 텍스트를 즉시 비우고 스냅샷·Preview·AXAML에 정적 Text를 기록하지 않으며, 런타임 Text Binding은 그대로 보존할 수 있습니다.

SelectableTextBlock 편집기는 `Text`와 선택 시각 상태를 분리해 관리합니다. `SelectionBrush`와 `SelectionForegroundBrush`는 solid color 또는 `Transparent`로 정규화하며, `SelectionStart`와 `SelectionEnd`는 사용자의 Preview 상호작용 상태이므로 문서에 저장하지 않습니다. `Text`가 Binding이면 생성 AXAML은 정적 텍스트를 중복 출력하지 않습니다.

SplitView Pane Behavior 편집기는 `Inline`·`CompactInline`·`Overlay`·`CompactOverlay` 표시 모드와 `IsPaneOpen`, `OpenPaneLength`, `CompactPaneLength`, `PanePlacement`를 검증합니다. `PaneBackground`는 solid color 또는 `Transparent`로 편집하며, 기존 `DynamicResource` 표현식은 AXAML 왕복에서 보존됩니다. Pane와 Content의 실제 디자이너 자식은 `Assign to SplitView...`에서 별도로 배치합니다.

TabControl Behavior 편집기는 `TabStripPlacement`를 `Top`·`Bottom`·`Left`·`Right` 중에서 선택하고, 선택 탭 콘텐츠의 가로·세로 정렬을 독립적으로 설정합니다. 기존 탭 항목과 탭별 단일 자식, `SelectedIndex` Binding은 유지하면서 새 값은 Canvas·Preview·AXAML 왕복에 함께 반영합니다.

MaskedTextBox 편집기는 .NET `MaskedTextProvider`로 Mask를 검증하고, `0`·`9`·`L`·`?` 같은 mask token과 literal 문자를 그대로 AXAML에 보존합니다. 공통 Text Input 편집기에서 inherited TextBox 속성을 함께 조정할 수 있으며, `Text`가 Binding이면 정적 텍스트를 중복 출력하지 않습니다.

AutoCompleteBox 편집기는 정적 `ItemsSource` suggestion을 `Edit Items / Columns...`에서 관리하고, `MinimumPopulateDelay`를 밀리초 또는 invariant TimeSpan으로 입력받습니다. AXAML은 `AvaloniaList<Object>` property element로 정적 suggestions를 보존하며, `Text`가 Binding이면 디자인 텍스트를 중복 출력하지 않습니다. `AsyncPopulator`, `ItemSelector`, `TextSelector`는 임의 델리게이트를 생성하지 않고 원본 코드에서 연결하도록 남깁니다.

ItemsControl은 문자열 항목을 `Edit Items / Columns...`에서 한 줄씩 편집합니다. 정적 `Items`는 `<ItemsControl.Items>`와 `x:String`으로 출력하고, `ItemsSource` Binding이 있으면 정적 항목을 중복 출력하지 않습니다. 복잡한 item template와 데이터 모델은 원본 AXAML 또는 ViewModel 영역에서 연결하도록 남깁니다.

Selection Behavior 편집기의 SelectedIndex `-1`은 선택 없음을 의미하며 정적 항목 범위를 벗어날 수 없습니다. ListBox와 TreeView의 SelectionMode는 Multiple·Toggle·AlwaysSelected를 독립적으로 조합하고, 항목이 있는 ListBox에서 AlwaysSelected를 켜면 유효한 선택 인덱스가 필요합니다. Editable ComboBox는 선택된 항목이 있으면 Text를 해당 항목에서 파생하고, 자유 입력 Text를 설정하면 SelectedIndex를 `-1`로 사용합니다. 소스의 ItemsSource가 Binding이면 디자인 시점에 항목 수를 알 수 없으므로 상한 검증을 런타임 데이터에 맡깁니다.

Date & Time Input 편집기는 날짜를 `yyyy-MM-dd`, 시간을 `HH:mm` 또는 `HH:mm:ss`로 입력합니다. DatePicker는 MinYear·MaxYear와 SelectedDate를 함께 검증하고 날짜 구성 요소를 모두 숨기는 설정을 거부합니다. CalendarDatePicker는 선택·표시 날짜가 DisplayDateStart·DisplayDateEnd 범위 안에 있는지 확인하며 Custom 형식일 때 유효한 .NET 날짜 format을 요구합니다. Calendar는 `SingleDate`·`SingleRange`·`MultipleRange`·`None` 선택 모드와 `Month`·`Year`·`Decade` 표시 모드를 지원하며, `None`에서는 SelectedDate를 함께 지정할 수 없습니다. TimePicker의 분·초 증분은 1~59이고 시계는 `12HourClock` 또는 `24HourClock`을 사용합니다. `{`로 시작하는 날짜 format은 AXAML 출력에서 `{}` 접두사로 이스케이프되고 가져올 때 원문으로 복원됩니다.

ColorPicker 편집기는 색상을 `#AARRGGBB` 형식으로 정규화하고 Rgba/Hsva 모델, 스펙트럼 구성·모양, 알파 위치와 팔레트·스펙트럼·컴포넌트·hex 입력 표시를 함께 편집합니다. PaletteColors 같은 컬렉션은 별도 편집 대상이며 이번 워크플로에서는 변경하지 않습니다. 알파가 비활성화된 Avalonia ColorPicker는 색상 알파를 `FF`로 정규화할 수 있으므로, 디자이너는 해당 런타임 동작을 그대로 보존합니다.

Toggle & Choice Behavior 편집기의 Indeterminate 상태는 three-state가 활성화된 경우에만 적용되며 AXAML에서는 `IsChecked="{x:Null}"`로 보존됩니다. ClickMode는 포인터를 놓을 때 실행하는 `Release`와 누르는 즉시 실행하는 `Press`를 지원합니다. RadioButton은 GroupName으로 상호 배타 그룹을 구성하고 ToggleSwitch는 상태와 별도로 OnContent·OffContent 표시 문구를 편집합니다. Content 또는 IsChecked에 Binding이 있으면 생성 AXAML은 해당 정적 값을 중복 출력하지 않습니다.

Disclosure & Scrolling 편집기는 `Edit Content...` 또는 Content 할당으로 구성한 실제 자식 계층을 변경하지 않고 컨테이너 동작만 편집합니다. Expander는 Down·Up·Left·Right 방향과 콘텐츠 정렬을 지원하며 IsExpanded Binding이 있으면 정적 값을 중복 출력하지 않습니다. ScrollViewer는 축마다 Disabled·Auto·Hidden·Visible scrollbar를 선택하고, 부모로의 scroll chaining과 thumb drag 중 deferred scrolling, 포커스 이동 시 bring-into-view를 제어합니다. Snap points는 축마다 None·Mandatory·MandatorySingle 타입과 Near·Center·Far 정렬을 설정합니다.

ContentControl과 UserControl은 Toolbox에서 배치한 뒤 `Edit > Edit Content...`로 fallback TextBlock을 편집하거나 `Assign as Container Content...`로 실제 단일 자식을 연결합니다. UserControl은 ContentControl과 구별되는 `<UserControl>` 태그를 유지하면서도 같은 단일 Content 슬롯 규칙을 사용합니다. 디자이너 자식이 연결되면 fallback 텍스트는 중복 출력하지 않으며, Content Binding이 있으면 정적 콘텐츠도 출력하지 않습니다. 두 모드는 Canvas·Object Tree·Undo/Redo·Preview·Draft·Full·UserControl AXAML 왕복에서 같은 계층으로 유지됩니다.

GridSplitter Behavior 편집기는 `ResizeDirection`의 Auto·Columns·Rows, `ResizeBehavior`의 BasedOnAlignment·CurrentAndNext·PreviousAndCurrent·PreviousAndNext, `ShowsPreview`, `KeyboardIncrement`, `DragIncrement`를 검증합니다. 실제 splitter는 `Assign to Grid Cell...`로 Grid 행/열에 배치하며, 생성 AXAML은 Grid attached properties와 GridSplitter 동작 속성을 함께 보존합니다.

Canvas 그룹화는 같은 부모를 공유하는 형제 컨트롤만 대상으로 합니다. root 컨트롤을 묶으면 bounding box 위치에 새 Canvas가 생성되고, Canvas 자식을 묶으면 원래 Canvas 안에 중첩됩니다. 그룹을 해제해도 자식의 화면 좌표와 형제 z-order를 유지하며, 서로 다른 Grid·StackPanel·Content 슬롯을 섞는 작업은 레이아웃 좌표 손실을 막기 위해 거부합니다.

다중 선택 Arrange는 root 요소 또는 같은 Canvas의 직접 자식만 대상으로 합니다. Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·TabControl·SplitView·Content 자식은 부모가 좌표와 크기를 관리하므로 정렬을 거부하고, 해당 컨테이너의 전용 할당·순서 편집기를 사용하도록 안내합니다. Canvas 형제의 정렬은 `Canvas.Left`·`Canvas.Top` 로컬 좌표를 갱신하므로 부모를 이동해도 정렬 결과가 유지됩니다.

다중 선택 `Edit > Lay Out Selected`는 같은 root 또는 같은 Canvas의 잠금 해제된 형제 컨트롤을 새 StackPanel로 감쌉니다. `Horizontally`는 기존 너비를 주축 크기로, `Vertically`는 기존 높이를 주축 크기로 사용하고 8px 간격을 적용합니다. 새 컨테이너는 선택 영역 위치에 배치되며 Canvas 안에서 실행해도 자식의 화면 위치와 부모 상대 좌표를 보존하고, 전체 작업은 하나의 Undo 항목으로 기록됩니다.

같은 메뉴의 `Grid (Auto)`는 선택 개수의 제곱근을 기준으로 행·열을 만들고 선택 순서대로 셀을 채웁니다. 새 Grid는 선택 영역의 좌상단에 배치되며 Canvas 자식으로 실행할 때 부모 상대 좌표와 형제 순서를 보존하고, 각 컨트롤의 `Grid.Row`·`Grid.Column`과 Grid definitions를 AXAML에 출력합니다.

`UniformGrid (Auto)`는 같은 자동 행·열 계산을 사용하되 모든 셀을 같은 크기로 만들고 8px의 RowSpacing·ColumnSpacing을 적용합니다. 선택 순서와 Canvas 부모 상대 좌표를 보존하며, Rows·Columns·FirstColumn·spacing과 자식 순서를 AXAML에 기록합니다.

`DockPanel (Horizontal/Vertical)`은 선택 순서대로 가로 방향에는 `Left`, 세로 방향에는 `Top`으로 도킹하고 마지막 자식은 `LastChildFill`로 남겨 선택 컨트롤의 원래 주축 크기를 유지합니다. `WrapPanel (Horizontal/Vertical)`은 선택 컨트롤 중 가장 큰 크기를 항목 크기로 사용하고 8px의 항목·줄 간격과 제곱근 기반 행·열을 자동 계산합니다. 두 레이아웃 모두 root 또는 같은 Canvas 형제에서 실행할 수 있으며, Canvas 안에서는 컨테이너의 상대 좌표와 형제 순서를 보존하고 Undo/Redo·Preview·AXAML 왕복을 지원합니다.

`Break Selected Layout`은 선택한 컨테이너를 제거하고 직접 자식들을 root 또는 원래 Canvas에 다시 연결합니다. Grid·StackPanel·DockPanel·WrapPanel·UniformGrid는 현재 화면 bounds를 유지하고, Canvas 안의 레이아웃은 부모 상대 좌표와 형제 순서를 유지하며 해제된 자식들을 다시 선택합니다.

선택 영역 Toolbox 프리셋은 두 개 이상의 root 컨트롤 또는 같은 Canvas의 직접 형제 컨트롤을 선택한 뒤 `File > Add Selection to Toolbox...`에서 등록합니다. 선택 영역의 좌상단을 기준으로 상대 좌표를 저장하고, root 선택은 컨트롤 목록으로, Canvas 형제 선택은 bounding box Canvas와 로컬 자식 계층으로 보존합니다. 등록 시점의 크기·시각 속성·텍스트·콘텐츠도 함께 복원하며, 배치 후 생성된 Canvas와 자식 컨트롤을 모두 선택합니다. 등록된 프리셋은 현재 세션의 Toolbox 검색 결과에 즉시 나타나며, `File > Export Selected Toolbox Preset...`으로 `*.toolbox-preset.json` 팩을 저장할 수 있습니다. `File > Load Toolbox Preset Pack...`은 JSON 문법·중복 이름·지원 타입·bounds·root/Canvas 계층을 검증한 뒤 일괄 등록하므로 실패한 팩이 Toolbox를 부분적으로 변경하지 않습니다. Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·TabControl·SplitView·Content 자식은 현재 프리셋 배치 경로가 부모 전용 메타데이터를 복원하지 않으므로 등록을 거부합니다. 예시는 [toolbox-preset.example.json](docs/toolbox-preset.example.json)을 참고하세요.

컨테이너를 단독 선택한 상태에서 `Edit > Copy`, `Cut`, `Paste`, `Duplicate`를 실행하면 선택된 컨테이너의 모든 하위 요소를 하나의 계층으로 처리합니다. Canvas의 로컬 좌표와 Grid·StackPanel·TabControl·Content 등의 부모 배치 메타데이터를 유지하며, 계층 밖의 부모를 참조하는 자식은 기존 부모 참조를 유지합니다. 잘라내기는 잠긴 하위 요소가 포함된 계층을 거부해 부분 삭제를 방지합니다.

Image Source & Rendering 편집기는 로컬 파일 경로와 `file://` URI를 지원하며 빈 Source를 적용하면 현재 이미지를 해제합니다. 존재하지 않거나 디코딩할 수 없는 파일은 기존 Source와 렌더링 상태를 변경하지 않지만, 가져온 AXAML에서 파일을 찾을 수 없는 경우에는 Source 메타데이터를 보존해 프로젝트 이동 후 다시 연결할 수 있습니다. Stretch와 StretchDirection 외에 bitmap interpolation 품질, antialias/aliased edge, compositing blending 모드를 설정할 수 있고 Source Binding이 있으면 생성 AXAML은 정적 Source를 중복 출력하지 않습니다.

Button Actions & Commands 편집기는 Release·Press ClickMode와 Avalonia key gesture 형식의 HotKey를 지원하고, host Window의 기본·취소 동작을 지정합니다. CommandParameter는 정적 문자열로 설정하거나 `Edit Bindings...`에서 ViewModel 경로에 연결할 수 있으며 Command 자체도 Binding으로 선언합니다. Content나 CommandParameter에 Binding이 있으면 생성 AXAML은 정적 값을 중복 출력하지 않고, Click 핸들러와 Command는 같은 Button에 함께 선언할 수 있습니다. 기존 `Edit Click Handler...` 명령은 같은 이벤트 메타데이터를 편집합니다.

문서 루트 편집기에서 선택한 종류는 일반 저장과 Full AXAML 복사에 반영됩니다. UserControl은 Window 전용 속성을 사용하지 않으며, `File > Export UserControl AXAML...`은 현재 문서 종류와 관계없이 재사용 가능한 UserControl을 생성합니다.

AXAML 소스 편집기의 `Validate`와 `Preview`는 현재 디자인과 Undo 스택을 변경하지 않습니다. `Apply`는 파싱에 성공한 문서만 반영하며, 현재 저장 경로를 유지하고 전체 변경을 한 번의 Undo/Redo 작업으로 기록합니다.

캔버스 뷰포트는 아트보드의 원본 레이아웃 크기와 렌더 줌을 분리해 유지합니다. `Zoom In`·`Zoom Out`을 실행하면 ScrollViewer의 실제 콘텐츠 영역도 같은 배율로 갱신되므로 확대된 컨트롤을 가로·세로 스크롤하며 편집할 수 있고, `Fit to View`는 현재 뷰포트 크기를 기준으로 배율을 계산합니다. 중간 마우스 드래그는 선택·이동과 분리된 팬 입력으로 ScrollViewer 오프셋만 이동하고, Ctrl+휠은 포인터 아래의 아트보드 좌표를 고정한 채 확대·축소하므로 둘 다 문서 Undo 기록을 만들지 않습니다. 가로·세로 디자인 룰러는 같은 오프셋과 배율을 사용해 화면 가장자리의 눈금과 라벨을 실제 아트보드 좌표로 표시하며, 포인터가 뷰포트 안에 있을 때는 청록색 기준선으로 해당 좌표를 강조합니다. 룰러에서 만든 가이드라인은 주황색으로 캔버스에 표시되고 이동·리사이즈 시 Smart Snap에 참여합니다. 가이드는 문서 AXAML과 Undo 스택에 포함되지 않는 작업 보조 상태이며 새 문서·AXAML을 열면 초기화됩니다.

## 로드맵

- ~~v0.3: 이동/리사이즈 기즈모~~ ✅
- ~~v0.4: bodong PropertyGrid 실제 연동~~ ✅
- ~~v0.5: .axaml 저장/로드~~ ✅
- ~~v0.6: 실제 드래그&드롭, 삭제, 언두~~ ✅

## 컴포넌트 팩

`File > Load Component Pack...`에서 JSON 팩을 불러오면 현재 세션의 Toolbox에 별칭 컨트롤을 추가할 수 있습니다. 각 항목은 이미 지원되는 Avalonia 타입을 기반으로 하며, 표시 이름, 기본 크기, 기본 속성을 지정합니다. 예시는 [component-pack.example.json](docs/component-pack.example.json)을 참고하세요. 캔버스에서 컨트롤 하나를 선택한 뒤 `File > Export Selected as Component Pack...`을 사용하면 해당 크기와 시각 속성을 재사용 가능한 JSON 팩으로 저장할 수 있습니다.

컴포넌트 팩은 단일 컨트롤 정의를 공유하고, Toolbox 프리셋 팩은 여러 root 컨트롤의 상대 배치와 시각 상태를 공유합니다. 두 JSON 팩은 서로 다른 메뉴와 스키마를 사용하므로 프리셋 레이아웃을 컴포넌트 팩으로 불러오지 않습니다.

`File > Export UserControl AXAML...`은 현재 캔버스를 재사용 가능한 `UserControl` 레이아웃으로 내보냅니다. 코드비하인드를 추가할 때는 생성된 루트에 프로젝트의 `x:Class`를 지정하면 됩니다.
