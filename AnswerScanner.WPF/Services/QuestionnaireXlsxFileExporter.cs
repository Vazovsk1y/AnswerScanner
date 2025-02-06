using System.Reflection;
using AnswerScanner.WPF.Services.Interfaces;
using ClosedXML.Report;

namespace AnswerScanner.WPF.Services;

internal class QuestionnaireXlsxFileExporter : IQuestionnaireFileExporter
{
    private const string TemplateResourceName = "AnswerScanner.WPF.QuestionnairesExportTemplate.xlsx";
    
    public void Export(IReadOnlyCollection<QuestionnaireExportModel> questionnaires, string filePath)
    {
        using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName);
        using var template = new XLTemplate(templateStream);

        var data = new
        {
            Questionnaires = questionnaires,
            AnswerIndexes =  Enumerable
                .Range(1, questionnaires.MaxBy(e => e.Questions.Count)?.Questions.Count ?? 0)
                .ToList(),
        };

        template.AddVariable(data);
        template.Generate();
        
        var firstWs = template.Workbook.Worksheets.First();

        var rowIndex = 2;
        const int rowsOffset = 3;
        foreach (var item in questionnaires)
        {
            var columnIndex = 2;
            foreach (var question in item.Questions)
            {
                if (data.AnswerIndexes.Contains(question.Number))
                {
                    var cell = firstWs.Cell(rowIndex, columnIndex);
                    cell.Style.Font.FontName = "Arial";
                    cell.Style.Font.FontSize = 12;
                    cell.Value = question.Answer;
                }
                
                columnIndex++;
            }
            rowIndex += rowsOffset;
        }
        
        foreach (var column in firstWs.ColumnsUsed())
        {
            if (column.ColumnNumber() == 1)
            {
                column.AdjustToContents();
            }
            else
            {
                column.Width = 5;
            }
        }
        
        template.SaveAs(filePath);
    }
}