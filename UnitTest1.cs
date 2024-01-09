[TestClass]
public class HomeControllerTest
{
    [TestMethod]
    public void IndexViewTest()
    {
        // Arrange
        var controller = new HomeController();

        // Act
        var result = controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ViewName);
    }
}
