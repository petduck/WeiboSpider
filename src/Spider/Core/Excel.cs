using System;
using System.Collections.Generic;
using System.Text;
using ClosedXML.Excel;
using System.Reflection;
using System.ComponentModel;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Spider.Core
{
    /// <summary>
    /// Excel处理
    /// </summary>
    public class Excel
    {
        static XLWorkbook workbook;
        bool isnew;
        string filename;

        public Excel(string _filename)
        {
            filename = _filename;
            if (System.IO.File.Exists(_filename))
            {
                isnew = false;
                workbook = new XLWorkbook(_filename);
            }
            else
            {
                isnew = true;
                workbook = new XLWorkbook();
            }
        }

        //插入数据
        public void Insert<T>(List<T> data) where T : new()
        {
            
            //反射获得表头
            Type type = typeof(T);
            T t = new T();

            if (!workbook.Worksheets.TryGetWorksheet(type.Name, out IXLWorksheet worksheet))
            {
                worksheet= workbook.Worksheets.Add(type.Name);
            }
            worksheet.Columns("A", "H").AdjustToContents();
            // 获得此模型的公共属性 
            PropertyInfo[] propertys = t.GetType().GetProperties();
            int i = 1;
            Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();
            foreach (PropertyInfo pi in propertys)
            {
                var descp = pi.Name;

                keyValuePairs.Add(descp, i);

                Object[] objs = pi.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if (objs != null && objs.Length > 0)
                {
                    descp = ((DescriptionAttribute)objs[0]).Description;
                }

                worksheet.Cell(1, i).Value = descp;
                worksheet.Cell(1, i).Style.Font.SetBold(true);                
                i++;
            }
            //写入
            int maxrows = worksheet.LastRowUsed().RowNumber();
            //worksheet.Cells().DataType = XLDataType.Text;
            for (int index = 1; index <= data.Count; index++)
            {
                var item = data[index - 1];
                propertys = item.GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    var name = pi.Name;
                    var value = Convert.ToString(pi.GetValue(item));
                    if (name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        value = $"'{value}";
                        worksheet.Cell(maxrows + index, keyValuePairs[name]).Style.IncludeQuotePrefix = true;
                    }
                    worksheet.Cell(maxrows + index, keyValuePairs[name]).Value = value;
                }
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        public void Save()
        {
            if (isnew)
            {
                workbook.SaveAs(filename);
                isnew = false;
            }
            else
            {
                workbook.Save();
            }
            
        }
    }
}
