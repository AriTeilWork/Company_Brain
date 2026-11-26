using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CompanyBrain.Client.Api;
using CompanyBrain.Client.ViewModels;
using CompanyBrain.Client.Views;

namespace CompanyBrain.Client.Tests;

[TestClass]
[DoNotParallelize]
[TestCategory("UI")]
public sealed class DesktopUiTests
{
    private static HeadlessUnitTestSession _session = null!;

    [ClassInitialize]
    public static void Initialize(TestContext _) =>
        _session = HeadlessUnitTestSession.StartNew(typeof(UiTestAppBuilder));

    [ClassCleanup]
    public static Task Cleanup() => Task.Run(_session.Dispose);

    [TestMethod]
    [DataRow("Devices", typeof(DevicesView))]
    [DataRow("Customers", typeof(CustomersView))]
    [DataRow("Support guides", typeof(GuidesView))]
    [DataRow("Ask", typeof(AskView))]
    public Task SidebarShowsOnlyTheSelectedScreen(string name, Type viewType) => WithWindow((window, model) =>
    {
        var api = model.Api;
        model.Notice = "Previous screen notice";
        var button = Named<Button>(window, name);
        Click(window, button);

        var views = window.GetVisualDescendants().OfType<UserControl>()
            .Where(view => view is DevicesView or CustomersView or GuidesView or AskView).ToArray();
        Assert.AreEqual(4, views.Length);
        Assert.AreEqual(1, views.Count(view => view.IsEffectivelyVisible));
        Assert.IsTrue(views.Single(view => view.GetType() == viewType).IsEffectivelyVisible);
        Assert.IsTrue(button.Classes.Contains("active"));
        Assert.AreSame(api, model.Api);
        Assert.IsFalse(model.HasNotice);
    });

    [TestMethod]
    [DataRow("Devices", "Search devices", "LT-1028")]
    [DataRow("Support guides", "Search support guides", "after hours")]
    [DataRow("Ask", "Question for Company Brain", "Where is the loan laptop?")]
    public Task TextInputUpdatesItsBoundValue(string screen, string field, string text) => WithWindow((window, model) =>
    {
        Click(window, Named<Button>(window, screen));
        var input = Named<TextBox>(window, field);
        Assert.IsTrue(input.Focus());
        input.SelectAll();
        window.KeyTextInput(text);

        Assert.AreEqual(text, input.Text);
        Assert.AreEqual(text, screen switch
        {
            "Devices" => model.DeviceQuery,
            "Support guides" => model.GuideQuery,
            _ => model.Question,
        });
    });

    [TestMethod]
    public Task SelectingAnotherDeviceUpdatesDetailsAndHidesTheMissingTicketLink() => WithWindow((window, model) =>
    {
        var results = Named<ListBox>(window, "Device results");
        var view = window.GetVisualDescendants().OfType<DevicesView>().Single();
        var ticketLink = view.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Classes.Contains("link") && Equals(button.CommandParameter, "Tickets"));
        Assert.IsTrue(ticketLink.IsEffectivelyVisible);
        Click(window, results.ContainerFromIndex(1)!);

        Assert.AreEqual(model.Devices[1], results.SelectedItem);
        Assert.AreEqual(model.Devices[1], model.SelectedDevice);
        Assert.IsFalse(ticketLink.IsEffectivelyVisible);
        Assert.IsTrue(view.GetVisualDescendants().OfType<TextBlock>()
            .Any(text => text.Text == model.Devices[1].SerialNumber));
    });

    [TestMethod]
    public Task RelatedRecordLinksNavigateInBothDirections() => WithWindow((window, model) =>
    {
        Click(window, ActionButton(window, "Open customer"));
        Assert.IsTrue(model.IsCustomers);

        var deviceLink = window.GetVisualDescendants().OfType<CustomersView>().Single()
            .GetVisualDescendants().OfType<Button>().Single(button => Equals(button.CommandParameter, "Devices"));
        Click(window, deviceLink);

        Assert.IsTrue(model.IsDevices);
        Assert.IsTrue(window.GetVisualDescendants().OfType<DevicesView>().Single().IsEffectivelyVisible);
    });

    [TestMethod]
    [DataRow("[1] Premium IT support agreement", "Customers")]
    [DataRow("[3] Loan device policy", "Guides")]
    public Task AskSourceLinksOpenTheRelatedScreen(string text, string section) => WithWindow((window, model) =>
    {
        Click(window, Named<Button>(window, "Ask"));
        var sourceLink = window.GetVisualDescendants().OfType<AskView>().Single()
            .GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content is TextBlock label && label.Text == text);
        Click(window, sourceLink);

        Assert.AreEqual(section, model.Section);
    });

    [TestMethod]
    [DataRow("Devices", "Search", "Search is a placeholder")]
    [DataRow("Devices", "Tickets", "Ticket details are a placeholder")]
    [DataRow("Customers", "PDF", "document preview placeholder")]
    [DataRow("Support guides", "Search", "Search is a placeholder")]
    [DataRow("Support guides", "AfterHours", "guide preview placeholder")]
    [DataRow("Ask", "Ask", "sample answer")]
    public Task PlaceholderActionsShowADismissibleNotice(string screen, string action, string expected) => WithWindow((window, model) =>
    {
        Click(window, Named<Button>(window, screen));
        var button = window.GetVisualDescendants().OfType<Button>()
            .First(candidate => candidate.IsEffectivelyVisible && candidate.Command == model.PlaceholderCommand
                && Equals(candidate.CommandParameter, action));
        Click(window, button);

        Assert.IsTrue(model.HasNotice);
        Assert.IsTrue(window.GetVisualDescendants().OfType<TextBlock>()
            .Any(text => text.IsEffectivelyVisible && text.Text?.Contains(expected) == true));
        Click(window, ActionButton(window, "Dismiss"));

        Assert.IsFalse(model.HasNotice);
        Assert.IsFalse(ActionButton(window, "Dismiss").IsEffectivelyVisible);
    });

    [TestMethod]
    public Task CaptionButtonsMaximizeRestoreMinimizeAndClose() => WithWindow((window, _) =>
    {
        Click(window, Named<Button>(window, "Maximize or restore window"));
        Assert.AreEqual(WindowState.Maximized, window.WindowState);
        Click(window, Named<Button>(window, "Maximize or restore window"));
        Assert.AreEqual(WindowState.Normal, window.WindowState);
        Click(window, Named<Button>(window, "Minimize window"));
        Assert.AreEqual(WindowState.Minimized, window.WindowState);
        window.WindowState = WindowState.Normal;
        Dispatcher.UIThread.RunJobs();

        var closed = false;
        window.Closed += (_, _) => closed = true;
        Click(window, Named<Button>(window, "Close window"));

        Assert.IsTrue(closed);
        Assert.IsFalse(window.IsVisible);
    });

    [TestMethod]
    public Task TitleBarDoubleClickMaximizesAndRightClickLeavesStateUnchanged() => WithWindow((window, _) =>
    {
        var titleBar = window.GetVisualDescendants().OfType<Grid>().Single(grid => grid.Height == 32);
        var point = titleBar.TranslatePoint(new Rect(titleBar.Bounds.Size).Center, window)!.Value;
        window.MouseDown(point, MouseButton.Right);
        window.MouseUp(point, MouseButton.Right);
        Assert.AreEqual(WindowState.Normal, window.WindowState);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.AreEqual(WindowState.Normal, window.WindowState);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);

        Assert.AreEqual(WindowState.Maximized, window.WindowState);
    });

    private static Task WithWindow(Action<MainWindow, MainViewModel> check) => _session.Dispatch(() =>
    {
        using var http = new HttpClient(new NoRequestsHandler()) { BaseAddress = new Uri(ApiSettings.DefaultBaseUrl) };
        var model = new MainViewModel(new ApiClient(http));
        var window = new MainWindow { DataContext = model };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            check(window, model);
        }
        finally
        {
            window.Close();
        }
    }, CancellationToken.None);

    private static T Named<T>(MainWindow window, string name) where T : Control =>
        window.GetVisualDescendants().OfType<T>().Single(control => AutomationProperties.GetName(control) == name);

    private static Button ActionButton(MainWindow window, string text) =>
        window.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, text));

    private static void Click(MainWindow window, Control control)
    {
        control.BringIntoView();
        Dispatcher.UIThread.RunJobs();
        var point = control.TranslatePoint(new Rect(control.Bounds.Size).Center, window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class NoRequestsHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new AssertFailedException("Creating and navigating the shell must not send an HTTP request.");
    }
}

internal static class UiTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .WithInterFont()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
