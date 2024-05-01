using SubredditMonitorExercise.Types.Reddit;

namespace Tests.Reddit;

[TestFixture]
public class KindTests
{
    [Test]
    public void Kind_CanBeImplicitlyConverted()
    {
        var kind = new Kind<string> { Data = "Some Data" };
        
        string data = kind;
        
        Assert.That(data, Is.EqualTo("Some Data"));
    }
}