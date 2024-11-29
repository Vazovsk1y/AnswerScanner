using System.Reflection;
using AnswerScanner.WPF.Services.Interfaces;
using ClosedXML.Excel;
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
            AnswerIndexes = Enumerable.Range(1, questionnaires.MaxBy(e => e.Questions.Count)?.Questions.Count ?? default),
        };

        template.AddVariable(data);
        template.Generate();
        
        var firstWs = template.Workbook.Worksheets.First();
        for (var i = 0; i < questionnaires.Count; i++)
        {
            var answers = questionnaires.ElementAt(i).Questions;
            var columnIndex = i + 2;
            var rowIndex = 2;
            
            for (var j = 0; j < answers.Count; j++)
            {
                rowIndex += 1;
                var cell = firstWs.Cell(rowIndex, columnIndex);
                cell.Value = answers.ElementAt(j).Answer;
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
        }
        
        firstWs.Columns().AdjustToContents();
        template.SaveAs(filePath);
    }
}