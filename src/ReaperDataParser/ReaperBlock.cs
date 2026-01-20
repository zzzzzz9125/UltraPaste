using System;
using System.Collections.Generic;
using System.IO;

namespace ReaperDataParser
{
    public class ReaperBlock
    {
        public List<string> Lines { get; set; }
        public ReaperBlock Parent { get; set; }
        public List<ReaperBlock> Children { get; set; }
        public static string[] SubStrings = new string[] { "TRACKSKIP" };
        public string Type
        {
            get
            {
                if (Lines == null || Lines.Count == 0)
                {
                    return null;
                }

                string firstLine = Lines[0];
                if (string.IsNullOrEmpty(firstLine) || firstLine[0] != '<')
                {
                    return null;
                }

                int separatorIndex = firstLine.IndexOf(' ');
                string typeToken = separatorIndex >= 0 ? firstLine.Substring(0, separatorIndex) : firstLine;
                return typeToken.TrimStart('<').ToUpperInvariant();
            }
            set
            {
                if (Lines == null || Lines.Count == 0 || string.IsNullOrEmpty(Lines[0]))
                {
                    return;
                }

                string[] tokens = Lines[0].Split(' ');
                tokens[0] = string.Format("<{0}", value);
                Lines[0] = string.Join(" ", tokens);
            }
        }
        public int Level { get { return Parent != null ? Parent.Level + 1 : 0; } }

        public bool HasChildren { get { return Children != null && Children.Count > 0; } }

        public ReaperBlock()
        {
            Lines = new List<string>();
            Children = new List<ReaperBlock>();
        }

        public ReaperBlock(List<string> lines)
        {
            Lines = new List<string>();
            Children = new List<ReaperBlock>();
            if (lines == null)
            {
                return;
            }

            ReaperBlock currentBlock = this;
            foreach (string rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                string line = rawLine;
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string directive = tokens.Length > 0 ? tokens[0].ToUpperInvariant() : string.Empty;
                if (!string.IsNullOrEmpty(directive) && ((IList<string>)SubStrings).Contains(directive))
                {
                    ReaperBlock child = currentBlock.AddNewChild();
                    child.Lines.Add(line);
                    continue;
                }

                char firstChar = line[0];
                if (firstChar == '<')
                {
                    currentBlock = currentBlock.AddNewChild();
                }

                currentBlock.Lines.Add(line);

                if (firstChar == '>')
                {
                    if (currentBlock.Parent == null)
                    {
                        throw new InvalidDataException("Encountered a closing block marker without a matching opening block.");
                    }

                    currentBlock = currentBlock.Parent;
                }
            }
        }

        public ReaperBlock AddNewChild()
        {
            ReaperBlock child = new ReaperBlock() { Parent = this };
            Children.Add(child);
            return child;
        }
    }
}
