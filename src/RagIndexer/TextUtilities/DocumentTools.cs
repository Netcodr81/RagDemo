using System.Text;

namespace RagIndexer.TextUtilitites;

public static class DocumentTools
{
    public static string CleanContent(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t' && c != '\f')
            {
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString().Trim();
    }
}