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

DataGrid가 포함된 생성 AXAML을 다른 프로젝트에서 사용할 때는 같은 Avalonia 버전의 `Avalonia.Controls.DataGrid` 패키지와 `avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml` 스타일 include가 필요합니다.

바인딩 편집기는 선택 타입에서 지원하는 속성을 대화상자에 표시합니다. 생성 AXAML은 ViewModel 타입을 알 수 없는 디자이너 문서가 compiled-bindings 설정과 독립적으로 컴파일되도록 `ReflectionBinding`을 사용합니다.

AXAML 소스 편집기의 `Validate`와 `Preview`는 현재 디자인과 Undo 스택을 변경하지 않습니다. `Apply`는 파싱에 성공한 문서만 반영하며, 현재 저장 경로를 유지하고 전체 변경을 한 번의 Undo/Redo 작업으로 기록합니다.

## 로드맵

- ~~v0.3: 이동/리사이즈 기즈모~~ ✅
- ~~v0.4: bodong PropertyGrid 실제 연동~~ ✅
- ~~v0.5: .axaml 저장/로드~~ ✅
- ~~v0.6: 실제 드래그&드롭, 삭제, 언두~~ ✅

## 컴포넌트 팩

`File > Load Component Pack...`에서 JSON 팩을 불러오면 현재 세션의 Toolbox에 별칭 컨트롤을 추가할 수 있습니다. 각 항목은 이미 지원되는 Avalonia 타입을 기반으로 하며, 표시 이름, 기본 크기, 기본 속성을 지정합니다. 예시는 [component-pack.example.json](docs/component-pack.example.json)을 참고하세요. 캔버스에서 컨트롤 하나를 선택한 뒤 `File > Export Selected as Component Pack...`을 사용하면 해당 크기와 시각 속성을 재사용 가능한 JSON 팩으로 저장할 수 있습니다.

`File > Export UserControl AXAML...`은 현재 캔버스를 재사용 가능한 `UserControl` 레이아웃으로 내보냅니다. 코드비하인드를 추가할 때는 생성된 루트에 프로젝트의 `x:Class`를 지정하면 됩니다.
