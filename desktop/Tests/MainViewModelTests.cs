using CompanyBrain.Client.ViewModels;

namespace CompanyBrain.Client.Tests;

[TestClass]
public sealed class MainViewModelTests
{
    [TestMethod]
    public void DesignerCanCreateTheInitialScreenWithoutApiConfiguration()
    {
        var model = new MainViewModel();

        Assert.IsTrue(model.IsDevices);
        Assert.IsFalse(model.IsCustomers);
        Assert.IsFalse(model.IsGuides);
        Assert.IsFalse(model.IsAsk);
        Assert.IsFalse(model.HasNotice);
        Assert.IsNotNull(model.SelectedDevice);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("Unknown")]
    public void InvalidNavigationLeavesTheCurrentScreenAndNoticeIntact(string? section)
    {
        var model = new MainViewModel { Notice = "Current notice" };

        model.NavigateCommand.Execute(section);

        Assert.IsTrue(model.IsDevices);
        Assert.AreEqual("Current notice", model.Notice);
    }
}
