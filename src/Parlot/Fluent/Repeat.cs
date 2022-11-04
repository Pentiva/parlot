using Parlot.Compilation;
using Parlot.Rewriting;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Parlot.Fluent
{
    public sealed class Repeat<T> : Parser<List<T>>, ISeekable, ICompilable
    {
        private readonly Parser<T> _parser;
        private readonly int       _count;

        public Repeat(Parser<T> parser, int count)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _count  = count;
        }

        /// <inheritdoc />
        public CompilationResult Compile(CompilationContext context)
        {
            CompilationResult result = new CompilationResult();

            ParameterExpression success = context.DeclareSuccessVariable(result, true);
            ParameterExpression value = context.DeclareValueVariable(result, Expression.New(typeof(List<T>)));

            CompilationResult parserCompileResult = _parser.Build(context);
            LabelTarget breakLabel = Expression.Label($"break{context.NextNumber}");

            ParameterExpression start = context.DeclarePositionVariable(result);

            ParameterExpression i = Expression.Variable(typeof(int), $"i{context.NextNumber}");
            result.Variables.Add(i);
            result.Body.Add(Expression.Assign(i, Expression.Constant(0, typeof(int))));

            ParameterExpression count = Expression.Variable(typeof(int), $"count{context.NextNumber}");
            result.Variables.Add(count);
            result.Body.Add(Expression.Assign(count, Expression.Constant(_count, typeof(int))));

            // success = true;
            // value = new List<T>();
            // start = context.Cursor.Position
            // i = 0
            // count = _count
            //
            // while true
            //     if i >= count
            //         break
            //     parser instructions
            //     if !parser.Success
            //         success = false
            //         resetPosition(start)
            //         break
            //     else
            //         value.Add(parser.Value)
            //     i = i + 1
            //     continue

            result.Body.Add(Expression.Block(
                                             parserCompileResult.Variables,
                                             Expression.Loop(
                                                             Expression.Block(
                                                                              Expression
                                                                              .IfThen(Expression.GreaterThanOrEqual(i, count),
                                                                                   Expression.Break(breakLabel)),
                                                                              Expression.Block(parserCompileResult.Body),
                                                                              Expression
                                                                              .IfThenElse(Expression.Not(parserCompileResult.Success),
                                                                                       Expression.Block(
                                                                                            Expression.Assign(success,
                                                                                             Expression
                                                                                             .Constant(false)),
                                                                                            context.ResetPosition(start),
                                                                                            Expression.Break(breakLabel)
                                                                                           ),
                                                                                       Expression.Block(
                                                                                            context.DiscardResult
                                                                                                ? Expression.Empty()
                                                                                                : Expression
                                                                                                .Call(value,
                                                                                                     typeof(List<T>)
                                                                                                     .GetMethod("Add")!,
                                                                                                     parserCompileResult
                                                                                                     .Value)
                                                                                           )
                                                                                      ),
                                                                              Expression.Assign(i, Expression.Increment(i))),
                                                             breakLabel)
                                            ));

            return result;
        }

        public bool CanSeek => _parser is ISeekable { CanSeek: true };

        public char[] ExpectedChars => _parser is ISeekable seekable ? seekable.ExpectedChars : Array.Empty<char>();

        public bool SkipWhitespace => _parser is ISeekable { SkipWhitespace: true };

        public override bool Parse(ParseContext context, ref ParseResult<List<T>> result)
        {
            context.EnterParser(this);

            List<T> results = new List<T>();

            TextPosition startA = context.Scanner.Cursor.Position;

            int start = context.Scanner.Cursor.Offset;
            int end = 0;

            ParseResult<T> parsed = new ParseResult<T>();

            for (int i = 0; i < _count; i++)
            {
                if (!_parser.Parse(context, ref parsed))
                {
                    context.Scanner.Cursor.ResetPosition(startA);
                    return false;
                }

                end = parsed.End;
                results.Add(parsed.Value);
            }

            result = new ParseResult<List<T>>(start, end, results);
            return true;
        }
    }
}
