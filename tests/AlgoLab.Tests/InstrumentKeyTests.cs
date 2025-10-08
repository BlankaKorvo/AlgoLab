using AlgoLab.Core;
using AlgoLab.Core.Models.Enums;
using NUnit.Framework;

namespace AlgoLab.Tests;

public class InstrumentKeyTests
{
    [Test] 
    public void Ticker_Requires_ClassCode()
    { 
        Assert.Throws<ArgumentException>(()=>InstrumentKey.Ticker("SBER","")); 
    }
    [Test] 
    public void Uid_Allows_No_ClassCode()
    { 
        var k=InstrumentKey.Uid("TCS00A103X00"); 
        Assert.That(k.Type, Is.EqualTo(InstrumentIdTypeCore.Uid)); 
        Assert.That(k.ClassCode, Is.Null); 
    }
}
