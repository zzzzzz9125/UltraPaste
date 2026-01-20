using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CapCutDataParser
{
    internal sealed class JsonParser
    {
        private readonly string json;
        private int index;

        private JsonParser(string json)
        {
            this.json = json;
        }

        public static object Parse(string json)
        {
            var parser = new JsonParser(json ?? string.Empty);
            var value = parser.ParseValue();
            parser.SkipWhitespace();
            if (parser.index != parser.json.Length)
            {
                throw new FormatException($"Unexpected character '{parser.Peek}' at position {parser.index}.");
            }

            return value;
        }

        private object ParseValue()
        {
            SkipWhitespace();
            if (index >= json.Length)
            {
                return null;
            }

            switch (Peek)
            {
                case '{':
                    return ParseObject();
                case '[':
                    return ParseArray();
                case '"':
                    return ParseString();
                case 't':
                    ParseLiteral("true");
                    return true;
                case 'f':
                    ParseLiteral("false");
                    return false;
                case 'n':
                    ParseLiteral("null");
                    return null;
                default:
                    if (Peek == '-' || char.IsDigit(Peek))
                    {
                        return ParseNumber();
                    }

                    throw new FormatException($"Unexpected character '{Peek}' at position {index}.");
            }
        }

        private Dictionary<string, object> ParseObject()
        {
            var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Expect('{');
            SkipWhitespace();
            if (Peek == '}')
            {
                index++;
                return obj;
            }

            while (true)
            {
                SkipWhitespace();
                var key = ParseString();
                SkipWhitespace();
                Expect(':');
                obj[key] = ParseValue();
                SkipWhitespace();
                if (Peek == ',')
                {
                    index++;
                    continue;
                }

                if (Peek == '}')
                {
                    index++;
                    break;
                }

                throw new FormatException($"Expected ',' or '}}' at position {index}.");
            }

            return obj;
        }

        private List<object> ParseArray()
        {
            var list = new List<object>();
            Expect('[');
            SkipWhitespace();
            if (Peek == ']')
            {
                index++;
                return list;
            }

            while (true)
            {
                list.Add(ParseValue());
                SkipWhitespace();
                if (Peek == ',')
                {
                    index++;
                    continue;
                }

                if (Peek == ']')
                {
                    index++;
                    break;
                }

                throw new FormatException($"Expected ',' or ']' at position {index}.");
            }

            return list;
        }

        private string ParseString()
        {
            var sb = new StringBuilder();
            Expect('"');
            while (index < json.Length)
            {
                var ch = json[index++];
                if (ch == '"')
                {
                    return sb.ToString();
                }

                if (ch == '\\')
                {
                    if (index >= json.Length)
                    {
                        throw new FormatException("Unterminated escape sequence.");
                    }

                    ch = json[index++];
                    switch (ch)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            sb.Append(ParseUnicode());
                            break;
                        default:
                            throw new FormatException($"Unsupported escape sequence '\\{ch}'.");
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }

            throw new FormatException("Unterminated string literal.");
        }

        private char ParseUnicode()
        {
            if (index + 4 > json.Length)
            {
                throw new FormatException("Invalid unicode escape sequence.");
            }

            var code = json.Substring(index, 4);
            index += 4;
            return (char)Convert.ToInt32(code, 16);
        }

        private object ParseNumber()
        {
            int start = index;
            if (Peek == '-')
            {
                index++;
            }

            while (index < json.Length && char.IsDigit(json[index]))
            {
                index++;
            }

            if (index < json.Length && json[index] == '.')
            {
                index++;
                while (index < json.Length && char.IsDigit(json[index]))
                {
                    index++;
                }
            }

            if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
            {
                index++;
                if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                {
                    index++;
                }

                while (index < json.Length && char.IsDigit(json[index]))
                {
                    index++;
                }
            }

            var number = json.Substring(start, index - start);
            if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new FormatException($"Invalid number '{number}'.");
        }

        private void ParseLiteral(string literal)
        {
            for (int i = 0; i < literal.Length; i++)
            {
                if (index + i >= json.Length || json[index + i] != literal[i])
                {
                    throw new FormatException($"Invalid literal '{literal}'.");
                }
            }

            index += literal.Length;
        }

        private void Expect(char expected)
        {
            if (Peek != expected)
            {
                throw new FormatException($"Expected '{expected}' at position {index}.");
            }

            index++;
        }

        private void SkipWhitespace()
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        private char Peek => index < json.Length ? json[index] : '\0';
    }
}
