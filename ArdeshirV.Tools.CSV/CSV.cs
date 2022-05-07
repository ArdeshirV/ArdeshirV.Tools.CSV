#region Header
// CSV.cs : CSV class that provivds CSV file reader/wrtier/editor.
// Copyright© 2019-2022 ArdeshirV@protonmail.com, Licensed under GPLv3+

using System;
using System.IO;
using System.Collections.Generic;

#endregion Header
//---------------------------------------------------------------------------------------
namespace ArdeshirV.Tools.CSV
{
    /// <summary>
    /// CSV class that provivds CSV file reader/wrtier/editor.
    /// </summary>
    public class CSV
    {
        public delegate bool DetectHeaderCallback(string Line);

        /// <summary>
        /// CSVLine holds CSV file lines.
        /// </summary>
        public class CSVLine
        {
            private string[] stringArrItems;
            private readonly string stringSeperator;

            private CSVLine() { }

            public CSVLine(string Line, string Seperator = ",")
            {
                stringSeperator = Seperator;
                if (string.IsNullOrEmpty(Line) || Line.Length <= 0)
                    stringArrItems = new string[] { };
                else {
                    int index = 0;
                    stringArrItems = Line.Split(new string[] { Seperator }, StringSplitOptions.None);
                    foreach (string str in stringArrItems)
                        stringArrItems[index++] = RemoveQuotes(str.Trim(), new char []{ '\"' });
                }
            }
            private string RemoveQuotes(string strWithQuotes, char[] quotes)
            {
                string result = strWithQuotes;
                foreach (char quote in quotes)
                {
                    int index = result.IndexOf(quote);
                    int LastIndex = result.LastIndexOf(quote);
                    if (index != -1 && LastIndex != -1 && index != LastIndex)
                        result = result.Substring(index + 1, LastIndex - index - 1);
                    // System.Windows.Forms.MessageBox.Show(result);  // Test
                }
                return result;
            }

            public string this[int intFieldIndex]
            {
                get
                {
                    if (Items.Length <= intFieldIndex || intFieldIndex < 0)
                        throw new IndexOutOfRangeException(string.Format(
                            "Error: index={0} is out of range of CSVLine object in \"this[int]\" operator", intFieldIndex));
                    return Items[intFieldIndex];
                }
                set
                {
                    Items[intFieldIndex] = value;
                }
            }

            public int this[string stringHeadName]
            {
                get
                {
                    int intFieldNumber = IndexOf(stringHeadName);
                    if (intFieldNumber < 0)
                        throw new IndexOutOfRangeException(string.Format(
                            "Error: \"{0}\" is not exists in CSVLine object that accessed with \"this[string]\" operator", stringHeadName));
                    return intFieldNumber;
                }
            }

            public bool Contains(string Head, bool ignoreCase = true)
            {
                return IndexOf(Head, ignoreCase) != -1;
            }

            public bool Contains(string[] Heads, bool ignoreCase = true)
            {
                foreach (string head in Heads)
                    if (IndexOf(head, ignoreCase) < 0)
                		return false;
                return true;
            }

            public int IndexOf(string Head, bool ignoreCase = true)
            {
                string target = (ignoreCase) ? Head.ToLower() : Head;

                for (int index = 0; index < Items.Length; index++)
                    if (target == ((ignoreCase) ? Items[index].ToLower() : Items[index]))
                        return index;
                return -1;
            }

            public string[] Items {
            	get {
            		return stringArrItems;
            	}
            }

            public int Length
            {
                get
                {
                    return Items.Length;
                }
            }

            public override string ToString()
            {
                if (Items.Length > 0)
                    return string.Join(stringSeperator, Items);
                else
                    return string.Empty;
            }

            public string Seperator
            {
                get
                {
                    return stringSeperator;
                }
            }
        }

        private string CSVFileName;
        private CSVLine[] CSVLineBody;
        private bool boolIsHeaderExists;
        private CSVLine[] CSVLineComments;
        private readonly string stringSeperator;
        private DetectHeaderCallback DetectHeader;

        private CSV() { }

        /// <summary>
        /// Opens specified CSV file.
        /// </summary>
        /// <param name="stringCSVFilePathName">File name and path to the specified CSV file</param>
        /// <param name="IsHeaderExists">Is there any header in specified CSV file?</param>
        /// <param name="DetectHeader">Callback method that detect header by processing each line of CSV file</param>
        /// <param name="Seperator">The seperator between CSV file columns</param>
        public CSV(string stringCSVFilePathName, bool IsHeaderExists,
                   DetectHeaderCallback DetectHeader = null, string Seperator = ",")
        {
            this.stringSeperator = Seperator;
            this.DetectHeader = DetectHeader;
            this.boolIsHeaderExists = IsHeaderExists;
            Load(stringCSVFilePathName);
        }

        public void Load(string stringCSVFilePathName)
        {
            ImportLines(File.ReadAllLines(this.CSVFileName = stringCSVFilePathName));
        }

        public void Save()
        {
            File.WriteAllLines(CSVFileName, ExportLines());
        }

        public void SaveAs(string stringFileName)
        {
            File.WriteAllLines(this.CSVFileName = stringFileName, ExportLines());
        }

        public override string ToString()
        {
            return string.Concat(ExportLines());
        }

        private void ImportLines(string[] stringLines)
        {
            int intHeader = -1;
            CSVLineBody = new CSVLine[] { };
            CSVLineComments = new CSVLine[] { };

            if (boolIsHeaderExists)
            {
                if (DetectHeader == null)
                {
                    if (stringLines.Length > 0)
                    {
                        intHeader = 0;
                        Header = new CSVLine(stringLines[0], stringSeperator);
                    }
                }
                else
                {
                    for (int index = 0; index < stringLines.Length; index++)
                    {
                        if (DetectHeader(stringLines[index]))
                        {
                            intHeader = index;
                            Header = new CSVLine(stringLines[index], stringSeperator);
                            break;
                        }
                    }
                }
            }

            if (intHeader < 0)
            {
                CSVLineComments = new CSVLine[] { };
                Header = new CSVLine(string.Empty);

                CSVLineBody = new CSVLine[stringLines.Length];
                for (int index = 0; index < stringLines.Length; index++)
                    CSVLineBody[index] = new CSVLine(stringLines[index], stringSeperator);
            }
            else if (intHeader == 0)
            {
                CSVLineComments = new CSVLine[] { };

                CSVLineBody = new CSVLine[stringLines.Length - 1];
                for (int index = 1; index < stringLines.Length; index++)
                    CSVLineBody[index - 1] = new CSVLine(stringLines[index], stringSeperator);
            }
            else if (intHeader > 0)
            {
                CSVLineComments = new CSVLine[intHeader];
                for (int index = 0; index < intHeader; index++)
                    CSVLineComments[index] = new CSVLine(stringLines[index], stringSeperator);

                CSVLineBody = new CSVLine[stringLines.Length - intHeader - 1];
                for (int index = intHeader + 1; index < stringLines.Length; index++)
                    CSVLineBody[index - intHeader - 1] = new CSVLine(stringLines[index], stringSeperator);
            }
        }

        private string[] ExportLines()
        {
            int intLength = 0;

            if (CSVLineComments != null)
                intLength += CSVLineComments.Length;

            if (Header != null && Header.Length > 0)
                intLength++;

            if (CSVLineBody != null)
                intLength += CSVLineBody.Length;

            List<CSVLine> lines = new List<CSVLine>(intLength);

            if (CSVLineComments != null)
                lines.AddRange(CSVLineComments);

            if (Header != null && Header.Length > 0)
                lines.Add(Header);

            if (CSVLineBody != null)
                lines.AddRange(CSVLineBody);

            string[] stringLines = new string[lines.Count];
            for (int index = 0; index < lines.Count; index++)
                stringLines[index] = lines[index].ToString() + Environment.NewLine;

            return stringLines;
        }

        public int GetIndexOfHead(string stringHead)
        {
            for (int index = 0; index < Header.Items.Length; index++)
                if (Header.Items[index] == stringHead)
                    return index;
            return -1;
        }

        public string this[int intLineNumer, int intHeaderIndex]
        {
            get
            {
                return CSVLineBody[intLineNumer][intHeaderIndex];
            }
            set
            {
                CSVLineBody[intLineNumer][intHeaderIndex] = value;
            }
        }

        public string this[int intLineNumer, string stringHeaderName]
        {
            get
            {
                return CSVLineBody[intLineNumer][Header.IndexOf(stringHeaderName)];
            }
            set
            {
                CSVLineBody[intLineNumer][Header.IndexOf(stringHeaderName)] = value;
            }
        }

        public CSVLine this[int intLineNumer]
        {
            get
            {
                return CSVLineBody[intLineNumer];
            }
            set
            {
                CSVLineBody[intLineNumer] = value;
            }
        }

        public CSVLine Header { get; set; }

        public CSVLine[] Comments
        {
            get
            {
                return CSVLineComments;
            }
        }

        public CSVLine[] Body
        {
            get
            {
                return CSVLineBody;
            }
        }
    }
}
