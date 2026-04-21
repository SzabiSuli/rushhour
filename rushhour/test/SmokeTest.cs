namespace rushhour.test;

using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SmokeTest {
    [TestCase]
    public void TestTest() {
        AssertBool(false).IsFalse();
    }
}