using System.Collections.Generic;

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
            return Lines != null && Lines.Count > 0 && Lines[0] != null && Lines[0].Substring(0, 1) == "<" ? (Lines[0].Split(' ')[0].TrimStart('<')).ToUpper() : null;
        }
        set
        {
            if (Lines == null || Lines.Count == 0)
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
        ReaperBlock currentBlock = this;
        foreach (string line in lines)
        {
            if (line == null)
            {
                continue;
            }

            if (((IList<string>)SubStrings).Contains(line.Split(' ')[0].ToUpper()))
            {
                ReaperBlock child = currentBlock.AddNewChild();
                child.Lines.Add(line);
                continue;
            }

            string firstChar = line.Substring(0, 1);
            if (firstChar == "<")
            {
                currentBlock = currentBlock.AddNewChild();
            }

            currentBlock.Lines.Add(line);

            if (firstChar == ">")
            {
                if (currentBlock.Parent != null)
                {
                    currentBlock = currentBlock.Parent;
                }
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