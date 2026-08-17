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
- **Toolbox**: 내장 컨트롤, 복합 프리셋, JSON 컴포넌트 팩. 외부 팩 파일 경로도 워크스페이스 세션에 기록해 재시작 후 자동 복원
- **Toolbox 카테고리 UX**: 내장 컨트롤을 Layout·Containers·Input·Display·Shapes로 자동 분류하고, 우선순위별로 정렬한 접기/펼치기 그룹과 카드의 카테고리 칩·Avalonia 타입 힌트를 표시하며, Component Pack의 선택적 `category` 메타데이터와 이름/타입 검색을 함께 적용
- **Toolbox Recent & Favorites**: Canvas에 배치한 최근 8개 항목을 `Recent` 그룹에 표시하고 별표로 즐겨찾기를 고정하며, 두 상태를 Workspace Session에 저장·복원
- **Toolbox 키보드 작업**: 검색창 `Enter`로 첫 결과를 아트보드 중앙에 빠르게 배치하고, `Escape`로 일반 검색어를 초기화하며, 카테고리 내부 ListBox에서 방향키로 이동한 뒤 `Enter`로 배치·`Space`로 즐겨찾기를 토글
- **Toolbox 배치 모드**: `Ctrl+Alt+T`로 검색창에 포커스하고 `Ctrl+Alt+P` 또는 Toolbox 헤더의 `Pick`으로 첫 검색 결과를 선택해 배치 모드를 시작하며, 헤더 `Cancel`·`Ctrl+Alt+P` 재입력·`Escape`로 종료
- **Toolbox 배치 미리보기**: 배치 모드에서 Canvas 위에 항목의 실제 기본 크기·그리드 스냅 좌표를 ghost로 표시하고, 클릭 시 미리 본 위치에 배치하며 Canvas 밖·드래그 작업에서는 미리보기를 숨김
- **Toolbox 드래그 viewport 추적**: 큰 아트보드에서 Toolbox 항목을 드래그해 viewport 가장자리에 머물면 ScrollViewer가 수평·수직으로 자동 이동하고, 이동한 좌표에서 target preview·drop 검증을 다시 계산
- **편집 drag viewport 추적**: 요소 이동·리사이즈와 marquee 선택 중에도 viewport 가장자리 auto-pan을 사용해 큰 아트보드의 보이지 않는 영역까지 연속 편집
- **가이드 drag viewport 추적**: ruler에서 만든 수평·수직 guide를 viewport 가장자리까지 끌면 auto-pan하고, 새 offset의 실제 아트보드 좌표에 guide를 계속 배치
- **Toolbox 배치 viewport 추적**: `Ctrl+Alt+P` 배치 모드에서 빈 Canvas 가장자리로 ghost를 이동하면 ScrollViewer가 자동 이동하고, 새 offset의 좌표로 placement preview를 다시 계산
- **Toolbox 컨테이너 배치**: 배치 모드에서 포인터 아래 수용 가능한 컨테이너를 주황색 outline으로 강조하고, 클릭하면 Grid 셀·StackPanel/DockPanel/WrapPanel/UniformGrid 순서·Canvas 상대 좌표·Content 슬롯 규칙을 사용해 해당 컨테이너에 삽입
- **Toolbox 정밀 target 피드백**: Grid·UniformGrid는 예상 셀 bounds, StackPanel·DockPanel·WrapPanel은 삽입선, TabControl·SplitView·Content는 슬롯 bounds를 표시하고 ghost 상세에 target label을 함께 표시
- **Component Pack Plugins**: `IComponentPackPlugin`을 구현한 외부 DLL을 `File > Load Component Pack Plugin...`에서 로드하고, 플러그인 경로를 세션에 저장해 Toolbox 정의를 재사용
- **Component Pack 관리**: `File > Manage Component Packs...`에서 JSON/DLL 팩의 출처·컴포넌트 목록을 확인하고 Toolbox에서 제거하며, 현재 문서가 사용하는 타입은 디자인 전용 placeholder로 보존
- **Custom Control Metadata**: `DesignOnly: true` 컴포넌트 팩으로 외부 Avalonia 타입을 디자인 타임 플레이스홀더로 등록하고, 커스텀 기본 속성·Preview 문구·AXAML 타입명을 보존
- **선택 영역 Toolbox 프리셋**: 여러 root 컨트롤을 상대 좌표·현재 속성과 함께 Toolbox에 등록하고 JSON 팩으로 저장·불러오기
- **배치**: 클릭-투-플레이스와 드래그 앤 드롭으로 실제 Avalonia 컨트롤 생성
- **캔버스 뷰포트**: 큰 아트보드와 확대 상태를 양축 자동 스크롤로 탐색하고, Desktop·Tablet·Mobile·사용자 지정 아트보드 크기와 회전, Zoom In/Out·Actual Size·Fit to View·Fit Selected to View·25~200% Zoom Presets와 스크롤 콘텐츠 크기를 동기화하며 `Ctrl+=`/`Ctrl+-`/`Ctrl+0`/`F`/`Ctrl+Shift+F` 단축키, `Ctrl+Alt+Arrow` 키보드 팬, 중간 마우스 드래그 팬, Ctrl+휠 포인터 중심 줌, 키보드·View 메뉴·Zoom Preset viewport 중심 줌, 아트보드 크기 변경 시 문서 중심 보존을 지원
- **임시 viewport 팬**: Canvas·요소 위에서 `Space`를 누른 채 왼쪽 드래그하면 선택·배치를 잠시 멈추고 viewport를 이동하며, 중간 마우스 드래그 팬과 함께 사용할 수 있고 Space 해제 시 포인터 캡처를 안전하게 종료
- **선택 영역 viewport 맞춤**: `Ctrl+Shift+F` 또는 View/context menu의 `Fit Selected to View`로 하나 이상의 선택 컨트롤을 안전한 여백과 함께 확대해 viewport 중앙에 맞추고, 큰 아트보드의 선택 위치까지 offset을 동기화
- **아트보드 배경**: White·Soft Gray·Ink 프리셋과 사용자 지정 `#RRGGBB`/`#AARRGGBB` 색상을 편집하고 Undo·Preview·AXAML 왕복에 보존
- **디자인 룰러**: 가로·세로 눈금을 ScrollViewer 오프셋과 렌더 줌에 동기화하고 포인터 기준선을 표시해 현재 화면의 아트보드 좌표를 확인
- **디자인 가이드**: 가로·세로 룰러에서 드래그해 가이드라인을 만들고 캔버스 이동·리사이즈 Smart Snap 후보로 사용하며, 캔버스 밖으로 드래그하면 제거합니다. View 메뉴에서 표시·가이드 스냅을 각각 끄거나 전체 가이드를 지울 수 있습니다.
- **리사이즈 Smart Snap**: 8방향 핸들로 크기를 조정할 때 아트보드 경계·중앙선, 디자인 가이드, 다른 컨트롤의 모서리·중앙선에 맞추고 스냅 기준선을 표시하며 최소 10px 크기를 보호합니다. 크기 변경은 이동과 같은 Undo·AXAML 왕복 흐름을 사용합니다.
- **다중 선택 리사이즈**: 같은 root 또는 같은 Canvas의 여러 컨트롤을 선택하면 bounding box 핸들로 위치·크기를 비율 조정하고 Canvas 자식의 로컬 좌표도 동기화합니다. Grid·StackPanel·Content 자식이나 서로 다른 부모를 섞은 선택은 좌표 손실을 막기 위해 리사이즈를 차단합니다.
- **다중 선택 bounds 직접 편집**: `Edit > Edit Selection Bounds...`에서 같은 root 또는 같은 Canvas 형제 선택의 union bounds `X/Y/W/H`를 숫자로 입력하고, 상대 배치·크기를 함께 비율 조정하며 하나의 Transform Undo로 기록합니다. 잠긴 컨트롤·부모 레이아웃 자식·서로 다른 부모가 섞인 선택은 기존 리사이즈 규칙으로 차단합니다.
- **Selection Bounds 빠른 접근**: 다중 선택의 숫자 bounds 편집을 Design Toolbar의 `Bounds`, Canvas Context Menu, `Ctrl+Alt+B`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **다중 선택 공통 속성**: `Edit > Edit Common Properties...`에서 선택된 여러 컨트롤의 공통 Margin·정렬·Opacity·입력/표시 상태를 한 번에 적용하고, 서로 다른 값은 비워 둔 채 개별 값을 유지하며 하나의 Undo 작업으로 기록
- **Common Properties 빠른 접근**: 다중 선택 공통 속성 편집을 Design Toolbar의 `Common`, Canvas Context Menu, `Ctrl+Alt+M`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Layout Properties 빠른 접근**: 선택 컨트롤의 Margin·Padding·Alignment·Min/Max 크기 편집을 Design Toolbar의 `Layout`, Canvas Context Menu, `Ctrl+Alt+L`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Typography Properties 빠른 접근**: 글꼴 인식 컨트롤의 Font·Size·Style·Weight·정렬·줄바꿈 편집을 Design Toolbar의 `Typography`, Canvas Context Menu, `Ctrl+Alt+Y`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Transform Properties 빠른 접근**: 선택 컨트롤의 이동·회전·크기·기울기·변환 기준점 편집을 Design Toolbar의 `Transform`, Canvas Context Menu, `Ctrl+Alt+X`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Accessibility & Navigation 빠른 접근**: 선택 컨트롤의 스크린리더 메타데이터와 키보드 포커스 순서 편집을 Design Toolbar의 `Accessibility`, Canvas Context Menu, `Ctrl+Alt+A`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Interaction & Rendering 빠른 접근**: 선택 컨트롤의 표시·입력 참여·클리핑·RTL·포인터 커서 편집을 Design Toolbar의 `Interaction`, Canvas Context Menu, `Ctrl+Alt+E`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Visual Effects 빠른 접근**: 선택 컨트롤의 Blur·Drop Shadow 효과 편집을 Design Toolbar의 `Effects`, Canvas Context Menu, `Ctrl+Alt+F`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Range & Value 빠른 접근**: Slider·ProgressBar·NumericUpDown의 범위·값·눈금·증감 편집을 Design Toolbar의 `Range`, Canvas Context Menu, `Ctrl+Alt+Shift+R`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Text Input 빠른 접근**: TextBox의 입력·multiline·password·undo 정책 편집을 Design Toolbar의 `Text Input`, Canvas Context Menu, `Ctrl+Alt+Shift+T`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **SelectableTextBlock 빠른 접근**: 선택 가능한 텍스트의 Text·선택 브러시·전경색 편집을 Design Toolbar의 `Selectable Text`, Canvas Context Menu, `Ctrl+Alt+Shift+B`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **MaskedTextBox 빠른 접근**: MaskedTextBox의 Mask·PromptChar·prompt 숨김 정책 편집을 Design Toolbar의 `Masked Text`, Canvas Context Menu, `Ctrl+Alt+Shift+M`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Selection Behavior 빠른 접근**: ComboBox·ListBox·TreeView의 선택·검색·자동 스크롤 정책 편집을 Design Toolbar의 `Selection`, Canvas Context Menu, `Ctrl+Alt+Shift+Q`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Date & Time 빠른 접근**: DatePicker·CalendarDatePicker·Calendar·TimePicker의 날짜·시간·범위·표시 정책 편집을 Design Toolbar의 `Date & Time`, Canvas Context Menu, `Ctrl+Alt+Shift+D`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **ColorPicker 빠른 접근**: ColorPicker의 색상·스펙트럼·알파·팔레트·입력 표시 정책 편집을 Design Toolbar의 `Color Picker`, Canvas Context Menu, `Ctrl+Alt+Shift+P`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **AutoCompleteBox 빠른 접근**: AutoCompleteBox의 입력·watermark·자동 완성·최소 접두사·populate 지연·FilterMode·drop-down 정책 편집을 Design Toolbar의 `AutoComplete`, Canvas Context Menu, `Ctrl+Alt+Shift+A`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Toggle & Choice 빠른 접근**: CheckBox·RadioButton·ToggleSwitch·ToggleButton의 상태·three-state·ClickMode·콘텐츠·그룹·On/Off 문구 편집을 Design Toolbar의 `Toggle & Choice`, Canvas Context Menu, `Ctrl+Alt+Shift+O`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Disclosure & Scrolling 빠른 접근**: Expander의 header·expanded·전개 방향과 ScrollViewer의 scrollbar·auto-hide·chaining·deferred/focus scrolling·snap 정책 편집을 Design Toolbar의 `Disclosure & Scroll`, Canvas Context Menu, `Ctrl+Alt+Shift+U`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **SplitView Pane 빠른 접근**: SplitView의 DisplayMode·pane 열림·pane 길이·배치 방향·light-dismiss·pane 배경 편집을 Design Toolbar의 `SplitView Pane`, Canvas Context Menu, `Ctrl+Alt+Shift+V`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **TabControl 빠른 접근**: TabControl의 탭 스트립 위치와 선택 탭 콘텐츠 정렬 편집을 Design Toolbar의 `TabControl`, Canvas Context Menu, `Ctrl+Alt+Shift+K`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **DataGrid Behavior 빠른 접근**: DataGrid의 표 동작·열 조작·고정 열·행·열 크기·스크롤 정책 편집을 Design Toolbar의 `DataGrid`, Canvas Context Menu, `Ctrl+Alt+Shift+E`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **GridSplitter Behavior 빠른 접근**: GridSplitter의 방향·resize behavior·preview·keyboard/drag 증분 편집을 Design Toolbar의 `GridSplitter`, Canvas Context Menu, `Ctrl+Alt+Shift+J`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Image Source & Rendering 빠른 접근**: Image의 Source·Stretch·StretchDirection·bitmap interpolation·edge·blending 편집을 Design Toolbar의 `Image Source`, Canvas Context Menu, `Ctrl+Alt+Shift+I`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Button Actions & Commands 빠른 접근**: Button의 Content·ClickMode·HotKey·기본/취소·CommandParameter·Click handler 편집을 Design Toolbar의 `Button Actions`, Canvas Context Menu, `Ctrl+Alt+Shift+N`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Event Handler Map 빠른 접근**: 문서 전체의 `ControlName | EventName | HandlerName` 이벤트 매핑 편집을 Design Toolbar의 `Event Map`, Canvas Context Menu, `Ctrl+Alt+Shift+L`에서 같은 대화상자와 Undo 흐름으로 바로 실행합니다.
- **Preview Interaction Log 빠른 접근**: 이미 열린 Live Preview의 `Interaction Log`를 기록을 지우지 않고 다시 활성화하거나 새 Preview를 열어 Design Toolbar의 `Interaction Log`, Canvas Context Menu, `Ctrl+Alt+Shift+Z`에서 즉시 확인합니다. 기존 `Preview`·`Ctrl+R` 새로고침 경로는 그대로 유지합니다.
- **Arrange 키보드 단축키**: 캔버스에서 다중 선택 후 `Ctrl+Shift+Left/Right/Up/Down`으로 선택 컨트롤을 좌·우·상·하 경계에 정렬하고 `Ctrl+Shift+E/M`으로 가로 중앙·세로 중앙에 정렬하며 `Ctrl+Alt+H/V`로 가로·세로 균등 분배하고 기존 Arrange Undo/AXAML 흐름을 그대로 사용
- **레이어 순서 키보드 단축키**: 선택 컨트롤을 `Ctrl+]`/`Ctrl+[`로 한 단계 앞·뒤로 이동하고 `Ctrl+Shift+]`/`Ctrl+Shift+[`로 맨 앞·뒤로 보내며 기존 Order Undo/AXAML 흐름과 선택 상태를 유지
- **아트보드 중앙 정렬 단축키**: root 선택을 `Ctrl+Alt+Shift+X/Y`로 가로·세로 중앙에 배치하고 `Ctrl+Alt+Shift+C`로 양축 중앙에 배치하며 기존 Center on Artboard Undo/AXAML 흐름을 사용
- **그룹 편집 키보드 단축키**: 다중 선택을 `Ctrl+G`로 Canvas 그룹으로 묶고 선택된 Canvas를 `Ctrl+Shift+U`로 해제하며 기존 계층·Undo·Preview·AXAML 흐름을 유지
- **레이아웃 키보드 단축키**: `Ctrl+Alt+Shift+1..8`로 StackPanel·Grid·UniformGrid·DockPanel·WrapPanel 레이아웃을 직접 적용하고 `Ctrl+Shift+B`로 선택된 레이아웃을 해제하며 기존 Undo·계층·AXAML 흐름을 유지
- **잠금 인식 구조 해제**: `Ungroup Selected Canvas`와 `Break Selected Layout`은 잠긴 컨테이너 자식이 포함된 계층을 거부해 부분적인 부모·좌표 변경을 방지하고, 잠금 해제 후 기존 해제·Undo 흐름을 유지
- **그리드/스냅 키보드 단축키**: `Ctrl+Alt+G`로 디자인 그리드를 표시·숨기고 `Ctrl+Alt+Shift+G`로 Grid snap을 토글하며 기존 문서 설정·Undo/AXAML 메타데이터를 유지
- **Workspace 패널 키보드 단축키**: `Ctrl+Alt+1/2/3`으로 Toolbox·Object Tree·Property Inspector를 토글하고 `Ctrl+Alt+0`으로 기본 패널 레이아웃을 복원하며 기존 세션 저장 상태와 메뉴 체크를 동기화
- **Design Toolbar**: 캔버스 상단의 Qt Designer식 도구 모음에서 Select/Pick 작업 모드, New·Save·Undo/Redo·Copy/Paste/Delete·Copy Style/Paste Style·Preview, Align/Distribute/Size, z-order·아트보드 가장자리/중앙 정렬, Group/Ungroup/Break, Stack/Grid/Uniform/Dock/Wrap Layout, Lock·Grid/Snap·Guides, Desktop/Tablet/Mobile/Rotate/Custom Artboard, Fit/Zoom 명령을 가로 스크롤 가능한 한 줄에서 실행하고 선택·배치 상태에 따라 사용할 수 없는 작업을 자동 비활성화
- **Canvas Pan Tool**: Design Toolbar의 `Pan` 또는 `View > Pan Canvas (H)`로 전용 패닝 모드를 켜고 좌클릭 드래그로 확대된 아트보드를 이동하며, `H`를 다시 누르거나 `Select`를 선택하면 기존 선택 도구로 돌아가고 Toolbox 배치 모드와 상호 배타적으로 동작
- **Canvas Context Menu**: 빈 Design Surface를 우클릭하면 Copy/Cut/Paste·Style·AXAML, Duplicate/Delete, Lock, z-order, Arrange, 아트보드 정렬, Group/Ungroup/Break, Preview/Fit 명령을 현재 선택 상태에 맞는 조건으로 바로 실행
- **Selection Breadcrumb**: Design Surface 헤더에 현재 선택 컨트롤의 `Parent / Child` 계층 경로를 표시하고 각 경로 항목을 클릭해 해당 컨테이너를 바로 선택하며, `Up`·`Into` 버튼으로 기존 Escape 부모 선택·Enter 자식 진입도 직접 실행하고 재부모화·이름 변경을 즉시 반영
- **Selection Bounds HUD**: Design Surface 헤더에 선택 영역의 아트보드 기준 `X/Y/W/H` bounds를 실시간 표시해 단일·다중 선택의 위치와 크기를 메뉴를 열지 않고 확인
- **Aggregate Selection Adorner**: 다중 선택 시 8방향 핸들을 연결하는 전체 선택 영역 외곽선을 표시해 이동·리사이즈 대상과 선택 범위를 캔버스에서 즉시 식별
- **줌 독립 선택 외곽선**: Aggregate Selection Adorner 외곽선의 StrokeThickness를 확대 배율과 반비례하게 조정해 25~200%에서도 화면상 약 1px 선명도를 유지
- **Grid Size Quick Controls**: Design Toolbar의 Grid 그룹에서 `4`·`8`·`16`px 프리셋과 Custom Grid Size 대화상자를 메뉴 이동 없이 바로 실행
- **Grid Size 상태 피드백**: Design Toolbar에 현재 Grid 간격을 `px` 단위로 표시하고 프리셋·Custom·문서 복원·Undo/Redo 변경을 즉시 반영
- **패널 포커스 복구**: 숨겨진 패널에서도 `Ctrl+Alt+T/P/I` 또는 `Ctrl+F`를 누르면 필요한 Toolbox·Object Tree·Property Inspector를 자동으로 다시 표시한 뒤 검색·배치 작업으로 진입
- **리사이즈 비율 잠금**: 코너 핸들을 `Shift`와 함께 드래그하면 단일 컨트롤과 다중 선택 bounding box의 원래 가로·세로 비율을 유지하며, 잠금 중에는 Smart Snap보다 비율을 우선합니다.
- **줌 독립 리사이즈 핸들**: 선택 핸들의 화면상 클릭 영역을 약 10px로 유지해 25~200% 확대·축소에서도 핸들이 지나치게 작거나 커지지 않으며, 줌 변경 시 문서 좌표 크기를 자동으로 역산합니다.
- **디자인 그리드**: 그리드 표시·Snap to Grid를 전환하고 4·8·16px 프리셋 또는 4~32px 사용자 지정 간격을 편집하며 문서 설정·Undo/Redo·Preview·AXAML 메타데이터에 보존
- **계층 클립보드**: 컨테이너를 선택해 복사·잘라내기·붙여넣기·복제하면 내부 자식 계층과 부모별 배치 메타데이터를 함께 보존하고, 선택된 지원 컨테이너가 있으면 `Ctrl+V`/Paste로 해당 부모의 슬롯에 삽입하며, 붙여넣은 부모 이름은 새 이름으로 재매핑
- **컨테이너 편집**: Grid 셀, StackPanel 순서·주축 크기, DockPanel 순서·방향·크기·LastChildFill, WrapPanel 순서·방향·항목 크기·간격·정렬, UniformGrid 순서·행·열·첫 열·간격, 중첩 Canvas 로컬 좌표·직접 변형·z-order, TabControl 탭 정의·탭별 단일 자식·활성 페이지·TabStripPlacement·콘텐츠 정렬, SplitView Pane·Content 슬롯·Inline/Overlay·배치 방향, Border·ContentControl·UserControl·ScrollViewer·Expander의 단일 Content 자식을 편집하고 재귀 Object Tree·AXAML·미리보기에 보존
- **Canvas 자식 기하 편집**: Property Inspector의 X/Y/W/H 입력을 Canvas 자식에서는 로컬 `Canvas.Left`·`Canvas.Top`·Width·Height로 전환해 직접 편집하고, 부모 기준 화면 좌표·Object Tree·Undo/Redo·AXAML을 동기화
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
- **Tab Order Map**: `Edit Tab Order Map...`에서 전체 컨트롤의 `TabIndex | ControlName` 목록을 한 번에 편집하고 중복 순서·존재하지 않는 이름·잠긴 컨트롤 변경을 검증하며, 명시적 순서는 캔버스 `TAB #` 배지와 AXAML에 반영
- **단일 Tab Order 편집**: `Edit Tab Order...`에서 선택 컨트롤의 순서를 `0~10000`, `auto`, `-1`로 편집하고 자동 순서를 `auto`로 표시하며, Tab Order Map과 동일한 범위 검증·Undo 규칙을 사용
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
- **Event Handler Map**: `ControlName | EventName | HandlerName` 형식으로 공통 포인터·키보드·포커스·수명 이벤트와 Button·TextBox·선택 컨트롤 등의 타입별 이벤트 핸들러를 일괄 편집하고, 잠금 보호·유효성 검증·Undo/Redo·Preview·AXAML 왕복을 지원
- **Preview Interaction Log**: Live Preview에서 연결된 Button·TextBox·선택·토글·포인터·키보드·포커스 이벤트를 발생시키고 `Control.Event -> HandlerName` 형식의 최근 로그로 디자인 타임 연결을 즉시 확인
- **Preview Theme Variants**: `View > Preview Theme`에서 Live Preview를 System Default·Light·Dark 테마로 전환하고, 열린 Preview 창을 닫지 않은 채 현재 컨트롤 트리와 상호작용 로그를 유지하며 Avalonia Fluent 리소스 변형을 즉시 확인
- **Style Clipboard**: `Edit > Style Clipboard`, Design Toolbar, Canvas Context Menu 또는 `Ctrl+Shift+C/V`에서 선택 컨트롤의 외형·타이포그래피·렌더링·효과·상호작용·스타일 클래스와 리소스 참조만 복사해 하나 또는 여러 대상에 붙여넣고, 콘텐츠·위치·크기·계층·바인딩·이벤트는 보존
- **Undo History Timeline**: `Edit > History...`, Design Toolbar, Canvas Context Menu 또는 `Ctrl+Alt+Shift+F`에서 현재 문서와 Undo·Redo 작업을 한 번에 확인하고 원하는 지점으로 이동
- **Document Tabs**: 여러 AXAML 문서를 동시에 탭으로 열고 전환하며, 문서별 캔버스·dirty 상태·Undo/Redo history를 독립적으로 보존합니다. `Ctrl+N`/`File > New`는 새 탭, `Ctrl+W`/`File > Close Tab`은 현재 탭을 닫고, `File > Open...`은 문서를 새 탭으로 엽니다. 탭 헤더를 드래그하면 원하는 순서로 재배치하고, 드롭 위치를 세션에 보존합니다. 같은 탭 명령은 Design Toolbar의 `Tabs` 그룹과 디자인 캔버스 Context Menu에서도 실행할 수 있습니다.
- **Document Tab Duplication**: `File > Duplicate Tab`, 탭 컨텍스트 메뉴 또는 `Ctrl+Alt+D`로 현재 문서를 독립된 새 dirty 탭으로 복제합니다. 문서 내용·Undo/Redo·줌·선택·Property Inspector 탐색 상태를 함께 복사하고 저장 경로는 비워 새 파일로 저장하게 합니다.
- **Document Tab Naming**: `File > Rename Tab...`, 탭 컨텍스트 메뉴 또는 `Ctrl+Alt+R`로 열린 문서 탭에 1-80자 별칭을 지정합니다. 저장 파일명과 별칭을 분리하고, 별칭은 열린 탭·닫힌 탭 기록·세션 복원·창 제목에 유지합니다.
- **Document Tab Middle-Click Close**: 탭 헤더를 중간 클릭하면 기존 dirty 확인을 거쳐 해당 문서 탭을 닫습니다. 취소하거나 저장에 실패하면 원래 활성 탭과 편집 상태를 유지합니다.
- **Document Tab Direct Shortcuts**: `Ctrl+1`~`Ctrl+9`로 1-9번째 문서 탭을 즉시 활성화하고, `Ctrl+Tab` 순환이나 탭 드래그/이동과 함께 사용할 수 있습니다.
- **Document Tab MRU Switching**: `View > Switch to Recent Document Tab`, Design Toolbar의 `Recent`, Canvas Context Menu 또는 `Ctrl+Alt+Tab`으로 가장 최근에 사용한 다른 탭으로 전환하고, `Ctrl+Alt+Shift+Tab`으로 MRU 목록의 더 오래된 탭을 순환합니다. 최근 사용 순서는 세션 JSON에 보존됩니다.
- **Document Tab Quick Switcher**: `View > Quick Switch Document Tab...` 또는 `Ctrl+K`로 열린 탭의 별칭·파일 경로·탭 번호·저장 상태를 검색하고 Enter로 즉시 전환합니다. 결과에는 `Ctrl+1..9` 바로가기와 `Saved`·`Modified`·`Unsaved` 상태가 표시되며, 위·아래 키는 끝에서 처음/처음에서 끝으로 순환합니다.
- **Recent AXAML Files**: `File > Open Recent Files...`, Design Toolbar의 `Recent Files`, Canvas Context Menu 또는 `Ctrl+Shift+O`에서 최근 파일을 이름·경로로 검색하고 Enter로 새 문서 탭에 엽니다. 누락된 경로는 `Missing`으로 표시하며 선택하면 최근 목록에서 제거합니다.
- **Project Workspace Explorer**: `File > Open Project Folder...`로 작업 폴더를 선택하면 `Project Explorer... (Ctrl+Shift+P)`에서 폴더·`.axaml`·`.xaml` 파일 계층을 탐색하고 Enter 또는 파일 더블클릭으로 새 문서 탭에 엽니다. 폴더는 `[+]`/`[-]`와 Enter·더블클릭으로 접고 펼치며 Left/Right 방향키로 접기·펼치기·부모/첫 자식 이동을 수행하고 검색 시 일치 파일의 조상 폴더를 유지합니다. 명시적인 tree item template가 깊이·폴더 상태·상대 경로를 표시하며, 우클릭한 행을 먼저 선택한 뒤 선택한 파일·폴더에 context menu의 relative/full path 복사, `Ctrl+C` full path 복사, `Open in File Manager` 위치 열기를 적용합니다. 현재 문서 파일은 다시 열 때 자동으로 reveal하고 폴더 접기 상태도 저장·복원합니다. `bin`·`obj`·`.git` 등 생성 폴더는 제외하며 선택한 프로젝트 폴더와 파일 목록은 다음 실행에도 복원됩니다.
- **Project Workspace File Watcher**: 열린 프로젝트 폴더의 AXAML 생성·삭제·이동을 자동 감지해 Explorer 파일 목록과 열려 있는 Project Explorer dialog를 갱신하고, 현재 문서가 외부에서 변경되면 `Reload Current File (Ctrl+Shift+R)`을 활성화합니다. dirty 문서는 기존 저장·폐기 확인을 거친 뒤에만 외부 내용을 적용합니다.
- **Tab View Navigation**: `Ctrl+Tab`/`Ctrl+Shift+Tab`으로 문서 탭을 순환하고, `Ctrl+Shift+PageUp/PageDown` 또는 탭 컨텍스트 메뉴로 활성 탭을 좌우 이동하며, 탭별 캔버스 줌·스크롤 위치와 Object Tree 선택을 전환·세션 복원 때 보존합니다.
- **Viewport 복원 우선순위**: 문서 탭을 복원하는 짧은 pending 구간에는 선택 컨트롤 자동 스크롤을 보류해 저장된 Canvas 위치가 Object Tree/Canvas 선택 추적에 의해 덮어써지지 않도록 합니다.
- **Workspace Panels**: `View > Panels`에서 Toolbox·Object Tree·Property Inspector를 독립적으로 숨기거나 다시 표시하고, 패널 크기와 가시성·Object Tree 분할 위치를 세션에 저장합니다. `Reset Panel Layout`으로 기본 작업 공간을 복원합니다.
- **Workspace Session Restore**: 앱을 정상적으로 닫으면 열린 탭 목록·활성 탭·현재 AXAML·저장 기준 스냅샷·줌·Canvas 스크롤 위치·Object Tree 선택·Property Inspector 탐색 상태를 로컬 세션에 저장하고, 다음 실행 시 dirty 문서를 포함해 복원합니다. 세션 JSON이 손상되면 현재 새 문서 상태를 유지하고 안전하게 시작합니다.
- **문서 탭별 Design Guide**: 가로·세로 ruler에서 만든 guide 좌표와 `Show Design Guides`·`Snap to Guides` 설정을 문서 탭마다 독립 보존해 탭 전환·앱 재시작·닫힌 탭 복원 후에도 되살리고, 새 문서·새 AXAML·명시적 `Clear Design Guides`에서는 해당 탭만 초기화합니다.
- **선택 요소 AXAML 재사용**: 하나 이상의 선택 컨트롤을 하위 계층·리소스·스타일·바인딩·컨트롤 전용 선언과 함께 독립 UserControl AXAML로 클립보드 복사하거나 파일로 내보내며, 다중 선택은 bounding box 기준의 상대 Canvas 좌표를 보존
- **AXAML Fragment Clipboard**: `Edit > Paste AXAML from Clipboard` 또는 `Ctrl+Alt+V`로 OS 클립보드의 Window/UserControl AXAML을 현재 문서에 새 컨트롤로 붙여넣고, 선택된 잠금 해제 지원 컨테이너가 있으면 해당 부모의 Grid·StackPanel·Canvas·Tab·Content 슬롯 규칙으로 배치하며 이름 충돌·색상 리소스·스타일 충돌을 자동으로 분리하고 하나의 Undo 작업으로 되돌림
- **문서 루트 속성**: Window/UserControl 루트 종류와 Window 제목·리사이즈·시작 위치, 루트 Min/Max 크기를 편집하고 Undo/Redo, Preview, Draft·Full AXAML 왕복에 보존
- **벡터 Shape 편집**: Rectangle, Ellipse, Line, Path의 Fill·Stroke·대시·끝점·결합 스타일과 반지름·점 좌표를 편집하고, 검증된 Path geometry를 리소스·Undo/Redo·복제·미리보기·AXAML 왕복에 보존
- **요소 선택**: 배치된 요소 클릭 시 파란 외곽선
- **방향 인식 Marquee 선택**: 캔버스에서 왼쪽→오른쪽으로 드래그하면 사각형 안에 완전히 포함된 컨트롤만 선택하고, 오른쪽→왼쪽으로 드래그하면 사각형과 교차한 컨트롤을 선택하며 `Ctrl` 또는 `Shift`로 기존 선택에 추가
- **안전한 Marquee 선택**: 잠긴 컨트롤과 현재 아트보드에 표시되지 않는 컨트롤은 marquee 일괄 선택에서 제외하고, 직접 클릭 시 검사할 수 있는 기존 잠금 동작은 유지
- **Marquee 제거 선택**: 캔버스 빈 영역에서 `Alt+드래그`하면 보이는 잠금 해제 컨트롤만 현재 선택에서 제거하고, 잠긴·숨겨진 컨트롤과 작은 클릭은 기존 선택을 보존
- **안전한 Select All**: Edit/Canvas context menu와 `Ctrl+A`는 현재 아트보드에 표시되는 잠금 해제 컨트롤만 선택하고, 잠긴·숨겨진 요소의 직접 검사는 유지
- **잠금 인식 Copy/Duplicate**: 잠긴 컨트롤은 직접 검사할 수 있지만 `Copy`·`Duplicate`는 잠금 해제 선택만 처리하고, 혼합 선택에서는 잠금 해제 계층만 복사·복제하며 거부된 명령은 기존 클립보드를 보존
- **문서 리소스 보존 clipboard**: 내부 `Copy`·`Cut`은 선택 계층과 함께 색상 리소스·문서 스타일 정의를 캡처하고, 다른 문서 탭에 `Paste`할 때 충돌 키·스타일 클래스를 별칭 처리하며 `DynamicResource`·`Classes` 참조까지 함께 재작성
- **겹친 요소 순환 선택**: `Alt+클릭`으로 포인터 아래의 visible 컨트롤을 앞쪽부터 순환 선택하고, `Alt+Shift+클릭`으로 반대 방향으로 이동하며 잠긴 요소도 속성 검사를 위해 순환
- **키보드 선택 순환**: 캔버스에 포커스가 있을 때 `Tab`/`Shift+Tab`으로 보이고 활성화된 `Focusable=true`, `IsTabStop=true` 컨트롤을 순환 선택하고 `Shift+Tab`은 Tab Order anchor부터 다음 target까지 visible 범위를 기존 선택에 누적하며, 명시 `TabIndex`를 낮은 값부터 적용하고 `auto/-1` 요소는 기존 Canvas 순서를 유지
- **Canvas 경계 선택**: 캔버스에 포커스가 있을 때 `Home`/`End`로 Canvas 순서의 첫/마지막 visible 컨트롤을 즉시 선택하고 `Shift+Home/End`로 anchor부터 해당 경계까지 visible 범위를 기존 선택에 누적하며 Object Tree를 동기화하고, hidden 요소는 경계 후보에서 제외
- **Canvas 순차 선택**: 캔버스에 포커스가 있을 때 `PageUp`/`PageDown`으로 Canvas 순서의 이전/다음 visible 컨트롤을 순환 선택하고, `Shift+PageUp/PageDown`으로 anchor부터 다음 target까지 visible 범위를 기존 선택에 누적하며 hidden 요소를 건너뛰고 Object Tree를 동기화
- **Object Tree 자동 동기화**: 배치된 요소가 루트(Window) 아래에 추가
- **Object Tree 다중 선택 표시**: 다중 선택 항목에는 `SEL`, 잠긴 항목에는 `LOCK` 마커를 표시하고 헤더에 선택 개수를 표시
- **Object Tree 다중 선택 입력**: 트리에서 일반 클릭은 단일 선택, `Ctrl+클릭`은 선택 항목 추가/해제로 동작
- **Canvas 다중 선택 입력**: 캔버스에서 일반 클릭은 단일 선택, `Ctrl+클릭`은 선택 항목을 토글하고 `Shift+클릭`은 anchor부터 대상까지 visible Canvas 순서 범위를 선택하며 `Ctrl+Shift+클릭`은 해당 범위를 기존 선택에 추가하고, 범위 입력은 이동·더블클릭 편집으로 이어지지 않음
- **Canvas 범위 선택**: hidden 요소를 건너뛰는 Canvas 순서 anchor 범위와 Object Tree 동기화를 지원하고 reverse 범위에서도 클릭한 target을 활성 요소로 유지하며, 유효한 anchor가 없으면 기존 단일 추가·토글 선택으로 안전하게 fallback
- **계층 부모 선택**: 캔버스에서 단일 컨테이너 자식을 선택한 뒤 `Escape`를 누르면 부모 컨테이너를 선택하고, root·다중 선택에서는 기존처럼 선택을 해제하며 Toolbox 배치·marquee 취소를 우선
- **계층 자식 진입**: 캔버스에서 단일 컨테이너를 선택한 뒤 `Enter`로 첫 visible 직접 자식, `Shift+Enter`로 마지막 visible 직접 자식을 선택하며, 자식이 없거나 다중 선택이면 기존 입력을 유지
- **계층 형제 탐색**: 캔버스에서 `Alt+Left/Up`으로 같은 부모의 이전 visible 형제, `Alt+Right/Down`으로 다음 visible 형제를 선택하고, `Alt+Shift+Arrow`의 기존 10px nudge와 형제가 없는 경계에서의 기존 이동을 보존
- **Canvas 방향 선택**: `Ctrl+Arrow`로 현재 선택 중심에서 해당 방향의 가장 가까운 보이는 컨트롤을 선택하며, 같은 축으로 겹치는 후보·축 거리·보조 거리·Canvas 순서를 결정 규칙으로 사용하고 대상이 없으면 기존 nudge 동작을 유지
- **Object Tree 범위 선택**: 트리의 현재 표시 순서에서 일반 클릭을 anchor로 삼아 `Shift+클릭`, `Shift+Up/Down` 또는 `Shift+Home/End`로 행 범위를 선택하고, `Ctrl+Shift+클릭`, `Ctrl+Shift+Up/Down` 또는 `Ctrl+Shift+Home/End`로 기존 선택에 범위를 추가하며 접힌 자식과 검색 결과 순서를 안전하게 반영
- **Object Tree 컨텍스트 편집**: 트리 행을 우클릭해 Rename·Lock/Unlock·Copy·Cut·Duplicate·Delete 실행
- **Object Tree 계층 편집**: 트리 행의 컨텍스트 메뉴에서 지원 컨테이너 할당과 부모 컨테이너 해제를 바로 실행
- **Object Tree 순서 편집**: StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas 자식의 순서를 `Move Earlier/Later`로 바로 변경
- **Object Tree 탐색 상태**: 트리 노드의 펼침 상태를 계층 재빌드 후에도 보존하고, Canvas·키보드·Undo/탭 전환으로 자식을 선택하면 접힌 부모 경로를 자동으로 펼치며 선택 행을 화면에 스크롤
- **선택 대상 viewport 추적**: Object Tree·Canvas 키보드·문서 탭 전환으로 visible 컨트롤을 선택하면 Object Tree 선택 행과 Canvas active 요소를 각각 자동 스크롤해 큰 아트보드에서도 현재 작업 대상을 잃지 않음
- **Object Tree 드래그 재배치**: 지원 컨테이너의 자식 행을 같은 부모의 다른 행 앞에 드래그해 순서를 변경하거나 Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas·TabControl·SplitView·Content 컨테이너 행으로 드래그해 부모를 변경하며, 잠긴 대상·순환 계층·가득 찬 슬롯은 거부
- **Object Tree 드롭 피드백**: 드래그 중 유효한 대상 행은 초록색, 거부되는 대상 행은 빨간색으로 강조하고 드롭·취소·트리 이탈 시 상태를 정리
- **Object Tree 삽입 위치 표시**: 같은 부모의 행 위쪽/아래쪽 절반에 드롭하면 앞/뒤 삽입선을 표시하고, 표시된 위치 그대로 StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas 순서를 변경
- **Object Tree 검색 순환**: 검색 결과를 `Enter`로 다음, `Shift+Enter`로 이전 항목으로 순환하고 현재 위치/전체 개수를 표시하며 `Escape`로 검색을 지우고 TreeView 포커스로 복귀
- **Object Tree 키보드 편집**: 트리에 포커스가 있을 때 방향키는 계층을 탐색하고, `F2`로 이름 변경, `Delete`/`Backspace`로 삭제, `Ctrl+L`로 잠금 전환
- **이동/리사이즈 기즈모**: 선택된 요소를 드래그로 이동, 8방향 핸들로 리사이즈 (최소 10px)
- **PropertyGrid 연동**: 선택된 컨트롤의 속성을 bodong PropertyGrid로 실시간 편집
- **Property Inspector 탐색**: 선택 컨트롤 타입을 헤더에 표시하고 `Categories`/`Flat`, `Expand`/`Collapse`로 속성 그룹과 표시 밀도를 즉시 전환하며, 내장 카테고리 순서·알파벳 속성 정렬을 함께 제공
- **Property Inspector 검색**: 전용 필터 입력창과 Clear·Escape 초기화, `Ctrl+Alt+I` 포커스를 제공하고 선택 컨트롤·문서 탭이 바뀌어도 필터를 유지해 속성 이름을 즉시 좁히며, Escape 후 PropertyGrid 포커스로 속성 편집을 바로 이어감
- **Appearance 편집**: 배경·전경·테두리·두께·모서리를 편집하고 Undo/Redo, 미리보기, AXAML 왕복에 보존
- **색상 리소스**: 문서 단위 SolidColorBrush를 편집하고 DynamicResource로 컨트롤에 적용
- **클래스·상태 스타일**: `[Button.primary:pointerover]` 형식의 Setter, 선택 컨트롤별 상태 선택기와 캔버스 배지, 대화형 미리보기, 로컬 속성 우선순위 지원
- **이벤트 선언**: Button Click 핸들러 이름 편집 및 AXAML 내보내기
- **AXAML 워크플로**: Window 저장/열기, UserControl 내보내기, 복사, 현재 문서 유효성 확인과 런타임 미리보기
- **Live Preview**: Preview 창을 한 번 열어두면 컨트롤 배치·속성·Undo/Redo·AXAML 적용과 문서 로드 결과를 별도 창에 자동 반영하고, Preview 창은 하나만 유지
- **Crash-safe 저장**: 기존 AXAML을 `.bak`으로 원자적으로 보존하고 File > Recover Backup...에서 복구하며, 복구 결과를 dirty 상태와 Undo/Redo에 연결
- **텍스트 편집기 키보드 흐름**: 공통 텍스트·AXAML·Binding·Items/Columns·Sample Data와 속성 편집기 전체에서 `Escape`로 변경을 취소하고 `Ctrl+Enter`로 적용하며, multiline 입력의 일반 `Enter`는 그대로 유지
- **디자인 ↔ 소스 왕복 편집**: 전체 Window/UserControl AXAML을 직접 편집하고, 캔버스를 바꾸지 않는 검증·미리보기 후 파일 경로를 유지한 채 단일 Undo/Redo 작업으로 적용하며 `Escape`로 취소·`Ctrl+Enter`로 적용
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
9. `Edit > Edit AXAML Source...`에서 전체 AXAML을 검증·미리보기하고 캔버스에 적용합니다. `Escape`는 변경을 버리고 닫으며 `Ctrl+Enter`는 유효한 소스를 바로 적용합니다.
10. `Edit > Edit Root Properties...`에서 Window/UserControl 루트와 Window 동작·루트 크기 제약 편집
11. `Edit > Edit Sample Data...`에서 JSON 샘플 DataContext를 검증하고 바인딩된 컨트롤에 적용
12. `Edit > Edit Layout Properties...`에서 선택 컨트롤의 Margin·Padding·Alignment·Min/Max 크기 편집
12a. 두 개 이상의 호환 컨트롤을 선택한 뒤 `Edit > Edit Selection Bounds...`에서 union bounds의 `X/Y/W/H`를 직접 입력해 선택 영역을 정밀 배치·확대/축소
12b. Canvas 안의 자식 컨트롤을 선택하면 Property Inspector의 `Left`·`Top`·`W`·`H`에서 부모 Canvas 기준 로컬 좌표와 크기를 직접 편집
12c. 두 개 이상의 호환 컨트롤을 선택한 뒤 Design Toolbar의 `Bounds`, Canvas Context Menu 또는 `Ctrl+Alt+B`로 같은 Selection Bounds 편집기를 바로 엽니다.
12d. 두 개 이상의 컨트롤을 선택한 뒤 Design Toolbar의 `Common`, Canvas Context Menu 또는 `Ctrl+Alt+M`으로 공통 Margin·정렬·Opacity·상태 편집기를 바로 엽니다.
12e. 컨트롤을 선택한 뒤 Design Toolbar의 `Layout`, Canvas Context Menu 또는 `Ctrl+Alt+L`로 Margin·Padding·Alignment·Min/Max 크기 편집기를 바로 엽니다.
12f. 글꼴 인식 컨트롤을 선택한 뒤 Design Toolbar의 `Typography`, Canvas Context Menu 또는 `Ctrl+Alt+Y`로 Font·Size·Style·Weight·정렬·줄바꿈 편집기를 바로 엽니다.
12g. 컨트롤을 선택한 뒤 Design Toolbar의 `Transform`, Canvas Context Menu 또는 `Ctrl+Alt+X`로 이동·회전·크기·기울기·변환 기준점 편집기를 바로 엽니다.
12h. 컨트롤을 선택한 뒤 Design Toolbar의 `Accessibility`, Canvas Context Menu 또는 `Ctrl+Alt+A`로 스크린리더 메타데이터와 키보드 포커스 순서 편집기를 바로 엽니다.
12i. 컨트롤을 선택한 뒤 Design Toolbar의 `Interaction`, Canvas Context Menu 또는 `Ctrl+Alt+E`로 표시·입력 참여·클리핑·RTL·포인터 커서 편집기를 바로 엽니다.
12j. 컨트롤을 선택한 뒤 Design Toolbar의 `Effects`, Canvas Context Menu 또는 `Ctrl+Alt+F`로 Blur·Drop Shadow 효과 편집기를 바로 엽니다.
12k. Slider·ProgressBar·NumericUpDown을 선택한 뒤 Design Toolbar의 `Range`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+R`로 범위·값·눈금·증감 편집기를 바로 엽니다.
12l. TextBox를 선택한 뒤 Design Toolbar의 `Text Input`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+T`로 입력·multiline·password·undo 정책 편집기를 바로 엽니다.
12m. SelectableTextBlock을 선택한 뒤 Design Toolbar의 `Selectable Text`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+B`로 텍스트·선택 브러시·전경색 편집기를 바로 엽니다.
12n. MaskedTextBox를 선택한 뒤 Design Toolbar의 `Masked Text`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+M`으로 Mask·PromptChar·prompt 숨김 정책 편집기를 바로 엽니다.
12o. ComboBox·ListBox·TreeView를 선택한 뒤 Design Toolbar의 `Selection`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+Q`로 선택·검색·자동 스크롤 정책 편집기를 바로 엽니다.
12p. DatePicker·CalendarDatePicker·Calendar·TimePicker를 선택한 뒤 Design Toolbar의 `Date & Time`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+D`로 날짜·시간·범위·표시 정책 편집기를 바로 엽니다.
12q. ColorPicker를 선택한 뒤 Design Toolbar의 `Color Picker`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+P`로 색상·스펙트럼·알파·팔레트·입력 표시 정책 편집기를 바로 엽니다.
12r. AutoCompleteBox를 선택한 뒤 Design Toolbar의 `AutoComplete`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+A`로 입력·자동 완성·filter·지연·drop-down 정책 편집기를 바로 엽니다.
12s. CheckBox·RadioButton·ToggleSwitch·ToggleButton을 선택한 뒤 Design Toolbar의 `Toggle & Choice`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+O`로 상태·three-state·ClickMode·콘텐츠·그룹·On/Off 문구 편집기를 바로 엽니다.
12t. Expander 또는 ScrollViewer를 선택한 뒤 Design Toolbar의 `Disclosure & Scroll`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+U`로 전개·헤더·스크롤·snap 정책 편집기를 바로 엽니다.
12u. SplitView를 선택한 뒤 Design Toolbar의 `SplitView Pane`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+V`로 pane 표시·열림·길이·배치·light-dismiss·배경 정책 편집기를 바로 엽니다.
12v. TabControl을 선택한 뒤 Design Toolbar의 `TabControl`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+K`로 탭 스트립 위치와 선택 탭 콘텐츠 정렬 편집기를 바로 엽니다.
12w. DataGrid를 선택한 뒤 Design Toolbar의 `DataGrid`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+E`로 표 동작·열 조작·고정 열·행·열 크기·스크롤 정책 편집기를 바로 엽니다.
12x. GridSplitter를 선택한 뒤 Design Toolbar의 `GridSplitter`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+J`로 방향·resize behavior·preview·keyboard/drag 증분 편집기를 바로 엽니다.
12y. Button을 선택한 뒤 Design Toolbar의 `Button Actions`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+N`으로 Content·ClickMode·HotKey·기본/취소·CommandParameter·Click handler 편집기를 바로 엽니다.
12z. Image를 선택한 뒤 Design Toolbar의 `Image Source`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+I`로 Source·Stretch·StretchDirection·bitmap interpolation·edge·blending 편집기를 바로 엽니다.
12aa. Design Toolbar의 `Event Map`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+L`로 문서 전체의 `ControlName | EventName | HandlerName` 이벤트 핸들러 매핑 편집기를 바로 엽니다.
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
32. `File > Copy Selected AXAML`로 하나 이상의 선택 컨트롤을 클립보드에 복사하거나 `File > Export Selected AXAML...`로 독립 UserControl AXAML 파일로 내보냅니다. 여러 root 또는 같은 Canvas의 직접 형제만 다중 선택 내보내기를 지원하며, `Edit > Paste AXAML from Clipboard` 또는 `Ctrl+Alt+V`는 선택된 잠금 해제 지원 컨테이너가 있으면 그 안에, 없으면 문서 root에 병합합니다. 일반 `Ctrl+V` 내부 디자이너 clipboard도 같은 선택 컨테이너 대상 규칙을 사용하고 복사 시점의 문서 색상 리소스·스타일 정의도 함께 병합하며 두 clipboard는 서로 분리됩니다.
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
52. Object Tree 검색에서 `Enter`를 누르면 다음 일치 컨트롤, `Shift+Enter`를 누르면 이전 일치 컨트롤을 선택하고 결과 위치를 확인하며 `Escape`는 검색을 지우고 트리 탐색으로 돌아갑니다.
53. 캔버스를 클릭한 뒤 `Tab`/`Shift+Tab`을 누르면 보이고 활성화된 `Focusable=true`, `IsTabStop=true` 컨트롤만 대상으로 명시 `TabIndex`를 낮은 값부터 선택하고, `Shift+Tab`은 Tab Order anchor부터 이전 target까지 범위를 기존 선택에 누적합니다. `auto/-1` 요소는 기존 Canvas 순서로 순환하며 숨겨진 TabControl 페이지와 비활성·비포커스·탭 제외 컨트롤은 자동으로 건너뜁니다.
54. 여러 컨트롤을 선택하면 Object Tree의 각 항목에 `SEL` 마커와 헤더의 선택 개수가 표시되고, 잠긴 컨트롤에는 `LOCK` 마커가 표시됨
55. Object Tree에서 일반 클릭은 하나만 선택하고, `Ctrl+클릭`은 여러 컨트롤을 추가 선택하거나 선택 해제함
56. Object Tree 행을 우클릭하면 해당 컨트롤을 먼저 선택한 뒤 Rename·Lock/Unlock·Copy·Cut·Duplicate·Delete 명령을 실행할 수 있음
57. Object Tree 행의 `Assign to Container`에서 Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas·TabControl·SplitView·Content를 선택하거나 `Remove from Container`로 부모를 해제
58. 컨테이너 자식 행에서 `Move Earlier`/`Move Later`를 선택하면 지원되는 부모의 형제 순서가 바뀌고, Grid·TabControl·Content·SplitView는 전용 배치 방식을 안내함
59. Object Tree에서 컨테이너를 펼치거나 접으면 순서 변경·Undo/Redo·AXAML 적용 뒤에도 상태가 유지되고, Canvas에서 하위 컨트롤을 선택하면 부모가 자동으로 펼쳐짐
60. Object Tree의 잠기지 않은 컨테이너 자식 행을 같은 부모의 다른 자식 행 위로 드래그하면 해당 위치 앞으로 이동하고, 컨테이너 행으로 드래그하면 유효한 부모로 재배치됨
61. Toolbox 컨트롤을 Canvas 위로 드래그하면 포인터 아래 가장 안쪽의 수용 가능한 컨테이너를 강조하고, StackPanel·DockPanel·WrapPanel·UniformGrid의 삽입 위치와 Grid 셀을 포인터에 맞춰 한 번의 Undo 작업으로 배치함
62. Object Tree에 포커스를 둔 뒤 방향키로 계층을 탐색하고 `F2`로 이름 변경, `Delete`/`Backspace`로 삭제, `Ctrl+L`로 잠금 전환을 실행
63. 같은 root 또는 같은 Canvas의 형제 컨트롤을 여러 개 선택한 뒤 Edit > `Lay Out Selected`에서 `Horizontally (StackPanel)` 또는 `Vertically (StackPanel)`을 선택하면 새 레이아웃 컨테이너로 감싸고 Object Tree·Preview·Undo/Redo 결과를 확인
64. 같은 선택 상태에서 `Grid (Auto)`를 선택하면 자동 행·열 Grid를 만들고 선택 순서대로 컨트롤을 셀에 배치하며, AXAML 검증에서 `Grid.Row`·`Grid.Column`과 행/열 정의를 확인
65. 같은 선택 상태에서 `UniformGrid (Auto)`를 선택하면 동일 크기 셀과 8px 간격의 UniformGrid를 만들고 Rows·Columns·자식 순서를 AXAML과 Preview에서 확인
66. 레이아웃 컨테이너를 선택한 뒤 Edit > `Break Selected Layout`을 선택하면 컨테이너를 제거하고 자식 컨트롤을 원래 계층·좌표·순서로 복원
67. 같은 선택 상태에서 `DockPanel (Horizontal/Vertical)` 또는 `WrapPanel (Horizontal/Vertical)`을 선택하면 선택 순서 기반 자동 레이아웃을 만들고 도킹 방향·행/열·간격을 AXAML과 Preview에서 확인
68. Edit > `Edit Tab Order Map...`에서 `TabIndex | ControlName` 형식으로 전체 포커스 순서를 편집하고, `auto`/`-1`로 자동 순서를 복원하며 캔버스의 `TAB #` 배지를 확인
69. Edit > `Edit Event Handler Map...`에서 `ControlName | EventName | HandlerName` 형식으로 여러 컨트롤의 이벤트 핸들러를 일괄 편집하고, 지원되지 않는 이벤트·잠긴 컨트롤·중복 항목 검증 결과를 확인
70. View > `Live Preview`에서 컨트롤을 클릭하거나 입력·선택·토글·포인터·키보드·포커스 동작을 수행하고, 창 하단 Interaction Log에서 `Control.Event -> HandlerName` 연결 결과를 확인합니다. Toolbar·Canvas Context Menu의 `Preview Interaction Log` 또는 `Ctrl+Alt+Shift+Z`는 이미 열린 Preview의 로그를 지우지 않고 다시 활성화합니다.
71. 한 컨트롤을 선택해 Edit > `Style Clipboard > Copy Style` 또는 `Ctrl+Shift+C`를 실행한 뒤 하나 이상의 대상 컨트롤을 선택해 Edit > `Style Clipboard > Paste Style` 또는 `Ctrl+Shift+V`를 실행하고, Design Toolbar·Canvas Context Menu에서도 같은 명령을 사용할 수 있으며 잠긴 대상은 건너뛴 결과를 확인
72. Edit > `History...`, Design Toolbar의 `History`, Canvas Context Menu 또는 `Ctrl+Alt+Shift+F`를 열어 Current document state와 Undo·Redo 작업을 확인하고 원하는 항목을 선택해 여러 단계를 한 번에 이동
73. `Ctrl+N` 또는 `File > New`로 새 문서 탭을 만들고 탭 제목을 클릭해 문서를 전환합니다. `File > Open...`은 AXAML을 새 탭으로 열며 `Ctrl+W` 또는 탭의 `x` 버튼으로 현재 탭을 닫습니다.
74. 앱을 정상적으로 종료한 뒤 다시 실행하면 마지막 탭 구성과 dirty 편집 내용이 자동 복원됩니다. 복원 데이터는 `%LocalAppData%/AvaloniaUIDesigner/session.json`에 저장됩니다.
75. `Ctrl+Tab`으로 다음 문서 탭, `Ctrl+Shift+Tab`으로 이전 문서 탭을 선택합니다. 각 탭은 마지막 줌 배율과 선택한 컨트롤을 독립적으로 유지합니다.
75a. `View > Panels`에서 Toolbox·Object Tree·Property Inspector를 필요한 조합으로 표시하거나 숨기고, `Reset Panel Layout`으로 기본 크기와 표시 상태를 복원합니다. 패널 상태는 앱 종료 후 세션에 저장됩니다.
75b. 문서 탭 헤더를 좌우로 드래그하면 포인터가 놓인 탭 앞 또는 뒤로 이동하며, 이동 중 대상 탭이 주황색으로 강조됩니다. 활성 탭과 탭 순서는 앱 재시작 후에도 유지됩니다.
75c. 활성 탭에서 `Ctrl+Shift+PageUp/PageDown`을 누르거나 탭을 우클릭해 `Move Tab Left/Right`를 선택하면 탭 순서를 키보드·컨텍스트 메뉴로 변경할 수 있습니다. 같은 메뉴의 `Close Tab`은 해당 탭의 dirty 확인을 거칩니다.
75d. 탭 컨텍스트 메뉴의 `Close Tabs to Right`와 `Close Other Tabs`는 먼저 모든 대상 탭의 dirty 확인을 끝낸 뒤 일괄 제거하며, 중간에 취소하면 탭을 제거하지 않고 원래 활성 탭으로 돌아갑니다.
75e. `File > Save All` 또는 `Ctrl+Alt+S`는 dirty 상태인 모든 문서 탭을 저장하고, 저장 중 취소하거나 실패하면 원래 활성 탭으로 돌아갑니다. 파일 경로가 없는 탭은 기존 `Save AXAML` 대화상자를 순서대로 사용합니다.
75f. `File > Close All Tabs` 또는 `Ctrl+Shift+W`는 모든 문서 탭의 dirty 확인을 끝낸 뒤 탭을 닫고, 마지막 탭을 새 빈 `Untitled` 문서로 재사용합니다. 중간에 취소하거나 실패하면 탭 구성과 원래 활성 탭을 그대로 유지합니다.
75g. `File > Reopen Closed Tab` 또는 `Ctrl+Shift+T`는 현재 세션에서 마지막으로 닫은 문서 탭을 복원하며, 문서 내용·dirty 기준·Undo/Redo·줌·선택·Property Inspector 상태를 함께 되살립니다. 닫힌 탭 기록은 최대 20개까지 유지됩니다.
75h. 닫힌 탭 기록도 `%LocalAppData%/AvaloniaUIDesigner/session.json`의 `ClosedTabs`로 저장되므로 앱을 다시 실행한 뒤에도 `Reopen Closed Tab`으로 마지막 작업을 되살릴 수 있습니다. 이전 버전 세션에 이 필드가 없어도 문서 탭 복원은 정상 진행됩니다.
75i. `File > Duplicate Tab`, 탭 컨텍스트 메뉴 또는 `Ctrl+Alt+D`는 현재 문서를 새 `Untitled` dirty 탭으로 복제합니다. 원본과 독립된 문서·Undo/Redo·줌·선택·Property Inspector 상태를 유지하며, 새 경로를 지정해 저장할 수 있습니다.
75j. `File > Rename Tab...`, 탭 컨텍스트 메뉴 또는 `Ctrl+Alt+R`로 문서 탭 별칭을 지정할 수 있습니다. 별칭은 1-80자의 한 줄 이름으로 검증되며 파일 경로와 독립적으로 열린 탭·닫힌 탭 복원·세션 JSON·창 제목에 반영됩니다.
75k. 문서 탭 헤더를 중간 클릭하면 `Close Tab`과 동일한 dirty 확인을 거쳐 해당 탭을 닫습니다. 마지막 탭은 닫히지 않으며, 취소 시 원래 활성 탭으로 복원됩니다.
75l. `Ctrl+1`~`Ctrl+9`는 현재 탭 순서의 1-9번째 문서 탭을 즉시 선택합니다. 존재하지 않는 번호도 입력을 소비해 캔버스나 텍스트 편집기에 잘못 전달되지 않습니다.
75m. `View > Quick Switch Document Tab...` 또는 `Ctrl+K`는 열린 문서 탭의 별칭·저장 경로·1-based 탭 번호(`2`, `#2`, `Tab 2`)·`dirty`/`modified`/`saved`/`unsaved` 상태를 검색합니다. 결과 행에는 `Ctrl+1..9` 또는 `Tab N` 힌트와 저장 상태가 표시되며, 검색창과 결과 목록의 위/아래 방향키는 끝에서 반대쪽으로 순환합니다. `Enter`는 선택 탭으로 전환하고 `Escape`는 취소합니다.
75n. 캔버스가 포커스를 가진 상태에서 `Ctrl+=`/`Ctrl+Plus`는 10% 확대, `Ctrl+-`/`Ctrl+Minus`는 10% 축소, `Ctrl+0`은 100% 실제 크기, `F`는 현재 뷰포트에 맞춤을 실행합니다. 확대·축소는 활성 문서 탭별 Zoom 상태에 보존되며 텍스트 입력 중인 TextBox에는 전달되지 않습니다.
75o. `View > Zoom Presets`에서 25%·50%·75%·100%·125%·150%·200%를 즉시 선택하거나 `Custom...`에서 25~200%의 소수점 배율을 입력할 수 있습니다. 잘못된 값은 적용하지 않고 현재 배율을 유지합니다.
75p. 캔버스에서 두 개 이상의 컨트롤을 선택한 뒤 `Ctrl+Shift+Left/Right/Up/Down`을 누르면 각각 선택 영역의 좌·우·상·하 경계에 맞춰 정렬하고, `Ctrl+Shift+E/M`은 가로 중앙·세로 중앙에 맞춥니다. 세 개 이상 선택하면 `Ctrl+Alt+H/V`로 가로·세로 간격을 균등 분배할 수 있고, `Ctrl+Alt+Shift+W/H/S`로 선택된 컨트롤의 너비·높이·전체 크기를 맞출 수 있습니다. 크기 맞춤은 현재 주 선택 컨트롤을 기준으로 하며, Object Tree에 포커스가 있으면 방향키 탐색을 우선합니다. 모든 작업 결과는 하나의 Undo 작업과 AXAML에 반영됩니다.
75q. 선택 컨트롤의 z-order는 `Ctrl+]`/`Ctrl+[`로 한 단계 앞·뒤로 이동하고, `Ctrl+Shift+]`/`Ctrl+Shift+[`로 맨 앞·뒤로 이동합니다. 잠긴 컨트롤은 제외되며, Order 명령은 Object Tree 순서·AXAML·Undo/Redo와 함께 갱신됩니다.
75r. root 컨트롤을 선택한 뒤 `Ctrl+Alt+Shift+X/Y`로 아트보드 가로·세로 중앙에 배치하거나 `Ctrl+Alt+Shift+C`로 양축 중앙에 배치합니다. 잠긴 컨트롤과 Canvas 자식은 기존 Center on Artboard 정책에 따라 변경하지 않으며, 결과는 하나의 Undo 작업으로 기록됩니다.
75s. `Ctrl+Alt+G`로 디자인 그리드를 표시하거나 숨기고, `Ctrl+Alt+Shift+G`로 컨트롤 배치·이동·리사이즈의 Grid snap을 켜거나 끕니다. `Ctrl+Shift+G`는 기존처럼 디자인 가이드만 지우며, 세 명령은 서로 다른 작업으로 Undo할 수 있습니다.
75t. `Ctrl+Alt+1`/`2`/`3`으로 Toolbox·Object Tree·Property Inspector 패널을 각각 표시하거나 숨기고, `Ctrl+Alt+0`으로 기본 패널 크기와 가시성을 복원합니다. 숫자 키패드의 0~3도 지원하며, `View > Panels` 메뉴와 세션 저장 상태가 같은 패널 상태를 공유합니다.
75u. Toolbox·Object Tree·Property Inspector 패널을 숨긴 상태에서도 각각 `Ctrl+Alt+T/P`, `Ctrl+F`, `Ctrl+Alt+I`를 누르면 대상 패널이 자동으로 표시되고 검색 포커스 또는 Toolbox 배치 모드가 이어집니다.
75v. 두 개 이상의 같은 root 또는 같은 Canvas 형제 컨트롤을 선택한 뒤 `Ctrl+G`를 누르면 `Group Selected into Canvas`와 동일하게 묶이고, 그룹 Canvas를 선택한 뒤 `Ctrl+Shift+U`를 누르면 원래 부모·좌표를 보존하며 해제됩니다. 두 작업은 각각 하나의 Undo 단계로 기록됩니다.
75w. 캔버스 빈 영역에서 왼쪽→오른쪽으로 marquee를 그리면 컨트롤 bounds가 선택 사각형 안에 완전히 들어온 요소만 선택하고, 오른쪽→왼쪽으로 그리면 일부가 걸친 요소도 선택합니다. `Ctrl` 또는 `Shift`를 누른 채 드래그하면 기존 선택을 유지한 채 결과를 추가합니다.
75x. 두 개 이상의 컨트롤을 선택한 뒤 `Ctrl+Alt+Shift+1`/`2`로 가로·세로 StackPanel, `3`으로 Grid, `4`로 UniformGrid, `5`/`6`으로 가로·세로 DockPanel, `7`/`8`로 가로·세로 WrapPanel을 즉시 적용합니다. 레이아웃 컨테이너를 선택한 뒤 `Ctrl+Shift+B`를 누르면 `Break Selected Layout`과 동일하게 자식들을 원래 부모와 좌표 정책으로 되돌립니다.
75y. Marquee 선택은 `IsLocked`인 컨트롤과 숨겨진 Tab 페이지처럼 `IsVisibleOnArtboard`가 false인 요소를 자동으로 건너뜁니다. 잠긴 컨트롤을 직접 클릭하면 기존처럼 선택해 속성을 검사할 수 있습니다.
75z. 겹친 컨트롤을 편집할 때 `Alt+클릭`은 포인터 아래 visible 요소를 z-order 앞쪽부터 순환 선택하고, `Alt+Shift+클릭`은 뒤쪽 방향으로 순환합니다. 현재 선택이 같은 hit stack에 있으면 다음 요소로 이동하며, 숨겨진 요소는 제외하고 잠긴 요소는 속성 검사를 위해 선택할 수 있습니다.
75aa. Object Tree에서 일반 클릭한 행이 선택 anchor가 됩니다. `Shift+클릭` 또는 `Shift+Up/Down`은 현재 펼쳐진 계층과 검색 결과에 표시된 순서로 anchor부터 대상까지 선택하고, `Ctrl+Shift+클릭` 또는 `Ctrl+Shift+Up/Down`은 기존 선택을 유지한 채 범위를 추가합니다. 접힌 부모 아래의 보이지 않는 자식은 범위에 포함하지 않습니다.
75ab. Edit 메뉴, Canvas context menu, `Ctrl+A`의 Select All은 `IsVisibleOnArtboard=true`이고 `IsLocked=false`인 요소만 선택합니다. 선택 가능한 요소가 없으면 기존 선택을 비우고 상태바에 안내하며, Object Tree나 직접 클릭으로 잠긴·숨겨진 요소를 검사하는 기능은 바뀌지 않습니다.
75ac. 잠긴 컨트롤을 직접 선택해도 Object Tree와 Property Inspector에서 검사할 수 있지만 `Edit > Copy`·`Duplicate`와 `Ctrl+C`·`Ctrl+D`는 잠금 해제 선택만 처리합니다. 잠금 해제 요소와 잠긴 요소를 함께 선택하면 잠금 해제 계층만 복사·복제하고, 잠긴 요소만 대상으로 한 명령은 문서와 기존 클립보드를 변경하지 않습니다.
75ad. 캔버스에서 일반 클릭한 요소가 Canvas 선택 anchor가 됩니다. `Shift+클릭`은 anchor부터 대상까지 visible Canvas 순서 범위를 선택하고, `Ctrl+Shift+클릭`은 기존 선택을 유지한 채 범위를 추가하며 reverse 방향에서도 클릭한 target을 Object Tree 활성 행으로 유지합니다. hidden 요소는 범위에서 제외하며, 유효한 anchor가 없으면 기존 Shift 단일 추가로 fallback하고, `Ctrl+클릭` 토글·잠긴 컨트롤 직접 검사·범위 입력의 이동/Quick Edit 차단을 유지합니다.
75ae. `Ungroup Selected Canvas`와 `Break Selected Layout`은 선택된 컨테이너가 잠겼거나 직접 자식 중 하나라도 잠겨 있으면 실행하지 않습니다. 상태바에 잠금 보호 이유를 표시하고 계층·좌표·History를 변경하지 않으며, 잠금 해제 후 같은 명령을 다시 실행할 수 있습니다.
75af. Canvas marquee는 `Ctrl+드래그`와 `Shift+드래그`를 같은 additive 선택으로 처리합니다. modifier가 없으면 기존 선택을 교체하고, 좌우 드래그 방향의 포함/교차 판정과 잠긴·숨겨진 컨트롤 제외 정책은 그대로 유지합니다.
75ag. Canvas 빈 영역에서 `Alt+드래그`하면 marquee 결과에 포함된 보이는 잠금 해제 컨트롤만 현재 선택에서 제거합니다. 잠긴·숨겨진 선택은 보존하고, 이동하지 않은 작은 Alt 클릭도 선택을 비우지 않으며, 좌우 드래그의 포함/교차 판정은 일반 marquee와 같습니다.
75ah. 캔버스에 포커스가 있고 단일 컨테이너 자식을 선택한 상태에서 `Escape`를 누르면 Object Tree·Property Inspector 선택을 부모 컨테이너로 함께 올립니다. 부모가 없는 root 선택, 다중 선택, 진행 중인 marquee 또는 Toolbox 배치에서는 기존처럼 선택 도구·배치 취소를 우선합니다.
75ai. 캔버스에 포커스가 있고 단일 컨테이너를 선택한 상태에서 `Enter`는 첫 visible 직접 자식, `Shift+Enter`는 마지막 visible 직접 자식으로 선택을 내립니다. 숨겨진 자식만 있는 컨테이너·자식이 없는 컨트롤·다중 선택·Toolbox 배치 모드에서는 Window가 키를 소비하지 않으며, Object Tree와 Property Inspector 선택은 기존 동기화 경로를 사용합니다.
75aj. 캔버스에 포커스가 있고 단일 요소를 선택한 상태에서 `Alt+Left/Up`은 같은 부모의 이전 visible 형제, `Alt+Right/Down`은 다음 visible 형제로 선택을 이동합니다. 현재 선택이 hidden이면 방향에 맞는 첫/마지막 visible sibling으로 복구하고, root 요소도 Window 형제로 취급하며, `Alt+Shift+Arrow`는 기존 10px nudge를 유지합니다.
75ak. 캔버스 `Tab`/`Shift+Tab` 선택 순환은 `Edit Tab Order...` 또는 `Edit Tab Order Map...`으로 지정한 `TabIndex`를 낮은 값부터 반영합니다. `auto/-1` 컨트롤은 명시 순서 뒤에 원래 Canvas 순서로 배치되고, 숨겨진 컨트롤은 계속 제외되며 선택이 끝나면 처음/마지막으로 wrap합니다. `Shift+Tab`은 anchor부터 이전 target까지 Tab Order 범위를 기존 선택에 누적합니다.
75al. Canvas `Tab`/`Shift+Tab`은 `IsTabStop=false`로 제외된 컨트롤을 건너뜁니다. `Edit > Include / Exclude from Tab Navigation`의 상태가 즉시 순환 후보에 반영되며, 모든 후보가 제외되면 기존처럼 선택 실패 상태를 표시합니다.
75am. Canvas `Tab`/`Shift+Tab`은 `IsEnabled=false` 또는 `Focusable=false`인 컨트롤도 건너뛰어 Avalonia 키보드 포커스 조건과 맞춥니다. 활성화·포커스 가능·탭 정지 조건이 모두 맞는 후보만 기존 `TabIndex`와 Canvas 순서로 순환합니다.
75an. 캔버스에서 단일 요소를 선택한 뒤 `Ctrl+Left/Right/Up/Down`은 해당 방향에 있는 보이는 후보 중 같은 축으로 겹치는 요소를 우선하고 축 거리·보조 거리·Canvas 순서로 가장 가까운 요소를 선택합니다. 후보가 없거나 다중 선택이면 입력을 소비하지 않아 기존 화살표 nudge 경로를 보존합니다.
75ao. Canvas의 선택·marquee·겹침 hit-test·Toolbox drop·키보드 탐색은 요소 자신의 `Visual.IsVisible`과 모든 부모의 visibility를 함께 확인합니다. 숨겨진 부모 아래 자식이나 `Visual.IsVisible=false` 요소는 캔버스 후보에서 제외되지만 Object Tree와 직접 검사를 통한 편집 경로는 유지합니다.
75ap. Escape 부모 선택·Enter 자식 진입·Alt 형제 탐색도 같은 visibility 판정을 사용해 hidden 계층을 건너뜁니다. hidden 선택에서 형제 탐색을 시작하면 이전 방향은 마지막 visible sibling, 다음 방향은 첫 visible sibling을 선택해 키보드 탐색이 멈추지 않습니다.
75aq. 캔버스에 포커스가 있고 Toolbox 배치 모드가 아닐 때 `Home`은 Canvas 순서의 첫 visible 컨트롤, `End`는 마지막 visible 컨트롤을 선택하고, `Shift+Home/End`는 anchor부터 해당 경계까지 visible 범위를 기존 선택에 누적합니다. `Ctrl`·`Alt`가 붙은 입력이나 후보가 없는 캔버스에서는 기존 입력과 상태를 보존합니다.
75ar. 캔버스에 포커스가 있고 Toolbox 배치 모드가 아닐 때 `PageUp`은 Canvas 순서의 이전 visible 컨트롤, `PageDown`은 다음 visible 컨트롤을 선택해 끝에서 반대쪽으로 wrap하고, `Shift+PageUp/PageDown`은 해당 요소를 기존 선택에 누적합니다. `Ctrl+Shift+PageUp/PageDown` 문서 탭 이동은 기존 동작을 유지합니다.
75as. Object Tree에 포커스가 있을 때 `Shift+Up/Down`은 현재 펼쳐진 visible 행 순서에서 anchor부터 이전/다음 target까지 범위를 선택하고, `Ctrl+Shift+Up/Down`은 기존 선택에 범위를 추가합니다. 첫/마지막 visible 행에서는 범위를 wrap하지 않으며, 접힌 계층과 검색 결과 순서를 그대로 사용합니다.
75at. Object Tree에 포커스가 있을 때 `Shift+Home/End`는 anchor부터 첫/마지막 visible 행까지 범위를 선택하고, `Ctrl+Shift+Home/End`는 기존 선택에 해당 경계 범위를 추가합니다. 검색 결과·펼침 상태를 반영하며 현재 경계에서 다시 누르면 선택과 active 행을 유지합니다.
75au. Canvas·Object Tree·Undo/Redo·문서 탭 전환으로 선택이 바뀌면 Object Tree는 접힌 조상 경로를 먼저 펼치고 선택된 행을 자동으로 `ScrollIntoView`합니다. 지연된 선택 변경이 뒤늦게 실행되어도 최신 선택 행만 스크롤해 이전 선택으로 되돌아가지 않습니다.
75av. Object Tree·Canvas 키보드·문서 탭 전환으로 visible 컨트롤을 선택하면 DesignHost도 최신 active 요소를 `ScrollIntoView`해 큰 아트보드의 viewport를 따라갑니다. hidden·부모 visibility 제외 요소는 직접 검사 정책을 유지하며 자동 이동하지 않습니다.
75aw. Toolbox 항목을 아트보드로 드래그할 때 viewport 가장자리 48px 안쪽에 포인터가 있으면 수평·수직 ScrollViewer offset을 한 번에 24px씩 자동 이동합니다. 이동 후 Grid·StackPanel·Content target preview와 drop 판정을 새 좌표로 갱신하고, 이미 scroll 한계에 도달하면 offset을 더 변경하지 않습니다.
75ax. 캔버스 요소 이동·리사이즈 또는 marquee 선택 중에도 같은 edge auto-pan을 사용합니다. 새 ScrollViewer offset을 반영한 DesignHost 좌표로 Transform·Smart Snap·포함/교차 marquee 판정을 계속 계산하며, middle-button viewport pan은 별도 입력으로 유지합니다.
75ay. 가로·세로 ruler에서 guide를 드래그하는 동안에도 viewport 가장자리 auto-pan을 사용합니다. scroll offset이 바뀐 뒤 guide coordinate를 다시 계산해 화면 좌표가 아니라 실제 아트보드 좌표를 유지하며, guide를 viewport 밖으로 놓으면 기존 제거 정책을 적용합니다.
75az. `Ctrl+Alt+P` Toolbox 배치 모드에서 빈 Canvas 가장자리로 ghost를 이동하면 같은 edge auto-pan을 사용합니다. offset을 먼저 갱신한 뒤 새 DesignSurface 좌표로 ghost와 target hint를 다시 계산해 큰 아트보드 끝 영역에도 클릭 배치할 수 있습니다.
75ba. Canvas 또는 요소 위에서 `Space`를 누른 채 왼쪽 드래그하면 임시 viewport pan을 시작해 선택·marquee·Toolbox 배치 입력을 가로채지 않습니다. Space를 먼저 놓으면 다음 pointer move에서 캡처와 pan 상태를 정리하고, 중간 버튼 팬과 기존 배치·편집 드래그는 그대로 유지합니다.
75bb. `Ctrl+Alt+Left/Right/Up/Down`은 큰 아트보드 viewport를 96px씩 이동하고 ScrollViewer의 양축 경계에서 offset을 clamp합니다. Object Tree에 포커스가 있으면 기존 계층 방향키 탐색을 우선하며, View 메뉴와 키보드 도움말에 팬 입력을 표시합니다.
75bc. `Ctrl+Shift+F` 또는 Canvas context menu의 `Fit Selected to View`는 선택 bounds와 viewport 여백으로 25~200% 배율을 계산하고, zoom layout 이후 선택 중심을 다시 계산해 ScrollViewer offset을 맞춥니다. 선택이 없으면 문서·zoom·history를 변경하지 않고 안내 상태만 표시하며, 기존 `Ctrl+F` Object Tree 검색과 `F` 전체 canvas fit은 유지합니다.
75bd. `Ctrl+=`/`Ctrl+Plus`/`Ctrl+-`/`Ctrl+Minus`와 `View > Zoom In`/`Zoom Out`은 viewport 중앙의 아트보드 좌표를 확대·축소 전후 동일하게 유지합니다. 기존 Ctrl+휠은 포인터 좌표를 기준으로 동작하며 모든 입력은 layout 갱신 후에도 ScrollViewer offset을 재적용합니다.
75be. `Ctrl+0`, `View > Actual Size`, `View > Zoom Presets` 및 `Custom...`도 같은 viewport 중앙 좌표 보존 경로를 사용합니다. 25~200% 범위 밖이거나 유한하지 않은 preset 값은 적용하지 않으며 Fit to View·Fit Selected to View처럼 의도적으로 화면을 재구성하는 명령은 기존 중심 맞춤 정책을 유지합니다.
75bf. Desktop·Tablet·Mobile·Rotate·Custom Artboard Size 변경은 layout 이전 viewport 중앙의 아트보드 좌표를 저장하고 새 ScrollViewer extent·scrollbar 상태에 맞춰 offset을 다시 적용합니다. 따라서 큰 아트보드에서 작업하던 위치가 preset 전환으로 튀지 않으며, 새 아트보드 경계를 벗어난 좌표는 기존 clamp 정책을 사용합니다.
75bg. Object Tree 검색창에서 `Escape`를 누르면 검색어·검색 결과 표시를 지우고 TreeView로 포커스를 돌려보냅니다. 현재 Canvas 선택과 Object Tree 선택은 유지하므로 검색 결과를 확인한 뒤 즉시 방향키·F2·Delete·`Ctrl+L` 계층 편집을 이어갈 수 있습니다.
75bh. Property Inspector 필터에서 `Escape`를 누르면 필터·결과를 지우고 focusable PropertyGrid로 포커스를 돌려보냅니다. 속성 검색을 끝낸 뒤 즉시 방향키 기반 탐색과 속성 편집을 이어갈 수 있습니다.
75bi. Toolbox 검색창에서 일반 상태로 `Escape`를 누르면 검색어·필터 결과만 지우고 검색 포커스를 유지합니다. 배치 모드 중 `Escape`는 검색어와 현재 Canvas 선택을 보존한 채 배치 모드와 ghost만 취소합니다.
75bj. `Edit AXAML Source...`의 multiline 편집기에서 `Escape`는 소스 변경을 적용하지 않고 대화상자를 닫으며, `Ctrl+Enter`는 기존 Apply 검증·단일 Undo 경로를 실행합니다. 유효하지 않은 소스는 오류를 표시한 채 편집을 계속할 수 있습니다.
75bk. 이름·스타일·색상 리소스·경로·툴팁·이벤트 맵 등 공통 텍스트 편집기에서도 `Escape`는 취소 결과를 반환하고 `Ctrl+Enter`는 Apply 결과를 반환합니다. multiline 편집의 일반 `Enter`는 줄바꿈을 유지합니다.
75bl. Binding·Items/Columns·Sample Data 전문 편집기도 같은 키보드 계약을 사용합니다. `Ctrl+Enter`의 검증이 실패하면 오류를 편집기에 남기고, `Escape`는 변경·Undo 기록 없이 취소합니다.
75bm. Grid Definitions·DataGrid Behavior·Typography·Transform 속성 편집기는 입력 컨트롤 종류와 관계없이 dialog 포커스에서 `Escape` 취소와 `Ctrl+Enter` 적용을 지원하며, 검증 실패 시 대화상자를 유지합니다.
75bn. Accessibility·Interaction·Effects·Range·Text Input·SelectableTextBlock 속성 편집기도 기존 Apply Click 검증 경로를 키보드에서 재사용해 dialog 포커스에서 `Escape` 취소와 `Ctrl+Enter` 적용을 지원합니다.
75bo. SplitView·TabControl·MaskedTextBox·Selection·Date & Time·ColorPicker·AutoCompleteBox·Toggle·Disclosure & Scrolling·Image·Button·Layout·Root·GridSplitter 속성 편집기도 기존 Apply Click 경로를 재사용해 dialog 포커스에서 `Escape` 취소와 `Ctrl+Enter` 적용을 지원합니다.
75bp. OS 클립보드의 Window/UserControl AXAML을 `Edit > Paste AXAML from Clipboard` 또는 `Ctrl+Alt+V`로 현재 문서에 붙여넣을 수 있습니다. imported control 이름은 `_copy` 계열로 고유화되고, 색상 리소스·스타일 클래스 충돌은 별칭 처리되며 모든 컨트롤·리소스·스타일 변경은 하나의 Undo entry로 기록됩니다.
75bq. 여러 root 또는 같은 Canvas의 직접 형제 컨트롤을 선택해 `Copy Selected AXAML`/`Export Selected AXAML...`을 실행하면 선택 bounds 크기의 `UserControl > Canvas` 래퍼가 생성되고 각 루트는 좌상단 기준 상대 `Canvas.Left`/`Canvas.Top`으로 출력됩니다. Grid·StackPanel·Content 자식이나 서로 다른 부모가 섞인 선택은 좌표 손실을 막기 위해 거부되며, 단일 선택은 기존 독립 루트 형식을 유지합니다.
75br. AXAML fragment를 붙여넣을 때 잠금 해제된 지원 컨테이너 하나가 선택되어 있으면 imported root마다 해당 부모의 빈 Grid 셀, StackPanel/DockPanel/WrapPanel/UniformGrid 순서, Canvas 로컬 좌표, TabControl/SplitView 슬롯 또는 단일 Content 관계를 사용합니다. 슬롯이 부족하거나 단일 Content에 여러 root를 붙이려 하면 전체 작업을 거부하며, 붙여넣은 컨트롤은 새 선택으로 전환되고 Undo 한 번으로 컨트롤·부모 관계·리소스·스타일 변경을 함께 되돌립니다.
75bs. 지원 컨테이너 context paste는 AXAML 파싱 시점의 root 이름을 별도로 추적해 문서 적용 후에도 모든 imported root를 정확히 재부모화합니다. 잠긴 imported root도 부모 슬롯 배치만 허용하고 기존 잠금 상태는 유지하며, 여러 root 중 하나라도 슬롯 검증에 실패하면 전체 컨트롤·부모 관계·리소스·스타일 변경을 원자적으로 rollback합니다.
75bt. 내부 `Ctrl+V` hierarchy clipboard도 선택된 잠금 해제 지원 컨테이너를 대상으로 사용합니다. Canvas는 원래 아트보드 좌표와 target origin의 차이를 로컬 좌표로 보정하고, 다른 컨테이너는 기존 빈 셀·순서·탭·Content 슬롯 재부모화 경로를 사용하며, 대상 슬롯이 부족하면 붙여넣기 전 문서 snapshot으로 복원합니다.
75bu. 내부 `Copy`·`Cut`은 선택 계층뿐 아니라 복사 시점의 색상 리소스와 문서 스타일을 clipboard metadata로 보존합니다. 다른 문서 탭에 `Ctrl+V`하면 충돌 리소스와 스타일 클래스는 `_Imported` 계열로 고유화되고, 스냅샷의 `DynamicResource`·`Classes` 참조도 같은 별칭으로 재작성되며 컨트롤·부모 관계·리소스·스타일 변경 전체가 하나의 Undo entry로 되돌아갑니다.
75bv. 가로·세로 ruler에서 만든 Design Guide와 `Show Design Guides`·`Snap to Guides` 상태는 활성 문서 탭 전환 시 이전 탭 snapshot에 저장되고 새 탭의 snapshot을 복원합니다. 따라서 여러 화면을 번갈아 정렬해도 guide가 섞이지 않으며, 새 문서·새 AXAML·명시적 `Clear Design Guides`는 현재 탭의 guide만 비웁니다.
75bw. Design Guide snapshot은 문서 탭 세션 JSON에도 함께 저장되므로 정상 종료 후 앱을 다시 시작하거나 닫힌 탭을 다시 열어도 가이드 좌표·표시·스냅 상태가 복원됩니다. 기존 세션 JSON에 guide 필드가 없거나 잘못된 좌표가 포함된 경우에는 현재 아트보드 범위의 유효한 좌표만 적용하고 나머지는 기본값으로 안전하게 무시합니다.
75bx. 문서 탭의 Canvas ScrollViewer 가로·세로 오프셋도 탭 상태와 세션 JSON에 저장됩니다. 탭 전환·앱 재시작·닫힌 탭 복원 후 새 문서의 실제 viewport/extent 범위에 맞춰 안전하게 clamp하므로 확대된 아트보드에서 작업하던 위치를 잃지 않으며, 기존 세션에는 `(0, 0)`을 적용합니다.
75by. viewport 복원에는 pending 세대가 부여되며, 이전 탭의 늦은 `LayoutUpdated`/dispatcher callback은 무시됩니다. 복원 pending 동안에는 선택 요소의 자동 `ScrollIntoView`도 보류하고, 저장 오프셋을 적용한 뒤 idle 시점에만 정상 선택 추적을 재개해 빠른 탭 순환에서도 작업 위치가 흔들리지 않습니다.
75bz. `Edit Tab Order...`의 초기값은 자동 순서일 때 `2147483647` 대신 `auto`로 표시하며, `auto`/`-1`은 Avalonia 자동 순서로 정규화합니다. 단일 편집기와 `Edit Tab Order Map...`은 `0~10000` 범위·중복 검증·Undo/Redo 규칙을 같은 parser로 공유해 입력 결과가 달라지지 않습니다.
75ca. Design Toolbar의 `Tabs` 그룹 또는 디자인 캔버스 Context Menu에서 `New Tab`, `Quick Switch`, `Duplicate`, `Close`, `Reopen Closed Tab`을 실행할 수 있으며, 기존 `Ctrl+N`·`Ctrl+K`·`Ctrl+Alt+D`·`Ctrl+W`·`Ctrl+Shift+T` 단축키와 동일한 명령을 공유합니다.
75cb. `View > Switch to Recent Document Tab`, Design Toolbar의 `Recent`, Canvas Context Menu 또는 `Ctrl+Alt+Tab`은 현재 탭을 제외한 가장 최근 사용 탭으로 전환합니다. `Ctrl+Alt+Shift+Tab`은 MRU 목록의 더 오래된 방향으로 이동하며, 최근 사용 순서는 탭 세션의 `RecentDocumentTabIndexes`로 저장·복원됩니다.
75cc. `File > Open Recent Files...`, Design Toolbar의 `Recent Files`, Canvas Context Menu 또는 `Ctrl+Shift+O`는 최근 8개 AXAML 경로를 파일명·전체 경로로 검색합니다. 결과에는 `Available`/`Missing` 상태가 표시되고, `Enter`는 선택 파일을 새 탭으로 열며 누락 파일은 목록에서 자동 제거합니다.
75cd. `File > Open Project Folder...`로 선택한 작업 폴더는 `Project Explorer... (Ctrl+Shift+P)`에서 AXAML 파일을 상대 경로로 검색할 수 있습니다. `bin`·`obj`·`.git`·`.vs`·`node_modules`는 스캔에서 제외하고, `Refresh Project Files`로 새 파일을 다시 읽으며 선택 폴더는 `%LocalAppData%/AvaloniaUIDesigner/project-workspace.json`에 저장·복원됩니다.
75ce. 열린 프로젝트 폴더의 파일 watcher는 AXAML 생성·삭제·이동을 350ms debounce 후 Explorer 목록에 반영합니다. 현재 문서 파일의 외부 변경은 파일 timestamp를 내부 저장 시점과 비교해 감지하며, `Reload Current File (Ctrl+Shift+R)`은 dirty 확인·AXAML 검증·Undo 초기화·현재 탭 유지 순서로 안전하게 다시 로드합니다.
75cf. `View > Preview Theme`의 `System Default`·`Light`·`Dark` 선택은 Preview 창의 `RequestedThemeVariant`만 전환해 문서·Undo 상태를 변경하지 않습니다. 열린 Preview 창에 즉시 적용되고, Preview 새로고침·문서 변경·Interaction Log 재진입 후에도 선택한 변형을 유지합니다.
75cg. `Project Explorer... (Ctrl+Shift+P)`는 상대 경로를 폴더·파일 계층으로 표시합니다. 빈 검색에서는 폴더 접기/펼치기 상태와 순환 키보드 선택을 유지하고, 검색어가 있으면 일치한 AXAML 파일과 필요한 조상 폴더만 `[+]`/`[-]` 트리로 남겨 Enter로 바로 엽니다.
75ch. Project Explorer를 다시 열면 `CurrentDocumentPath`에 해당하는 AXAML 파일까지 조상 폴더를 자동으로 펼쳐 현재 작업 위치를 reveal합니다. 폴더 접기 상태는 `%LocalAppData%/AvaloniaUIDesigner/project-workspace.json`의 `CollapsedFolders`로 저장하며, 이전 버전의 문자열 경로 형식도 계속 읽습니다.
75ci. Project Explorer 목록에 포커스가 있으면 `Left`는 펼친 폴더를 접고 접힌 폴더·파일에서 부모로 이동하며, `Right`는 접힌 폴더를 펼치고 펼친 폴더에서 첫 자식으로 이동합니다. 방향키로 변경한 폴더 상태도 `CollapsedFolders`에 즉시 저장됩니다.
75cj. Project Explorer 파일은 더블클릭으로 기존 Enter와 같은 새 문서 탭 열기·dirty 보호 흐름을 사용하며, 폴더 더블클릭은 접기/펼치기와 `CollapsedFolders` 저장을 수행합니다.
75ck. Project Explorer를 열어 둔 상태에서 watcher가 AXAML 생성·삭제·이동을 감지하면 검색어·현재 선택·저장된 폴더 접기 상태를 유지한 채 tree와 일치 개수를 갱신합니다. dialog가 닫히면 collection change 구독도 해제합니다.
75cl. Project Explorer 선택 노드의 context menu에서 `Copy Relative Path`·`Copy Full Path`를 실행할 수 있으며, 목록에 포커스가 있으면 `Ctrl+C`가 full path를 OS clipboard에 복사하고 상태바에 결과를 표시합니다. 파일은 실제 경로를 사용하고 폴더는 현재 workspace root와 상대 경로를 조합합니다.
75cm. Project Explorer 선택 노드의 context menu에서 `Open in File Manager`를 실행하면 파일은 부모 폴더를, 폴더는 해당 폴더를 OS 기본 파일 관리자로 엽니다. 유효하지 않거나 접근할 수 없는 위치는 상태바에 실패 원인을 표시하고 디자이너 실행을 중단하지 않습니다.
75cn. Project Explorer는 선행 공백 기반 문자열 대신 명시적인 item template로 폴더 깊이·`[+]`/`[-]` 상태·`[F]` 파일 marker·상대 경로를 렌더링해 폰트와 플랫폼에 관계없이 계층을 유지합니다.
75co. Project Explorer에서 행을 우클릭하면 context menu를 열기 전에 해당 `ListBoxItem`을 선택해 `Copy Relative Path`·`Copy Full Path`·`Open in File Manager`가 마우스로 지정한 행에 일관되게 적용됩니다.
76. `File > Load Component Pack...` 또는 `File > Load Toolbox Preset Pack...`으로 외부 Toolbox 팩을 추가하면 파일 경로가 세션에 등록되어 다음 실행 때 자동으로 다시 로드됩니다. 파일이 없어도 문서 탭 복원은 계속되며 상태바에 경고가 표시됩니다.
77. 외부 프로젝트의 컨트롤은 Component Pack 항목에 `designOnly: true`, `avaloniaTypeName`, `previewText`, `defaultProperties`를 지정해 등록합니다. 디자이너에서는 타입명 플레이스홀더로 편집하고, 생성 AXAML에는 원래 커스텀 타입과 속성을 출력합니다. 예시는 [custom-component-pack.example.json](docs/custom-component-pack.example.json)을 참고하세요.
78. `File > Load Component Pack Plugin...`에서 `IComponentPackPlugin`을 구현한 신뢰할 수 있는 DLL을 선택하면 플러그인이 제공한 Component Pack을 Toolbox에 등록합니다. DLL 경로는 세션 JSON의 `ComponentPluginPaths`에 저장되고, 앱 재시작 시 플러그인·JSON 팩·프리셋 팩 순서로 복원됩니다.
79. `File > Manage Component Packs...`에서 로드된 JSON/DLL 팩의 출처와 컴포넌트를 확인하고 `Remove Pack`으로 Toolbox 등록과 세션 경로를 제거합니다. 현재 문서나 프리셋에서 사용 중인 커스텀 타입은 AXAML 타입명을 유지하는 디자인 전용 placeholder로 남으며, 플러그인 DLL 자체는 앱 재시작 전까지 프로세스에 로드된 상태입니다.
80. 여러 컨트롤을 선택한 뒤 `Edit > Edit Common Properties...`를 열면 공통 Margin·Horizontal/VerticalAlignment·Opacity·IsEnabled·IsVisible·IsHitTestVisible을 일괄 편집합니다. 혼합 값은 빈 입력 또는 3상태 체크박스로 표시되며, 빈 값은 각 컨트롤의 기존 값을 유지합니다.
81. Toolbox 상단 카테고리 선택기에서 `All categories`, Layout, Containers, Input, Display, Shapes 또는 외부 팩이 제공한 카테고리를 고르고, 카테고리 헤더를 접거나 펼치며 정렬된 카드의 카테고리 칩·Avalonia 타입 힌트를 확인하고 검색어로 결과를 좁힙니다. 검색·필터 후에도 각 그룹의 접힘 상태와 선택 항목을 유지합니다.
82. Toolbox 카드의 별표 버튼으로 항목을 `Favorites` 그룹에 고정하거나 해제하고, Canvas에 배치한 항목은 최근 사용 순서로 `Recent` 그룹에 자동 기록됩니다. 최근 항목은 최대 8개이며 두 그룹의 상태는 세션 JSON에 함께 복원됩니다.
83. Toolbox 검색창에 이름 또는 타입을 입력하고 `Enter`를 누르면 첫 번째 결과가 선택되어 아트보드 중앙에 빠르게 배치됩니다. 카테고리 내부 목록은 방향키로 탐색할 수 있으며, `Enter`는 선택 항목 배치, `Space`는 즐겨찾기 토글입니다.
84. `Ctrl+Alt+T`로 Toolbox 검색창에 포커스하고 `Ctrl+Alt+P` 또는 헤더 `Pick`으로 배치 모드를 켭니다. 선택된 항목은 헤더에 표시되며 Canvas 클릭으로 계속 배치할 수 있고, 헤더 `Cancel`, `Ctrl+Alt+P` 재입력 또는 `Escape`로 선택을 해제합니다.
85. 배치 모드에서 Canvas 위로 포인터를 움직이면 반투명 ghost와 항목명, 기본 크기, 스냅된 `x, y`가 표시됩니다. 포인터를 Canvas 밖으로 옮기거나 드래그 앤 드롭을 시작하면 ghost가 숨겨지고, 클릭하면 표시된 좌표에 배치됩니다.
86. 배치 모드에서 Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·Canvas·TabControl·SplitView·Content 컨테이너 위로 포인터를 옮기면 수용 가능한 target이 주황색 outline으로 강조됩니다. 해당 영역을 클릭하면 Toolbox 컨트롤이 target에 자동 삽입되고, 빈 셀·삽입 순서·단일 Content 슬롯 검증 결과가 상태바와 Object Tree에 반영됩니다.
87. 정밀 target feedback은 Grid/UniformGrid의 `R# C#` 셀, StackPanel/DockPanel/WrapPanel의 `insert #` 삽입선, TabControl/SplitView/Content의 슬롯 bounds를 ghost 상세에 표시해 클릭 전에 실제 계층 배치 결과를 확인하게 합니다.
88. Properties 패널 헤더에서 선택 컨트롤의 타입을 확인하고 `Categories`/`Flat`으로 속성 그룹을 전환하거나 `Expand`/`Collapse`로 모든 카테고리를 펼치고 접습니다. Quick Filter에 속성명을 입력하면 현재 선택 컨트롤의 속성을 즉시 좁힙니다.
89. Properties 패널의 `Filter properties...` 입력창에 속성명을 입력하고 `Clear` 또는 `Escape`로 초기화합니다. `Ctrl+Alt+I`는 필터에 포커스하고 현재 입력을 선택하며, 컨트롤을 바꿔도 필터가 유지됩니다.
90. 캔버스 상단 `Design` 도구 모음은 메뉴를 열지 않고도 문서 저장·Undo/Redo·Live Preview, 선택 정렬·분배·크기 맞춤, Canvas 그룹·레이아웃 해제, StackPanel/Grid/UniformGrid 생성, Fit/Zoom을 실행합니다. 가로 공간이 부족하면 한 줄을 수평 스크롤하며, 다중 선택이 필요한 버튼은 선택 상태와 같은 검증 규칙으로 자동 비활성화됩니다.
91. Selection Breadcrumb의 각 `Parent / Child` 항목은 클릭 가능한 선택 버튼으로 렌더링되어 깊은 중첩 계층의 원하는 컨테이너로 한 번에 이동합니다. `DesignElement.ParentName` 또는 `DisplayName`이 바뀌면 경로 항목·툴팁·선택 요약이 즉시 다시 계산되어 Object Tree 재탐색 없이 현재 편집 컨텍스트를 유지합니다.
92. Design Toolbar에 `Copy`·`Paste`·`Delete`, `Front/Back/Forward/Backward`, 아트보드 `X/Y/XY` 중앙 정렬, `Dock H/V`·`Wrap H/V`, `Lock`, `Grid`·`Snap`을 추가해 반복적인 메뉴 탐색 없이 기본 Form Editor 작업을 수행합니다. 선택이 없거나 다중 선택이 필요한 조건에서는 동일한 기존 검증 규칙으로 버튼을 비활성화하며 Grid/Snap 토글은 View 메뉴 상태와 동기화됩니다.
93. Design Toolbar의 `Select`는 현재 Toolbox 배치 모드와 배치 미리보기를 즉시 취소하고 Canvas 선택 도구로 돌아갑니다. `Pick`은 Toolbox의 첫 표시 항목을 기준으로 배치 모드를 토글하며 현재 `Place: ...` 상태와 `Ctrl+Alt+P` 경로를 같은 상태 바인딩으로 유지합니다. `Copy Style`·`Paste Style`은 기존 시각 스타일 클립보드를 Toolbar에서 바로 재사용합니다.
94. `Edit > Align to Artboard` 또는 Design Toolbar의 `Form` 명령은 선택된 잠금 해제 root 컨트롤을 아트보드 Left/Right/Top/Bottom 가장자리에 직접 배치합니다. `Ctrl+Alt+Shift+Arrow` 단축키도 같은 명령을 사용하며 Canvas·Grid·StackPanel 등 부모 레이아웃이 위치를 관리하는 자식은 거부하고, 이동 결과는 하나의 Undo/Redo와 AXAML 변경으로 기록합니다.
95. Design Toolbar의 `Guides > Show`·`Snap`·`Clear`는 View 메뉴의 Design Guides 상태와 같은 탭별 상태를 사용합니다. 다른 문서 탭으로 전환하거나 세션을 복원해도 현재 탭의 가이드 표시·Guide Snap 토글이 Toolbar와 메뉴에 함께 반영되며, `Clear`는 현재 탭의 수평·수직 가이드만 제거합니다.
96. Design Toolbar의 `Artboard` 그룹에서 `Desktop`(1280 x 800)·`Tablet`(1024 x 768)·`Mobile`(390 x 844)·`Rotate`·`Custom`을 바로 실행하고 현재 `Width x Height`를 실시간으로 확인합니다. 프리셋·회전·사용자 지정 크기는 기존 View 메뉴와 같은 viewport 중심 보존·문서 설정·세션 복원 경로를 사용합니다.
97. Canvas 빈 영역의 Context Menu가 `Copy`·`Copy Style`·`Cut`·`Paste`·`Paste Style`·`Paste AXAML`·`Duplicate`·`Delete`와 `Order`·`Arrange`·`Center/Align to Artboard`·`Group/Ungroup/Break`를 노출합니다. 선택이 없거나 다중 선택이 필요한 명령은 기존 Toolbar/Edit 메뉴와 동일하게 비활성화되어 우클릭 작업에서도 편집 조건이 달라지지 않습니다.
98. Design Toolbar의 `Pan`과 `View > Pan Canvas (H)`는 패닝 모드를 토글합니다. 모드가 활성화되면 빈 영역·컨트롤·선택 핸들 위에서 좌클릭 드래그가 선택이나 리사이즈 대신 ScrollViewer 오프셋을 이동하고, Toolbar/Menu 체크·Hand 커서·상태 표시를 함께 갱신합니다. `H`를 다시 누르거나 `Select`를 선택하면 복귀하며, Pick/Toolbox 배치 진입 시 Pan 모드는 자동으로 해제됩니다.
99. 선택 Adorner의 8방향 리사이즈 핸들은 아트보드 확대 배율과 반비례하는 문서 좌표 크기를 사용해 화면상 약 10px로 고정됩니다. 따라서 25%에서는 충분한 hit target을 확보하고 200%에서도 핸들이 선택 영역을 과도하게 가리지 않으며, 단일·다중 선택과 기존 Smart Snap/Shift 비율 잠금 동작은 그대로 유지됩니다.
100. Design Surface 헤더의 Selection Bounds HUD는 선택된 컨트롤들의 실제 아트보드 bounds를 `X: ... Y: ... W: ... H: ...` 형식으로 표시합니다. 단일·다중 선택 변경과 이동·리사이즈·속성 편집에 즉시 반응하며, 선택이 없을 때는 숨겨져 기존 헤더와 계층 breadcrumb 공간을 침범하지 않습니다.
101. 다중 선택의 Selection Adorner는 선택된 컨트롤들의 union bounds에 맞춰 투명한 외곽선을 그리고, 8방향 리사이즈 핸들과 같은 아트보드 좌표로 갱신됩니다. 외곽선은 입력을 가로채지 않아 기존 컨트롤 선택·드래그·리사이즈 동작을 유지합니다.
102. Aggregate Selection Adorner 외곽선은 현재 ZoomScale의 역수로 StrokeThickness를 계산합니다. 따라서 25%에서는 문서상 4px, 100%에서는 1px, 200%에서는 0.5px을 사용해 각 확대 단계에서 화면상 약 1px으로 보이며, 줌 변경 시 bounds·핸들과 함께 즉시 갱신됩니다.
103. Design Toolbar의 Grid 그룹은 `4`, `8`, `16`, `...` 버튼을 제공해 Grid Size 메뉴를 열지 않고도 자주 쓰는 간격을 즉시 적용하거나 Custom Grid Size 대화상자를 열 수 있습니다. 버튼은 기존 `SetCanvasGridSize`와 같은 Undo·문서 메타데이터·상태 메시지 경로를 사용합니다.
104. Grid 그룹의 현재 간격 표시(`8 px`, 사용자 지정 `12.5 px` 등)는 `Canvas.GridSize` 변경 알림에 연결되어 프리셋·Custom 입력·AXAML 문서 메타데이터 로드·Undo/Redo 뒤에도 실제 스냅 간격과 일치합니다.
105. 두 개 이상의 같은 root 또는 같은 Canvas 형제 컨트롤을 선택해 `Edit > Edit Selection Bounds...`를 열면 현재 union bounds가 `X/Y/W/H` 입력값으로 채워집니다. `X/Y`는 0 이상, `W/H`는 최소 10px로 검증하고, 적용 시 각 컨트롤의 상대 위치·크기를 같은 비율로 변환해 Canvas 로컬 좌표·Preview·AXAML·하나의 Transform Undo에 반영합니다. Grid·StackPanel·Content 자식, 잠긴 컨트롤, 서로 다른 부모가 섞인 선택에서는 메뉴 규칙과 상태 메시지로 안전하게 차단합니다.
106. Canvas 자식을 선택하면 Property Inspector의 첫 번째 기하 행이 문서 절대 `X/Y` 대신 부모 기준 `Left/Top`으로 표시됩니다. 값을 잃으면 기존 Canvas reflow가 `DesignElement.X/Y`를 부모 위치와 합산해 다시 계산하고, Width/Height 변경과 함께 하나의 layout Transform Undo, Canvas.Left/Top AXAML, Preview 결과를 유지합니다. Canvas 이외 부모 레이아웃 자식의 기하 입력은 계속 비활성화됩니다.
107. Selection Bounds 편집은 Design Toolbar의 `Bounds`, Canvas Context Menu의 `Edit Selection Bounds... (Ctrl+Alt+B)`, Edit 메뉴, `Ctrl+Alt+B`에서 동일한 공용 명령으로 접근합니다. 다중 선택 조건·잠금·부모 레이아웃 검증, 숫자 입력, 비율 변환, Transform Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
108. Common Properties 편집은 Design Toolbar의 `Common`, Canvas Context Menu의 `Edit Common Properties... (Ctrl+Alt+M)`, Edit 메뉴, `Ctrl+Alt+M`에서 동일한 공용 명령으로 접근합니다. 혼합값 표시, 빈 값 유지, 3상태 체크박스, 다중 선택 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
109. Layout Properties 편집은 Design Toolbar의 `Layout`, Canvas Context Menu의 `Edit Layout Properties... (Ctrl+Alt+L)`, Edit 메뉴, `Ctrl+Alt+L`에서 동일한 공용 명령으로 접근합니다. 단일 선택·잠금 검증, Margin·Padding·Alignment·Min/Max 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
110. Typography Properties 편집은 Design Toolbar의 `Typography`, Canvas Context Menu의 `Edit Typography Properties... (Ctrl+Alt+Y)`, Edit 메뉴, `Ctrl+Alt+Y`에서 동일한 공용 명령으로 접근합니다. 지원 타입·잠금 검증, Font·Size·Style·Weight·정렬·줄바꿈 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
111. Transform Properties 편집은 Design Toolbar의 `Transform`, Canvas Context Menu의 `Edit Transform Properties... (Ctrl+Alt+X)`, Edit 메뉴, `Ctrl+Alt+X`에서 동일한 공용 명령으로 접근합니다. 단일 선택·잠금 검증, 이동·회전·크기·기울기·변환 기준점 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
112. Accessibility & Navigation 편집은 Design Toolbar의 `Accessibility`, Canvas Context Menu의 `Edit Accessibility & Navigation... (Ctrl+Alt+A)`, Edit 메뉴, `Ctrl+Alt+A`에서 동일한 공용 명령으로 접근합니다. 단일 선택·잠금 검증, 스크린리더 메타데이터·접근성 뷰·Tab navigation 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
113. Interaction & Rendering 편집은 Design Toolbar의 `Interaction`, Canvas Context Menu의 `Edit Interaction & Rendering... (Ctrl+Alt+E)`, Edit 메뉴, `Ctrl+Alt+E`에서 동일한 공용 명령으로 접근합니다. 단일 선택·잠금·지원 속성 검증, 표시·입력 참여·클리핑·RTL·포인터 커서 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
114. Visual Effects 편집은 Design Toolbar의 `Effects`, Canvas Context Menu의 `Edit Visual Effects... (Ctrl+Alt+F)`, Edit 메뉴, `Ctrl+Alt+F`에서 동일한 공용 명령으로 접근합니다. 단일 선택·잠금·지원 속성 검증, Blur·Drop Shadow 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
115. Range & Value 편집은 Design Toolbar의 `Range`, Canvas Context Menu의 `Edit Range & Value... (Ctrl+Alt+Shift+R)`, Edit 메뉴, `Ctrl+Alt+Shift+R`에서 동일한 공용 명령으로 접근합니다. Slider·ProgressBar·NumericUpDown 타입·잠금·범위 검증, 값·눈금·증감 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
116. Text Input 편집은 Design Toolbar의 `Text Input`, Canvas Context Menu의 `Edit Text Input... (Ctrl+Alt+Shift+T)`, Edit 메뉴, `Ctrl+Alt+Shift+T`에서 동일한 공용 명령으로 접근합니다. TextBox 타입·잠금·입력 정책 검증, multiline·password·undo 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
117. SelectableTextBlock 편집은 Design Toolbar의 `Selectable Text`, Canvas Context Menu의 `Edit SelectableTextBlock... (Ctrl+Alt+Shift+B)`, Edit 메뉴, `Ctrl+Alt+Shift+B`에서 동일한 공용 명령으로 접근합니다. SelectableTextBlock 타입·잠금·선택 스타일 검증, Text·선택 브러시·전경색 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
118. MaskedTextBox 편집은 Design Toolbar의 `Masked Text`, Canvas Context Menu의 `Edit MaskedTextBox... (Ctrl+Alt+Shift+M)`, Edit 메뉴, `Ctrl+Alt+Shift+M`에서 동일한 공용 명령으로 접근합니다. MaskedTextBox 타입·잠금·mask 검증, Mask·PromptChar·prompt 숨김 정책 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
119. Selection Behavior 편집은 Design Toolbar의 `Selection`, Canvas Context Menu의 `Edit Selection Behavior... (Ctrl+Alt+Shift+Q)`, Edit 메뉴, `Ctrl+Alt+Shift+Q`에서 동일한 공용 명령으로 접근합니다. ComboBox·ListBox·TreeView 타입·잠금·선택 정책 검증, 선택 인덱스·검색·자동 스크롤·다중 선택·ComboBox 입력 정책, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
120. Date & Time 편집은 Design Toolbar의 `Date & Time`, Canvas Context Menu의 `Edit Date & Time Input... (Ctrl+Alt+Shift+D)`, Edit 메뉴, `Ctrl+Alt+Shift+D`에서 동일한 공용 명령으로 접근합니다. DatePicker·CalendarDatePicker·Calendar·TimePicker 타입·잠금·날짜 범위 검증, 날짜·시간·표시 형식·선택 모드 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
121. ColorPicker 편집은 Design Toolbar의 `Color Picker`, Canvas Context Menu의 `Edit ColorPicker... (Ctrl+Alt+Shift+P)`, Edit 메뉴, `Ctrl+Alt+Shift+P`에서 동일한 공용 명령으로 접근합니다. ColorPicker 타입·잠금·색상 입력 검증, 색상 모델·스펙트럼·알파·팔레트·입력 표시 정책, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
122. AutoCompleteBox 편집은 Design Toolbar의 `AutoComplete`, Canvas Context Menu의 `Edit AutoCompleteBox... (Ctrl+Alt+Shift+A)`, Edit 메뉴, `Ctrl+Alt+Shift+A`에서 동일한 공용 명령으로 접근합니다. AutoCompleteBox 타입·잠금·입력 검증, Text·watermark·completion·최소 접두사·populate 지연·FilterMode·drop-down 정책, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
123. Toggle & Choice 편집은 Design Toolbar의 `Toggle & Choice`, Canvas Context Menu의 `Edit Toggle & Choice Behavior... (Ctrl+Alt+Shift+O)`, Edit 메뉴, `Ctrl+Alt+Shift+O`에서 동일한 공용 명령으로 접근합니다. CheckBox·RadioButton·ToggleSwitch·ToggleButton 타입·잠금·상태 검증, checked/unchecked/indeterminate·three-state·ClickMode·콘텐츠 정렬·Radio 그룹·Switch On/Off 문구 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
124. Disclosure & Scrolling 편집은 Design Toolbar의 `Disclosure & Scroll`, Canvas Context Menu의 `Edit Disclosure & Scrolling... (Ctrl+Alt+Shift+U)`, Edit 메뉴, `Ctrl+Alt+Shift+U`에서 동일한 공용 명령으로 접근합니다. Expander·ScrollViewer 타입·잠금·중첩 Content 검증, header·expanded·전개 방향·콘텐츠 정렬과 양축 scrollbar·auto-hide·chaining·deferred/focus scrolling·snap 정책, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
125. SplitView Pane 편집은 Design Toolbar의 `SplitView Pane`, Canvas Context Menu의 `Edit SplitView Pane Behavior... (Ctrl+Alt+Shift+V)`, Edit 메뉴, `Ctrl+Alt+Shift+V`에서 동일한 공용 명령으로 접근합니다. SplitView 타입·잠금·pane 입력 검증, DisplayMode·IsPaneOpen·OpenPaneLength·CompactPaneLength·PanePlacement·UseLightDismissOverlayMode·PaneBackground 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
126. TabControl Behavior 편집은 Design Toolbar의 `TabControl`, Canvas Context Menu의 `Edit TabControl Behavior... (Ctrl+Alt+Shift+K)`, Edit 메뉴, `Ctrl+Alt+Shift+K`에서 동일한 공용 명령으로 접근합니다. TabControl 타입·잠금·탭 동작 검증, TabStripPlacement·HorizontalContentAlignment·VerticalContentAlignment 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
127. DataGrid Behavior 편집은 Design Toolbar의 `DataGrid`, Canvas Context Menu의 `Edit DataGrid Behavior... (Ctrl+Alt+Shift+E)`, Edit 메뉴, `Ctrl+Alt+Shift+E`에서 동일한 공용 명령으로 접근합니다. DataGrid 타입·잠금·표 동작 검증, AutoGenerateColumns·IsReadOnly·HeadersVisibility·GridLinesVisibility·SelectionMode·ClipboardCopyMode·열 조작·FrozenColumnCount·행·열 크기·축별 scrollbar 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
128. GridSplitter Behavior 편집은 Design Toolbar의 `GridSplitter`, Canvas Context Menu의 `Edit GridSplitter Behavior... (Ctrl+Alt+Shift+J)`, Edit 메뉴, `Ctrl+Alt+Shift+J`에서 동일한 공용 명령으로 접근합니다. GridSplitter 타입·잠금·동작 검증, ResizeDirection·ResizeBehavior·ShowsPreview·KeyboardIncrement·DragIncrement 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
129. Image Source & Rendering 편집은 Design Toolbar의 `Image Source`, Canvas Context Menu의 `Edit Image Source & Rendering... (Ctrl+Alt+Shift+I)`, Edit 메뉴, `Ctrl+Alt+Shift+I`에서 동일한 공용 명령으로 접근합니다. Image 타입·잠금·Source 검증, Source·Stretch·StretchDirection·RenderOptions.BitmapInterpolationMode·RenderOptions.EdgeMode·RenderOptions.BitmapBlendingMode 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
130. Button Actions & Commands 편집은 Design Toolbar의 `Button Actions`, Canvas Context Menu의 `Edit Button Actions & Commands... (Ctrl+Alt+Shift+N)`, Edit 메뉴, `Ctrl+Alt+Shift+N`에서 동일한 공용 명령으로 접근합니다. Button 타입·잠금·동작 검증, Content·ClickMode·HotKey·IsDefault·IsCancel·CommandParameter·Click handler 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.
131. Event Handler Map 편집은 Design Toolbar의 `Event Map`, Canvas Context Menu의 `Edit Event Handler Map... (Ctrl+Alt+Shift+L)`, Edit 메뉴, `Ctrl+Alt+Shift+L`에서 동일한 공용 명령으로 접근합니다. 문서 전체 이벤트 목록의 컨트롤 존재·이벤트 지원·잠금·중복 검증, `ControlName | EventName | HandlerName` 입력, 적용·Undo 결과는 진입 경로와 관계없이 동일하게 유지됩니다.

DataGrid가 포함된 생성 AXAML을 다른 프로젝트에서 사용할 때는 같은 Avalonia 버전의 `Avalonia.Controls.DataGrid` 패키지와 `avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml` 스타일 include가 필요합니다.

DataGrid Behavior 편집기는 열 정의와 분리된 표 동작을 관리합니다. `HeadersVisibility`, `GridLinesVisibility`, `SelectionMode`, `ClipboardCopyMode`, 열 조작 허용 여부, `FrozenColumnCount`, 행·열 크기, `ColumnWidth`와 축별 scrollbar를 원자적으로 검증하며, `RowHeight=Auto` 같은 Avalonia의 `NaN` 기본값과 `MaxColumnWidth=Infinity`도 AXAML에서 안전하게 보존합니다.

선택 AXAML 내보내기는 선택 컨트롤을 독립 `UserControl` 루트로 감싸며, 단일 선택 루트의 디자인 surface 좌표는 제거하고 크기·이름·스타일·바인딩은 유지합니다. 여러 root 또는 같은 Canvas의 직접 형제를 선택하면 선택 bounds를 기준으로 `UserControl > Canvas`를 만들고 각 루트의 상대 좌표·크기를 출력합니다. 선택 컨트롤이 컨테이너이면 현재 하위 계층과 DataGrid 열, Menu/TreeView/ItemsControl 항목도 함께 출력하며, 동적 리소스와 문서 스타일을 사용하는 경우 필요한 선언도 포함합니다. 바인딩의 실제 DataContext와 이벤트 핸들러가 있는 코드 영역은 호스트 프로젝트에서 연결해야 합니다.

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

Canvas 그룹화는 같은 부모를 공유하는 형제 컨트롤만 대상으로 합니다. root 컨트롤을 묶으면 bounding box 위치에 새 Canvas가 생성되고, Canvas 자식을 묶으면 원래 Canvas 안에 중첩됩니다. 그룹을 해제해도 자식의 화면 좌표와 형제 z-order를 유지하며, 서로 다른 Grid·StackPanel·Content 슬롯을 섞는 작업은 레이아웃 좌표 손실을 막기 위해 거부합니다. 잠긴 직접 자식이 포함된 그룹은 `Ungroup Selected Canvas`가 거부해 잠금된 계층의 부모 관계를 부분적으로 바꾸지 않습니다.

다중 선택 Arrange는 root 요소 또는 같은 Canvas의 직접 자식만 대상으로 합니다. Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·TabControl·SplitView·Content 자식은 부모가 좌표와 크기를 관리하므로 정렬을 거부하고, 해당 컨테이너의 전용 할당·순서 편집기를 사용하도록 안내합니다. Canvas 형제의 정렬은 `Canvas.Left`·`Canvas.Top` 로컬 좌표를 갱신하므로 부모를 이동해도 정렬 결과가 유지됩니다.

다중 선택 `Edit > Lay Out Selected`는 같은 root 또는 같은 Canvas의 잠금 해제된 형제 컨트롤을 새 StackPanel로 감쌉니다. `Horizontally`는 기존 너비를 주축 크기로, `Vertically`는 기존 높이를 주축 크기로 사용하고 8px 간격을 적용합니다. 새 컨테이너는 선택 영역 위치에 배치되며 Canvas 안에서 실행해도 자식의 화면 위치와 부모 상대 좌표를 보존하고, 전체 작업은 하나의 Undo 항목으로 기록됩니다.

같은 메뉴의 `Grid (Auto)`는 선택 개수의 제곱근을 기준으로 행·열을 만들고 선택 순서대로 셀을 채웁니다. 새 Grid는 선택 영역의 좌상단에 배치되며 Canvas 자식으로 실행할 때 부모 상대 좌표와 형제 순서를 보존하고, 각 컨트롤의 `Grid.Row`·`Grid.Column`과 Grid definitions를 AXAML에 출력합니다.

`UniformGrid (Auto)`는 같은 자동 행·열 계산을 사용하되 모든 셀을 같은 크기로 만들고 8px의 RowSpacing·ColumnSpacing을 적용합니다. 선택 순서와 Canvas 부모 상대 좌표를 보존하며, Rows·Columns·FirstColumn·spacing과 자식 순서를 AXAML에 기록합니다.

`DockPanel (Horizontal/Vertical)`은 선택 순서대로 가로 방향에는 `Left`, 세로 방향에는 `Top`으로 도킹하고 마지막 자식은 `LastChildFill`로 남겨 선택 컨트롤의 원래 주축 크기를 유지합니다. `WrapPanel (Horizontal/Vertical)`은 선택 컨트롤 중 가장 큰 크기를 항목 크기로 사용하고 8px의 항목·줄 간격과 제곱근 기반 행·열을 자동 계산합니다. 두 레이아웃 모두 root 또는 같은 Canvas 형제에서 실행할 수 있으며, Canvas 안에서는 컨테이너의 상대 좌표와 형제 순서를 보존하고 Undo/Redo·Preview·AXAML 왕복을 지원합니다.

`Edit Tab Order Map...`은 `TabIndex | ControlName` 형식의 여러 줄 목록을 원자적으로 적용합니다. `0`, `1` 같은 명시적 값은 중복을 거부하고, `auto` 또는 `-1`은 Avalonia의 기본 자동 순서로 정규화합니다. 잠긴 컨트롤은 주석으로 표시되어 변경할 수 없으며, 적용된 값은 캔버스의 `TAB #` 배지·접근성 속성·AXAML·Undo/Redo에 함께 반영됩니다.

`Edit Event Handler Map...`은 `ControlName | EventName | HandlerName` 형식의 여러 줄 목록을 원자적으로 적용합니다. 공통 `PointerPressed`, `PointerReleased`, `PointerEntered`, `PointerExited`, `KeyDown`, `KeyUp`, `GotFocus`, `LostFocus`, `Tapped`, `DoubleTapped`, `TextInput` 등의 이벤트와 Button `Click`, TextBox `TextChanged`, 선택 컨트롤 `SelectionChanged`, 범위 컨트롤 `ValueChanged`, Expander `Expanded`/`Collapsed` 등을 지원합니다. 이벤트 이름과 핸들러 식별자를 검증하고, 컨트롤별로 지원되지 않는 이벤트·잠긴 컨트롤·중복 이벤트는 적용하지 않습니다. 기존 Button `Click` 편집과도 호환되며, 적용 결과는 Preview 스냅샷·Draft/Full/UserControl AXAML·Undo/Redo에 보존됩니다. 실제 핸들러 메서드 구현과 ViewModel 연결은 생성 AXAML을 사용하는 호스트 프로젝트에서 담당합니다.

Live Preview 하단의 `Interaction Log`는 위 이벤트 메타데이터를 안전한 디자인 타임 방식으로 확인합니다. Preview에서 발생한 이벤트는 최대 최근 8개까지 기록하며 컨트롤 이름·이벤트 이름·핸들러 이름을 보여주지만, 호스트 프로젝트의 실제 핸들러 메서드나 ViewModel 명령을 실행하지 않습니다. 따라서 AXAML을 호스트 프로젝트에 연결하기 전에 이벤트 선언과 대상 컨트롤이 올바른지 빠르게 검증할 수 있습니다.

`Preview Interaction Log`는 이미 열린 Preview 창을 다시 렌더링하지 않고 활성화하므로, 사용자가 남긴 최근 상호작용 기록을 보존한 채 로그로 돌아갈 수 있습니다. Preview가 아직 열리지 않았다면 동일한 Preview 창을 열고, 일반 `Live Preview` 또는 `Ctrl+R`은 기존처럼 현재 문서를 새로고침합니다.

`Style Clipboard`는 Edit 메뉴의 하위 메뉴, Design Toolbar, Canvas 빈 영역·선택 요소 Context Menu에서 같은 `Copy Style`·`Paste Style` 명령을 노출합니다. `Copy Style`은 하나의 잠금 해제 컨트롤에서 외형·타이포그래피·렌더링·효과·상호작용 속성, 스타일 클래스와 DynamicResource 참조만 캡처합니다. `Paste Style`은 선택된 잠금 해제 대상에 해당 값을 한 번의 Undo 작업으로 적용하며 Text/Content, 위치·크기, 부모 계층, 바인딩, 이벤트 핸들러, 항목 정의는 변경하지 않습니다. 서로 다른 컨트롤 타입에 붙여넣으면 해당 타입이 지원하는 스타일 속성만 적용하고 나머지는 안전하게 무시합니다.

`Edit > History...`는 현재 문서 상태를 기준으로 완료된 Undo 작업과 앞으로 적용할 Redo 작업을 함께 표시합니다. 항목을 선택하면 기존 snapshot 기반 Undo/Redo를 필요한 횟수만큼 수행해 해당 지점으로 이동하며, 이동 후에도 dirty 상태·Preview·AXAML·Object Tree를 기존 Undo 흐름과 동일하게 갱신합니다.

`Undo History Timeline`은 Edit 메뉴, Design Toolbar, Canvas Context Menu, `Ctrl+Alt+Shift+F`에서 같은 대화상자를 엽니다. Timeline 진입점은 현재 History가 있을 때만 활성화되고, 기존 `Undo`·`Redo`·`Ctrl+Z`·`Ctrl+Y` 동작과 `Ctrl+Alt+F` 효과 편집 및 `Ctrl+Shift+F` 선택 맞춤 단축키는 유지됩니다.

Workspace Session Restore는 종료 시 각 문서 탭의 현재 스냅샷과 마지막 저장 기준 스냅샷을 함께 저장하므로, 저장하지 않은 디자인 변경도 다음 실행에서 dirty 상태로 복원합니다. 세션 파일은 앱의 로컬 설정 영역에만 저장되며, 손상되거나 일부 문서가 파싱되지 않으면 세션 전체를 적용하지 않고 기본 새 문서로 시작합니다.

탭 전환은 문서 내용과 Undo/Redo history뿐 아니라 탭별 줌 배율·Canvas 스크롤 오프셋·Object Tree 선택 이름·Property Inspector 필터/카테고리/확장 상태도 저장합니다. 따라서 여러 화면을 번갈아 편집할 때 작업 위치와 속성 탐색 맥락을 잃지 않으며, `Ctrl+Tab` 순환 전환과 앱 재시작 후에도 같은 편집 맥락을 유지합니다.

Workspace 패널은 문서와 독립된 세션 전역 상태입니다. Toolbox·Object Tree·Property Inspector의 표시 여부, 마지막 패널 폭과 Object Tree 높이를 저장하며, 오래된 세션 JSON에는 이 필드가 없어도 기본 4-pane 레이아웃으로 복원합니다.

`Break Selected Layout`은 선택한 컨테이너를 제거하고 직접 자식들을 root 또는 원래 Canvas에 다시 연결합니다. Grid·StackPanel·DockPanel·WrapPanel·UniformGrid는 현재 화면 bounds를 유지하고, Canvas 안의 레이아웃은 부모 상대 좌표와 형제 순서를 유지하며 해제된 자식들을 다시 선택합니다. 컨테이너 또는 직접 자식이 잠겨 있으면 작업을 거부하고 문서·계층·Undo 상태를 그대로 보존합니다.

선택 영역 Toolbox 프리셋은 두 개 이상의 root 컨트롤 또는 같은 Canvas의 직접 형제 컨트롤을 선택한 뒤 `File > Add Selection to Toolbox...`에서 등록합니다. 선택 영역의 좌상단을 기준으로 상대 좌표를 저장하고, root 선택은 컨트롤 목록으로, Canvas 형제 선택은 bounding box Canvas와 로컬 자식 계층으로 보존합니다. 등록 시점의 크기·시각 속성·텍스트·콘텐츠도 함께 복원하며, 배치 후 생성된 Canvas와 자식 컨트롤을 모두 선택합니다. 등록된 프리셋은 현재 세션의 Toolbox 검색 결과에 즉시 나타나며, `File > Export Selected Toolbox Preset...`으로 `*.toolbox-preset.json` 팩을 저장할 수 있습니다. `File > Load Toolbox Preset Pack...`은 JSON 문법·중복 이름·지원 타입·bounds·root/Canvas 계층을 검증한 뒤 일괄 등록하므로 실패한 팩이 Toolbox를 부분적으로 변경하지 않습니다. Grid·StackPanel·DockPanel·WrapPanel·UniformGrid·TabControl·SplitView·Content 자식은 현재 프리셋 배치 경로가 부모 전용 메타데이터를 복원하지 않으므로 등록을 거부합니다. 예시는 [toolbox-preset.example.json](docs/toolbox-preset.example.json)을 참고하세요.

컨테이너를 단독 선택한 상태에서 `Edit > Copy`, `Cut`, `Paste`, `Duplicate`를 실행하면 선택된 컨테이너의 모든 하위 요소를 하나의 계층으로 처리합니다. Canvas의 로컬 좌표와 Grid·StackPanel·TabControl·Content 등의 부모 배치 메타데이터를 유지하며, 계층 밖의 부모를 참조하는 자식은 기존 부모 참조를 유지합니다. 잘라내기는 잠긴 하위 요소가 포함된 계층을 거부해 부분 삭제를 방지합니다.

Image Source & Rendering 편집기는 로컬 파일 경로와 `file://` URI를 지원하며 빈 Source를 적용하면 현재 이미지를 해제합니다. 존재하지 않거나 디코딩할 수 없는 파일은 기존 Source와 렌더링 상태를 변경하지 않지만, 가져온 AXAML에서 파일을 찾을 수 없는 경우에는 Source 메타데이터를 보존해 프로젝트 이동 후 다시 연결할 수 있습니다. Stretch와 StretchDirection 외에 bitmap interpolation 품질, antialias/aliased edge, compositing blending 모드를 설정할 수 있고 Source Binding이 있으면 생성 AXAML은 정적 Source를 중복 출력하지 않습니다.

Button Actions & Commands 편집기는 Release·Press ClickMode와 Avalonia key gesture 형식의 HotKey를 지원하고, host Window의 기본·취소 동작을 지정합니다. CommandParameter는 정적 문자열로 설정하거나 `Edit Bindings...`에서 ViewModel 경로에 연결할 수 있으며 Command 자체도 Binding으로 선언합니다. Content나 CommandParameter에 Binding이 있으면 생성 AXAML은 정적 값을 중복 출력하지 않고, Click 핸들러와 Command는 같은 Button에 함께 선언할 수 있습니다. 기존 `Edit Click Handler...` 명령은 같은 이벤트 메타데이터를 편집합니다.

문서 루트 편집기에서 선택한 종류는 일반 저장과 Full AXAML 복사에 반영됩니다. UserControl은 Window 전용 속성을 사용하지 않으며, `File > Export UserControl AXAML...`은 현재 문서 종류와 관계없이 재사용 가능한 UserControl을 생성합니다.

AXAML 소스 편집기의 `Validate`와 `Preview`는 현재 디자인과 Undo 스택을 변경하지 않습니다. `Apply`는 파싱에 성공한 문서만 반영하며, 현재 저장 경로를 유지하고 전체 변경을 한 번의 Undo/Redo 작업으로 기록합니다.

캔버스 뷰포트는 아트보드의 원본 레이아웃 크기와 렌더 줌을 분리해 유지합니다. `Zoom In`·`Zoom Out`을 실행하면 ScrollViewer의 실제 콘텐츠 영역도 같은 배율로 갱신되므로 확대된 컨트롤을 가로·세로 스크롤하며 편집할 수 있고, `Fit to View`는 현재 뷰포트 크기를 기준으로 배율을 계산합니다. 중간 마우스 드래그는 선택·이동과 분리된 팬 입력으로 ScrollViewer 오프셋만 이동하고, Ctrl+휠은 포인터 아래의 아트보드 좌표를 고정한 채 확대·축소하므로 둘 다 문서 Undo 기록을 만들지 않습니다. 아트보드 preset·회전·Custom Size 변경도 layout 갱신 전의 viewport 중앙 문서 좌표를 복원해 scrollbar 표시 상태가 달라져도 작업 위치를 유지합니다. 가로·세로 디자인 룰러는 같은 오프셋과 배율을 사용해 화면 가장자리의 눈금과 라벨을 실제 아트보드 좌표로 표시하며, 포인터가 뷰포트 안에 있을 때는 청록색 기준선으로 해당 좌표를 강조합니다. 문서 탭마다 Canvas ScrollViewer 오프셋을 저장하고 탭 전환·Workspace Session Restore·닫힌 탭 복원 후 실제 extent 범위에 맞춰 복원하며, 복원 pending 동안 선택 자동 추적이 저장 위치를 덮어쓰지 않도록 순서를 조정합니다. 룰러에서 만든 가이드라인은 주황색으로 캔버스에 표시되고 이동·리사이즈 시 Smart Snap에 참여합니다. 가이드는 문서 AXAML과 Undo 스택에 포함되지 않는 작업 보조 상태이며, 문서 탭 전환·Workspace Session Restore·닫힌 탭 복원에서 탭별 좌표와 표시·스냅 상태를 되살리고 새 문서·새 AXAML·명시적 Clear에서는 해당 탭만 초기화됩니다.

## 로드맵

- ~~v0.3: 이동/리사이즈 기즈모~~ ✅
- ~~v0.4: bodong PropertyGrid 실제 연동~~ ✅
- ~~v0.5: .axaml 저장/로드~~ ✅
- ~~v0.6: 실제 드래그&드롭, 삭제, 언두~~ ✅
- ~~v0.8: Component Pack 관리 및 안전한 제거~~ ✅
- ~~v0.9: 다중 선택 공통 속성 편집~~ ✅
- ~~v1.0: 카테고리화된 Toolbox, 정렬·접기/펼치기·메타데이터 표시 및 카테고리 필터~~ ✅
- ~~v1.1: Toolbox Recent 및 Favorites~~ ✅
- ~~v1.2: Toolbox 키보드 탐색 및 빠른 배치~~ ✅
- ~~v1.3: Toolbox 검색 포커스 및 명시적 배치 모드~~ ✅
- ~~v1.4: Canvas Toolbox 배치 미리보기와 스냅 좌표 표시~~ ✅
- ~~v1.5: Toolbox 컨테이너 target 강조 및 클릭 자동 삽입~~ ✅
- ~~v1.6: 컨테이너 셀·삽입선·슬롯 정밀 target feedback~~ ✅
- ~~v1.7: Object Tree 키보드 탐색 및 이름 변경·삭제·잠금 단축키~~ ✅
- ~~v1.8: Property Inspector 카테고리/평면 보기와 일괄 펼침 탐색~~ ✅
- ~~v1.9: Property Inspector 전용 필터·Clear·포커스 단축키~~ ✅
- ~~v1.10: 문서 탭별 Property Inspector 필터·카테고리·확장 상태와 세션 복원~~ ✅
- v1.11: Qt Designer식 Workspace 패널 표시/숨김·크기 복원·레이아웃 초기화
- v1.12: 문서 탭 드래그 재배치와 활성 순서 세션 복원
- v1.13: 문서 탭 키보드 이동·컨텍스트 메뉴 작업
- v1.14: 문서 탭 다중 닫기와 dirty 확인 일괄 처리
- v1.15: 여러 문서 탭의 dirty 상태 일괄 저장
- v1.16: 모든 문서 탭 닫기와 빈 문서 복원
- v1.17: 닫힌 문서 탭 복원과 편집 상태 유지
- v1.18: 닫힌 문서 탭 기록의 세션 저장·복원
- v1.19: 문서 탭 복제와 독립 편집 상태 유지
- v1.20: 문서 탭 별칭과 세션·창 제목 동기화
- v1.21: 문서 탭 중간 클릭 닫기와 dirty 확인 재사용
- v1.22: 문서 탭 1-9번 직접 선택 단축키
- v1.23: 문서 탭 별칭·경로 검색 기반 빠른 전환기
- v1.24: 캔버스 Zoom/Viewport 키보드 네비게이션
- v1.25: Zoom Preset 메뉴와 사용자 지정 배율 입력
- v1.26: 다중 선택 Arrange 키보드 단축키
- v1.27: 다중 선택 중앙 정렬 키보드 단축키
- v1.28: 다중 선택 컨트롤 균등 분배 키보드 단축키
- v1.29: 다중 선택 컨트롤 크기 맞춤 키보드 단축키
- v1.30: 선택 컨트롤 z-order 키보드 단축키
- v1.31: 아트보드 중앙 정렬 키보드 단축키
- v1.32: 디자인 그리드/스냅 키보드 단축키
- v1.33: Workspace 패널 키보드 단축키
- v1.34: 숨겨진 Workspace 패널의 포커스 단축키 자동 복구
- v1.35: Canvas 그룹/그룹 해제 키보드 단축키
- v1.36: 방향 인식 Marquee 다중 선택
- v1.37: 레이아웃 적용/해제 키보드 단축키
- v1.38: 잠금/가시성 인식 Marquee 선택
- v1.39: 겹친 컨트롤 Alt+클릭 순환 선택
- v1.40: Object Tree Shift 범위 선택
- v1.41: 잠금/가시성 인식 Select All
- v1.42: 잠금 인식 Copy/Duplicate
- v1.43: Canvas Shift+클릭 다중 선택
- v1.44: 잠금 인식 Ungroup/Break Selected Layout
- v1.45: Canvas Shift+드래그 additive Marquee
- v1.46: Canvas Alt+드래그 subtractive Marquee
- v1.47: Canvas Escape 부모 컨테이너 선택
- v1.48: Canvas Enter 자식 컨테이너 진입
- v1.49: Canvas Alt+Arrow 형제 선택
- v1.50: Tab Order 기반 Canvas 키보드 순환
- v1.51: Canvas Tab Navigation IsTabStop 필터
- v1.52: Canvas Tab Navigation 포커스 가능·활성화 필터
- v1.53: Canvas Ctrl+Arrow 방향 선택
- v1.54: Canvas 계층 visibility 인식 선택
- v1.55: Canvas Alt+Arrow 전체 방향 형제 탐색
- v1.56: 계층 키보드 탐색 visibility 필터
- v1.57: Canvas Home/End 경계 선택
- v1.58: Canvas PageUp/PageDown 순차 선택
- v1.59: Shift+PageUp/PageDown Canvas 누적 선택
- v1.60: Shift+Tab Tab Order 누적 선택
- v1.61: Shift+Home/End Canvas 경계 누적 선택
- v1.62: Canvas Shift+클릭 범위 선택
- v1.63: Canvas 범위 선택 reverse 활성 대상 동기화
- v1.64: Shift+PageUp/PageDown Canvas 연속 범위 누적 선택
- v1.65: Shift+Tab Tab Order 연속 범위 누적 선택
- v1.66: Object Tree Shift+Arrow 키보드 범위 선택
- v1.67: Shift+Home/End Canvas 경계 연속 범위 누적 선택
- v1.68: Object Tree Shift+Home/End 경계 범위 선택
- v1.69: Object Tree 선택 행 자동 스크롤
- v1.70: Canvas active 요소 자동 viewport 추적
- v1.71: Toolbox 드래그 viewport edge auto-pan
- v1.72: 요소 편집·marquee viewport edge auto-pan
- v1.73: ruler guide viewport edge auto-pan
- v1.74: Toolbox 배치 모드 viewport edge auto-pan
- v1.75: Space+왼쪽 드래그 임시 viewport pan
- v1.76: Ctrl+Alt+Arrow 키보드 viewport pan
- v1.77: 선택 영역 Fit Selected to View
- v1.78: 키보드·View 메뉴 Zoom viewport 중심 보존
- v1.79: Zoom Preset·Actual Size viewport 중심 보존
- v1.80: Artboard Size·Rotate viewport 중심 보존
- v1.81: Object Tree 검색 Escape 초기화·포커스 복귀
- v1.82: Property Inspector 필터 Escape 초기화·PropertyGrid 포커스 복귀
- v1.83: Toolbox 검색 Escape 초기화·배치 취소 선택 보존
- v1.84: AXAML Source Editor Escape 취소·Ctrl+Enter 적용
- v1.85: 공통 Text Editor Escape 취소·Ctrl+Enter 적용
- v1.86: Binding·Items·Sample Data 편집기 키보드 취소·적용
- v1.87: 주요 속성 편집기 dialog-level 키보드 취소·적용
- v1.88: Accessibility·Interaction·Effects·Range·Text Input·SelectableTextBlock dialog-level 키보드 취소·적용
- v1.89: 나머지 속성 편집기 dialog-level 키보드 취소·적용
- v1.90: OS clipboard AXAML fragment 붙여넣기와 리소스·스타일 충돌 병합
- v1.91: 다중 선택 AXAML 재사용과 상대 Canvas 좌표 내보내기
- v1.92: 선택 Canvas 대상 AXAML fragment 컨텍스트 붙여넣기
- v1.93: 지원 컨테이너 전반의 AXAML fragment 컨텍스트 붙여넣기
- v1.94: AXAML context paste root 추적·잠금 보존·원자적 rollback 안정화
- v1.95: 내부 clipboard의 문서 색상 리소스·스타일 보존 및 충돌 별칭 병합
- v1.96: 문서 탭별 Design Guide 좌표·표시·스냅 상태 보존
- v1.97: Design Guide 상태의 Workspace Session 저장·복원
- v1.98: 문서 탭별 Canvas viewport 스크롤 위치 저장·복원
- v1.99: 탭 viewport 복원과 선택 자동 추적 우선순위 안정화
- v2.00: 단일 Tab Order 편집기와 Tab Order Map 입력 규칙 통합
- v2.01: Qt Designer식 Canvas Design Toolbar 추가
- v2.02: Design Surface Selection Breadcrumb과 계층 컨텍스트 이동 추가
- v2.03: 클릭 가능한 Selection Breadcrumb과 계층 변경 실시간 갱신 추가
- v2.04: Design Toolbar 편집·z-order·중앙 정렬·Grid/Snap·Dock/Wrap 명령 확장
- v2.05: Design Toolbar Select/Pick 작업 모드·스타일 클립보드 명령 추가
- v2.06: 아트보드 가장자리 정렬 명령과 Form Toolbar 그룹 추가
- v2.07: Design Guides 표시·Guide Snap·Clear Toolbar 명령 추가
- v2.08: Artboard 프리셋·회전·Custom 크기 Toolbar 및 현재 크기 표시 추가
- v2.09: Canvas Context Menu 편집·정렬·레이아웃 명령 확장
- v2.10: 전용 Canvas Pan 작업 모드·H 단축키·패닝 상태 피드백 추가
- v2.11: 줌 독립 선택 리사이즈 핸들 추가
- v2.12: Selection Bounds HUD와 실시간 위치·크기 피드백 추가
- v2.13: 다중 선택 Aggregate Selection Adorner 추가
- v2.14: 줌 독립 Aggregate Selection Adorner 외곽선 추가
- v2.15: Design Toolbar Grid Size Quick Controls 추가
- v2.16: Design Toolbar 현재 Grid Size 상태 피드백 추가
- v2.17: 다중 선택 Selection Bounds 숫자 직접 편집 추가
- v2.18: Canvas 자식 로컬 기하값 Property Inspector 직접 편집 추가
- v2.19: Selection Bounds 편집 Toolbar·Context Menu·Ctrl+Alt+B 빠른 접근 추가
- v2.20: Common Properties 편집 Toolbar·Context Menu·Ctrl+Alt+M 빠른 접근 추가
- v2.21: Layout Properties 편집 Toolbar·Context Menu·Ctrl+Alt+L 빠른 접근 추가
- v2.22: Typography Properties 편집 Toolbar·Context Menu·Ctrl+Alt+Y 빠른 접근 추가
- v2.23: Transform Properties 편집 Toolbar·Context Menu·Ctrl+Alt+X 빠른 접근 추가
- v2.24: Accessibility & Navigation 편집 Toolbar·Context Menu·Ctrl+Alt+A 빠른 접근 추가
- v2.25: Interaction & Rendering 편집 Toolbar·Context Menu·Ctrl+Alt+E 빠른 접근 추가
- v2.26: Visual Effects 편집 Toolbar·Context Menu·Ctrl+Alt+F 빠른 접근 추가
- v2.27: Range & Value 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+R 빠른 접근 추가
- v2.28: Text Input 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+T 빠른 접근 추가
- v2.29: SelectableTextBlock 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+B 빠른 접근 추가
- v2.30: MaskedTextBox 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+M 빠른 접근 추가
- v2.31: Selection Behavior 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+Q 빠른 접근 추가
- v2.32: Date & Time 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+D 빠른 접근 추가
- v2.33: ColorPicker 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+P 빠른 접근 추가
- v2.34: AutoCompleteBox 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+A 빠른 접근 추가
- v2.35: Toggle & Choice 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+O 빠른 접근 추가
- v2.36: Disclosure & Scrolling 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+U 빠른 접근 추가
- v2.37: SplitView Pane 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+V 빠른 접근 추가
- v2.38: TabControl Behavior 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+K 빠른 접근 추가
- v2.39: DataGrid Behavior 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+E 빠른 접근 추가
- v2.40: GridSplitter Behavior 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+J 빠른 접근 추가
- v2.41: Button Actions & Commands 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+N 빠른 접근 추가
- v2.42: Image Source & Rendering 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+I 빠른 접근 추가
- v2.43: Event Handler Map 편집 Toolbar·Context Menu·Ctrl+Alt+Shift+L 빠른 접근 추가
- v2.44: Preview Interaction Log Toolbar·Context Menu·Ctrl+Alt+Shift+Z 빠른 접근 추가
- v2.45: Style Clipboard Edit 하위 메뉴·Toolbar·Context Menu·공용 단축키 빠른 접근 정리
- v2.46: Undo History Timeline Toolbar·Context Menu·Ctrl+Alt+Shift+F 빠른 접근 추가
- v2.47: Document Tabs Toolbar·Context Menu·기존 탭 단축키 빠른 접근 추가
- v2.48: Document Tab Quick Switcher 번호·저장 상태 검색·순환 키보드 탐색 추가
- v2.49: Document Tab MRU 전환 Toolbar·Context Menu·Ctrl+Alt+Tab·세션 복원 추가
- v2.50: Recent AXAML Files 검색 대화상자·Toolbar·Context Menu·Ctrl+Shift+O 추가
- v2.51: Project Workspace 폴더 스캔·Explorer 검색·Refresh·Ctrl+Shift+P·설정 복원 추가
- v2.52: Project Workspace File Watcher·외부 AXAML 변경 알림·안전한 Reload Current File·Ctrl+Shift+R 추가
- v2.53: Live Preview System Default·Light·Dark ThemeVariant 전환과 열린 창 즉시 적용 추가
- v2.54: Project Explorer 폴더·파일 계층·접기/펼치기·조상 유지 검색 추가
- v2.55: Project Explorer 현재 문서 reveal·폴더 접기 상태 저장·복원·구형 workspace 형식 호환 추가
- v2.56: Project Explorer Left/Right 트리 키보드 탐색·폴더 상태 즉시 저장 추가
- v2.57: Project Explorer 파일·폴더 더블클릭 열기·접기/펼치기 추가
- v2.58: 열린 Project Explorer dialog의 watcher 실시간 tree 갱신·선택/접기 상태 보존 추가
- v2.59: Project Explorer relative/full path 복사 context menu·Ctrl+C 추가
- v2.60: Project Explorer 선택 위치의 OS 파일 관리자 열기 context menu 추가
- v2.61: Project Explorer 명시적 tree item template·깊이/상태/경로 표시 추가
- v2.62: Project Explorer 우클릭 행 선택과 context menu 대상 일관성 추가

## 컴포넌트 팩

`File > Load Component Pack...`에서 JSON 팩을 불러오면 현재 세션의 Toolbox에 별칭 컨트롤을 추가할 수 있습니다. 각 항목은 이미 지원되는 Avalonia 타입을 기반으로 하며, 표시 이름, 기본 크기, 기본 속성, 선택적 `category`를 지정합니다. `category`를 생략하면 내장 타입은 기본 카테고리로 분류되고, 외부 디자인 전용 타입은 `General`로 표시됩니다. 예시는 [component-pack.example.json](docs/component-pack.example.json)을 참고하세요. 캔버스에서 컨트롤 하나를 선택한 뒤 `File > Export Selected as Component Pack...`을 사용하면 해당 크기·시각 속성과 원본 Component Pack의 `category`를 재사용 가능한 JSON 팩으로 저장할 수 있습니다. 파일 경로는 워크스페이스 세션에 함께 저장되므로 앱을 다시 실행해도 팩을 다시 선택할 필요가 없습니다.

외부 프로젝트의 커스텀 컨트롤은 `designOnly: true`를 사용해야 합니다. 디자이너는 실제 외부 assembly를 실행하지 않고 청색 플레이스홀더를 렌더링하며, 플레이스홀더에 표시할 문구는 `previewText`, 원래 컨트롤에 전달할 기본 AXAML 속성은 `defaultProperties`, Toolbox 필터 이름은 `category`로 정의합니다. `DesignOnly`가 없는 알 수 없는 타입은 팩 로드를 거부해 잘못된 AXAML 생성을 방지합니다. 예시는 [custom-component-pack.example.json](docs/custom-component-pack.example.json)을 참고하세요.

`File > Load Component Pack Plugin...`은 선택한 DLL에서 public parameterless `IComponentPackPlugin` 구현을 정확히 하나 찾아 `CreatePack()` 결과를 기존 Component Pack 검증기로 등록합니다. 플러그인 assembly는 로드 시 코드를 실행할 수 있으므로 신뢰할 수 있는 빌드만 불러와야 하며, 예제 구현은 [component-pack-plugin.example.cs](docs/component-pack-plugin.example.cs)을 참고하세요. 플러그인에서 제공하는 커스텀 타입은 `DesignOnly: true`와 `PreviewText`를 사용하면 외부 assembly의 실제 컨트롤을 디자이너 프로세스에서 실행하지 않고도 설계할 수 있습니다.

컴포넌트 팩은 단일 컨트롤 정의를 공유하고, Toolbox 프리셋 팩은 여러 root 컨트롤의 상대 배치와 시각 상태를 공유합니다. 두 JSON 팩은 서로 다른 메뉴와 스키마를 사용하므로 프리셋 레이아웃을 컴포넌트 팩으로 불러오지 않습니다. 두 팩의 원본 경로는 세션 JSON의 `ComponentPackPaths`와 `ToolboxPresetPackPaths`에 각각 저장되며, 누락되거나 손상된 팩은 해당 항목만 건너뛰고 문서 작업 공간은 유지합니다.

`File > Export UserControl AXAML...`은 현재 캔버스를 재사용 가능한 `UserControl` 레이아웃으로 내보냅니다. 코드비하인드를 추가할 때는 생성된 루트에 프로젝트의 `x:Class`를 지정하면 됩니다.
