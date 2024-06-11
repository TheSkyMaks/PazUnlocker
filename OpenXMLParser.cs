using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PazUnlocker
{
    internal class OpenXMLParser
    {
        public Dictionary<int, List<string>> ParseDocument(string fileName)
        {
            var rowsData = new Dictionary<int, List<string>>();
            var file = File.Open(fileName, FileMode.Open);
            using (file )
            {
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(file, false))
                {
                    var sheet = document.WorkbookPart.Workbook.GetFirstChild<Sheets>().Elements<Sheet>().First();
                    var worksheetPart = (WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id.Value);
                    var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                    foreach (var row in sheetData.Elements<Row>().Skip(1))
                    {
                        List<string> cellValues = new List<string>();
                        foreach (var cell in row.Descendants<Cell>())
                        {
                            var value = GetCellValue(cell, document.WorkbookPart);
                            cellValues.Add(value);
                        }
                        rowsData.Add((int)row.RowIndex.Value, cellValues);
                    }
                }
            }
            return rowsData;
        }

        private string GetCellValue(Cell cell, WorkbookPart wbPart)
        {
            var value = cell.InnerText;

            if (cell.DataType == null)
            {
                return value;
            }
            switch (cell.DataType.Value)
            {
                case CellValues.SharedString:

                    var stringTable = wbPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();

                    if (stringTable != null)
                    {
                        value = stringTable.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                    }
                    break;
            }

            return value;
        }
    }
}
