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

## 현재 상태 (v0.4)

- 4-Pane 레이아웃 (Toolbox / Canvas / Object Tree / Property Inspector)
- Toolbox 하드코딩 3종 (Button, TextBox, TextBlock)
- **클릭-투-플레이스 배치**: Toolbox 선택 → Canvas 클릭 → 실제 Avalonia 컨트롤 인스턴스 생성
- **요소 선택**: 배치된 요소 클릭 시 파란 외곽선
- **Object Tree 자동 동기화**: 배치된 요소가 루트(Window) 아래에 추가
- **이동/리사이즈 기즈모**: 선택된 요소를 드래그로 이동, 8방향 핸들로 리사이즈 (최소 10px)
- **PropertyGrid 연동**: 선택된 컨트롤의 속성을 bodong PropertyGrid로 실시간 편집
- 상태바 피드백

## 사용법

1. 좌측 Toolbox에서 컨트롤 선택 (Button / TextBox / TextBlock)
2. 중앙 Canvas 영역을 클릭 → 클릭 위치에 기본 크기로 생성
3. 생성된 요소를 클릭하여 선택 → 8개 핸들로 이동/리사이즈
4. 우측 하단 Properties 패널에서 속성 편집

## 로드맵

- ~~v0.3: 이동/리사이즈 기즈모~~ ✅
- ~~v0.4: bodong PropertyGrid 실제 연동~~ ✅
- v0.5: .axaml 저장/로드
- v0.6: 실제 드래그&드롭, 삭제, 언두
