using Parlot.Compilation;
using Parlot.Rewriting;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Parlot.Fluent
{
    public sealed class Excluding<T, U> : Parser<T>, ISeekable, ICompilable
    {
        private readonly Parser<T>    _parser;
        private readonly Parser<U>[] _parsers;

        public Excluding(Parser<T> parser, Parser<U>[] parsers)
        {
            _parser  = parser;
            _parsers = parsers;
        }

        public bool CanSeek => _parser is ISeekable { CanSeek: true };

        public char[] ExpectedChars => _parser is ISeekable seekable ? seekable.ExpectedChars : null;

        public bool SkipWhitespace => _parser is ISeekable { SkipWhitespace: true };

        public override bool Parse(ParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Position;

            if (!_parser.Parse(context, ref result))
            {
                context.Scanner.Cursor.ResetPosition(start);
                return false;
            }

            var end = context.Scanner.Cursor.Position;

            var tempResult = new ParseResult<U>();

            foreach (var t in _parsers)
            {
                context.Scanner.Cursor.ResetPosition(start);

                if (!t.Parse(context, ref tempResult))
                {
                    continue;
                }

                context.Scanner.Cursor.ResetPosition(start);
                return false;
            }

            context.Scanner.Cursor.ResetPosition(end);
            return true;
        }

        /// <inheritdoc />
        public CompilationResult Compile(CompilationContext context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, true);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

            var start = context.DeclarePositionVariable(result);
            var end = context.DeclarePositionVariable(result);

            // success = true;
            // value = _value;
            // start = context.Cursor.Position
            // end = context.Cursor.Position

            var endLabel = Expression.Label($"end_block{context.NextNumber}");

            var parserCompileResult = _parser.Build(context, true);

            var l = new List<Expression>
            {
                // parser instructions
                // 
                // if parser.Success
                //     value = parser.Value
                // else
                //     resetPosition(start)
                //     success = false
                //     goto end
                Expression.Block(parserCompileResult.Variables,
                                 Expression.Block(parserCompileResult.Body),
                                 Expression.IfThenElse
                                     (parserCompileResult.Success,
                                      Expression.Assign(value, parserCompileResult.Value),
                                      Expression.Block
                                          (context.ResetPosition(start),
                                           Expression.Assign(success, Expression.Constant(false)),
                                           Expression.Goto(endLabel)
                                          )
                                     )
                                ),
                // end = context.Cursor.Position
                Expression.Assign(end, context.Position()),
                // ..for every _parsers
                //     resetPosition(start)
                //     parser instructions
                //     if parser.Success
                //         resetPosition(start)
                //         success = false
                //         goto end
                //     resetPosition(start)
                Expression.Block(_parsers.Select(t => t.Build(context))
                                      .Select(pcr => Expression.Block
                                                  (pcr.Variables,
                                                   context.ResetPosition(start),
                                                   Expression.Block(pcr.Body),
                                                   Expression.IfThen
                                                       (pcr.Success,
                                                        Expression.Block
                                                            (context.ResetPosition(start),
                                                             Expression.Assign(success, Expression.Constant(false)),
                                                             Expression.Goto(endLabel)
                                                            )
                                                       ),
                                                   context.ResetPosition(start)
                                                  )
                                             )),
                // if success
                //     resetPosition(end)
                Expression.IfThen(success, context.ResetPosition(end)),
                Expression.Label(endLabel)
            };

            result.Body.Add(Expression.Block(l));

            return result;
        }
    }
}
