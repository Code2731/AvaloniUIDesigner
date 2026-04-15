namespace AvaloniaUIDesigner.App.Models;

// Toolbox에 표시되는 한 항목. 실제 컨트롤 인스턴스 생성은 이후 단계에서 추가.
public sealed record ToolboxItem(string DisplayName, string AvaloniaTypeName);
