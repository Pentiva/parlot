﻿using System;
using System.Collections.Generic;

namespace Parlot.Fluent
{
    public static class ParserBuilder
    {
        public static LiteralBuilder Literals => new();

        public static IParser<U> Then<T, U>(this IParser<T> parser, Func<T, U> conversion) => new Then<T, U>(parser, conversion);
        public static IParser<T> When<T>(this IParser<T> parser, Func<T, bool> predicate) => new When<T>(parser, predicate);

        public static IParser<ParseResult<object>> OneOf(params IParser[] parsers) => new OneOf(parsers);
        public static IParser<T> OneOf<T>(params IParser<T>[] parsers) => new OneOf<T>(parsers);
        public static IParser<IList<ParseResult<object>>> Sequence(params IParser[] parsers) => new Sequence(parsers);

        public static IParser<ValueTuple<T1, T2>> Sequence<T1, T2>(IParser<T1> parser1, IParser<T2> parser2) => new Sequence<T1, T2>(parser1, parser2);
        public static IParser<ValueTuple<T1, T2, T3>> Sequence<T1, T2, T3>(IParser<T1> parser1, IParser<T2> parser2, IParser<T3> parser3) => new Sequence<T1, T2, T3>(parser1, parser2, parser3);

        public static IParser<T> ZeroOrOne<T>(IParser<T> parser) => new ZeroOrOne<T>(parser);
        public static IParser<IList<T>> ZeroOrMany<T>(IParser<T> parser) => new ZeroOrMany<T>(parser);
        public static IParser<IList<T>> OneOrMany<T>(IParser<T> parser) => new OneOrMany<T>(parser);

        public static Deferred<T> Deferred<T>() => new();
        public static Between<T> Between<T>(string before, IParser<T> parser, string after) => new Between<T>(before, parser, after);
    }

    public class LiteralBuilder
    {
        public IParser<string> Text(string text) => new TextLiteral(text);
        public IParser<char> Char(char c) => new CharLiteral(c);
        public IParser<long> Integer() => new IntegerLiteral();
        public IParser<decimal> Decimal() => new DecimalLiteral();
        public IParser<TextSpan> String(StringLiteralQuotes quotes = StringLiteralQuotes.SingleOrDouble) => new StringLiteral(quotes);
    }
}
