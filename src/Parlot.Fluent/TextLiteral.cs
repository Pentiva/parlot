﻿using System;

namespace Parlot.Fluent
{
    public class TextLiteral : Parser<string>
    {
        private readonly bool _skipWhiteSpace;

        public TextLiteral(string text, bool skipWhiteSpace = true)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            _skipWhiteSpace = skipWhiteSpace;
        }

        public string Text { get; }

        public override bool Parse(Scanner scanner, out ParseResult<string> result)
        {
            if (_skipWhiteSpace)
            {
                scanner.SkipWhiteSpace();
            }

            var start = scanner.Cursor.Position;

            if (scanner.ReadText(Text))
            {
                result = new ParseResult<string>(scanner.Buffer, start, scanner.Cursor.Position, Text);
                return true;
            }
            else
            {
                result = ParseResult<string>.Empty;
                return false;
            }
        }
    }
}
