using System;
using Xunit;
namespace KinHub.Domain.Tests;

public class ProjectTests
{
    [Fact]
    public void Empty_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Project(" "));
    }

    [Fact]
    public void Name_is_trimmed()
    {
        Assert.Equal("Home", new Project(" Home ").Name);
    }
}
