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

## 현재 상태 (v0.1)

- 4-Pane 레이아웃 스켈레톤 (Toolbox / Canvas / Object Tree / Property Inspector)
- ViewModel 골격 (CommunityToolkit.Mvvm)
- Toolbox 하드코딩 3종 (Button, TextBox, TextBlock)
- 빌드 검증 완료

## 로드맵

- v0.2: 드래그&드롭 (Toolbox → Canvas)
- v0.3: bodong PropertyGrid 실제 연동
- v0.4: .axaml 저장/로드
- v0.5: 컨트롤 선택/이동/리사이즈 기즈모
