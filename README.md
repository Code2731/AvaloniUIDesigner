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
- **배치**: 클릭-투-플레이스와 드래그 앤 드롭으로 실제 Avalonia 컨트롤 생성
- **컨테이너 편집**: Grid 셀, StackPanel 순서·주축 크기, DockPanel 순서·방향·크기·LastChildFill, WrapPanel 순서·방향·항목 크기·간격·정렬, UniformGrid 순서·행·열·첫 열·간격, 중첩 Canvas 로컬 좌표·직접 변형·z-order, TabControl 탭 정의·탭별 단일 자식·활성 페이지, SplitView Pane·Content 슬롯·Inline/Overlay·배치 방향, Border·ScrollViewer·Expander의 단일 Content 자식을 편집하고 재귀 Object Tree·AXAML·미리보기에 보존
- **계층 항목 편집**: TreeView 항목을 `[-]`(펼침), `[+]`(접힘), 두 칸 들여쓰기 문법으로 편집하고 Undo/Redo, 복제, 미리보기, AXAML 왕복에 보존
- **메뉴 구조 편집**: Menu 항목을 두 칸 들여쓰기로 중첩하고 `---` 구분선, `[x]/[ ]` 체크, `(x)/( )` 라디오와 `{Group}`, `| Ctrl+N` 표시·실행 단축키를 편집해 Undo/Redo, 복제, 미리보기, AXAML 왕복에 보존
- **DataGrid 열 설계**: Text·CheckBox 열의 Header·Binding·Width·ReadOnly를 편집하고 샘플 행, Undo/Redo, 복제, 미리보기, AXAML 왕복에 보존
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
- **Selection Behavior 속성**: ComboBox·ListBox·TreeView의 선택·검색·자동 스크롤 정책과 ComboBox editable/placeholder/drop-down 표현, ListBox·TreeView 다중 선택 모드를 편집하고 Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **Date & Time Input 속성**: DatePicker의 날짜 범위·구성 요소·표시 형식, CalendarDatePicker의 표시 범위·선택 형식·watermark, TimePicker의 시간·증분·12/24시간·초 표시를 원자적으로 편집하고 Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **Toggle & Choice Behavior 속성**: CheckBox·RadioButton·ToggleSwitch·ToggleButton의 checked/unchecked/indeterminate 상태, three-state, ClickMode, 콘텐츠 정렬과 Radio 그룹·Switch On/Off 문구를 편집하고 Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **Disclosure & Scrolling 속성**: Expander의 header·expanded 상태·전개 방향·콘텐츠 정렬과 ScrollViewer의 양축 scrollbar·auto-hide·chaining·deferred/focus scrolling·snap 정책을 편집하고 실제 중첩 Content, Undo/Redo, 복제, Preview, Binding, Draft·Full·UserControl AXAML 왕복에 보존
- **문서 루트 속성**: Window/UserControl 루트 종류와 Window 제목·리사이즈·시작 위치, 루트 Min/Max 크기를 편집하고 Undo/Redo, Preview, Draft·Full AXAML 왕복에 보존
- **벡터 Shape 편집**: Rectangle, Ellipse, Line, Path의 Fill·Stroke·대시·끝점·결합 스타일과 반지름·점 좌표를 편집하고, 검증된 Path geometry를 리소스·Undo/Redo·복제·미리보기·AXAML 왕복에 보존
- **요소 선택**: 배치된 요소 클릭 시 파란 외곽선
- **Object Tree 자동 동기화**: 배치된 요소가 루트(Window) 아래에 추가
- **이동/리사이즈 기즈모**: 선택된 요소를 드래그로 이동, 8방향 핸들로 리사이즈 (최소 10px)
- **PropertyGrid 연동**: 선택된 컨트롤의 속성을 bodong PropertyGrid로 실시간 편집
- **Appearance 편집**: 배경·전경·테두리·두께·모서리를 편집하고 Undo/Redo, 미리보기, AXAML 왕복에 보존
- **색상 리소스**: 문서 단위 SolidColorBrush를 편집하고 DynamicResource로 컨트롤에 적용
- **클래스·상태 스타일**: `[Button.primary:pointerover]` 형식의 Setter, 선택 컨트롤별 상태 선택기와 캔버스 배지, 대화형 미리보기, 로컬 속성 우선순위 지원
- **이벤트 선언**: Button Click 핸들러 이름 편집 및 AXAML 내보내기
- **AXAML 워크플로**: Window 저장/열기, UserControl 내보내기, 복사, 현재 문서 유효성 확인과 런타임 미리보기
- **디자인 ↔ 소스 왕복 편집**: 전체 Window/UserControl AXAML을 직접 편집하고, 캔버스를 바꾸지 않는 검증·미리보기 후 파일 경로를 유지한 채 단일 Undo/Redo 작업으로 적용
- 상태바 피드백

## 사용법

1. 좌측 Toolbox에서 컨트롤 또는 프리셋 선택
2. 중앙 Canvas 영역을 클릭 → 클릭 위치에 기본 크기로 생성
3. 생성된 요소를 클릭하여 선택 → 8개 핸들로 이동/리사이즈
4. 우측 하단 Properties 패널에서 속성 편집
5. ComboBox, ListBox, TreeView, Menu, TabControl은 `Edit > Edit Items / Columns...`에서 항목 편집
6. Path는 `Edit > Edit Path Data...`에서 Avalonia geometry mini-language 편집
7. DataGrid는 `Edit > Edit Items / Columns...`에서 `Type | Header | Binding | Width | ReadOnly` 형식으로 열 편집
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
19. `Edit > Edit Text Input...`에서 TextBox의 입력·multiline·password·undo 정책 편집
20. `Edit > Edit Selection Behavior...`에서 ComboBox·ListBox·TreeView의 선택·검색·다중 선택 정책 편집
21. `Edit > Edit Date & Time Input...`에서 DatePicker·CalendarDatePicker·TimePicker의 값·범위·표시 형식 편집
22. `Edit > Edit Toggle & Choice Behavior...`에서 CheckBox·RadioButton·ToggleSwitch·ToggleButton의 상태·클릭·타입별 콘텐츠 편집
23. `Edit > Edit Disclosure & Scrolling...`에서 Expander의 전개 동작과 ScrollViewer의 scrollbar·snap 정책 편집

DataGrid가 포함된 생성 AXAML을 다른 프로젝트에서 사용할 때는 같은 Avalonia 버전의 `Avalonia.Controls.DataGrid` 패키지와 `avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml` 스타일 include가 필요합니다.

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

Selection Behavior 편집기의 SelectedIndex `-1`은 선택 없음을 의미하며 정적 항목 범위를 벗어날 수 없습니다. ListBox와 TreeView의 SelectionMode는 Multiple·Toggle·AlwaysSelected를 독립적으로 조합하고, 항목이 있는 ListBox에서 AlwaysSelected를 켜면 유효한 선택 인덱스가 필요합니다. Editable ComboBox는 선택된 항목이 있으면 Text를 해당 항목에서 파생하고, 자유 입력 Text를 설정하면 SelectedIndex를 `-1`로 사용합니다. 소스의 ItemsSource가 Binding이면 디자인 시점에 항목 수를 알 수 없으므로 상한 검증을 런타임 데이터에 맡깁니다.

Date & Time Input 편집기는 날짜를 `yyyy-MM-dd`, 시간을 `HH:mm` 또는 `HH:mm:ss`로 입력합니다. DatePicker는 MinYear·MaxYear와 SelectedDate를 함께 검증하고 날짜 구성 요소를 모두 숨기는 설정을 거부합니다. CalendarDatePicker는 선택·표시 날짜가 DisplayDateStart·DisplayDateEnd 범위 안에 있는지 확인하며 Custom 형식일 때 유효한 .NET 날짜 format을 요구합니다. TimePicker의 분·초 증분은 1~59이고 시계는 `12HourClock` 또는 `24HourClock`을 사용합니다. `{`로 시작하는 날짜 format은 AXAML 출력에서 `{}` 접두사로 이스케이프되고 가져올 때 원문으로 복원됩니다.

Toggle & Choice Behavior 편집기의 Indeterminate 상태는 three-state가 활성화된 경우에만 적용되며 AXAML에서는 `IsChecked="{x:Null}"`로 보존됩니다. ClickMode는 포인터를 놓을 때 실행하는 `Release`와 누르는 즉시 실행하는 `Press`를 지원합니다. RadioButton은 GroupName으로 상호 배타 그룹을 구성하고 ToggleSwitch는 상태와 별도로 OnContent·OffContent 표시 문구를 편집합니다. Content 또는 IsChecked에 Binding이 있으면 생성 AXAML은 해당 정적 값을 중복 출력하지 않습니다.

Disclosure & Scrolling 편집기는 `Edit Content...` 또는 Content 할당으로 구성한 실제 자식 계층을 변경하지 않고 컨테이너 동작만 편집합니다. Expander는 Down·Up·Left·Right 방향과 콘텐츠 정렬을 지원하며 IsExpanded Binding이 있으면 정적 값을 중복 출력하지 않습니다. ScrollViewer는 축마다 Disabled·Auto·Hidden·Visible scrollbar를 선택하고, 부모로의 scroll chaining과 thumb drag 중 deferred scrolling, 포커스 이동 시 bring-into-view를 제어합니다. Snap points는 축마다 None·Mandatory·MandatorySingle 타입과 Near·Center·Far 정렬을 설정합니다.

문서 루트 편집기에서 선택한 종류는 일반 저장과 Full AXAML 복사에 반영됩니다. UserControl은 Window 전용 속성을 사용하지 않으며, `File > Export UserControl AXAML...`은 현재 문서 종류와 관계없이 재사용 가능한 UserControl을 생성합니다.

AXAML 소스 편집기의 `Validate`와 `Preview`는 현재 디자인과 Undo 스택을 변경하지 않습니다. `Apply`는 파싱에 성공한 문서만 반영하며, 현재 저장 경로를 유지하고 전체 변경을 한 번의 Undo/Redo 작업으로 기록합니다.

## 로드맵

- ~~v0.3: 이동/리사이즈 기즈모~~ ✅
- ~~v0.4: bodong PropertyGrid 실제 연동~~ ✅
- ~~v0.5: .axaml 저장/로드~~ ✅
- ~~v0.6: 실제 드래그&드롭, 삭제, 언두~~ ✅

## 컴포넌트 팩

`File > Load Component Pack...`에서 JSON 팩을 불러오면 현재 세션의 Toolbox에 별칭 컨트롤을 추가할 수 있습니다. 각 항목은 이미 지원되는 Avalonia 타입을 기반으로 하며, 표시 이름, 기본 크기, 기본 속성을 지정합니다. 예시는 [component-pack.example.json](docs/component-pack.example.json)을 참고하세요. 캔버스에서 컨트롤 하나를 선택한 뒤 `File > Export Selected as Component Pack...`을 사용하면 해당 크기와 시각 속성을 재사용 가능한 JSON 팩으로 저장할 수 있습니다.

`File > Export UserControl AXAML...`은 현재 캔버스를 재사용 가능한 `UserControl` 레이아웃으로 내보냅니다. 코드비하인드를 추가할 때는 생성된 루트에 프로젝트의 `x:Class`를 지정하면 됩니다.
