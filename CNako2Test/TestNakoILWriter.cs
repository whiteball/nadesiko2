﻿using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Libnako.JPNCompiler;
using Libnako.Interpreter.ILCode;
using Libnako.JPNCompiler.ILWriter;

namespace NakoPluginTest
{
    [TestFixture]
    public class TestNakoILWriter
    {
        [Test]
        public void TestNakoILWriter1()
        {
            NakoCompiler ns = new NakoCompiler();
            NakoILWriter writer = new NakoILWriter(null);
            bool r;

            // (1)
            ns.source = "1+2*3";
            ns.Tokenize();
            ns.ParseOnlyValue();
            writer.Write(ns.TopNode);
            r = writer.Result.CheckTypes(new NakoILType[] {
                NakoILType.NOP,
                NakoILType.LD_CONST_INT,
                NakoILType.LD_CONST_INT,
                NakoILType.LD_CONST_INT,
                NakoILType.MUL,
                NakoILType.ADD
            });
            Assert.IsTrue(r);
        }
    }
}
