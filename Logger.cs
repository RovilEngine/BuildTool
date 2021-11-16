using System;
using System.Collections.Generic;
using System.Text;

public class Logger
{
    private static ConsoleColor def = ConsoleColor.Gray;
    private static ConsoleColor info = ConsoleColor.Cyan;
    private static ConsoleColor warn = ConsoleColor.Yellow;
    private static ConsoleColor err = ConsoleColor.Red;
    private static ConsoleColor ok = ConsoleColor.Green;
    private static ConsoleColor txt = ConsoleColor.White;
    private static ConsoleColor debug = ConsoleColor.Magenta;
    private static void _clr(ConsoleColor clr = ConsoleColor.Red)
    {
        Console.ForegroundColor = clr;
    }
    public static void Info(string text)
    {
        _clr(def);
        Console.Write("[");
        _clr(info);
        Console.Write("INFO");
        _clr(def);
        Console.Write("] ");
        _clr(txt);
        Console.WriteLine(text.ToString());
        _clr(ConsoleColor.DarkGray);
    }
    public static void Warn(string text)
    {
        _clr(def);
        Console.Write("[");
        _clr(warn);
        Console.Write("WARN");
        _clr(def);
        Console.Write("] ");
        _clr(txt);
        Console.WriteLine(text.ToString());
        _clr(ConsoleColor.DarkGray);
    }
    public static void Error(string text)
    {
        _clr(def);
        Console.Write("[");
        _clr(err);
        Console.Write("ERR");
        _clr(def);
        Console.Write("] ");
        _clr(txt);
        Console.WriteLine(text.ToString());
        _clr(ConsoleColor.DarkGray);
    }
    public static void Ok(string text)
    {
        _clr(def);
        Console.Write("[");
        _clr(ok);
        Console.Write("OK");
        _clr(def);
        Console.Write("] ");
        _clr(txt);
        Console.WriteLine(text.ToString());
        _clr(ConsoleColor.DarkGray);
    }
    public static void Debug(string text)
    {
        _clr(def);
        Console.Write("[");
        _clr(debug);
        Console.Write("OK");
        _clr(def);
        Console.Write("] ");
        _clr(txt);
        Console.WriteLine(text.ToString());
        _clr(ConsoleColor.DarkGray);
    }
}
